using System.Drawing;
using System.Windows.Forms;
using CSharpTrayShortcut.Application.Abstractions;
using CSharpTrayShortcut.Application.Configuration;
using CSharpTrayShortcut.Application.Menu;
using CSharpTrayShortcut.Domain.Menu;
using CSharpTrayShortcut.Domain.Text;
using CSharpTrayShortcut.Ui.Icons;
using CSharpTrayShortcut.Ui.Localization;
using CSharpTrayShortcut.Ui.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// « Application » désigne à la fois la couche applicative du dépôt et la classe statique de
// WinForms. L'alias lève l'ambiguïté sans renommer une couche pour des raisons d'outillage.
using WinFormsApplication = System.Windows.Forms.Application;

namespace CSharpTrayShortcut.Ui.Tray;

/// <summary>
/// Cycle de vie de l'application résidente : l'icône de la zone de notification, son menu, et
/// les trois commandes (SPEC-MENU-001, SPEC-CFG-004).
/// </summary>
/// <remarks>
/// <para>
/// C'est le chef d'orchestre, et rien de plus : il relit la configuration, demande le menu à la
/// couche application, le fait rendre, et réagit aux commandes. Aucune règle ne vit ici — c'est
/// ce qui a permis de ramener cette classe de 259 lignes mêlant énumération de dossiers, choix
/// d'icônes et gestion de handles GDI à un enchaînement lisible.
/// </para>
/// <para>
/// Aucune fenêtre n'est ouverte au démarrage : l'application vit dans la zone de notification.
/// </para>
/// </remarks>
public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly IConfigurationStore _store;
    private readonly IShortcutSource _source;
    private readonly MenuRenderer _renderer;
    private readonly IconRenderer _icons;
    private readonly TextService _texts;
    private readonly IServiceProvider _services;
    private readonly ILogger<TrayApplicationContext> _logger;
    private readonly NotifyIcon _notifyIcon;

    private TrayShortcutConfiguration _configuration = new();

    /// <summary>Construit le contexte et affiche l'icône.</summary>
    public TrayApplicationContext(
        IConfigurationStore store,
        IShortcutSource source,
        MenuRenderer renderer,
        IconRenderer icons,
        TextService texts,
        IServiceProvider services,
        ILogger<TrayApplicationContext> logger)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(icons);
        ArgumentNullException.ThrowIfNull(texts);
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(logger);

        _store = store;
        _source = source;
        _renderer = renderer;
        _icons = icons;
        _texts = texts;
        _services = services;
        _logger = logger;

        _notifyIcon = new NotifyIcon
        {
            ContextMenuStrip = new ContextMenuStrip(),
            Visible = true,
        };

        Refresh();
    }

    /// <summary>
    /// Relit la configuration, demande le dossier surveillé s'il manque, puis reconstruit
    /// l'icône et le menu (SPEC-CFG-004).
    /// </summary>
    private void Refresh()
    {
        _configuration = _store.Load();
        _texts.Apply(_configuration);

        EnsureWatchedFolder();
        UpdateTrayIcon();

        _renderer.Render(_notifyIcon.ContextMenuStrip!, _configuration, Execute);
    }

    /// <summary>
    /// Demande un dossier surveillé tant que la configuration n'en désigne pas un valide
    /// (SPEC-CFG-002).
    /// </summary>
    /// <remarks>
    /// <para>
    /// La boucle s'arrête aussi lorsque l'utilisateur annule : sans cette sortie, l'ancienne
    /// version réaffichait indéfiniment la même invite, et l'application ne pouvait être
    /// arrêtée que par le gestionnaire de tâches. Un dossier absent laisse simplement un menu
    /// réduit aux trois commandes.
    /// </para>
    /// <para>
    /// L'invite passe par <see cref="FolderBrowserDialog"/> plutôt que par une saisie libre :
    /// c'est le même geste qu'ailleurs dans Windows, et cela supprime toute une classe de
    /// fautes de frappe.
    /// </para>
    /// </remarks>
    private void EnsureWatchedFolder()
    {
        if (_configuration.Validate(_source.DirectoryExists) is null)
        {
            return;
        }

        using var dialogue = new FolderBrowserDialog
        {
            Description = _texts.Get(TextKeys.Config.FolderPrompt),
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false,
        };

        if (!string.IsNullOrWhiteSpace(_configuration.Path) && _source.DirectoryExists(_configuration.Path))
        {
            dialogue.SelectedPath = _configuration.Path;
        }

        if (dialogue.ShowDialog() != DialogResult.OK)
        {
            _logger.LogInformation(
                "Aucun dossier surveillé : le menu se limite aux commandes de l'application.");
            return;
        }

        _configuration.Path = dialogue.SelectedPath;

        if (!_store.Save(_configuration))
        {
            MessageBox.Show(
                _texts.Resolve(TextRef.Of(TextKeys.Error.SaveFailed, _store.ConfigurationFilePath)),
                _texts.Get(TextKeys.AppName),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    /// <summary>
    /// Applique l'icône de la zone de notification et son info-bulle (SPEC-ICON-002).
    /// </summary>
    private void UpdateTrayIcon()
    {
        var precedente = _notifyIcon.Icon;
        _notifyIcon.Icon = _icons.RenderIcon(IconSourceResolver.ForTray(_configuration));

        // L'ancienne icône est libérée après avoir été remplacée : la libérer avant laisserait
        // un instant où NotifyIcon référence un handle détruit.
        precedente?.Dispose();

        // Windows tronque l'info-bulle d'un NotifyIcon à 63 caractères : au-delà, elle
        // n'apparaît pas du tout plutôt que d'être coupée.
        var texte = _texts.Resolve(TextRef.Of(
            TextKeys.Menu.Tooltip,
            _texts.Get(TextKeys.AppName),
            _configuration.Path ?? string.Empty));

        _notifyIcon.Text = texte.Length > 63 ? texte[..63] : texte;
    }

    private void Execute(MenuCommand command)
    {
        switch (command)
        {
            case MenuCommand.Refresh:
                Refresh();
                break;

            case MenuCommand.Edit:
                Edit();
                break;

            case MenuCommand.Exit:
                Exit();
                break;

            default:
                _logger.LogWarning("Commande de menu inconnue : {Commande}", command);
                break;
        }
    }

    /// <summary>
    /// Ouvre la fenêtre d'édition des raccourcis personnalisés, et recharge tout à sa fermeture
    /// (SPEC-CFG-003).
    /// </summary>
    private void Edit()
    {
        var form = _services.GetRequiredService<EditForm>();
        form.FormClosed += (_, _) => Refresh();
        form.Show();
        form.Activate();
    }

    private void Exit()
    {
        _notifyIcon.Visible = false;
        WinFormsApplication.Exit();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _icons.Dispose();
            _notifyIcon.ContextMenuStrip?.Dispose();
            _notifyIcon.Icon?.Dispose();
            _notifyIcon.Dispose();
        }

        base.Dispose(disposing);
    }
}
