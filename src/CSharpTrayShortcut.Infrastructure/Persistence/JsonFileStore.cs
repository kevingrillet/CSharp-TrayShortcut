using System.Globalization;
using System.Text.Json;
using CSharpTrayShortcut.Application.Configuration;
using Microsoft.Extensions.Logging;

namespace CSharpTrayShortcut.Infrastructure.Persistence;

/// <summary>
/// Lecture et écriture d'un objet dans un fichier JSON, sans jamais lever d'exception
/// (SPEC-CFG-001, règle 3).
/// </summary>
/// <remarks>
/// <para>
/// Brique partagée par les dépôts de la couche infrastructure. Elle absorbe les trois
/// défaillances de la persistance sur poste de travail : le fichier n'existe pas encore, son
/// contenu est abîmé (édité à la main, tronqué par un arrêt brutal), ou le disque refuse
/// l'écriture. Dans les deux premiers cas l'appelant reçoit <see langword="null"/> et repart
/// sur des valeurs par défaut ; dans le troisième, il apprend l'échec par un booléen.
/// </para>
/// <para>
/// <b>Écriture en deux temps.</b> Le contenu part dans un fichier temporaire voisin, qui
/// remplace ensuite l'original. Une coupure de courant pendant l'enregistrement laisse donc
/// l'ancienne configuration intacte, au lieu d'un fichier à moitié écrit qui serait illisible
/// au prochain démarrage.
/// </para>
/// </remarks>
public sealed class JsonFileStore
{
    /// <summary>
    /// Format du fichier, défini par la couche application : c'est un contrat avec
    /// l'utilisateur, pas un détail d'écriture (voir <see cref="ConfigurationSerialization"/>).
    /// </summary>
    private static readonly JsonSerializerOptions Options = ConfigurationSerialization.Options;

    private readonly ILogger<JsonFileStore> _logger;

    /// <summary>Construit le magasin.</summary>
    /// <param name="logger">Journal, pour tracer les fichiers illisibles ou non écrits.</param>
    public JsonFileStore(ILogger<JsonFileStore> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <summary>
    /// Relit un objet depuis <paramref name="path"/>, ou rend <see langword="null"/> si le
    /// fichier est absent, vide ou illisible.
    /// </summary>
    public T? Load<T>(string path)
        where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var contenu = File.ReadAllText(path);
            return string.IsNullOrWhiteSpace(contenu)
                ? null
                : JsonSerializer.Deserialize<T>(contenu, Options);
        }
        catch (JsonException ex)
        {
            // Le contenu est en cause : le mettre de côté, pour que le prochain démarrage
            // reparte d'un fichier sain sans avoir perdu ce que l'utilisateur avait écrit.
            _logger.LogError(
                ex,
                "Fichier illisible, mis de côté et recréé : {Chemin}",
                path);
            MettreDeCote(path);
            return null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Le contenu n'est pas en cause : verrou momentané, droits, disque en panne.
            // Surtout ne pas déplacer le fichier — il est probablement parfaitement valide.
            _logger.LogWarning(
                ex,
                "Lecture impossible, valeurs par défaut employées : {Chemin}",
                path);
            return null;
        }
    }

    /// <summary>
    /// Renomme un fichier dont le contenu est inexploitable, en conservant l'original
    /// (SPEC-CFG-001, règle 7).
    /// </summary>
    /// <remarks>
    /// <para>
    /// L'écraser serait plus simple et impardonnable : un fichier édité à la main contient le
    /// travail de quelqu'un, et une accolade oubliée ne doit pas le faire disparaître. Le nom de
    /// sauvegarde porte donc un horodatage, pour qu'une seconde tentative ratée n'efface pas la
    /// première.
    /// </para>
    /// <para>
    /// Toute défaillance est absorbée : si la mise de côté échoue, l'appelant repart quand même
    /// des valeurs par défaut. Le pire cas est un avertissement de plus au prochain démarrage,
    /// pas un démarrage impossible.
    /// </para>
    /// </remarks>
    private void MettreDeCote(string path)
    {
        var horodatage = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var sauvegarde = $"{path}.{horodatage}.invalide";

        try
        {
            File.Move(path, sauvegarde, overwrite: true);
            _logger.LogInformation("Ancien contenu conservé dans {Sauvegarde}", sauvegarde);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Mise de côté impossible : {Chemin}", path);
        }
    }

    /// <summary>
    /// Écrit un objet dans <paramref name="path"/>, en créant le dossier au besoin.
    /// </summary>
    /// <returns>Vrai si l'écriture a abouti ; faux si le disque l'a refusée.</returns>
    public bool Save<T>(string path, T value)
        where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(value);

        var temporaire = path + ".tmp";

        try
        {
            var dossier = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dossier))
            {
                Directory.CreateDirectory(dossier);
            }

            File.WriteAllText(temporaire, JsonSerializer.Serialize(value, Options));

            // File.Move écrase : inutile de supprimer d'abord, ce qui laisserait une fenêtre
            // pendant laquelle aucun des deux fichiers n'existe.
            File.Move(temporaire, path, overwrite: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            _logger.LogError(ex, "Écriture impossible : {Chemin}", path);
            Supprimer(temporaire);
            return false;
        }
    }

    /// <summary>
    /// Supprime un fichier temporaire resté en place, sans jamais masquer l'erreur d'origine.
    /// </summary>
    private static void Supprimer(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception nettoyage) when (nettoyage is IOException or UnauthorizedAccessException)
        {
            // Un temporaire orphelin est sans conséquence : la prochaine écriture l'écrase.
            // Surtout, ne pas laisser cette exception masquer celle qui nous a menés ici.
        }
    }
}
