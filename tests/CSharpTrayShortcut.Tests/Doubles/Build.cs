using CSharpTrayShortcut.Application.Configuration;
using CSharpTrayShortcut.Application.Launching;
using CSharpTrayShortcut.Application.Menu;
using CSharpTrayShortcut.Domain.Menu;
using Microsoft.Extensions.Logging.Abstractions;

namespace CSharpTrayShortcut.Tests.Doubles;

/// <summary>
/// Fabrique les objets dont les tests ont besoin.
/// </summary>
/// <remarks>
/// Convention du dépôt : <b>aucun test ne construit un objet du domaine ou de la configuration
/// directement</b>. Tout passe par ici, pour qu'ajouter un champ obligatoire ne casse pas
/// trente tests, et pour que chaque test ne mentionne que ce qui le concerne réellement.
/// </remarks>
internal static class Build
{
    /// <summary>Dossier surveillé employé par tous les tests.</summary>
    internal const string Racine = @"C:\Toolbar";

    /// <summary>Configuration valide, dont chaque aspect peut être surchargé.</summary>
    internal static TrayShortcutConfiguration Configuration(
        string? path = Racine,
        bool? showRootFiles = null,
        string? folderIcon = null,
        string? trayIcon = null,
        params CustomShortcut[] customs) => new()
        {
            Path = path,
            ShowRootFiles = showRootFiles,
            PathFolderIcon = folderIcon,
            PathTrayIcon = trayIcon,
            CustomShortcuts = [.. customs],
        };

    /// <summary>Raccourci personnalisé.</summary>
    internal static CustomShortcut Custom(
        string? path,
        string? text = null,
        string? argument = null,
        string? image = null) => new()
        {
            Path = path,
            Text = text,
            Argument = argument,
            Image = image,
        };

    /// <summary>Compositeur de menu branché sur les doubles fournis.</summary>
    internal static MenuComposer Composer(FakeShortcutSource source, FakeShortcutTargetResolver? targets = null)
        => new(source, Icons(source, targets));

    /// <summary>Résolveur d'icônes branché sur les doubles fournis.</summary>
    internal static IconSourceResolver Icons(FakeShortcutSource source, FakeShortcutTargetResolver? targets = null)
        => new(source, targets ?? new FakeShortcutTargetResolver());

    /// <summary>Règle de réutilisation des images branchée sur le double fourni.</summary>
    internal static IconCachePolicy CachePolicy(FakeShortcutSource source) => new(source);

    /// <summary>Service de lancement branché sur les doubles fournis.</summary>
    internal static LaunchService Launcher(FakeShortcutSource source, FakeProcessLauncher launcher)
        => new(source, launcher, NullLogger<LaunchService>.Instance);

    /// <summary>Chemin complet d'un élément du dossier surveillé.</summary>
    internal static string Dans(string nom) => string.Concat(Racine, "\\", nom);

    /// <summary>Intitulés des entrées lançables d'un menu, dans l'ordre.</summary>
    internal static IReadOnlyList<string> LibellesLancables(IEnumerable<MenuEntry> entries)
        => [.. entries.OfType<LaunchEntry>().Select(entry => entry.Label)];

    /// <summary>Intitulés des entrées de dossier d'un menu, dans l'ordre.</summary>
    internal static IReadOnlyList<string> LibellesDossiers(IEnumerable<MenuEntry> entries)
        => [.. entries.OfType<FolderEntry>().Select(entry => entry.Label)];
}
