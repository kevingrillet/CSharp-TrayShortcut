using System.Text.Json;
using System.Text.Json.Serialization;

namespace CSharpTrayShortcut.Application.Configuration;

/// <summary>
/// Format du fichier de configuration (SPEC-CFG-001).
/// </summary>
/// <remarks>
/// <para>
/// Ces options vivent dans la couche application et non dans l'infrastructure : le format du
/// fichier est un <b>contrat avec l'utilisateur</b> — il est documenté dans le README et
/// s'édite à la main —, pas un détail de la façon dont on écrit sur le disque. C'est aussi ce
/// qui permet de le vérifier par un test, le projet de tests ne référençant pas
/// l'infrastructure.
/// </para>
/// <para>
/// <b>Pourquoi le convertisseur d'énumérations est indispensable.</b> Sans lui,
/// <c>System.Text.Json</c> n'accepte pour une énumération que sa valeur <i>numérique</i>. Un
/// fichier contenant <c>"Language": "French"</c> — la forme documentée, et la seule qu'un
/// humain écrirait — fait échouer la désérialisation de l'objet <b>entier</b> : la
/// configuration est alors mise au rebut au profit des valeurs par défaut, et le dossier
/// surveillé est oublié. Un réglage mal orthographié coûtait donc toute la configuration.
/// </para>
/// </remarks>
public static class ConfigurationSerialization
{
    /// <summary>Options de lecture et d'écriture du fichier de configuration.</summary>
    /// <remarks>
    /// Les noms de propriétés restent en <c>PascalCase</c>, contrairement à la convention
    /// <c>camelCase</c> de Forge Watcher : c'est la forme qu'emploient le README et toutes les
    /// configurations existantes. Changer de casse à l'écriture n'aurait rien apporté et aurait
    /// rendu la documentation fausse.
    /// </remarks>
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,

        // Une propriété nulle n'est pas écrite : le fichier ne se remplit pas de « null » pour
        // les réglages qu'on n'emploie pas, et reste lisible à l'œil.
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,

        // Les énumérations s'écrivent et se relisent par leur nom : « System », « French »,
        // « English ». Insensible à la casse, pour qu'un « french » saisi à la main passe.
        Converters = { new JsonStringEnumConverter(allowIntegerValues: true) },
    };
}
