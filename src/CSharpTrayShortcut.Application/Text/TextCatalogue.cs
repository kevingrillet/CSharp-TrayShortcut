using System.Collections;
using System.Globalization;
using System.Resources;
using CSharpTrayShortcut.Domain.Text;

namespace CSharpTrayShortcut.Application.Text;

/// <summary>
/// Formule les messages d'une langue : c'est le seul endroit du dépôt qui contienne des
/// phrases destinées à l'utilisateur (SPEC-UI-LANG-002).
/// </summary>
/// <remarks>
/// <para>
/// Les formulations vivent dans <c>Text/Strings.resx</c> (français, langue neutre du dépôt)
/// et <c>Text/Strings.en.resx</c> (anglais, assembly satellite). Le format est celui que tout
/// outil de traduction sait lire, et le repli de culture — <c>fr-CA</c> vers <c>fr</c> vers la
/// langue neutre — est assuré par <see cref="ResourceManager"/> sans code de notre part
/// (ADR-0004).
/// </para>
/// <para>
/// Les clés sont celles de <see cref="TextKeys"/>, jamais des chaînes libres. Comme certaines
/// se déduisent d'une énumération, l'accès reste par clé plutôt que par classe fortement
/// typée : un test de garde vérifie en retour que chaque clé employée existe dans les deux
/// langues.
/// </para>
/// <para>
/// Le catalogue est dans <b>Application</b> et non dans <c>Ui</c> : les tests ne référencent
/// que <c>Domain</c> et <c>Application</c>, et c'est ce qui permet de le mettre sous
/// garde-fou.
/// </para>
/// </remarks>
public sealed class TextCatalogue
{
    private static readonly ResourceManager Manager =
        new("CSharpTrayShortcut.Application.Text.Strings", typeof(TextCatalogue).Assembly);

    private static readonly TextCatalogue FrenchCatalogue = new(EffectiveLanguage.French);

    private static readonly TextCatalogue EnglishCatalogue = new(EffectiveLanguage.English);

    /// <summary>Culture employée pour interroger les ressources.</summary>
    /// <remarks>
    /// Le français passe par la culture invariante : les ressources françaises sont celles de
    /// l'assembly principal (<c>NeutralLanguage</c>), et viser la culture invariante y accède
    /// directement, sans faire chercher au runtime un assembly satellite qui n'existe pas.
    /// </remarks>
    private readonly CultureInfo _lookup;

    private TextCatalogue(EffectiveLanguage language)
    {
        Language = language;
        _lookup = language == EffectiveLanguage.English
            ? CultureInfo.GetCultureInfo("en")
            : CultureInfo.InvariantCulture;
    }

    /// <summary>Langue de ce catalogue.</summary>
    public EffectiveLanguage Language { get; }

    /// <summary>Catalogue de la langue indiquée.</summary>
    public static TextCatalogue For(EffectiveLanguage language)
        => language == EffectiveLanguage.English ? EnglishCatalogue : FrenchCatalogue;

    /// <summary>Clés portées par cette langue <b>seule</b>, sans repli sur la langue neutre.</summary>
    /// <remarks>
    /// C'est ce qui permet au test de parité de repérer une clé traduite d'un seul côté :
    /// avec le repli, l'anglais paraîtrait toujours complet.
    /// </remarks>
    public IReadOnlyCollection<string> Keys
    {
        get
        {
            var set = Manager.GetResourceSet(_lookup, createIfNotExists: true, tryParents: false);
            if (set is null)
            {
                return Array.Empty<string>();
            }

            var keys = new List<string>();
            foreach (DictionaryEntry entry in set)
            {
                if (entry.Key is string key)
                {
                    keys.Add(key);
                }
            }

            return keys;
        }
    }

    /// <summary>Vrai si la clé est formulée dans cette langue.</summary>
    public bool Knows(string key) => Manager.GetString(key, _lookup) is not null;

    /// <summary>Formulation brute d'une clé.</summary>
    /// <remarks>
    /// Une clé inconnue est retournée telle quelle : un intitulé technique dans un menu reste
    /// préférable à une exception qui ferait disparaître l'icône de la zone de notification.
    /// Le test de couverture des clés est là pour que le cas ne se produise pas en production.
    /// </remarks>
    public string Get(string key) => Manager.GetString(key, _lookup) ?? key;

    /// <summary>Formule un message et ses arguments, fragments imbriqués compris.</summary>
    public string Resolve(TextRef? text)
    {
        if (text is null)
        {
            return string.Empty;
        }

        var format = Get(text.Key);

        if (text.Arguments.Count == 0)
        {
            return format;
        }

        var arguments = new object?[text.Arguments.Count];
        for (var index = 0; index < arguments.Length; index++)
        {
            // Un argument peut être lui-même un message : c'est ainsi qu'un fragment
            // facultatif se compose sans imposer sa place dans la phrase.
            arguments[index] = text.Arguments[index] is TextRef nested
                ? Resolve(nested)
                : text.Arguments[index];
        }

        return string.Format(CultureInfo.CurrentCulture, format, arguments);
    }
}
