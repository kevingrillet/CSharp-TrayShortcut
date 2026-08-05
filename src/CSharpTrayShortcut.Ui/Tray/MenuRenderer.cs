using System.Drawing;
using System.Windows.Forms;
using CSharpTrayShortcut.Application.Configuration;
using CSharpTrayShortcut.Application.Launching;
using CSharpTrayShortcut.Application.Menu;
using CSharpTrayShortcut.Domain.Menu;
using CSharpTrayShortcut.Domain.Shortcuts;
using CSharpTrayShortcut.Domain.Text;
using CSharpTrayShortcut.Ui.Icons;
using CSharpTrayShortcut.Ui.Localization;
using Microsoft.Extensions.Logging;

namespace CSharpTrayShortcut.Ui.Tray;

/// <summary>
/// Traduit un menu décrit par la couche application en éléments WinForms (SPEC-MENU-001).
/// </summary>
/// <remarks>
/// <para>
/// Cette classe ne décide rien : ni l'ordre, ni la présence, ni les icônes, ni même la
/// réutilisation des images. Elle transforme une liste de <see cref="MenuEntry"/> en
/// <see cref="ToolStripItem"/>, branche les gestionnaires de clic, et gère la seule chose que
/// seule la présentation puisse gérer — la construction paresseuse des sous-menus.
/// </para>
/// <para>
/// <b>Les images appartiennent à <see cref="IconRenderer"/>.</b> Les premières versions
/// confiaient chaque image à son élément de menu et parcouraient l'arbre pour les libérer, en
/// prenant soin d'épargner l'icône de dossier partagée — un traitement particulier facile à
/// casser. Le cache du rendu d'icônes rend tout cela inutile
/// ([ADR-0006](../../../docs/adr/0006-cache-des-icones.md)) : ici, on demande une image et on
/// l'affiche, sans jamais la libérer.
/// </para>
/// </remarks>
public sealed class MenuRenderer
{
    private readonly MenuComposer _composer;
    private readonly IconRenderer _icons;
    private readonly TextService _texts;
    private readonly LaunchService _launcher;
    private readonly ILogger<MenuRenderer> _logger;

    private TrayShortcutConfiguration _configuration = new();
    private Action<MenuCommand>? _onCommand;

    /// <summary>Construit le rendu.</summary>
    /// <param name="composer">Composition du menu (couche application).</param>
    /// <param name="icons">Fabrication des images.</param>
    /// <param name="texts">Traduction des intitulés.</param>
    /// <param name="launcher">Lancement des éléments cliqués.</param>
    /// <param name="logger">Journal.</param>
    public MenuRenderer(
        MenuComposer composer,
        IconRenderer icons,
        TextService texts,
        LaunchService launcher,
        ILogger<MenuRenderer> logger)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(icons);
        ArgumentNullException.ThrowIfNull(texts);
        ArgumentNullException.ThrowIfNull(launcher);
        ArgumentNullException.ThrowIfNull(logger);

