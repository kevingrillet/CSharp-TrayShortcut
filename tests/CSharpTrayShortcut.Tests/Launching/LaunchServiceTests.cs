using CSharpTrayShortcut.Application.Launching;
using CSharpTrayShortcut.Domain.Shortcuts;
using CSharpTrayShortcut.Tests.Doubles;

namespace CSharpTrayShortcut.Tests.Launching;

/// <summary>Lancement de ce que l'utilisateur a cliqué.</summary>
[TestFixture]
public sealed class LaunchServiceTests
{
    [Test]
    [Category("SPEC-LAUNCH-001")]
    public void Un_fichier_existant_est_lance_avec_son_argument()
    {
        var chemin = @"C:\Windows\notepad.exe";
        var source = new FakeShortcutSource().Fichier(chemin);
        var lanceur = new FakeProcessLauncher();

        var cible = LaunchTarget.TryCreate(chemin, @"C:\notes.txt")!;
        var lance = Build.Launcher(source, lanceur).Launch(cible);

        Assert.Multiple(() =>
        {
            Assert.That(lance, Is.True);
            Assert.That(lanceur.Demandes, Has.Count.EqualTo(1));
            Assert.That(lanceur.Demandes[0].Path, Is.EqualTo(chemin));
            Assert.That(lanceur.Demandes[0].Argument, Is.EqualTo(@"C:\notes.txt"));
        });
    }

    [Test]
    [Category("SPEC-LAUNCH-002")]
    public void Une_cible_disparue_ne_lance_rien_et_ne_leve_pas_dexception()
    {
        var source = new FakeShortcutSource();
        var lanceur = new FakeProcessLauncher();

        var cible = LaunchTarget.TryCreate(@"D:\efface.exe")!;
        var lance = Build.Launcher(source, lanceur).Launch(cible);

        Assert.Multiple(() =>
        {
            Assert.That(lance, Is.False);
            Assert.That(
                lanceur.Demandes,
                Is.Empty,
                "Un menu se construit à un instant et se clique à un autre : la cible est "
                + "réexaminée au clic.");
        });
    }

    [Test]
    [Category("SPEC-LAUNCH-002")]
    public void Un_refus_du_systeme_est_rapporte_sans_exception()
    {
        var chemin = @"C:\interdit.exe";
        var source = new FakeShortcutSource().Fichier(chemin);
        var lanceur = new FakeProcessLauncher { Accepte = false };

        var lance = Build.Launcher(source, lanceur).Launch(LaunchTarget.TryCreate(chemin)!);

        Assert.Multiple(() =>
        {
            Assert.That(lance, Is.False);
            Assert.That(
                lanceur.Demandes,
                Has.Count.EqualTo(1),
                "Le lancement a bien été tenté : c'est le système qui l'a refusé.");
        });
    }

    [Test]
    [Category("SPEC-LAUNCH-003")]
    public void Un_dossier_est_une_cible_valide()
    {
        var source = new FakeShortcutSource().Dossier(@"C:\Outils");
        var lanceur = new FakeProcessLauncher();
        var service = Build.Launcher(source, lanceur);

        var cible = LaunchTarget.TryCreate(@"C:\Outils")!;

        Assert.Multiple(() =>
        {
            Assert.That(service.Inspect(cible), Is.EqualTo(LaunchAvailability.Directory));
            Assert.That(service.Launch(cible), Is.True);
        });
    }

    [TestCase("https://example.org/wiki")]
    [TestCase("http://intranet/outils")]
    [TestCase("mailto:support@example.org")]
    [Category("SPEC-LAUNCH-003")]
    public void Une_adresse_autorisee_est_une_cible_valide(string adresse)
    {
        var source = new FakeShortcutSource();
        var lanceur = new FakeProcessLauncher();
        var service = Build.Launcher(source, lanceur);

        var cible = LaunchTarget.TryCreate(adresse)!;

        Assert.Multiple(() =>
        {
            Assert.That(
                service.Inspect(cible),
                Is.EqualTo(LaunchAvailability.Uri),
                "Le README a toujours annoncé « lien ou exécutable » ; seul le contrôle "
                + "d'existence de fichier l'empêchait.");
            Assert.That(service.Launch(cible), Is.True);
        });
    }

