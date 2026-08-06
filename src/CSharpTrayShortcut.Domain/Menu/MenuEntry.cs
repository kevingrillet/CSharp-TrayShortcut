using CSharpTrayShortcut.Domain.Shortcuts;
using CSharpTrayShortcut.Domain.Text;

namespace CSharpTrayShortcut.Domain.Menu;

/// <summary>
/// Entrée du menu de la zone de notification, indépendamment de la façon de l'afficher
/// (SPEC-MENU-001).
/// </summary>
/// <remarks>
/// <para>
/// Le menu est décrit comme une <b>donnée</b> : une liste d'entrées que la couche application
/// compose et que la couche de présentation traduit en <c>ToolStripItem</c>. C'est ce qui rend
/// vérifiable « les dossiers viennent avant les fichiers », « la section des raccourcis
/// personnalisés n'apparaît que s'il y en a » ou « les commandes ferment le menu » — sans
/// jamais instancier de fenêtre.
/// </para>
/// <para>
/// Hiérarchie fermée (constructeur interne à l'assembly) : les cinq formes ci-dessous sont
/// exhaustives, et la présentation peut les traiter par filtrage de motif sans branche
/// « autre ».
/// </para>
/// </remarks>
public abstract record MenuEntry
{
    /// <summary>Empêche toute forme d'entrée déclarée hors de ce fichier.</summary>
    private protected MenuEntry()
    {
    }
}

/// <summary>
/// Sous-dossier du dossier surveillé. Son contenu n'est pas décrit ici : il est construit à
/// la demande, à la première ouverture (SPEC-MENU-003).
/// </summary>
/// <param name="Label">Nom affiché — le nom du dossier.</param>
/// <param name="Path">Chemin complet du dossier, à énumérer lors de l'ouverture.</param>
/// <param name="Icon">Image du dossier, commune à tous les dossiers (SPEC-ICON-002).</param>
public sealed record FolderEntry(string Label, string Path, IconSource Icon) : MenuEntry;

/// <summary>Élément lançable : un fichier du dossier surveillé, ou un raccourci personnalisé.</summary>
/// <param name="Label">Nom affiché.</param>
/// <param name="Target">Ce qu'il faut lancer au clic.</param>
/// <param name="Icon">Image de l'élément (SPEC-ICON-001).</param>
public sealed record LaunchEntry(string Label, LaunchTarget Target, IconSource Icon) : MenuEntry;

/// <summary>Trait de séparation entre deux blocs du menu.</summary>
/// <remarks>
/// Présent dans le modèle plutôt qu'ajouté au moment du rendu : la position des séparateurs
/// <i>est</i> une décision de composition (SPEC-MENU-001), et c'est à ce titre qu'elle se
/// teste.
/// </remarks>
public sealed record SeparatorEntry : MenuEntry
{
    /// <summary>Instance unique : un séparateur ne porte aucune donnée.</summary>
    public static readonly SeparatorEntry Instance = new();

    private SeparatorEntry()
    {
    }
}

/// <summary>
/// Regroupement d'entrées sous un intitulé traduit — la section des raccourcis personnalisés
/// (SPEC-MENU-005).
/// </summary>
/// <param name="Label">Intitulé traduit de la section.</param>
/// <param name="Children">Entrées de la section, déjà ordonnées.</param>
public sealed record GroupEntry(TextRef Label, IReadOnlyList<MenuEntry> Children) : MenuEntry;

/// <summary>Action de l'application elle-même (SPEC-MENU-001, règle 5).</summary>
/// <param name="Label">Intitulé traduit.</param>
/// <param name="Command">Action déclenchée au clic.</param>
public sealed record CommandEntry(TextRef Label, MenuCommand Command) : MenuEntry;
