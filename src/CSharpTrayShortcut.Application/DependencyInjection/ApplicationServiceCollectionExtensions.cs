using CSharpTrayShortcut.Application.Launching;
using CSharpTrayShortcut.Application.Menu;
using Microsoft.Extensions.DependencyInjection;

namespace CSharpTrayShortcut.Application.DependencyInjection;

/// <summary>
/// Enregistre les cas d'usage de la couche application.
/// </summary>
/// <remarks>
/// Chaque couche déclare ses propres services : la racine de composition n'a plus qu'à
/// enchaîner les extensions, sans connaître le détail de ce qu'elles contiennent. Ajouter une
/// règle ne demande donc pas de toucher au projet d'interface.
/// </remarks>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>Ajoute les services de la couche application.</summary>
    /// <remarks>
    /// Tous en <b>singleton</b> : ces services sont sans état — ils reçoivent la configuration
    /// en paramètre plutôt que de la retenir —, ce qui les rend partageables et évite de
    /// reconstruire un graphe d'objets à chaque ouverture de menu.
    /// </remarks>
    public static IServiceCollection AddTrayShortcutApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IconSourceResolver>();
        services.AddSingleton<IconCachePolicy>();
        services.AddSingleton<MenuComposer>();
        services.AddSingleton<LaunchService>();

        return services;
    }
}
