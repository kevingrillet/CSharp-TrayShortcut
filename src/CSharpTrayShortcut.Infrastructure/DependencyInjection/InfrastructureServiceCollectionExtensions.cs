using CSharpTrayShortcut.Application.Abstractions;
using CSharpTrayShortcut.Infrastructure.FileSystem;
using CSharpTrayShortcut.Infrastructure.Persistence;
using CSharpTrayShortcut.Infrastructure.Processes;
using CSharpTrayShortcut.Infrastructure.Shell;
using Microsoft.Extensions.DependencyInjection;

namespace CSharpTrayShortcut.Infrastructure.DependencyInjection;

/// <summary>
/// Branche les implémentations concrètes sur les ports de la couche application.
/// </summary>
/// <remarks>
/// C'est le seul fichier du dépôt où l'on voie, côte à côte, un port et son adaptateur. Toute
/// autre classe ne connaît que l'interface — c'est ce qui rend le disque, le shell et COM
/// remplaçables par des doubles en test.
/// </remarks>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>Ajoute les adaptateurs de la couche infrastructure.</summary>
    public static IServiceCollection AddTrayShortcutInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<JsonFileStore>();

        services.AddSingleton<IShortcutSource, DirectoryShortcutSource>();
        services.AddSingleton<IShortcutTargetResolver, ShellLinkTargetResolver>();
        services.AddSingleton<IProcessLauncher, ShellProcessLauncher>();
        services.AddSingleton<IConfigurationStore, JsonConfigurationStore>();

        return services;
    }
}
