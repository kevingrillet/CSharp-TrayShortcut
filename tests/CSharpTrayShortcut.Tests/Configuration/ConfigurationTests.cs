using System.Text.Json;
using CSharpTrayShortcut.Application.Configuration;
using CSharpTrayShortcut.Domain.Text;
using CSharpTrayShortcut.Tests.Doubles;

namespace CSharpTrayShortcut.Tests.Configuration;

/// <summary>Validation de la configuration.</summary>
[TestFixture]
[Category("SPEC-CFG-002")]
public sealed class ConfigurationValidationTests
{
    [Test]
    public void Une_configuration_dont_le_dossier_existe_est_valide()
    {
        var source = new FakeShortcutSource().Dossier(Build.Racine);

        Assert.That(Build.Configuration().Validate(source.DirectoryExists), Is.Null);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Un_dossier_non_renseigne_est_signale(string? chemin)
    {
        var source = new FakeShortcutSource();

        var probleme = Build.Configuration(path: chemin).Validate(source.DirectoryExists);

        Assert.That(probleme?.Key, Is.EqualTo(TextKeys.Config.PathMissing));
    }

    [Test]
    public void Un_dossier_inexistant_est_signale_avec_son_chemin()
    {
        var source = new FakeShortcutSource();

        var probleme = Build.Configuration(path: @"Z:\envole").Validate(source.DirectoryExists);

        Assert.Multiple(() =>
        {
            Assert.That(probleme?.Key, Is.EqualTo(TextKeys.Config.PathNotFound));
            Assert.That(
                probleme?.Arguments,
                Is.EqualTo(new object?[] { @"Z:\envole" }),
                "Le message doit nommer le dossier fautif : « dossier introuvable » sans le "
                + "chemin n'aide personne.");
        });
    }

    [Test]
    public void Un_dossier_illisible_est_traite_comme_inexistant()
    {
        // Le double déclare le dossier « existant mais illisible » ; DirectoryExists rend vrai.
        // Le cas gênant est celui d'un lecteur réseau déconnecté, où l'existence elle-même est
        // fausse : c'est ce que ce test fixe.
        var source = new FakeShortcutSource();

        Assert.That(
            Build.Configuration(path: @"\\serveur\partage").Validate(source.DirectoryExists),
            Is.Not.Null);
    }
}

/// <summary>Réglages et valeurs par défaut de la configuration.</summary>
[TestFixture]
[Category("SPEC-CFG-001")]
public sealed class ConfigurationDefaultsTests
{
    [Test]
    public void Une_configuration_neuve_est_exploitable()
    {
        var configuration = new TrayShortcutConfiguration();

        Assert.Multiple(() =>
        {
            Assert.That(configuration.CustomShortcuts, Is.Empty);
            Assert.That(configuration.ShowsRootFiles, Is.True);
            Assert.That(configuration.Language, Is.EqualTo(LanguagePreference.System));
        });
    }

    [TestCase(null, true)]
    [TestCase(true, true)]
    [TestCase(false, false)]
    public void Labsence_du_reglage_des_fichiers_racine_vaut_vrai(bool? regle, bool attendu)
    {
        Assert.That(Build.Configuration(showRootFiles: regle).ShowsRootFiles, Is.EqualTo(attendu));
    }
}

/// <summary>
/// Format du fichier de configuration : c'est un contrat avec l'utilisateur, qui l'édite à la
/// main et le lit dans le README.
/// </summary>
[TestFixture]
[Category("SPEC-CFG-001")]
public sealed class ConfigurationSerializationTests
{
    [TestCase("System", LanguagePreference.System)]
    [TestCase("French", LanguagePreference.French)]
    [TestCase("English", LanguagePreference.English)]
    [TestCase("french", LanguagePreference.French)]
    public void La_langue_se_lit_sous_forme_de_texte(string ecrit, LanguagePreference attendu)
    {
        var json = $$"""{ "Path": "C:\\Toolbar", "Language": "{{ecrit}}" }""";

        var configuration = JsonSerializer.Deserialize<TrayShortcutConfiguration>(
            json,
            ConfigurationSerialization.Options);

        Assert.That(
            configuration?.Language,
            Is.EqualTo(attendu),
            "Sans convertisseur d'énumérations, System.Text.Json n'accepte qu'une valeur "
            + "numérique : la forme documentée ferait échouer la lecture de l'objet ENTIER, et "
            + "le dossier surveillé serait oublié avec le reste.");
    }

    [Test]
    public void La_langue_sécrit_sous_forme_de_texte()
    {
        var configuration = Build.Configuration();
        configuration.Language = LanguagePreference.English;

        var json = JsonSerializer.Serialize(configuration, ConfigurationSerialization.Options);

        Assert.That(
            json,
            Does.Contain("\"Language\": \"English\""),
            "Le fichier doit rester lisible et modifiable à la main : un « 2 » n'apprendrait "
            + "rien à qui l'ouvre.");
    }

