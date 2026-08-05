namespace CSharpTrayShortcut.Domain.Shortcuts;

/// <summary>
/// Identifie une image dans le cache du rendu (SPEC-ICON-004).
/// </summary>
/// <remarks>
/// <para>
/// La clé n'est pas le chemin du fichier, et c'est tout l'intérêt. Windows distingue deux
/// familles :
/// </para>
/// <list type="bullet">
/// <item>
/// un <b>document</b> — <c>.pdf</c>, <c>.docx</c>, <c>.txt</c> — reçoit l'icône <b>associée à
/// son extension</b>. Trente fichiers PDF d'un même dossier partagent donc une seule image, et
/// une seule extraction suffit ;
/// </item>
/// <item>
/// un <b>exécutable</b> — <c>.exe</c>, <c>.dll</c>, <c>.ico</c> — porte sa <b>propre</b> icône.
/// La clé doit alors désigner le fichier, et son empreinte, pour qu'une mise à jour de
/// l'application change bien son image.
/// </item>
/// </list>
/// <para>
/// Sans cette distinction, un dossier de documents provoquait autant d'appels au shell qu'il
/// contenait de fichiers, pour produire des images identiques.
/// </para>
/// </remarks>
public sealed record IconCacheKey
{
    private IconCacheKey()
    {
    }

    /// <summary>Manière d'obtenir l'image.</summary>
    public required IconSourceKind Kind { get; init; }

    /// <summary>
    /// Ce qui distingue cette image des autres : une extension, ou un chemin.
    /// </summary>
    public required string Identity { get; init; }

    /// <summary>
    /// Empreinte du fichier, lorsque l'image en dépend. <see langword="null"/> pour une image
    /// qui ne dépend que d'une extension.
    /// </summary>
    public FileStamp? Stamp { get; init; }

    /// <summary>
    /// Image partagée par tous les fichiers d'une même extension.
    /// </summary>
    /// <param name="extension">Extension, point compris, telle que <c>.pdf</c>.</param>
    public static IconCacheKey ForExtension(string extension) => new()
    {
        Kind = IconSourceKind.ExtractFromFile,
        Identity = extension.ToLowerInvariant(),
    };

    /// <summary>
    /// Image propre à un fichier, invalidée dès que celui-ci change.
    /// </summary>
    /// <param name="kind">Manière d'obtenir l'image.</param>
    /// <param name="path">Chemin du fichier.</param>
    /// <param name="stamp">Empreinte du fichier, si elle a pu être lue.</param>
    public static IconCacheKey ForFile(IconSourceKind kind, string path, FileStamp? stamp) => new()
    {
        Kind = kind,
        Identity = path,
        Stamp = stamp,
    };
}
