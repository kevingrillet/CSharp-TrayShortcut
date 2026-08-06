using CSharpTrayShortcut.Application.Abstractions;
using CSharpTrayShortcut.Domain.Shortcuts;
using Microsoft.Extensions.Logging;

namespace CSharpTrayShortcut.Application.Launching;

/// <summary>Ce qu'une cible de lancement se révèle être (SPEC-LAUNCH-003).</summary>
public enum LaunchAvailability
{
    /// <summary>Rien d'exploitable : ni fichier, ni dossier, ni adresse.</summary>
    Missing = 0,

    /// <summary>Un fichier existant.</summary>
    File = 1,

    /// <summary>Un dossier existant : le shell l'ouvre dans l'explorateur.</summary>
    Directory = 2,

    /// <summary>Une adresse web ou de courriel : le shell l'ouvre dans l'application associée.</summary>
    Uri = 3,
}

/// <summary>
/// Lance ce que l'utilisateur a cliqué, après avoir vérifié que la cible mène quelque part
/// (SPEC-LAUNCH-001 à SPEC-LAUNCH-003).
/// </summary>
/// <remarks>
/// <para>
/// Ce service existe parce que l'ancien code lançait depuis le gestionnaire de clic lui-même,
/// et y levait des exceptions : un raccourci mal renseigné remontait alors jusqu'au
/// gestionnaire d'exceptions non gérées, qui écrit un rapport de plantage et <b>ferme
/// l'application</b>. Un clic sur une entrée périmée ne doit pas faire disparaître l'icône de
/// la zone de notification.
/// </para>
/// <para>
/// Un menu se construit à un instant et se clique à un autre : entre les deux, le fichier peut
/// avoir été déplacé, ou le partage réseau déconnecté. C'est pourquoi la cible est réexaminée
/// au clic et non seulement à la composition.
/// </para>
/// </remarks>
public sealed class LaunchService
{
    /// <summary>
    /// Schémas d'adresse acceptés (SPEC-LAUNCH-003).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Liste blanche, et non « tout ce qui ressemble à une URI » : passer n'importe quel schéma
    /// au shell revient à lui laisser exécuter des gestionnaires de protocole arbitraires
    /// depuis un fichier de configuration. On s'en tient à ce dont un menu de raccourcis a
    /// besoin.
    /// </para>
    /// <para>
    /// <b><c>file:</c> en est délibérément absent.</b> Un chemin Windows comme
    /// <c>D:\parti.exe</c> — comme un chemin UNC — est une URI <c>file:</c> parfaitement valide
    /// aux yeux de <see cref="Uri.TryCreate(string?, UriKind, out Uri?)"/>. L'y admettre
    /// classerait tout chemin disparu en adresse valide, et anéantirait
    /// SPEC-LAUNCH-002 : les chemins locaux doivent être jugés sur leur existence, pas sur leur
    /// syntaxe. Le schéma n'apporte d'ailleurs rien qu'un chemin nu ne fasse déjà.
    /// </para>
    /// </remarks>
    private static readonly string[] AllowedUriSchemes =
    [
        Uri.UriSchemeHttp,
        Uri.UriSchemeHttps,
        Uri.UriSchemeMailto,
    ];

    private readonly IShortcutSource _source;
    private readonly IProcessLauncher _launcher;
    private readonly ILogger<LaunchService> _logger;

    /// <summary>Construit le service.</summary>
    /// <param name="source">Accès au système de fichiers, pour vérifier la cible.</param>
    /// <param name="launcher">Lanceur de processus.</param>
    /// <param name="logger">Journal, pour tracer ce qui n'a pas pu être lancé.</param>
    public LaunchService(
        IShortcutSource source,
        IProcessLauncher launcher,
        ILogger<LaunchService> logger)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(launcher);
        ArgumentNullException.ThrowIfNull(logger);

        _source = source;
        _launcher = launcher;
        _logger = logger;
    }

    /// <summary>
    /// Ce que la cible désigne réellement, au moment où on regarde (SPEC-LAUNCH-003).
    /// </summary>
    /// <remarks>
    /// L'ordre des tests compte : le disque d'abord, l'adresse ensuite. Un chemin Windows
    /// comme <c>C:\Outils</c> est une URI <c>file:</c> valide aux yeux de
    /// <see cref="Uri.TryCreate(string?, UriKind, out Uri?)"/> ; commencer par l'adresse
    /// classerait donc tous les chemins locaux en <see cref="LaunchAvailability.Uri"/>.
    /// </remarks>
    public LaunchAvailability Inspect(LaunchTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (_source.FileExists(target.Path))
        {
            return LaunchAvailability.File;
        }

        if (_source.DirectoryExists(target.Path))
        {
            return LaunchAvailability.Directory;
        }

        return IsAllowedUri(target.Path) ? LaunchAvailability.Uri : LaunchAvailability.Missing;
    }

    /// <summary>
    /// Lance la cible si elle mène quelque part.
    /// </summary>
    /// <param name="target">Cible cliquée.</param>
    /// <returns>
    /// Vrai si le lancement a été demandé et accepté ; faux si la cible a disparu
    /// (SPEC-LAUNCH-002) ou si le shell a refusé.
    /// </returns>
    public bool Launch(LaunchTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);

        var availability = Inspect(target);
        if (availability == LaunchAvailability.Missing)
        {
            // Niveau information et non avertissement : une entrée devenue obsolète dans un
            // dossier surveillé est un événement ordinaire, pas un dysfonctionnement.
            _logger.LogInformation(
                "Cible introuvable, lancement abandonné : {Chemin}",
                target.Path);
            return false;
        }

        if (_launcher.Launch(target))
        {
            return true;
        }

        _logger.LogWarning(
            "Le système a refusé de lancer {Chemin} (nature : {Nature}).",
            target.Path,
            availability);
        return false;
    }

    private static bool IsAllowedUri(string path)
        => Uri.TryCreate(path, UriKind.Absolute, out var uri)
            && AllowedUriSchemes.Contains(uri.Scheme, StringComparer.OrdinalIgnoreCase);
}
