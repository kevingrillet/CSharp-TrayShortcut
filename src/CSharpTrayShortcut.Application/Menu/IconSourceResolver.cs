using CSharpTrayShortcut.Application.Abstractions;
using CSharpTrayShortcut.Application.Configuration;
using CSharpTrayShortcut.Domain.Shortcuts;

namespace CSharpTrayShortcut.Application.Menu;

/// <summary>
/// Décide <b>quelle</b> icône montrer pour chaque entrée de menu — jamais comment la dessiner
/// (SPEC-ICON-001 à SPEC-ICON-003).
/// </summary>
/// <remarks>
/// <para>
/// Toutes les règles d'icône du dépôt sont ici, et elles se vérifient sans écran : c'est le
/// bénéfice concret d'avoir fait de l'icône une <i>source</i> (<see cref="IconSource"/>) et
/// non une image.
/// </para>
/// <para>
/// Les trois règles, dans l'ordre où elles se posent : une icône explicitement désignée gagne
/// toujours ; à défaut on extrait celle du fichier visé ; et pour un raccourci Windows, « le
/// fichier visé » est la cible du raccourci, pas le raccourci lui-même — sans quoi tous les
/// <c>.lnk</c> partageraient la même image de flèche.
/// </para>
/// </remarks>
public sealed class IconSourceResolver
{
    /// <summary>Extension des raccourcis Windows, dont il faut suivre la cible.</summary>
    private const string ShortcutExtension = ".lnk";

    private readonly IShortcutSource _source;
    private readonly IShortcutTargetResolver _targets;

    /// <summary>Construit le résolveur.</summary>
    /// <param name="source">Accès au système de fichiers, pour vérifier l'existence.</param>
    /// <param name="targets">Lecture de la cible d'un raccourci Windows.</param>
    public IconSourceResolver(IShortcutSource source, IShortcutTargetResolver targets)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(targets);

        _source = source;
        _targets = targets;
    }

    /// <summary>
    /// Icône d'un fichier du dossier surveillé (SPEC-ICON-001, SPEC-ICON-003).
    /// </summary>
    /// <remarks>
    /// Un fichier absent rend <see cref="IconSource.None"/> plutôt qu'un repli : une entrée
    /// sans image reste lisible, alors qu'une image trompeuse ne l'est pas.
    /// </remarks>
    public IconSource ForFile(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !_source.FileExists(filePath))
        {
            return IconSource.None;
        }

        if (IsWindowsShortcut(filePath))
        {
            var target = _targets.ResolveTarget(filePath);

            // Cible illisible ou disparue : on retombe sur l'icône du raccourci lui-même,
            // qui existe toujours puisqu'on vient de le vérifier.
            if (!string.IsNullOrWhiteSpace(target) && _source.FileExists(target))
            {
                return IconSource.ExtractedFrom(target);
            }
        }

        return IconSource.ExtractedFrom(filePath);
    }

    /// <summary>
    /// Icône d'un raccourci personnalisé (SPEC-ICON-001, règle 2).
    /// </summary>
    /// <remarks>
    /// L'icône explicitement désignée n'est pas vérifiée sur le disque : c'est le seul endroit
    /// où l'utilisateur a dit ce qu'il voulait, et le repli du rendu suffit si le fichier
    /// manque. La vérifier ici obligerait à choisir un repli sans savoir pourquoi le premier
    /// choix a échoué.
    /// </remarks>
    public IconSource ForCustom(CustomShortcut custom)
    {
        ArgumentNullException.ThrowIfNull(custom);

        return string.IsNullOrWhiteSpace(custom.Image)
            ? ForFile(custom.Path)
            : IconSource.FromIconFile(custom.Image);
    }

    /// <summary>
    /// Icône commune aux dossiers : celle configurée, puis celle livrée avec l'application
    /// (SPEC-ICON-002).
    /// </summary>
    public static IconSource ForFolders(TrayShortcutConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return IconSource.FromIconFile(configuration.PathFolderIcon)
            .Or(IconSource.FromIconFile(TrayShortcutConfiguration.DefaultFolderIcon));
    }

    /// <summary>
    /// Icône de la zone de notification : celle configurée, puis celle livrée avec
    /// l'application (SPEC-ICON-002).
    /// </summary>
    public static IconSource ForTray(TrayShortcutConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return IconSource.FromIconFile(configuration.PathTrayIcon)
            .Or(IconSource.FromIconFile(TrayShortcutConfiguration.DefaultTrayIcon));
    }

    private static bool IsWindowsShortcut(string filePath)
        => Path.GetExtension(filePath).Equals(ShortcutExtension, StringComparison.OrdinalIgnoreCase);
}
