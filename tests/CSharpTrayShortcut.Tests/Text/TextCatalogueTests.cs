using System.Reflection;
using CSharpTrayShortcut.Application.Text;
using CSharpTrayShortcut.Domain.Menu;
using CSharpTrayShortcut.Domain.Text;
using CSharpTrayShortcut.Tests.Doubles;

namespace CSharpTrayShortcut.Tests.Text;

/// <summary>
/// Garde-fous du catalogue de textes : parité des langues et existence des clés
/// (SPEC-UI-LANG-002).
/// </summary>
/// <remarks>
/// Ces tests remplacent la classe fortement typée qu'on aurait pu générer depuis les
/// <c>.resx</c>. On y renonce parce qu'une partie des clés se déduit d'une énumération
/// (<see cref="TextKeys.MenuCommandLabel"/>) ; le filet est ici, et il attrape aussi bien la
/// clé oubliée que la traduction manquante.
/// </remarks>
[TestFixture]
[Category("SPEC-UI-LANG-002")]
public sealed class TextCatalogueTests
{
    private static TextCatalogue Francais => TextCatalogue.For(EffectiveLanguage.French);

    private static TextCatalogue Anglais => TextCatalogue.For(EffectiveLanguage.English);

    [Test]
    public void Les_deux_langues_portent_exactement_les_memes_cles()
    {
        var francais = Francais.Keys.ToHashSet(StringComparer.Ordinal);
        var anglais = Anglais.Keys.ToHashSet(StringComparer.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(
                francais.Except(anglais, StringComparer.Ordinal).Order(StringComparer.Ordinal),
                Is.Empty,
                "Clés présentes en français seulement : l'interface anglaise afficherait le "
                + "texte français à la place.");
            Assert.That(
                anglais.Except(francais, StringComparer.Ordinal).Order(StringComparer.Ordinal),
                Is.Empty,
                "Clés présentes en anglais seulement : signe d'une clé renommée d'un seul côté.");
        });
    }

    [Test]
    public void Le_catalogue_nest_pas_vide()
    {
        Assert.That(
            Francais.Keys,
            Is.Not.Empty,
            "Un catalogue vide ferait passer le test de parité sans rien vérifier.");
    }

    [Test]
    public void Chaque_cle_declaree_est_formulee_dans_les_deux_langues()
    {
        var manquantes = ClesDeclarees()
            .Where(cle => !Francais.Knows(cle) || !Anglais.Knows(cle))
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.That(
            manquantes,
            Is.Empty,
            $"""
             {manquantes.Count} clé(s) de TextKeys n'ont pas de formulation : {string.Join(", ", manquantes)}.
             Le catalogue rend la clé telle quelle quand elle est inconnue : l'utilisateur verrait
             donc « Menu.Refresh » dans son menu au lieu de « Actualiser ».
             """);
    }

    [Test]
    public void Chaque_commande_du_menu_a_son_intitule()
    {
        var sansIntitule = Enum.GetValues<MenuCommand>()
            .Where(commande => !Francais.Knows(TextKeys.MenuCommandLabel(commande)))
            .ToList();

        Assert.That(
            sansIntitule,
            Is.Empty,
            "Ajouter une valeur à MenuCommand oblige à ajouter sa formulation : sans ce test, "
            + "l'oubli n'apparaîtrait qu'à l'ouverture du menu.");
    }

    [Test]
    public void Une_cle_inconnue_est_rendue_telle_quelle_sans_exception()
    {
        Assert.That(
            Francais.Get("Cle.Qui.Nexiste.Pas"),
            Is.EqualTo("Cle.Qui.Nexiste.Pas"),
            "Un intitulé technique dans un menu reste préférable à une exception qui ferait "
            + "disparaître l'icône de la zone de notification.");
    }

    [Test]
    public void Les_arguments_dun_message_sont_mis_en_forme()
    {
        var message = Francais.Resolve(TextRef.Of(TextKeys.Config.PathNotFound, @"Z:\envole"));

        Assert.That(message, Does.Contain(@"Z:\envole"));
    }

    [Test]
    public void Un_fragment_imbrique_est_lui_aussi_formule()
    {
        var imbrique = TextRef.Of(TextKeys.AppName);
        var message = Francais.Resolve(TextRef.Of(TextKeys.Menu.Tooltip, imbrique, Build.Racine));

        Assert.Multiple(() =>
        {
            Assert.That(message, Does.Contain("Tray Shortcut"));
            Assert.That(
                message,
                Does.Not.Contain(TextKeys.AppName),
                "Un TextRef passé en argument doit être résolu, pas affiché comme clé.");
        });
    }

    [Test]
    public void Un_message_absent_donne_une_chaine_vide()
    {
        Assert.That(Francais.Resolve(null), Is.Empty);
    }

    [Test]
    public void Les_deux_langues_sont_bien_distinctes()
    {
        Assert.That(
            Anglais.Get(TextKeys.MenuCommandLabel(MenuCommand.Refresh)),
            Is.Not.EqualTo(Francais.Get(TextKeys.MenuCommandLabel(MenuCommand.Refresh))),
            "Si les deux catalogues rendaient la même chose, c'est que l'assembly satellite "
            + "anglais n'est pas chargé — et le test de parité passerait quand même.");
    }

    /// <summary>
    /// Toutes les clés constantes déclarées par <see cref="TextKeys"/>, classes imbriquées
    /// comprises.
    /// </summary>
    /// <remarks>
    /// Par réflexion plutôt qu'à la main : une clé ajoutée est ainsi couverte sans que personne
    /// ait à penser à compléter ce test.
    /// </remarks>
    private static IEnumerable<string> ClesDeclarees()
    {
        foreach (var cle in ConstantesDe(typeof(TextKeys)))
        {
            yield return cle;
        }

        foreach (var imbriquee in typeof(TextKeys).GetNestedTypes(BindingFlags.Public))
        {
            foreach (var cle in ConstantesDe(imbriquee))
            {
                yield return cle;
            }
        }
    }

    private static IEnumerable<string> ConstantesDe(Type type)
        => type.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(champ => champ.IsLiteral && champ.FieldType == typeof(string))
            .Select(champ => (string?)champ.GetRawConstantValue())
            .Where(valeur => !string.IsNullOrEmpty(valeur))
            .Select(valeur => valeur!);
}
