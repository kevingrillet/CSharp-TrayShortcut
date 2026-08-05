namespace CSharpTrayShortcut.Infrastructure.Persistence;

/// <summary>
/// Emplacements sur disque des fichiers de l'application (SPEC-CFG-001, ADR-0002).
/// </summary>
/// <remarks>
/// <para>
/// Tout vit dans <c>%APPDATA%\TrayShortcut</c>, et non plus à côté de l'exécutable comme dans
/// les versions antérieures. La raison est simple : un dossier sous <c>Program Files</c> n'est
/// pas accessible en écriture à un utilisateur ordinaire. L'ancien emplacement condamnait donc
/// l'application à ne fonctionner que depuis un dossier utilisateur, et faisait échouer
/// silencieusement l'enregistrement de la configuration ailleurs.
/// </para>
/// <para>
/// Conséquence agréable : la configuration survit au remplacement du dossier d'installation,
/// et deux comptes Windows sur la même machine ont chacun la leur.
/// </para>
/// </remarks>
public static class AppPaths
{
    /// <summary>Nom du dossier de données, sous <c>%APPDATA%</c>.</summary>
    private const string FolderName = "TrayShortcut";

    /// <summary>Dossier de données de l'utilisateur courant.</summary>
    public static string DataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        FolderName);

    /// <summary>Fichier de configuration.</summary>
    public static string ConfigurationFile { get; } = Path.Combine(DataDirectory, "config.json");

    /// <summary>Journal d'exécution.</summary>
    public static string LogFile { get; } = Path.Combine(DataDirectory, "log.txt");

    /// <summary>
    /// Dossier des icônes livrées avec l'application, à côté de l'exécutable.
    /// </summary>
    /// <remarks>
    /// C'est ce qui permet à la configuration de désigner une icône fournie par son seul nom
    /// (« folder_w11.ico ») au lieu d'un chemin absolu (SPEC-ICON-002, règle 2).
    /// </remarks>
    public static string BundledIconsDirectory { get; } = Path.Combine(AppContext.BaseDirectory, "Icons");

    /// <summary>
    /// Crée le dossier de données s'il n'existe pas, et rend vrai si l'on peut y écrire.
    /// </summary>
    public static bool EnsureDataDirectory()
    {
        try
        {
            Directory.CreateDirectory(DataDirectory);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>
    /// Chemin d'un rapport de plantage, nommé de façon à ne jamais écraser le précédent
    /// (SPEC-APP-002).
    /// </summary>
    /// <param name="momentUtc">Instant du plantage.</param>
    /// <param name="discriminant">
    /// Suffixe distinguant deux plantages de la même seconde.
    /// </param>
    public static string CrashReportFile(DateTime momentUtc, Guid discriminant)
    {
        var nom = string.Concat(
            "crash-",
            momentUtc.ToString("yyyy-MM-ddTHH-mm-ss", System.Globalization.CultureInfo.InvariantCulture),
            "-",
            discriminant.ToString("N"),
            ".txt");

        return Path.Combine(DataDirectory, nom);
    }
}
