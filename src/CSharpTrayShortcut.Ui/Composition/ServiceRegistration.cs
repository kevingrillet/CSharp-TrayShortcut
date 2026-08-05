using CSharpTrayShortcut.Application.DependencyInjection;
using CSharpTrayShortcut.Infrastructure.DependencyInjection;
using CSharpTrayShortcut.Infrastructure.Logging;
using CSharpTrayShortcut.Infrastructure.Persistence;
using CSharpTrayShortcut.Ui.Icons;
using CSharpTrayShortcut.Ui.Localization;
using CSharpTrayShortcut.Ui.Tray;
using CSharpTrayShortcut.Ui.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CSharpTrayShortcut.Ui.Composition;

/// <summary>
/// Racine de composition : le seul endroit du dépôt qui connaisse à la fois les ports et leurs
/// implémentations.
/// </summary>
/// <remarks>
/// Toute autre classe reçoit ses dépendances par constructeur. C'est ce qui permet de tester la
/// couche application avec des doubles, et ce qui rendrait un remplacement de WinForms — par
/// WPF, ou par un service Windows sans interface — limité à ce projet.
/// </remarks>
public static class ServiceRegistration
{
    /// <summary>Construit le conteneur de l'application.</summary>
    public static ServiceProvider Build()
    {
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            // Le dossier de données peut être inaccessible (profil itinérant en panne, disque
            // plein) : dans ce cas on renonce au journal plutôt qu'au démarrage.
            if (AppPaths.EnsureDataDirectory())
            {
                builder.AddProvider(new FileLoggerProvider(AppPaths.LogFile));
            }

            builder.SetMinimumLevel(LogLevel.Information);
        });

        services.AddTrayShortcutApplication();
        services.AddTrayShortcutInfrastructure();

        // Présentation.
        services.AddSingleton<TextService>();
        services.AddSingleton<IconRenderer>();
        services.AddSingleton<MenuRenderer>();
        services.AddSingleton<TrayApplicationContext>();

        // La fenêtre d'édition est transitoire : elle relit la configuration à sa construction
        // et se referme après enregistrement. En singleton, une fenêtre fermée ne pourrait pas
        // être réouverte (WinForms interdit d'afficher un Form disposé).
        services.AddTransient<EditForm>();

        return services.BuildServiceProvider();
    }
}