    [TestCase("ftp://serveur/fichier")]
    [TestCase("javascript:alert(1)")]
    [TestCase("ms-settings:privacy")]
    [TestCase(@"file://serveur/partage/outil.exe")]
    [TestCase(@"\\serveur\partage\outil.exe")]
    [Category("SPEC-LAUNCH-003")]
    public void Un_schema_hors_liste_blanche_est_refuse(string adresse)
    {
        var source = new FakeShortcutSource();
        var lanceur = new FakeProcessLauncher();
        var service = Build.Launcher(source, lanceur);

        var cible = LaunchTarget.TryCreate(adresse)!;

        Assert.Multiple(() =>
        {
            Assert.That(
                service.Inspect(cible),
                Is.EqualTo(LaunchAvailability.Missing),
                "Passer un schéma quelconque au shell reviendrait à laisser un fichier de "
                + "configuration déclencher n'importe quel gestionnaire de protocole.");
            Assert.That(lanceur.Demandes, Is.Empty);
        });
    }

    [Test]
    [Category("SPEC-LAUNCH-003")]
    public void Un_chemin_local_est_classe_comme_fichier_et_non_comme_adresse()
    {
        var chemin = @"C:\Outils\script.cmd";
        var source = new FakeShortcutSource().Fichier(chemin);
        var service = Build.Launcher(source, new FakeProcessLauncher());

        Assert.That(
            service.Inspect(LaunchTarget.TryCreate(chemin)!),
            Is.EqualTo(LaunchAvailability.File),
            "« C:\\… » est aussi une URI file: valide : tester le disque avant l'adresse "
            + "évite de classer tous les chemins locaux en adresses.");
    }

    [Test]
    [Category("SPEC-LAUNCH-002")]
    public void Un_chemin_local_disparu_nest_pas_sauve_par_le_schema_file()
    {
        var source = new FakeShortcutSource();
        var service = Build.Launcher(source, new FakeProcessLauncher());

        Assert.That(
            service.Inspect(LaunchTarget.TryCreate(@"D:\parti.exe")!),
            Is.EqualTo(LaunchAvailability.Missing),
            "Admettre le schéma file: dans la liste blanche classerait tout chemin disparu en "
            + "adresse valide, et viderait SPEC-LAUNCH-002 de son sens.");
    }

    [Test]
    [Category("SPEC-LAUNCH-001")]
    public void Un_fichier_prime_sur_un_dossier_de_meme_nom()
    {
        var chemin = @"C:\Ambigu";
        var source = new FakeShortcutSource().Fichier(chemin).Dossier(chemin);
        var service = Build.Launcher(source, new FakeProcessLauncher());

        Assert.That(service.Inspect(LaunchTarget.TryCreate(chemin)!), Is.EqualTo(LaunchAvailability.File));
    }
}

/// <summary>Construction d'une cible de lancement.</summary>
[TestFixture]
[Category("SPEC-LAUNCH-001")]
public sealed class LaunchTargetTests
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Une_cible_sans_chemin_nest_pas_constructible(string? chemin)
    {
        Assert.That(
            LaunchTarget.TryCreate(chemin),
            Is.Null,
            "Un LaunchTarget qui existe porte toujours un chemin exploitable : c'est ce qui "
            + "remplace les exceptions levées au milieu d'un menu.");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Un_argument_vide_devient_absent(string? argument)
    {
        var cible = LaunchTarget.TryCreate(@"C:\notepad.exe", argument);

        Assert.That(
            cible!.Argument,
            Is.Null,
            "Normaliser ici évite d'avoir à traiter « vide » et « absent » séparément ensuite.");
    }

    [Test]
    public void Deux_cibles_identiques_sont_egales()
    {
        var premiere = LaunchTarget.TryCreate(@"C:\a.exe", "-x");
        var seconde = LaunchTarget.TryCreate(@"C:\a.exe", "-x");

        Assert.That(premiere, Is.EqualTo(seconde));
    }
}