    [Test]
    public void Une_configuration_complete_fait_un_aller_retour_sans_perte()
    {
        var origine = Build.Configuration(
            showRootFiles: false,
            folderIcon: "folder_w11.ico",
            customs: Build.Custom(@"C:\a.exe", text: "Outil", argument: "-x", image: "i.ico"));
        origine.Language = LanguagePreference.French;

        var json = JsonSerializer.Serialize(origine, ConfigurationSerialization.Options);
        var relu = JsonSerializer.Deserialize<TrayShortcutConfiguration>(
            json,
            ConfigurationSerialization.Options);

        Assert.Multiple(() =>
        {
            Assert.That(relu?.Path, Is.EqualTo(origine.Path));
            Assert.That(relu?.ShowRootFiles, Is.False);
            Assert.That(relu?.PathFolderIcon, Is.EqualTo("folder_w11.ico"));
            Assert.That(relu?.Language, Is.EqualTo(LanguagePreference.French));
            Assert.That(relu?.CustomShortcuts, Has.Count.EqualTo(1));
            Assert.That(relu?.CustomShortcuts[0].Text, Is.EqualTo("Outil"));
            Assert.That(relu?.CustomShortcuts[0].Argument, Is.EqualTo("-x"));
        });
    }

    [Test]
    public void Le_fichier_ne_contient_que_des_reglages_reels()
    {
        var json = JsonSerializer.Serialize(Build.Configuration(), ConfigurationSerialization.Options);

        Assert.Multiple(() =>
        {
            Assert.That(
                json,
                Does.Not.Contain("ShowsRootFiles"),
                "« ShowsRootFiles » est la valeur calculée du réglage « ShowRootFiles ». L'écrire "
                + "afficherait à l'utilisateur un nom de réglage qui n'existe pas, et qui serait "
                + "ignoré s'il tentait de le modifier.");
            Assert.That(json, Does.Contain("\"Path\""));
        });
    }

    [Test]
    public void Un_reglage_non_renseigne_nest_pas_ecrit()
    {
        var json = JsonSerializer.Serialize(
            Build.Configuration(folderIcon: null),
            ConfigurationSerialization.Options);

        Assert.That(
            json,
            Does.Not.Contain("null"),
            "Un fichier rempli de « null » pour les réglages inutilisés est plus difficile à "
            + "lire, sans rien apporter.");
    }

    [Test]
    public void Les_noms_de_reglages_sont_insensibles_a_la_casse()
    {
        var json = """{ "path": "C:\\Toolbar", "showrootfiles": false }""";

        var configuration = JsonSerializer.Deserialize<TrayShortcutConfiguration>(
            json,
            ConfigurationSerialization.Options);

        Assert.Multiple(() =>
        {
            Assert.That(configuration?.Path, Is.EqualTo(@"C:\Toolbar"));
            Assert.That(configuration?.ShowsRootFiles, Is.False);
        });
    }

    [Test]
    public void Un_reglage_inconnu_du_fichier_est_ignore()
    {
        var json = """{ "Path": "C:\\Toolbar", "ReglageDuneVersionFuture": 42 }""";

        var configuration = JsonSerializer.Deserialize<TrayShortcutConfiguration>(
            json,
            ConfigurationSerialization.Options);

        Assert.That(
            configuration?.Path,
            Is.EqualTo(@"C:\Toolbar"),
            "Un fichier écrit par une version plus récente doit rester lisible.");
    }
}

/// <summary>Normalisation des raccourcis personnalisés avant écriture.</summary>
[TestFixture]
[Category("SPEC-CFG-003")]
public sealed class CustomShortcutTests
{
    [Test]
    public void Les_chaines_vides_deviennent_absentes_a_lenregistrement()
    {
        var normalise = Build.Custom(@"C:\a.exe", text: "  ", argument: string.Empty, image: "   ")
            .Normalized();

        Assert.Multiple(() =>
        {
            Assert.That(normalise.Text, Is.Null);
            Assert.That(normalise.Argument, Is.Null);
            Assert.That(normalise.Image, Is.Null);
            Assert.That(
                normalise.Path,
                Is.EqualTo(@"C:\a.exe"),
                "Le chemin est la seule donnée obligatoire : il n'est jamais normalisé.");
        });
    }

    [Test]
    public void Les_valeurs_renseignees_traversent_la_normalisation_intactes()
    {
        var normalise = Build.Custom(@"C:\a.exe", text: "Outil", argument: "-x", image: "i.ico")
            .Normalized();

        Assert.Multiple(() =>
        {
            Assert.That(normalise.Text, Is.EqualTo("Outil"));
            Assert.That(normalise.Argument, Is.EqualTo("-x"));
            Assert.That(normalise.Image, Is.EqualTo("i.ico"));
        });
    }

    [Test]
    public void Un_raccourci_sans_chemin_ne_produit_aucune_cible()
    {
        Assert.That(Build.Custom(path: null, text: "Orphelin").ToLaunchTarget(), Is.Null);
    }
}
