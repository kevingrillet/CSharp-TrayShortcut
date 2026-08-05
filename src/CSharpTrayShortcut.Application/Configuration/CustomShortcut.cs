using CSharpTrayShortcut.Domain.Shortcuts;

namespace CSharpTrayShortcut.Application.Configuration;

/// <summary>
/// Raccourci déclaré à la main dans la configuration, pour lancer quelque chose qui n'est pas
/// dans le dossier surveillé (SPEC-MENU-005).
/// </summary>
/// <remarks>
/// <para>
/// Classe mutable à propriétés publiques, et non <c>record</c> : c'est le type que
/// <c>System.Text.Json</c> désérialise et que la grille de la fenêtre d'édition lie
/// directement (SPEC-CFG-003). Un type immuable obligerait à en maintenir un jumeau mutable
/// pour l'édition, sans rien gagner.
/// </para>
/// <para>
/// Les chaînes sont volontairement <see langword="null"/> par défaut plutôt que vides : la
/// distinction porte du sens à l'écriture du fichier — une propriété absente du JSON reste
/// absente, au lieu d'être écrite comme <c>""</c>.
/// </para>
/// </remarks>
public sealed class CustomShortcut
{
    /// <summary>Argument ajouté au lancement.</summary>
    public string? Argument { get; set; }

    /// <summary>
    /// Fichier <c>.ico</c> à afficher. Si absent, l'icône est extraite de
    /// <see cref="Path"/> (SPEC-ICON-001, règle 2).
    /// </summary>
    public string? Image { get; set; }

    /// <summary>Chemin ou exécutable à lancer. Sans lui, l'entrée est ignorée.</summary>
    public string? Path { get; set; }

    /// <summary>
    /// Intitulé affiché. Si absent, le nom du fichier visé en tient lieu (SPEC-MENU-002,
    /// règle 4).
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Cible de lancement correspondante, ou <see langword="null"/> si le chemin est absent.
    /// </summary>
    public LaunchTarget? ToLaunchTarget() => LaunchTarget.TryCreate(Path, Argument);

    /// <summary>
    /// Copie où les chaînes vides sont remplacées par <see langword="null"/>, et les lignes
    /// sans chemin écartées.
    /// </summary>
    /// <remarks>
    /// Appliqué avant écriture : la grille d'édition produit des chaînes vides dès qu'une
    /// cellule a été visitée, et les écrire telles quelles remplirait le fichier de
    /// <c>""</c> parasites.
    /// </remarks>
    public CustomShortcut Normalized() => new()
    {
        Argument = Blank(Argument),
        Image = Blank(Image),
        Path = Path,
        Text = Blank(Text),
    };

    private static string? Blank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}
