using System.Globalization;
using CSharpTrayShortcut.Application.Configuration;
using CSharpTrayShortcut.Application.Text;
using CSharpTrayShortcut.Domain.Text;

namespace CSharpTrayShortcut.Ui.Localization;

/// <summary>
/// Langue courante de l'interface, et traduction des messages (SPEC-UI-LANG-001).
/// </summary>
/// <remarks>
/// <para>
/// Point d'entrée unique de la traduction côté interface : les fenêtres et le menu appellent
/// <see cref="Get(string)"/> ou <see cref="Resolve"/>, jamais le catalogue directement. Cela
/// permet de changer de langue en un seul endroit quand la configuration est rechargée.
/// </para>
/// <para>
/// Seule <see cref="CultureInfo.CurrentUICulture"/> est alignée sur la langue choisie ;
/// <see cref="CultureInfo.CurrentCulture"/> reste celle du poste. Quelqu'un qui lit
/// l'interface en anglais depuis un poste français attend toujours ses dates en jour/mois.
/// </para>
/// </remarks>
public sealed class TextService
{
    private TextCatalogue _catalogue = TextCatalogue.For(EffectiveLanguage.French);

    /// <summary>Langue effectivement employée.</summary>
    public EffectiveLanguage Language => _catalogue.Language;

    /// <summary>
    /// Aligne la langue courante sur le réglage de la configuration (SPEC-CFG-004).
    /// </summary>
    /// <param name="configuration">Configuration fraîchement relue.</param>
    public void Apply(TrayShortcutConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var langue = LanguageResolver.Resolve(configuration.Language, CultureInfo.InstalledUICulture);
        _catalogue = TextCatalogue.For(langue);

        CultureInfo.CurrentUICulture = langue == EffectiveLanguage.English
            ? CultureInfo.GetCultureInfo("en")
            : CultureInfo.GetCultureInfo("fr");
    }

    /// <summary>Formulation d'une clé dans la langue courante.</summary>
    public string Get(string key) => _catalogue.Get(key);

    /// <summary>Formulation d'un message et de ses arguments dans la langue courante.</summary>
    public string Resolve(TextRef? text) => _catalogue.Resolve(text);
}
