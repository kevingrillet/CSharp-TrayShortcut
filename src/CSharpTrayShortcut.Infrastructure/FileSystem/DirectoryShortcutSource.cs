using CSharpTrayShortcut.Application.Abstractions;
using CSharpTrayShortcut.Domain.Shortcuts;
using Microsoft.Extensions.Logging;

namespace CSharpTrayShortcut.Infrastructure.FileSystem;

/// <summary>
/// Source de raccourcis adossée au système de fichiers (SPEC-MENU-001, SPEC-MENU-004).
/// </summary>
/// <remarks>
/// <para>
/// Toute la tolérance aux pannes du disque est concentrée ici, comme l'exige le contrat de
/// <see cref="IShortcutSource"/> : un dossier auquel on n'a pas droit, un lecteur réseau
/// déconnecté, un dossier supprimé entre la construction du menu et son ouverture rendent une
/// liste vide. Un seul mauvais dossier ne fait donc jamais tomber le menu entier — c'était
/// l'objet du garde-fou <c>SafeEnumerate</c> de la version antérieure, désormais au bon
/// endroit.
/// </para>
/// <para>
/// Aucun ordre n'est garanti à la sortie : le tri est une décision d'affichage, prise par
/// <c>MenuComposer</c> (SPEC-MENU-002).
/// </para>
/// </remarks>
public sealed class DirectoryShortcutSource : IShortcutSource
{
    private readonly ILogger<DirectoryShortcutSource> _logger;

    /// <summary>Construit la source.</summary>
    /// <param name="logger">Journal, pour tracer les dossiers illisibles.</param>
    public DirectoryShortcutSource(ILogger<DirectoryShortcutSource> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetDirectories(string path)
        => Enumerer(path, Directory.GetDirectories);

    /// <inheritdoc />
    public IReadOnlyList<string> GetFiles(string path)
        => Enumerer(path, Directory.GetFiles);

    /// <inheritdoc />
    public bool DirectoryExists(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            return Directory.Exists(path);
        }
        catch (Exception ex) when (EstUnEchecDeDisque(ex))
        {
            // Directory.Exists avale déjà l'essentiel, mais pas un chemin syntaxiquement
            // invalide sur certains montages réseau.
            _logger.LogDebug(ex, "Dossier inaccessible : {Chemin}", path);
            return false;
        }
    }

    /// <inheritdoc />
    public bool FileExists(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            return File.Exists(path);
        }
        catch (Exception ex) when (EstUnEchecDeDisque(ex))
        {
            _logger.LogDebug(ex, "Fichier inaccessible : {Chemin}", path);
            return false;
        }
    }

    /// <inheritdoc />
    public FileStamp? GetFileStamp(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            var info = new FileInfo(path);

            // FileInfo ne touche le disque qu'ici, et met ses propriétés en cache : une seule
            // lecture de métadonnées pour les deux valeurs.
            return info.Exists
                ? new FileStamp(info.LastWriteTimeUtc, info.Length)
                : null;
        }
        catch (Exception ex) when (EstUnEchecDeDisque(ex))
        {
            _logger.LogDebug(ex, "Empreinte illisible : {Chemin}", path);
            return null;
        }
    }

    private IReadOnlyList<string> Enumerer(string path, Func<string, string[]> enumerer)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return [];
        }

        try
        {
            return enumerer(path);
        }
        catch (Exception ex) when (EstUnEchecDeDisque(ex))
        {
            _logger.LogInformation(
                ex,
                "Dossier illisible, ignoré dans le menu : {Chemin}",
                path);
            return [];
        }
    }

    /// <summary>
    /// Familles d'exceptions qu'un accès disque peut légitimement produire sur un poste de
    /// travail, et qui doivent toutes se traduire par « ce dossier n'est pas exploitable ».
    /// </summary>
    /// <remarks>
    /// Liste explicite plutôt qu'un <c>catch</c> général : une <c>NullReferenceException</c> ou
    /// une <c>OutOfMemoryException</c> est un défaut du programme, et l'avaler ici la rendrait
    /// invisible.
    /// </remarks>
    private static bool EstUnEchecDeDisque(Exception ex)
        => ex is IOException
            or UnauthorizedAccessException
            or System.Security.SecurityException
            or ArgumentException;
}
