using CSharpTrayShortcut.Application.Configuration;
using CSharpTrayShortcut.Application.Menu;
using CSharpTrayShortcut.Domain.Shortcuts;
using CSharpTrayShortcut.Tests.Doubles;

namespace CSharpTrayShortcut.Tests.Menu;

/// <summary>Choix de l'icône d'une entrée de menu.</summary>
[TestFixture]
public sealed class IconSourceResolverTests
{
    [Test]
    [Category("SPEC-ICON-001")]
    public void Licone_dun_fichier_est_extraite_du_fichier_lui_meme()
    {
        var fichier = Build.Dans("notepad.exe");
        var source = new FakeShortcutSource().Dossier(Build.Racine, fichiers: ["notepad.exe"]);

        var icone = Build.Icons(source).ForFile(fichier);

        Assert.Multiple(() =>
        {
            Assert.That(icone.Kind, Is.EqualTo(IconSourceKind.ExtractFromFile));
            Assert.That(icone.Path, Is.EqualTo(fichier));
        });
    }

    [Test]
    [Category("SPEC-ICON-001")]
    public void Un_fichier_absent_na_pas_dicone()
    {
        var source = new FakeShortcutSource().Dossier(Build.Racine);

        var icone = Build.Icons(source).ForFile(Build.Dans("disparu.exe"));

        Assert.That(
            icone.Kind,
            Is.EqualTo(IconSourceKind.None),
            "Une entrée sans image reste lisible ; une image trompeuse ne l'est pas.");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [Category("SPEC-ICON-001")]
    public void Un_chemin_vide_na_pas_dicone(string? chemin)
    {
        var source = new FakeShortcutSource();

        Assert.That(Build.Icons(source).ForFile(chemin).Kind, Is.EqualTo(IconSourceKind.None));
    }

    [Test]
    [Category("SPEC-ICON-001")]
    public void Licone_explicite_dun_raccourci_personnalise_prime_sur_lextraction()
    {
        var source = new FakeShortcutSource().Fichier(@"C:\Windows\notepad.exe");
        var custom = Build.Custom(@"C:\Windows\notepad.exe", image: "monicone.ico");

        var icone = Build.Icons(source).ForCustom(custom);

        Assert.Multiple(() =>
        {
            Assert.That(icone.Kind, Is.EqualTo(IconSourceKind.IconFile));
            Assert.That(icone.Path, Is.EqualTo("monicone.ico"));
        });
    }

    [Test]
    [Category("SPEC-ICON-001")]
    public void Sans_icone_explicite_un_raccourci_personnalise_extrait_celle_de_sa_cible()
    {
        var source = new FakeShortcutSource().Fichier(@"C:\Windows\notepad.exe");
        var custom = Build.Custom(@"C:\Windows\notepad.exe");

        var icone = Build.Icons(source).ForCustom(custom);

        Assert.Multiple(() =>
        {
            Assert.That(icone.Kind, Is.EqualTo(IconSourceKind.ExtractFromFile));
            Assert.That(icone.Path, Is.EqualTo(@"C:\Windows\notepad.exe"));
        });
    }

    [Test]
    [Category("SPEC-ICON-003")]
    public void Un_raccourci_windows_montre_licone_de_sa_cible()
    {
        var raccourci = Build.Dans("Word.lnk");
        var cible = @"C:\Program Files\Microsoft Office\WINWORD.EXE";

        var source = new FakeShortcutSource()
            .Dossier(Build.Racine, fichiers: ["Word.lnk"])
            .Fichier(cible);
        var cibles = new FakeShortcutTargetResolver().Cible(raccourci, cible);

        var icone = Build.Icons(source, cibles).ForFile(raccourci);

        Assert.Multiple(() =>
        {
            Assert.That(icone.Kind, Is.EqualTo(IconSourceKind.ExtractFromFile));
            Assert.That(
                icone.Path,
                Is.EqualTo(cible),
                "Sans suivre la cible, tous les raccourcis afficheraient la même image.");
        });
    }

    [Test]
    [Category("SPEC-ICON-003")]
    public void Un_raccourci_dont_la_cible_est_illisible_montre_sa_propre_icone()
    {
        var raccourci = Build.Dans("Perime.lnk");
        var source = new FakeShortcutSource().Dossier(Build.Racine, fichiers: ["Perime.lnk"]);

        var icone = Build.Icons(source).ForFile(raccourci);

        Assert.That(icone.Path, Is.EqualTo(raccourci));
    }

    [Test]
    [Category("SPEC-ICON-003")]
    public void Un_raccourci_dont_la_cible_a_disparu_montre_sa_propre_icone()
    {
        var raccourci = Build.Dans("Perime.lnk");
        var source = new FakeShortcutSource().Dossier(Build.Racine, fichiers: ["Perime.lnk"]);
        var cibles = new FakeShortcutTargetResolver().Cible(raccourci, @"D:\envole.exe");

        var icone = Build.Icons(source, cibles).ForFile(raccourci);

        Assert.That(icone.Path, Is.EqualTo(raccourci));
    }

    [Test]
    [Category("SPEC-ICON-003")]
    public void Lextension_de_raccourci_est_reconnue_sans_egard_a_la_casse()
    {
        var raccourci = Build.Dans("Word.LNK");
        var cible = @"C:\WINWORD.EXE";

        var source = new FakeShortcutSource()
            .Dossier(Build.Racine, fichiers: ["Word.LNK"])
            .Fichier(cible);
        var cibles = new FakeShortcutTargetResolver().Cible(raccourci, cible);

        Assert.That(Build.Icons(source, cibles).ForFile(raccourci).Path, Is.EqualTo(cible));
    }

    [Test]
    [Category("SPEC-ICON-002")]
    public void Licone_de_dossier_configuree_precede_celle_livree()
    {
        var icone = IconSourceResolver.ForFolders(Build.Configuration(folderIcon: "folder_w11.ico"));

        Assert.That(
            icone.Chain().Select(source => source.Path),
            Is.EqualTo(new[] { "folder_w11.ico", TrayShortcutConfiguration.DefaultFolderIcon }));
    }

    [Test]
    [Category("SPEC-ICON-002")]
    public void Sans_icone_de_dossier_configuree_seule_celle_livree_est_tentee()
    {
        var icone = IconSourceResolver.ForFolders(Build.Configuration(folderIcon: null));

        Assert.Multiple(() =>
        {
            Assert.That(icone.Path, Is.EqualTo(TrayShortcutConfiguration.DefaultFolderIcon));
            Assert.That(icone.Fallback, Is.Null, "Une source vide ne doit pas allonger la chaîne.");
        });
    }

    [Test]
    [Category("SPEC-ICON-002")]
    public void Licone_de_la_zone_de_notification_suit_la_meme_regle_de_repli()
    {
        var icone = IconSourceResolver.ForTray(Build.Configuration(trayIcon: @"D:\perso.ico"));

        Assert.That(
            icone.Chain().Select(source => source.Path),
            Is.EqualTo(new[] { @"D:\perso.ico", TrayShortcutConfiguration.DefaultTrayIcon }));
    }

    [Test]
    [Category("SPEC-ICON-002")]
    public void Tous_les_dossiers_partagent_la_meme_source_dicone()
    {
        var source = new FakeShortcutSource()
            .Dossier(Build.Racine, dossiers: ["Un", "Deux", "Trois"]);

        var entries = Build.Composer(source).ComposeRoot(Build.Configuration());
        var icones = entries.OfType<CSharpTrayShortcut.Domain.Menu.FolderEntry>()
            .Select(entry => entry.Icon)
            .Distinct()
            .ToList();

        Assert.That(
            icones,
            Has.Count.EqualTo(1),
            "L'égalité structurelle des sources est ce qui permet au rendu de ne fabriquer "
            + "qu'une seule image de dossier, quel qu'en soit le nombre.");
    }
}
