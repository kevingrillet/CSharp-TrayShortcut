using System.Globalization;
using CSharpTrayShortcut.Application.Configuration;
using CSharpTrayShortcut.Application.Text;

namespace CSharpTrayShortcut.Tests.Text;

/// <summary>Choix de la langue de l'interface.</summary>
[TestFixture]
[Category("SPEC-UI-LANG-001")]
public sealed class LanguageResolverTests
{
    [TestCase("en")]
    [TestCase("en-US")]
    [TestCase("en-GB")]
    public void Une_culture_anglaise_donne_langlais(string culture)
    {
        var langue = LanguageResolver.Resolve(
            LanguagePreference.System,
            CultureInfo.GetCultureInfo(culture));

        Assert.That(
            langue,
            Is.EqualTo(EffectiveLanguage.English),
            "La comparaison porte sur le code à deux lettres, pour couvrir toutes les variantes "
            + "sans les énumérer.");
    }

    [TestCase("fr")]
    [TestCase("fr-CA")]
    [TestCase("de-DE")]
    [TestCase("ja-JP")]
    public void Toute_autre_culture_donne_le_francais(string culture)
    {
        var langue = LanguageResolver.Resolve(
            LanguagePreference.System,
            CultureInfo.GetCultureInfo(culture));

        Assert.That(
            langue,
            Is.EqualTo(EffectiveLanguage.French),
            "Le français est la langue neutre du dépôt : c'est le seul repli qui trouve toujours "
            + "une formulation.");
    }

    [Test]
    public void La_culture_invariante_donne_le_francais()
    {
        var langue = LanguageResolver.Resolve(LanguagePreference.System, CultureInfo.InvariantCulture);

        Assert.That(langue, Is.EqualTo(EffectiveLanguage.French));
    }

    [Test]
    public void Une_culture_absente_donne_le_francais()
    {
        Assert.That(
            LanguageResolver.Resolve(LanguagePreference.System, null),
            Is.EqualTo(EffectiveLanguage.French));
    }

    [Test]
    public void Un_choix_explicite_ignore_la_culture_du_poste()
    {
        var anglaise = CultureInfo.GetCultureInfo("en-US");
        var francaise = CultureInfo.GetCultureInfo("fr-FR");

        Assert.Multiple(() =>
        {
            Assert.That(
                LanguageResolver.Resolve(LanguagePreference.French, anglaise),
                Is.EqualTo(EffectiveLanguage.French));
            Assert.That(
                LanguageResolver.Resolve(LanguagePreference.English, francaise),
                Is.EqualTo(EffectiveLanguage.English));
        });
    }
}
