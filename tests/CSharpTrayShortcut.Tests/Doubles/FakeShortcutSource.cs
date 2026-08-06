using CSharpTrayShortcut.Application.Abstractions;
using CSharpTrayShortcut.Domain.Shortcuts;

namespace CSharpTrayShortcut.Tests.Doubles;

/// <summary>
/// Arborescence en mémoire, à la place du système de fichiers.
/// </summary>
/// <remarks>
/// <para>
/// C'est ce double qui rend les règles du menu vérifiables : décrire un dossier illisible, un
/// partage réseau vide ou un fichier disparu tient ici en une ligne, là où le vrai disque
/// demanderait des dossiers temporaires, des droits particuliers, et resterait incapable de
/// simuler un lecteur déconnecté.
/// </para>
/// <para>
/// L'ordre de restitution est délibérément <b>celui de la déclaration</b>, jamais trié : c'est
/// ce qui permet de vérifier que le tri d'affichage est bien l'œuvre de <c>MenuComposer</c>
/// (SPEC-MENU-002) et non un effet de bord du système de fichiers.
/// </para>
/// </remarks>
internal sealed class FakeShortcutSource : IShortcutSource
{
    private readonly Dictionary<string, List<string>> _sousDossiers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<string>> _fichiers = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _dossiersExistants = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _fichiersExistants = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _illisibles = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, FileStamp> _empreintes = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Nombre d'énumérations demandées, pour vérifier la construction paresseuse.</summary>
    internal int Enumerations { get; private set; }

    /// <summary>
    /// Déclare un dossier existant, avec son contenu direct.
    /// </summary>
    /// <param name="chemin">Chemin du dossier.</param>
    /// <param name="dossiers">Noms des sous-dossiers, dans l'ordre de restitution voulu.</param>
    /// <param name="fichiers">Noms des fichiers, dans l'ordre de restitution voulu.</param>
    internal FakeShortcutSource Dossier(string chemin, string[]? dossiers = null, string[]? fichiers = null)
    {
        _dossiersExistants.Add(chemin);
        _sousDossiers[chemin] = [];
        _fichiers[chemin] = [];

        foreach (var nom in dossiers ?? [])
        {
            var complet = Combiner(chemin, nom);
            _sousDossiers[chemin].Add(complet);
            _dossiersExistants.Add(complet);
        }

        foreach (var nom in fichiers ?? [])
        {
            var complet = Combiner(chemin, nom);
            _fichiers[chemin].Add(complet);
            _fichiersExistants.Add(complet);
        }

        return this;
    }

    /// <summary>Déclare un fichier existant sans le rattacher à l'énumération d'un dossier.</summary>
    internal FakeShortcutSource Fichier(string chemin)
    {
        _fichiersExistants.Add(chemin);
        return this;
    }

    /// <summary>
    /// Déclare un dossier dont la lecture échoue : droits refusés, lecteur réseau coupé,
    /// dossier supprimé entre-temps (SPEC-MENU-004).
    /// </summary>
    /// <remarks>
    /// Le dossier reste « existant » : c'est précisément le cas gênant — il apparaît dans le
    /// menu, et son contenu est vide quand on l'ouvre.
    /// </remarks>
    internal FakeShortcutSource Illisible(string chemin)
    {
        _dossiersExistants.Add(chemin);
        _illisibles.Add(chemin);
        return this;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetDirectories(string path)
    {
        Enumerations++;
        return _illisibles.Contains(path) ? [] : Lire(_sousDossiers, path);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetFiles(string path)
        => _illisibles.Contains(path) ? [] : Lire(_fichiers, path);

    /// <inheritdoc />
    public bool DirectoryExists(string path)
        => !string.IsNullOrWhiteSpace(path) && _dossiersExistants.Contains(path);

    /// <inheritdoc />
    public bool FileExists(string path)
        => !string.IsNullOrWhiteSpace(path) && _fichiersExistants.Contains(path);

    /// <inheritdoc />
    public FileStamp? GetFileStamp(string path)
    {
        if (!FileExists(path))
        {
            return null;
        }

        return _empreintes.TryGetValue(path, out var empreinte)
            ? empreinte
            // Empreinte stable par défaut : sans elle, deux appels rendraient des valeurs
            // différentes et aucune entrée de cache ne serait jamais réutilisée.
            : new FileStamp(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), 1024);
    }

    /// <summary>
    /// Déclare une empreinte particulière, pour simuler un fichier modifié entre deux rendus
    /// (SPEC-ICON-004, règle 3).
    /// </summary>
    internal FakeShortcutSource Empreinte(string chemin, FileStamp empreinte)
    {
        _fichiersExistants.Add(chemin);
        _empreintes[chemin] = empreinte;
        return this;
    }

    private static IReadOnlyList<string> Lire(Dictionary<string, List<string>> table, string path)
        => string.IsNullOrWhiteSpace(path) || !table.TryGetValue(path, out var contenu) ? [] : contenu;

    private static string Combiner(string dossier, string nom)
        => string.Concat(dossier.TrimEnd('\\'), "\\", nom);
}
