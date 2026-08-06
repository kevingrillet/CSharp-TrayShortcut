using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;

namespace CSharpTrayShortcut.Infrastructure.Logging;

/// <summary>
/// Journal texte dans le dossier de données de l'utilisateur, avec rotation (SPEC-APP-003).
/// </summary>
/// <remarks>
/// <para>
/// Une application résidente sans console n'a aucun endroit où se plaindre : sans ce journal,
/// « le menu est vide » ou « mon raccourci ne fait rien » ne serait diagnosticable qu'au
/// débogueur. Un fichier texte suffit — la question posée est toujours « qu'est-ce qui a été
/// ignoré, et pourquoi ».
/// </para>
/// <para>
/// Rotation à 1 Mo avec un seul fichier de sauvegarde : assez pour couvrir plusieurs jours
/// d'usage, borné pour ne jamais remplir un disque à l'insu de l'utilisateur.
/// </para>
/// </remarks>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private const long MaxBytes = 1024 * 1024;

    private readonly string _path;
    private readonly LogLevel _minimum;
    private readonly Lock _gate = new();

    /// <summary>Construit le fournisseur.</summary>
    /// <param name="path">Chemin du fichier journal.</param>
    /// <param name="minimum">Niveau à partir duquel une entrée est écrite.</param>
    public FileLoggerProvider(string path, LogLevel minimum = LogLevel.Information)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        _path = path;
        _minimum = minimum;
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => new FileLogger(this, categoryName);

    /// <inheritdoc />
    public void Dispose()
    {
        // Rien à libérer : chaque écriture ouvre et referme le fichier. Sur le volume de
        // journalisation d'une application de menu — quelques lignes par ouverture —, le coût
        // est négligeable, et le fichier reste lisible pendant que l'application tourne.
    }

    private void Write(string categoryName, LogLevel level, string message, Exception? exception)
    {
        var ligne = new StringBuilder()
            .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))
            .Append(" [").Append(Abreger(level)).Append("] ")
            .Append(categoryName)
            .Append(" — ")
            .Append(message);

        if (exception is not null)
        {
            ligne.AppendLine().Append(exception);
        }

        lock (_gate)
        {
            try
            {
                Rotationner();
                File.AppendAllText(_path, ligne.AppendLine().ToString());
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Journaliser est un service, pas une obligation : un disque plein ou un
                // fichier verrouillé ne doit pas empêcher l'application de fonctionner.
            }
        }
    }

    private static string Abreger(LogLevel level) => level switch
    {
        LogLevel.Trace => "TRACE",
        LogLevel.Debug => "DEBUG",
        LogLevel.Information => "INFO ",
        LogLevel.Warning => "WARN ",
        LogLevel.Error => "ERROR",
        LogLevel.Critical => "FATAL",
        _ => "     ",
    };

    /// <summary>
    /// Renomme le journal en <c>.1</c> dès qu'il dépasse la taille maximale.
    /// </summary>
    /// <remarks>
    /// Appelé sous le verrou d'écriture : deux rotations simultanées perdraient une génération.
    /// </remarks>
    private void Rotationner()
    {
        var info = new FileInfo(_path);
        if (!info.Exists || info.Length < MaxBytes)
        {
            return;
        }

        // Une seule génération conservée : le fichier précédent est écrasé.
        File.Move(_path, _path + ".1", overwrite: true);
    }

    /// <summary>Journal d'une catégorie donnée.</summary>
    private sealed class FileLogger : ILogger
    {
        private readonly FileLoggerProvider _provider;
        private readonly string _categoryName;

        internal FileLogger(FileLoggerProvider provider, string categoryName)
        {
            _provider = provider;
            _categoryName = categoryName;
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel)
            => logLevel != LogLevel.None && logLevel >= _provider._minimum;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);

            if (!IsEnabled(logLevel))
            {
                return;
            }

            _provider.Write(_categoryName, logLevel, formatter(state, exception), exception);
        }
    }
}
