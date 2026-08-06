using System.Threading;
using System.Windows.Forms;
using CSharpTrayShortcut.Infrastructure.Persistence;
using CSharpTrayShortcut.Ui.Composition;
using CSharpTrayShortcut.Ui.Tray;
using Microsoft.Extensions.DependencyInjection;

// « Application » désigne à la fois la couche applicative du dépôt et la classe statique de
// WinForms. L'alias lève l'ambiguïté sans renommer une couche pour des raisons d'outillage.
using WinFormsApplication = System.Windows.Forms.Application;

namespace CSharpTrayShortcut.Ui;

/// <summary>
/// Point d'entrée : instance unique, filet de sécurité contre les erreurs non gérées, puis
/// boucle de messages (SPEC-APP-001, SPEC-APP-002).
/// </summary>
internal static class Program
{
    /// <summary>
    /// Nom du mutex garantissant l'instance unique.
    /// </summary>
    /// <remarks>
    /// Sans portée explicite, le mutex est local à la session : deux utilisateurs connectés
    /// simultanément sur la même machine ont chacun droit à leur icône, ce qui est le
    /// comportement attendu.
    /// </remarks>
    private const string SingleInstanceMutexName = "CSharp_TrayShortcut_SingleInstance";

    [STAThread]
    private static void Main()
    {
        // Une seconde instance ajouterait une seconde icône dans la zone de notification, sans
        // moyen de les distinguer (SPEC-APP-001).
        using var mutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out var premiere);
        if (!premiere)
        {
            return;
        }

        try
        {
            ApplicationConfiguration.Initialize();

            // CatchException : les exceptions du fil d'interface passent par ThreadException
            // plutôt que de terminer le processus sans rien écrire.
            WinFormsApplication.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            WinFormsApplication.ThreadException += (_, e) => Fatal(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (_, e) => Fatal(e.ExceptionObject as Exception);

            using var services = ServiceRegistration.Build();
            WinFormsApplication.Run(services.GetRequiredService<TrayApplicationContext>());
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }

    /// <summary>
    /// Écrit un rapport de plantage, prévient l'utilisateur, puis termine (SPEC-APP-002).
    /// </summary>
    /// <remarks>
    /// <para>
    /// L'ancienne version se contentait d'écrire un fichier à côté de l'exécutable et
    /// disparaissait en silence : l'icône s'évanouissait sans explication, et le rapport était
    /// introuvable pour qui ne connaissait pas le dossier d'installation. Le rapport va
    /// maintenant dans le dossier de données, et son chemin est affiché.
    /// </para>
    /// <para>
    /// Le message est en dur et non traduit : à ce stade, le conteneur — donc le catalogue de
    /// textes — peut être précisément ce qui a échoué.
    /// </para>
    /// </remarks>
    private static void Fatal(Exception? exception)
    {
        var rapport = WriteCrashReport(exception);

        try
        {
            MessageBox.Show(
                rapport is null
                    ? "Une erreur inattendue a interrompu Tray Shortcut."
                    : $"Une erreur inattendue a interrompu Tray Shortcut.\n\nDétail écrit dans :\n{rapport}",
                "Tray Shortcut",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (Exception)
        {
            // Si même afficher une boîte de message échoue, il ne reste rien à tenter.
        }

        // Environment.Exit et non Application.Exit : la boucle de messages peut être dans un
        // état qui l'empêche de traiter une demande de fermeture propre.
        Environment.Exit(1);
    }

    /// <summary>
    /// Écrit le détail de l'erreur dans le dossier de données, et rend son chemin.
    /// </summary>
    /// <remarks>
    /// Toute défaillance est avalée : un échec d'écriture ne doit jamais masquer le plantage
    /// d'origine, qui est la seule information utile.
    /// </remarks>
    private static string? WriteCrashReport(Exception? exception)
    {
        try
        {
            if (!AppPaths.EnsureDataDirectory())
            {
                return null;
            }

            var chemin = AppPaths.CrashReportFile(DateTime.UtcNow, Guid.NewGuid());
            File.WriteAllText(
                chemin,
                exception?.ToString() ?? "Erreur inconnue : aucune exception n'a été fournie.");

            return chemin;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
