using CSharpTrayShortcut.Application.Menu;
using CSharpTrayShortcut.Domain.Shortcuts;
using CSharpTrayShortcut.Tests.Doubles;

namespace CSharpTrayShortcut.Tests.Menu;

/// <summary>Sous quelle clé une image peut être réutilisée.</summary>
[TestFixture]
[Category("SPEC-ICON-004")]
public sealed class IconCachePolicyTests
{
    [TestCase(".pdf")]
    [TestCase(".docx")]
    [TestCase(".txt")]
    [TestCase(".csv")]
    public void Deux_documents_de_meme_extension_partagent_une_seule_image(string extension)
    {
        var premier = Build.Dans($"rapport{extension}");
        var second = Build.Dans($"annexe{extension}");
        var source = new FakeShortcutSource().Fichier(premier).Fichier(second);
        var policy = Build.CachePolicy(source);

        var cle1 = policy.KeyFor(IconSource.ExtractedFrom(premier));
        var cle2 = policy.KeyFor(IconSource.ExtractedFrom(second));

        Assert.That(
            cle1,
            Is.EqualTo(cle2),
            "Windows donne à un document l'icône associée à son type : trente PDF dans un "
            + "dossier ne doivent coûter qu'une seule extraction, pas trente.");
    }

    [Test]
    public void La_casse_de_lextension_est_indifferente()
    {
        var source = new FakeShortcutSource().Fichier(@"C:\a.PDF").Fichier(@"C:\b.pdf");
        var policy = Build.CachePolicy(source);

        Assert.That(
            policy.KeyFor(IconSource.ExtractedFrom(@"C:\a.PDF")),
            Is.EqualTo(policy.KeyFor(IconSource.ExtractedFrom(@"C:\b.pdf"))));
    }

    [TestCase(".exe")]
    [TestCase(".dll")]
    [TestCase(".ico")]
    [TestCase(".cpl")]
    public void Deux_executables_de_meme_extension_ne_partagent_pas_leur_image(string extension)
    {
        var premier = $@"C:\Outils\alpha{extension}";
        var second = $@"C:\Outils\beta{extension}";
        var source = new FakeShortcutSource().Fichier(premier).Fichier(second);
        var policy = Build.CachePolicy(source);

        Assert.That(
            policy.KeyFor(IconSource.ExtractedFrom(premier)),
            Is.Not.EqualTo(policy.KeyFor(IconSource.ExtractedFrom(second))),
            "Un exécutable porte sa propre icône : les confondre afficherait la même image "
            + "pour deux applications différentes.");
    }

    [Test]
    public void Un_fichier_sans_extension_est_traite_comme_un_executable()
    {
        var source = new FakeShortcutSource().Fichier(@"C:\Outils\lanceur");

        Assert.That(
            IconCachePolicy.DependsOnFileContent(@"C:\Outils\lanceur"),
            Is.True,
            "Sans extension, il n'y a pas d'icône de type à réutiliser : la prudence coûte au "
            + "pire une extraction de plus.");
        Assert.That(Build.CachePolicy(source).KeyFor(IconSource.ExtractedFrom(@"C:\Outils\lanceur")),
            Is.Not.Null);
    }

    [Test]
    public void Une_icone_designee_explicitement_est_propre_a_son_chemin()
    {
        var source = new FakeShortcutSource().Fichier(@"C:\Icones\a.ico").Fichier(@"C:\Icones\b.ico");
        var policy = Build.CachePolicy(source);

        Assert.That(
            policy.KeyFor(IconSource.FromIconFile(@"C:\Icones\a.ico")),
            Is.Not.EqualTo(policy.KeyFor(IconSource.FromIconFile(@"C:\Icones\b.ico"))),
            "Deux fichiers .ico différents ont la même extension mais pas la même image.");
    }

    [Test]
    public void Une_source_vide_na_pas_de_cle()
    {
        var source = new FakeShortcutSource();

        Assert.That(Build.CachePolicy(source).KeyFor(IconSource.None), Is.Null);
    }

    [Test]
    public void Un_fichier_modifie_invalide_son_image()
    {
        var chemin = @"C:\Outils\app.exe";
        var avant = new FakeShortcutSource()
            .Empreinte(chemin, new FileStamp(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), 1000));
        var apres = new FakeShortcutSource()
            .Empreinte(chemin, new FileStamp(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), 2000));

        Assert.That(
            Build.CachePolicy(avant).KeyFor(IconSource.ExtractedFrom(chemin)),
            Is.Not.EqualTo(Build.CachePolicy(apres).KeyFor(IconSource.ExtractedFrom(chemin))),
            "Mettre à jour une application doit changer son icône dans le menu, sans quoi le "
            + "cache mentirait jusqu'au prochain redémarrage.");
    }

    [Test]
    public void Un_fichier_inchange_reutilise_son_image()
    {
        var chemin = @"C:\Outils\app.exe";
        var source = new FakeShortcutSource()
            .Empreinte(chemin, new FileStamp(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), 1000));
        var policy = Build.CachePolicy(source);

        Assert.That(
            policy.KeyFor(IconSource.ExtractedFrom(chemin)),
            Is.EqualTo(policy.KeyFor(IconSource.ExtractedFrom(chemin))),
            "Sans cette stabilité, aucune entrée de cache ne survivrait à une actualisation.");
    }

    [Test]
    public void Un_fichier_absent_reste_distinguable_dun_fichier_present()
    {
        var source = new FakeShortcutSource();
        var cle = Build.CachePolicy(source).KeyFor(IconSource.ExtractedFrom(@"C:\parti.exe"));

        Assert.Multiple(() =>
        {
            Assert.That(cle, Is.Not.Null, "La clé existe : c'est l'échec de fabrication qui sera mis en cache.");
            Assert.That(
                cle!.Stamp,
                Is.Null,
                "Une empreinte absente vaut « je ne sais pas » : l'entrée sera considérée "
                + "périmée dès que le fichier réapparaîtra avec une empreinte.");
        });
    }

    [Test]
    public void Les_maillons_communs_de_deux_chaines_partagent_leur_image()
    {
        var source = new FakeShortcutSource();
        var policy = Build.CachePolicy(source);

        // Deux configurations différentes, même repli : l'icône livrée avec l'application.
        var avecPersonnalisation = IconSourceResolver.ForFolders(
            Build.Configuration(folderIcon: "perso.ico"));
        var sansPersonnalisation = IconSourceResolver.ForFolders(Build.Configuration());

        var repli = avecPersonnalisation.Chain().Last();

        Assert.Multiple(() =>
        {
            Assert.That(
                policy.KeyFor(repli),
                Is.EqualTo(policy.KeyFor(sansPersonnalisation)),
                "La clé porte sur un maillon, pas sur la chaîne entière : c'est ce qui permet "
                + "de ne pas refabriquer l'icône livrée pour chaque chaîne qui s'y replie.");
            Assert.That(
                policy.KeyFor(avecPersonnalisation),
                Is.Not.EqualTo(policy.KeyFor(repli)));
        });
    }
}
