using System.Drawing;
using CSharpTrayShortcut.Application.Menu;
using CSharpTrayShortcut.Domain.Shortcuts;
using CSharpTrayShortcut.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace CSharpTrayShortcut.Ui.Icons;

/// <summary>
/// Fabrique — et réutilise — les images décrites par un <see cref="IconSource"/>
/// (SPEC-ICON-001 à SPEC-ICON-004).
/// </summary>
/// <remarks>
/// <para>
/// Seule classe du dépôt à connaître <c>System.Drawing</c>. Elle ne décide rien : la source et
/// sa chaîne de replis lui sont données toutes faites par
/// <see cref="IconSourceResolver"/>, et la clé sous laquelle une image se réutilise par
/// <see cref="IconCachePolicy"/>. Son travail se réduit à « chercher dans le cache, sinon
/// fabriquer, sinon passer au repli ».
/// </para>
/// <para>
/// <b>Propriété des images.</b> C'est le rendu qui possède les images, jamais les éléments de
/// menu — un <c>ToolStripItem</c> ne libère pas la sienne. Le cache les retient pour la durée de
/// vie de l'application, et les libère à l'éviction ou à la fermeture
/// ([ADR-0006](../../../docs/adr/0006-cache-des-icones.md)).
/// </para>
/// <para>
/// <b>Éviction aux frontières de rendu seulement.</b> Une image évincée alors qu'un menu vivant
/// la référence encore serait peinte après libération. L'éviction n'a donc lieu que dans
/// <see cref="BeginRender"/>, au moment où le menu précédent est abandonné, et jamais pendant la
/// construction d'un sous-menu.
/// </para>
/// </remarks>
public sealed class IconRenderer : IDisposable
{
    /// <summary>
    /// Nombre d'images conservées avant de commencer à évincer.
    /// </summary>
    /// <remarks>
    /// Chaque image consomme un handle graphique, ressource limitée par processus. La borne
    /// existe pour qu'une arborescence de plusieurs milliers d'éléments, parcourue au fil du
    /// temps, ne finisse pas par les épuiser. Elle est large : un dossier de raccourcis normal
    /// n'y arrive jamais, et le cas où elle mord est celui où le cache sert le moins.
    /// </remarks>
    private const int MaxEntries = 512;

    private readonly IconCachePolicy _policy;
    private readonly ILogger<IconRenderer> _logger;
    private readonly Dictionary<IconCacheKey, Entry> _cache = [];

    /// <summary>
    /// Numéro du rendu courant, employé pour repérer les images qui ne servent plus.
    /// </summary>
    private long _generation;

    /// <summary>Construit le fabricant d'images.</summary>
    /// <param name="policy">Règle de réutilisation des images.</param>
    /// <param name="logger">Journal, pour tracer les icônes illisibles.</param>
    public IconRenderer(IconCachePolicy policy, ILogger<IconRenderer> logger)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(logger);

