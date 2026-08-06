using System.ComponentModel;
using System.Diagnostics;
using CSharpTrayShortcut.Application.Abstractions;
using CSharpTrayShortcut.Domain.Shortcuts;
using Microsoft.Extensions.Logging;

namespace CSharpTrayShortcut.Infrastructure.Processes;

/// <summary>
/// Lance une cible par le shell de Windows (SPEC-LAUNCH-001).
/// </summary>
/// <remarks>
/// <para>
/// <c>UseShellExecute = true</c> est indispensable : c'est ce qui fait respecter les
/// associations de fichiers — un <c>.docx</c> ouvre Word, un dossier ouvre l'explorateur, une
/// adresse ouvre le navigateur. Sans ce réglage, seuls les exécutables se lanceraient.
/// </para>
/// <para>
/// Aucune exception ne sort de cette classe : un refus du shell devient un <c>false</c>,
/// conformément au contrat de <see cref="IProcessLauncher"/>. C'est ce qui empêche un clic sur
/// une entrée périmée de terminer l'application.
/// </para>
/// </remarks>
public sealed class ShellProcessLauncher : IProcessLauncher
{
    private readonly ILogger<ShellProcessLauncher> _logger;

    /// <summary>Construit le lanceur.</summary>
    /// <param name="logger">Journal, pour tracer les lancements refusés.</param>
    public ShellProcessLauncher(ILogger<ShellProcessLauncher> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc />
    public bool Launch(LaunchTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);

        var demarrage = new ProcessStartInfo(target.Path)
        {
            UseShellExecute = true,
        };

        if (target.Argument is not null)
        {
            demarrage.Arguments = target.Argument;
        }

        try
        {
            // On libère aussitôt le handle : l'application lancée vit sa vie, et conserver son
            // objet Process retiendrait une ressource système sans qu'on l'observe jamais.
            Process.Start(demarrage)?.Dispose();
            return true;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or PlatformNotSupportedException)
        {
            // Win32Exception couvre le cas courant : aucune application associée à l'extension,
            // exécutable bloqué par une stratégie de groupe, ou refus de l'antivirus.
            _logger.LogWarning(ex, "Lancement refusé par le système : {Chemin}", target.Path);
            return false;
        }
    }
}
