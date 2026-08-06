using CSharpTrayShortcut.Application.Abstractions;
using CSharpTrayShortcut.Application.Configuration;
using CSharpTrayShortcut.Domain.Menu;
using CSharpTrayShortcut.Domain.Shortcuts;
using CSharpTrayShortcut.Domain.Text;

namespace CSharpTrayShortcut.Application.Menu;

/// <summary>
/// Compose le menu de la zone de notification à partir de la configuration et du contenu du
/// dossier surveillé (SPEC-MENU-001 à SPEC-MENU-005).
/// </summary>
/// <remarks>
/// <para>
/// Cas d'usage central de l'application, et le seul endroit qui décide de l'<b>ordre</b> et de
/// la <b>présence</b> des entrées. Il ne rend aucune image, n'ouvre aucune fenêtre et ne
/// touche au disque qu'à travers <see cref="IShortcutSource"/> : la totalité de ses règles se
/// vérifie avec une arborescence décrite en trois lignes dans un test.
/// </para>
/// <para>
/// <b>Un niveau à la fois.</b> <see cref="ComposeRoot"/> n'énumère que le premier niveau ;
/// le contenu d'un sous-dossier est demandé par <see cref="ComposeFolder"/> à sa première
/// ouverture (SPEC-MENU-003). C'est ce qui évite de parcourir toute une arborescence — parfois
/// un partage réseau — au démarrage de l'application.
/// </para>
/// </remarks>
public sealed class MenuComposer
{
    /// <summary>
    /// Ordre d'affichage des dossiers et des fichiers (SPEC-MENU-002, règle 1).
    /// </summary>
    /// <remarks>
    /// <see cref="StringComparer.InvariantCultureIgnoreCase"/> et non
    /// <c>CurrentCultureIgnoreCase</c> : on veut un ordre qui tienne compte des accents —
    /// « Éditeurs » près de « Editeurs », pas rejeté après « Zip » comme le ferait une
    /// comparaison ordinale — tout en restant <b>le même sur toutes les machines</b>. Un ordre
    /// dépendant de la culture du poste rendrait les tests non reproductibles.
    /// </remarks>
    private static readonly StringComparer DisplayOrder = StringComparer.InvariantCultureIgnoreCase;

    private readonly IShortcutSource _source;
    private readonly IconSourceResolver _icons;

    /// <summary>Construit le compositeur.</summary>
    /// <param name="source">Accès au dossier surveillé.</param>
    /// <param name="icons">Règles de choix des icônes.</param>
    public MenuComposer(IShortcutSource source, IconSourceResolver icons)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(icons);

        _source = source;
        _icons = icons;
    }

    /// <summary>
    /// Menu racine : le premier niveau du dossier surveillé, la section des raccourcis
    /// personnalisés, puis les commandes de l'application (SPEC-MENU-001).
    /// </summary>
    /// <param name="configuration">Configuration courante.</param>
    /// <remarks>
    /// Les commandes sont ajoutées <b>en toutes circonstances</b>, y compris quand le dossier
    /// surveillé est vide ou illisible : sans elles, une mauvaise configuration rendrait
    /// l'application impossible à corriger et même à quitter autrement que par le gestionnaire
    /// de tâches.
    /// </remarks>
    public IReadOnlyList<MenuEntry> ComposeRoot(TrayShortcutConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var entries = new List<MenuEntry>();

        if (!string.IsNullOrWhiteSpace(configuration.Path))
        {
            entries.AddRange(ComposeDirectory(configuration.Path, configuration, isRoot: true));
        }

        AppendCustomShortcuts(entries, configuration);
        AppendCommands(entries);

        return entries;
    }

    /// <summary>
    /// Contenu d'un sous-dossier, construit à son ouverture (SPEC-MENU-003).
    /// </summary>
    /// <param name="path">Dossier à énumérer.</param>
    /// <param name="configuration">Configuration courante, pour l'icône de dossier.</param>
    /// <remarks>
    /// Hors racine, les fichiers sont toujours affichés : le réglage
    /// <see cref="TrayShortcutConfiguration.ShowRootFiles"/> ne concerne que le premier niveau
    /// (SPEC-MENU-001, règle 3).
    /// </remarks>
    public IReadOnlyList<MenuEntry> ComposeFolder(string path, TrayShortcutConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return string.IsNullOrWhiteSpace(path)
            ? []
            : ComposeDirectory(path, configuration, isRoot: false);
    }

    private List<MenuEntry> ComposeDirectory(
        string path,
        TrayShortcutConfiguration configuration,
        bool isRoot)
    {
        var folderIcon = IconSourceResolver.ForFolders(configuration);
        var entries = new List<MenuEntry>();

        foreach (var directory in _source.GetDirectories(path).OrderBy(Path.GetFileName, DisplayOrder))
        {
            entries.Add(new FolderEntry(
                Label: Path.GetFileName(directory) ?? directory,
                Path: directory,
                Icon: folderIcon));
        }

        if (isRoot && !configuration.ShowsRootFiles)
        {
            return entries;
        }

        foreach (var file in _source.GetFiles(path).OrderBy(Path.GetFileName, DisplayOrder))
        {
            var target = LaunchTarget.TryCreate(file);
            if (target is null)
            {
                continue;
            }

            entries.Add(new LaunchEntry(
                Label: Path.GetFileNameWithoutExtension(file),
                Target: target,
                Icon: _icons.ForFile(file)));
        }

        return entries;
    }

    /// <summary>
    /// Ajoute la section des raccourcis personnalisés, si au moins un est exploitable
    /// (SPEC-MENU-005).
    /// </summary>
    private void AppendCustomShortcuts(List<MenuEntry> entries, TrayShortcutConfiguration configuration)
    {
        var customs = configuration.CustomShortcuts
            .Select(custom => (custom, target: custom.ToLaunchTarget()))
            // Une ligne sans chemin est une ligne à moitié remplie : on l'ignore en silence
            // plutôt que d'afficher une entrée qui ne ferait rien (SPEC-MENU-005, règle 1).
            .Where(pair => pair.target is not null)
            .Select(pair => new LaunchEntry(
                Label: LabelOf(pair.custom, pair.target!),
                Target: pair.target!,
                Icon: _icons.ForCustom(pair.custom)))
            .OrderBy(entry => entry.Label, DisplayOrder)
            .ToList();

        if (customs.Count == 0)
        {
            return;
        }

        entries.Add(SeparatorEntry.Instance);
        entries.Add(new GroupEntry(TextRef.Of(TextKeys.Menu.Customs), customs));
    }

    /// <summary>
    /// Ajoute le bloc des commandes de l'application (SPEC-MENU-001, règle 5).
    /// </summary>
    private static void AppendCommands(List<MenuEntry> entries)
    {
        entries.Add(SeparatorEntry.Instance);

        foreach (var command in Enum.GetValues<MenuCommand>())
        {
            entries.Add(new CommandEntry(TextRef.Of(TextKeys.MenuCommandLabel(command)), command));
        }
    }

    /// <summary>
    /// Intitulé d'un raccourci personnalisé : celui saisi, à défaut le nom du fichier visé
    /// (SPEC-MENU-002, règle 4).
    /// </summary>
    private static string LabelOf(CustomShortcut custom, LaunchTarget target)
        => string.IsNullOrWhiteSpace(custom.Text)
            ? Path.GetFileNameWithoutExtension(target.Path)
            : custom.Text;
}