        _composer = composer;
        _icons = icons;
        _texts = texts;
        _launcher = launcher;
        _logger = logger;
    }

    /// <summary>
    /// Reconstruit intégralement le menu contextuel à partir de la configuration.
    /// </summary>
    /// <param name="strip">Menu contextuel à remplir.</param>
    /// <param name="configuration">Configuration courante.</param>
    /// <param name="onCommand">Action à exécuter pour une commande de l'application.</param>
    public void Render(
        ContextMenuStrip strip,
        TrayShortcutConfiguration configuration,
        Action<MenuCommand> onCommand)
    {
        ArgumentNullException.ThrowIfNull(strip);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(onCommand);

        _configuration = configuration;
        _onCommand = onCommand;

        strip.Items.Clear();

        // Le menu précédent est abandonné : c'est le seul instant où aucune image ne peut être
        // référencée par un menu vivant, donc le seul où l'éviction est sans danger.
        _icons.BeginRender();

        Fill(strip.Items, _composer.ComposeRoot(configuration));
    }

    private void Fill(ToolStripItemCollection items, IReadOnlyList<MenuEntry> entries)
    {
        foreach (var entry in entries)
        {
            items.Add(Create(entry));
        }
    }

    private ToolStripItem Create(MenuEntry entry) => entry switch
    {
        SeparatorEntry => new ToolStripSeparator(),
        FolderEntry folder => CreateFolder(folder),
        LaunchEntry launch => CreateLaunch(launch),
        GroupEntry group => CreateGroup(group),
        CommandEntry command => CreateCommand(command),

        // La hiérarchie de MenuEntry est fermée : ce cas ne peut se produire qu'en ajoutant une
        // forme d'entrée sans compléter ce filtrage. Mieux vaut échouer ici, à la première
        // ouverture du menu, que d'afficher silencieusement un menu incomplet.
        _ => throw new NotSupportedException(
            $"Forme d'entrée de menu non prise en charge par le rendu : {entry.GetType().Name}."),
    };

    /// <summary>
    /// Élément de dossier : un espace réservé donne la flèche d'ouverture, et le contenu réel
    /// est construit à la première ouverture (SPEC-MENU-003).
    /// </summary>
    private ToolStripMenuItem CreateFolder(FolderEntry folder)
    {
        var item = new ToolStripMenuItem
        {
            Text = folder.Label,
            Image = Image(folder.Icon),
            Tag = folder.Path,
        };

        // Sans au moins un enfant, WinForms n'affiche pas la flèche et n'émet jamais
        // DropDownOpening : le dossier paraîtrait vide.
        item.DropDownItems.Add(new ToolStripMenuItem());
        item.DropDownOpening += PopulateOnce;

        return item;
    }

    private void PopulateOnce(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem item || item.Tag is not string path)
        {
            return;
        }

        // Désabonnement immédiat : le contenu d'un sous-dossier n'est construit qu'une fois par
        // rendu. « Actualiser » repart d'un menu neuf, donc d'une nouvelle énumération.
        item.DropDownOpening -= PopulateOnce;
        item.DropDownItems.Clear();

        var entries = _composer.ComposeFolder(path, _configuration);
        if (entries.Count == 0)
        {
            item.DropDownItems.Add(new ToolStripMenuItem(_texts.Get(TextKeys.Menu.Empty))
            {
                Enabled = false,
            });
            return;
        }

        Fill(item.DropDownItems, entries);
    }

    private ToolStripMenuItem CreateLaunch(LaunchEntry launch)
    {
        var item = new ToolStripMenuItem
        {
            Text = launch.Label,
            Image = Image(launch.Icon),
            Tag = launch.Target,
        };

        item.Click += Launch;
        return item;
    }

    private void Launch(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem { Tag: LaunchTarget target })
        {
            return;
        }

        // Le service journalise déjà le motif ; ici on ne fait que constater. Surtout, aucune
        // exception ne remonte : un clic sur une entrée périmée ne doit pas fermer
        // l'application (SPEC-LAUNCH-002).
        if (!_launcher.Launch(target))
        {
            _logger.LogDebug("Lancement sans effet pour {Chemin}", target.Path);
        }
    }

    private ToolStripMenuItem CreateGroup(GroupEntry group)
    {
        var item = new ToolStripMenuItem
        {
            Text = _texts.Resolve(group.Label),
        };

        Fill(item.DropDownItems, group.Children);
        return item;
    }

    private ToolStripMenuItem CreateCommand(CommandEntry command)
    {
        var item = new ToolStripMenuItem
        {
            Text = _texts.Resolve(command.Label),
            Tag = command.Command,
        };

        item.Click += (_, _) => _onCommand?.Invoke(command.Command);
        return item;
    }

    /// <summary>
    /// Image d'une source. Elle appartient au cache du rendu d'icônes : on ne la libère jamais
    /// ici.
    /// </summary>
    private Bitmap? Image(IconSource source) => _icons.RenderBitmap(source);
}
