namespace CSharpTrayShortcut.Domain.Shortcuts;

/// <summary>Manière d'obtenir l'image d'une entrée de menu (SPEC-ICON-001).</summary>
public enum IconSourceKind
{
    /// <summary>Aucune image : l'entrée s'affiche sans icône.</summary>
    None = 0,

    /// <summary>Le chemin désigne un fichier <c>.ico</c> à charger tel quel.</summary>
    IconFile = 1,

    /// <summary>
    /// Le chemin désigne un fichier quelconque dont il faut extraire l'icône associée.
    /// </summary>
    ExtractFromFile = 2,
}

/// <summary>
/// D'où vient l'image d'une entrée de menu — <b>pas</b> l'image elle-même (SPEC-ICON-001).
/// </summary>
/// <remarks>
/// <para>
/// C'est la clé du découpage : décider quelle icône montrer est une règle, la fabriquer est
/// du dessin. La règle (icône explicite, sinon extraction de la cible, sinon repli) vit dans
/// la couche application et se teste sans Windows ; la fabrication vit dans la couche de
/// présentation, seule à connaître <c>System.Drawing</c>.
/// </para>
/// <para>
/// Sans cette séparation, le domaine porterait des <c>Bitmap</c> — donc des ressources GDI à
/// libérer, donc l'impossibilité de tester la règle sans écran.
/// </para>
/// <para>
/// <b>Le repli fait partie de la source.</b> « L'icône configurée, sinon celle livrée avec
/// l'application » est une décision, pas un détail de rendu : elle est donc décrite ici, par
/// <see cref="Fallback"/>, et non laissée à l'initiative de celui qui dessine. Le rendu se
/// contente de descendre la chaîne jusqu'à ce qu'une source donne une image (SPEC-ICON-002).
/// </para>
/// </remarks>
public sealed record IconSource
{
    /// <summary>Aucune image.</summary>
    public static readonly IconSource None = new() { Kind = IconSourceKind.None };

    private IconSource()
    {
    }

    /// <summary>Manière d'obtenir l'image.</summary>
    public required IconSourceKind Kind { get; init; }

    /// <summary>
    /// Chemin concerné, ou <see langword="null"/> quand <see cref="Kind"/> vaut
    /// <see cref="IconSourceKind.None"/>.
    /// </summary>
    public string? Path { get; init; }

    /// <summary>
    /// Source à essayer si celle-ci ne donne rien, ou <see langword="null"/> s'il n'y a pas de
    /// repli.
    /// </summary>
    public IconSource? Fallback { get; init; }

    /// <summary>Fichier <c>.ico</c> à charger tel quel, ou aucune image si le chemin est vide.</summary>
    public static IconSource FromIconFile(string? path)
        => string.IsNullOrWhiteSpace(path)
            ? None
            : new IconSource { Kind = IconSourceKind.IconFile, Path = path };

    /// <summary>
    /// Fichier dont il faut extraire l'icône associée, ou aucune image si le chemin est vide.
    /// </summary>
    public static IconSource ExtractedFrom(string? path)
        => string.IsNullOrWhiteSpace(path)
            ? None
            : new IconSource { Kind = IconSourceKind.ExtractFromFile, Path = path };

    /// <summary>
    /// Cette source, avec <paramref name="fallback"/> ajouté au bout de sa chaîne de replis.
    /// </summary>
    /// <remarks>
    /// Les sources vides s'effacent au lieu d'allonger la chaîne : chaîner un repli sur
    /// « aucune image » rend directement le repli, et replier sur « aucune image » ne change
    /// rien. La chaîne obtenue ne contient donc que des sources réellement tentables.
    /// </remarks>
    public IconSource Or(IconSource fallback)
    {
        ArgumentNullException.ThrowIfNull(fallback);

        if (Kind == IconSourceKind.None)
        {
            return fallback;
        }

        if (fallback.Kind == IconSourceKind.None)
        {
            return this;
        }

        return this with
        {
            Fallback = Fallback is null ? fallback : Fallback.Or(fallback),
        };
    }

    /// <summary>Cette source puis ses replis, dans l'ordre d'essai.</summary>
    public IEnumerable<IconSource> Chain()
    {
        for (var source = this; source is not null; source = source.Fallback)
        {
            yield return source;
        }
    }
}