        _policy = policy;
        _logger = logger;
    }

    /// <summary>Nombre d'images actuellement en cache. Exposé pour le diagnostic.</summary>
    public int CachedImageCount => _cache.Count;

    /// <summary>
    /// Ouvre un nouveau rendu et libère les images devenues inutiles (SPEC-ICON-004, règle 4).
    /// </summary>
    /// <remarks>
    /// À appeler avant de reconstruire le menu, et à ce moment-là seulement : c'est le seul
    /// instant où aucune image ne peut être référencée par un menu vivant.
    /// </remarks>
    public void BeginRender()
    {
        _generation++;

        if (_cache.Count <= MaxEntries)
        {
            return;
        }

        // On ne libère que ce qui n'a pas servi lors des deux derniers rendus : les images du
        // rendu qui vient de s'achever portent « _generation - 1 » et pourraient resservir
        // immédiatement si la configuration n'a pas changé.
        var perimees = _cache
            .Where(entree => entree.Value.Generation < _generation - 1)
            .Select(entree => entree.Key)
            .ToList();

        foreach (var cle in perimees)
        {
            _cache[cle].Image?.Dispose();
            _cache.Remove(cle);
        }

        _logger.LogDebug(
            "Cache d'icônes : {Evincees} image(s) libérée(s), {Restantes} conservée(s).",
            perimees.Count,
            _cache.Count);
    }

    /// <summary>
    /// Première image obtenue en descendant la chaîne de replis, ou <see langword="null"/> si
    /// aucune source ne donne rien.
    /// </summary>
    /// <remarks>
    /// L'image appartient au cache : l'appelant ne doit <b>pas</b> la libérer. Elle reste
    /// valable jusqu'au prochain <see cref="BeginRender"/>.
    /// </remarks>
    public Bitmap? RenderBitmap(IconSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        foreach (var candidat in source.Chain())
        {
            var image = Obtenir(candidat);
            if (image is not null)
            {
                return image;
            }
        }

        return null;
    }

    /// <summary>
    /// Première icône obtenue en descendant la chaîne de replis, pour la zone de notification.
    /// </summary>
    /// <remarks>
    /// Hors cache, et l'appelant en devient propriétaire : un <c>NotifyIcon</c> exige un
    /// <see cref="Icon"/> et non un <see cref="Bitmap"/>, et cette icône n'est fabriquée qu'une
    /// fois par rechargement de configuration — la mettre en cache n'apporterait rien.
    /// </remarks>
    public Icon? RenderIcon(IconSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        foreach (var candidat in source.Chain())
        {
            var icone = Load(candidat);
            if (icone is not null)
            {
                return icone;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var entree in _cache.Values)
        {
            entree.Image?.Dispose();
        }

        _cache.Clear();
    }

    /// <summary>
    /// Image d'une source élémentaire, depuis le cache ou fraîchement fabriquée.
    /// </summary>
    /// <remarks>
    /// Les échecs sont mis en cache eux aussi, sous forme d'entrée à image nulle : sans cela, un
    /// fichier d'icône absent serait retenté à chaque entrée de menu qui le désigne.
    /// </remarks>
    private Bitmap? Obtenir(IconSource source)
    {
        var cle = _policy.KeyFor(source);
        if (cle is null)
        {
            return null;
        }

        if (_cache.TryGetValue(cle, out var connue))
        {
            // Marque l'entrée comme servie lors de ce rendu, pour qu'elle échappe à l'éviction.
            _cache[cle] = connue with { Generation = _generation };
            return connue.Image;
        }

        using var icone = Load(source);
        // ToBitmap produit une copie indépendante : l'icône source peut être libérée aussitôt.
        var image = icone?.ToBitmap();

        _cache[cle] = new Entry(image, _generation);
        return image;
    }

    private Icon? Load(IconSource source) => source.Kind switch
    {
        IconSourceKind.IconFile => LoadIconFile(source.Path),
        IconSourceKind.ExtractFromFile => ExtractFrom(source.Path),
        _ => null,
    };

    /// <summary>
    /// Charge un fichier <c>.ico</c>, en acceptant qu'il soit désigné par son seul nom quand il
    /// est livré avec l'application (SPEC-ICON-002, règle 2).
    /// </summary>
    private Icon? LoadIconFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        foreach (var candidat in Emplacements(path))
        {
            try
            {
                if (File.Exists(candidat))
                {
                    return new Icon(candidat);
                }
            }
            catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
            {
                // Fichier qui n'est pas une icône valide, tronqué, ou verrouillé : on tente
                // l'emplacement suivant, puis le repli de la chaîne.
                _logger.LogDebug(ex, "Icône illisible : {Chemin}", candidat);
            }
        }

        return null;
    }

    /// <summary>
    /// Extrait l'icône associée à un fichier quelconque (exécutable, document, raccourci déjà
    /// résolu par la couche application).
    /// </summary>
    private Icon? ExtractFrom(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        try
        {
            return Icon.ExtractAssociatedIcon(path);
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
        {
            // Icône abîmée, chemin trop long, défaillance du shell : une entrée sans image
            // reste utilisable.
            _logger.LogDebug(ex, "Extraction d'icône impossible : {Chemin}", path);
            return null;
        }
    }

    /// <summary>
    /// Emplacements où chercher un fichier d'icône, dans l'ordre : tel quel, puis parmi celles
    /// livrées avec l'application.
    /// </summary>
    private static IEnumerable<string> Emplacements(string path)
    {
        yield return path;

        // Un nom seul (« folder_w11.ico ») désigne une icône fournie. Un chemin absolu ou
        // relatif comportant un séparateur n'a rien à faire dans ce dossier.
        if (!Path.IsPathRooted(path) && Path.GetFileName(path) == path)
        {
            yield return Path.Combine(AppPaths.BundledIconsDirectory, path);
        }
    }

    /// <summary>Image en cache, et numéro du dernier rendu où elle a servi.</summary>
    private readonly record struct Entry(Bitmap? Image, long Generation);
}
