using CSharpTrayShortcut.Application.Abstractions;
using CSharpTrayShortcut.Application.Configuration;
using Microsoft.Extensions.Logging;

namespace CSharpTrayShortcut.Infrastructure.Persistence;

/// <summary>
/// Configuration persistée en JSON dans le dossier de données de l'utilisateur
/// (SPEC-CFG-001).
/// </summary>
/// <remarks>
/// Implémentation du port <see cref="IConfigurationStore"/>. La tolérance aux fichiers abîmés et
/// l'écriture en deux temps sont des propriétés de <see cref="JsonFileStore"/> ; ce qui
/// s'ajoute ici est la <b>garantie qu'un fichier lisible existe toujours après une
/// lecture</b> (SPEC-CFG-001, règle 7).
/// </remarks>
public sealed class JsonConfigurationStore : IConfigurationStore
{
    private readonly JsonFileStore _store;
    private readonly ILogger<JsonConfigurationStore> _logger;

    /// <summary>Construit le dépôt.</summary>
    /// <param name="store">Magasin JSON sous-jacent.</param>
    /// <param name="logger">Journal.</param>
    public JsonConfigurationStore(JsonFileStore store, ILogger<JsonConfigurationStore> logger)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(logger);

        _store = store;
        _logger = logger;
    }

    /// <inheritdoc />
    public string ConfigurationFilePath => AppPaths.ConfigurationFile;

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Quand aucune configuration n'a pu être relue — fichier absent, vide, ou mis de côté parce
    /// qu'il était abîmé —, une configuration par défaut est <b>écrite sur le disque</b> avant
    /// d'être retournée.
    /// </para>
    /// <para>
    /// Sans cette recréation, l'utilisateur dont le fichier avait été mis de côté se retrouvait
    /// sans rien à éditer, et l'application redemandait le dossier surveillé à chaque
    /// démarrage. Avec elle, il retrouve un fichier valide, à la bonne place, montrant la forme
    /// attendue — et son ancien contenu voisine dans un fichier <c>.invalide</c>.
    /// </para>
    /// <para>
    /// L'échec de cette écriture n'est pas bloquant : <see cref="JsonFileStore"/> l'a déjà
    /// journalisé, et la configuration par défaut reste utilisable en mémoire.
    /// </para>
    /// </remarks>
    public TrayShortcutConfiguration Load()
    {
        var configuration = _store.Load<TrayShortcutConfiguration>(AppPaths.ConfigurationFile);
        if (configuration is not null)
        {
            return configuration;
        }

        var defaut = new TrayShortcutConfiguration();

        if (_store.Save(AppPaths.ConfigurationFile, defaut))
        {
            _logger.LogInformation(
                "Configuration par défaut recréée : {Chemin}",
                AppPaths.ConfigurationFile);
        }

        return defaut;
    }

    /// <inheritdoc />
    public bool Save(TrayShortcutConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // Les lignes à moitié remplies de la fenêtre d'édition sont écartées ici, et non à la
        // composition du menu : le fichier reste ainsi le reflet de ce qui sera affiché
        // (SPEC-CFG-003, règle 3).
        configuration.CustomShortcuts =
        [
            .. configuration.CustomShortcuts
                .Where(custom => !string.IsNullOrWhiteSpace(custom.Path))
                .Select(custom => custom.Normalized())
        ];

        return _store.Save(AppPaths.ConfigurationFile, configuration);
    }
}
