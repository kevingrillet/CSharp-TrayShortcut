using System.Globalization;
using CSharpTrayShortcut.Application.Configuration;

namespace CSharpTrayShortcut.Application.Text;

/// <summary>Langue effectivement employée par l'interface (SPEC-UI-LANG-001).</summary>
public enum EffectiveLanguage
{
    /// <summary>Français — langue neutre du dépôt.</summary>
    French = 0,

    /// <summary>Anglais.</summary>
    English = 1,
}

/// <summary>
/// Traduit un réglage de langue en langue effective (SPEC-UI-LANG-001).
/// </summary>
/// <remarks>
/// <para>
/// Fonction pure, et c'est le point : « suivre Windows » est une règle avec des cas limites —
/// <c>en-GB</c>, <c>fr-CA</c>, une culture inconnue, une culture invariante — et chacun se
/// vérifie en une ligne de test dès lors que la culture est un paramètre plutôt qu'une lecture
/// de <see cref="CultureInfo.CurrentUICulture"/>.
/// </para>
/// <para>
/// Le repli est le <b>français</b> et non l'anglais : c'est la langue neutre déclarée par
/// <c>Directory.Build.props</c>, donc celle qui est compilée dans l'assembly principal. Une
/// culture inconnue trouve ainsi toujours une formulation.
/// </para>
/// </remarks>
public static class LanguageResolver
{
    /// <summary>
    /// Langue effective pour un réglage donné et une culture système donnée.
    /// </summary>
    /// <param name="preference">Réglage choisi par l'utilisateur.</param>
    /// <param name="systemCulture">
    /// Culture d'interface de Windows, consultée uniquement pour
    /// <see cref="LanguagePreference.System"/>.
    /// </param>
    public static EffectiveLanguage Resolve(LanguagePreference preference, CultureInfo? systemCulture)
        => preference switch
        {
            LanguagePreference.French => EffectiveLanguage.French,
            LanguagePreference.English => EffectiveLanguage.English,
            _ => FromCulture(systemCulture),
        };

    /// <summary>
    /// Langue déduite d'une culture : anglais si la culture appartient à la famille anglaise,
    /// français sinon.
    /// </summary>
    /// <remarks>
    /// On compare le <b>nom à deux lettres</b> et non le nom complet, pour que <c>en-GB</c>,
    /// <c>en-US</c> et <c>en</c> donnent le même résultat sans les énumérer.
    /// </remarks>
    private static EffectiveLanguage FromCulture(CultureInfo? culture)
    {
        if (culture is null)
        {
            return EffectiveLanguage.French;
        }

        // La culture invariante n'a pas de nom ISO exploitable : son TwoLetterISOLanguageName
        // vaut « iv ». Elle tombe donc naturellement dans le repli français.
        return string.Equals(culture.TwoLetterISOLanguageName, "en", StringComparison.OrdinalIgnoreCase)
            ? EffectiveLanguage.English
            : EffectiveLanguage.French;
    }
}
