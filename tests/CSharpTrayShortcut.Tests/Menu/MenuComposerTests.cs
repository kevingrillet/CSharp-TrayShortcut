using CSharpTrayShortcut.Domain.Menu;
using CSharpTrayShortcut.Domain.Text;
using CSharpTrayShortcut.Tests.Doubles;

namespace CSharpTrayShortcut.Tests.Menu;

/// <summary>Composition du menu de la zone de notification.</summary>
[TestFixture]
public sealed class MenuComposerTests
{
    [Test]
    [Category("SPEC-MENU-001")]
    public void Le_menu_racine_presente_les_dossiers_puis_les_fichiers()
    {
        var source = new FakeShortcutSource()
            .Dossier(Build.Racine, dossiers: ["Bureautique"], fichiers: ["notepad.exe"]);

        var entries = Build.Composer(source).ComposeRoot(Build.Configuration());

        Assert.Multiple(() =>
        {
            Assert.That(entries[0], Is.InstanceOf<FolderEntry>(), "Les dossiers viennent en premier.");
            Assert.That(entries[1], Is.InstanceOf<LaunchEntry>(), "Puis les fichiers.");
        });
    }

    [Test]
    [Category("SPEC-MENU-001")]
    public void Les_trois_commandes_terminent_toujours_le_menu()
    {
        var source = new FakeShortcutSource().Dossier(Build.Racine);

        var entries = Build.Composer(source).ComposeRoot(Build.Configuration());

        var commandes = entries.OfType<CommandEntry>().Select(entry => entry.Command).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(
                commandes,
                Is.EqualTo(new[] { MenuCommand.Refresh, MenuCommand.Edit, MenuCommand.Exit }),
                "Les trois commandes doivent être présentes, dans cet ordre.");
            Assert.That(
                entries[^4],
                Is.InstanceOf<SeparatorEntry>(),
                "Un séparateur isole le bloc des commandes.");
        });
    }

    [Test]
    [Category("SPEC-MENU-001")]
    public void Un_dossier_surveille_absent_laisse_un_menu_reduit_aux_commandes()
    {
        var source = new FakeShortcutSource();

        var entries = Build.Composer(source).ComposeRoot(Build.Configuration(path: null));

        Assert.That(
            entries.OfType<CommandEntry>().Count(),
            Is.EqualTo(3),
            "Sans les commandes, une configuration incomplète rendrait l'application impossible "
            + "à corriger et même à quitter.");
    }

    [Test]
    [Category("SPEC-MENU-001")]
    public void Les_fichiers_de_la_racine_disparaissent_quand_le_reglage_les_masque()
    {
        var source = new FakeShortcutSource()
            .Dossier(Build.Racine, dossiers: ["Bureautique"], fichiers: ["notepad.exe"]);

        var entries = Build.Composer(source)
            .ComposeRoot(Build.Configuration(showRootFiles: false));

        Assert.Multiple(() =>
        {
            Assert.That(Build.LibellesLancables(entries), Is.Empty);
            Assert.That(Build.LibellesDossiers(entries), Is.EqualTo(new[] { "Bureautique" }));
        });
    }

    [Test]
    [Category("SPEC-MENU-001")]
    public void Le_reglage_absent_du_fichier_affiche_les_fichiers_de_la_racine()
    {
        var source = new FakeShortcutSource().Dossier(Build.Racine, fichiers: ["notepad.exe"]);

        var entries = Build.Composer(source).ComposeRoot(Build.Configuration(showRootFiles: null));

        Assert.That(
            Build.LibellesLancables(entries),
            Is.EqualTo(new[] { "notepad" }),
            "L'absence du réglage vaut « true », pour ne pas changer le comportement des "
            + "configurations écrites avant son apparition.");
    }

    [Test]
    [Category("SPEC-MENU-001")]
    public void Le_reglage_ne_masque_que_la_racine_pas_les_sous_dossiers()
    {
        var sousDossier = Build.Dans("Bureautique");
        var source = new FakeShortcutSource()
            .Dossier(Build.Racine, dossiers: ["Bureautique"], fichiers: ["notepad.exe"])
            .Dossier(sousDossier, fichiers: ["word.lnk"]);

        var entries = Build.Composer(source)
            .ComposeFolder(sousDossier, Build.Configuration(showRootFiles: false));

        Assert.That(Build.LibellesLancables(entries), Is.EqualTo(new[] { "word" }));
    }

    [Test]
    [Category("SPEC-MENU-002")]
    public void Dossiers_et_fichiers_sont_ordonnes_en_ignorant_la_casse_et_les_accents()
    {
        var source = new FakeShortcutSource().Dossier(
            Build.Racine,
            dossiers: ["Zip", "Éditeurs", "audio"],
            fichiers: ["zebra.exe", "Alpha.exe", "éclair.exe"]);

        var entries = Build.Composer(source).ComposeRoot(Build.Configuration());

        Assert.Multiple(() =>
        {
            Assert.That(
                Build.LibellesDossiers(entries),
                Is.EqualTo(new[] { "audio", "Éditeurs", "Zip" }),
                "L'ordre doit être celui qu'attend un lecteur francophone, pas l'ordre des "
                + "points de code.");
            Assert.That(
                Build.LibellesLancables(entries),
                Is.EqualTo(new[] { "Alpha", "éclair", "zebra" }));
        });
    }

    [Test]
    [Category("SPEC-MENU-002")]
    public void Un_fichier_est_intitule_sans_son_extension()
    {
        var source = new FakeShortcutSource()
            .Dossier(Build.Racine, fichiers: ["Notepad++.lnk", "rapport.docx"]);

        var entries = Build.Composer(source).ComposeRoot(Build.Configuration());

        Assert.That(
            Build.LibellesLancables(entries),
            Is.EqualTo(new[] { "Notepad++", "rapport" }));
    }

    [Test]
    [Category("SPEC-MENU-003")]
    public void Le_menu_racine_nenumere_que_le_premier_niveau()
    {
        var source = new FakeShortcutSource()
            .Dossier(Build.Racine, dossiers: ["Bureautique"])
            .Dossier(Build.Dans("Bureautique"), dossiers: ["Modèles"])
            .Dossier(Build.Dans(@"Bureautique\Modèles"));

        var composer = Build.Composer(source);
        _ = composer.ComposeRoot(Build.Configuration());

        Assert.That(
            source.Enumerations,
            Is.EqualTo(1),
            "Parcourir toute l'arborescence au démarrage coûterait, sur un partage réseau, "
            + "plusieurs secondes avant l'apparition de l'icône.");
    }

    [Test]
    [Category("SPEC-MENU-003")]
    public void Le_contenu_dun_sous_dossier_est_construit_a_son_ouverture()
    {
        var sousDossier = Build.Dans("Bureautique");
        var source = new FakeShortcutSource()
            .Dossier(Build.Racine, dossiers: ["Bureautique"])
            .Dossier(sousDossier, dossiers: ["Modèles"], fichiers: ["word.lnk"]);

        var entries = Build.Composer(source).ComposeFolder(sousDossier, Build.Configuration());

        Assert.Multiple(() =>
        {
            Assert.That(Build.LibellesDossiers(entries), Is.EqualTo(new[] { "Modèles" }));
            Assert.That(Build.LibellesLancables(entries), Is.EqualTo(new[] { "word" }));
            Assert.That(
                entries.OfType<CommandEntry>(),
                Is.Empty,
                "Les commandes n'appartiennent qu'au menu racine.");
        });
    }

    [Test]
    [Category("SPEC-MENU-004")]
    public void Un_dossier_illisible_rend_un_sous_menu_vide_sans_faire_echouer_le_menu()
    {
        var interdit = Build.Dans("Interdit");
        var source = new FakeShortcutSource()
            .Dossier(Build.Racine, dossiers: ["Interdit", "Public"], fichiers: ["notepad.exe"])
            .Illisible(interdit);

        var composer = Build.Composer(source);
        var racine = composer.ComposeRoot(Build.Configuration());
        var contenu = composer.ComposeFolder(interdit, Build.Configuration());

        Assert.Multiple(() =>
        {
            Assert.That(
                Build.LibellesDossiers(racine),
                Is.EqualTo(new[] { "Interdit", "Public" }),
                "Le dossier reste visible : on ne sait pas qu'il est illisible avant de l'ouvrir.");
            Assert.That(contenu, Is.Empty, "Son contenu est vide, sans exception.");
            Assert.That(Build.LibellesLancables(racine), Is.EqualTo(new[] { "notepad" }));
        });
    }

    [Test]
    [Category("SPEC-MENU-004")]
    public void Une_racine_illisible_laisse_le_menu_utilisable()
    {
        var source = new FakeShortcutSource().Illisible(Build.Racine);

        var entries = Build.Composer(source).ComposeRoot(Build.Configuration());

        Assert.That(entries.OfType<CommandEntry>().Count(), Is.EqualTo(3));
    }

    [Test]
    [Category("SPEC-MENU-005")]
    public void Les_raccourcis_personnalises_forment_une_section_a_part()
    {
        var source = new FakeShortcutSource().Dossier(Build.Racine);
        var configuration = Build.Configuration(
            customs:
            [
                Build.Custom(@"C:\Program Files\Notepad++\notepad++.exe", text: "Notepad++"),
            ]);

        var entries = Build.Composer(source).ComposeRoot(configuration);
        var section = entries.OfType<GroupEntry>().Single();

        Assert.Multiple(() =>
        {
            Assert.That(section.Label.Key, Is.EqualTo(TextKeys.Menu.Customs));
            Assert.That(Build.LibellesLancables(section.Children), Is.EqualTo(new[] { "Notepad++" }));
            Assert.That(
                entries.TakeWhile(entry => entry is not GroupEntry).Last(),
                Is.InstanceOf<SeparatorEntry>(),
                "Un séparateur précède la section.");
        });
    }

    [Test]
    [Category("SPEC-MENU-005")]
    public void Aucune_section_personnalisee_quand_il_ny_en_a_aucun()
    {
        var source = new FakeShortcutSource().Dossier(Build.Racine);

        var entries = Build.Composer(source).ComposeRoot(Build.Configuration());

        Assert.That(entries.OfType<GroupEntry>(), Is.Empty);
    }

    [Test]
    [Category("SPEC-MENU-005")]
    public void Un_raccourci_personnalise_sans_chemin_est_ignore_en_silence()
    {
        var source = new FakeShortcutSource().Dossier(Build.Racine);
        var configuration = Build.Configuration(
            customs:
            [
                Build.Custom(path: null, text: "Ligne à moitié remplie"),
                Build.Custom(path: "   ", text: "Espaces seulement"),
                Build.Custom(@"C:\Windows\notepad.exe", text: "Bloc-notes"),
            ]);

        var entries = Build.Composer(source).ComposeRoot(configuration);
        var section = entries.OfType<GroupEntry>().Single();

        Assert.That(
            Build.LibellesLancables(section.Children),
            Is.EqualTo(new[] { "Bloc-notes" }),
            "Une entrée sans chemin ne ferait rien au clic : mieux vaut ne pas l'afficher.");
    }

    [Test]
    [Category("SPEC-MENU-005")]
    public void Les_raccourcis_personnalises_sont_ordonnes_par_intitule()
    {
        var source = new FakeShortcutSource().Dossier(Build.Racine);
        var configuration = Build.Configuration(
            customs:
            [
                Build.Custom(@"C:\z.exe", text: "Zip"),
                Build.Custom(@"C:\a.exe", text: "Archivage"),
            ]);

        var entries = Build.Composer(source).ComposeRoot(configuration);
        var section = entries.OfType<GroupEntry>().Single();

        Assert.That(
            Build.LibellesLancables(section.Children),
            Is.EqualTo(new[] { "Archivage", "Zip" }));
    }

    [Test]
    [Category("SPEC-MENU-002")]
    public void Un_raccourci_personnalise_sans_intitule_prend_le_nom_du_fichier_vise()
    {
        var source = new FakeShortcutSource().Dossier(Build.Racine);
        var configuration = Build.Configuration(
            customs: [Build.Custom(@"C:\Program Files\Notepad++\notepad++.exe")]);

        var entries = Build.Composer(source).ComposeRoot(configuration);
        var section = entries.OfType<GroupEntry>().Single();

        Assert.That(
            Build.LibellesLancables(section.Children),
            Is.EqualTo(new[] { "notepad++" }),
            "Sans repli, l'entrée serait affichée sans texte et donc impossible à cliquer "
            + "sciemment.");
    }

    [Test]
    [Category("SPEC-MENU-005")]
    public void Largument_dun_raccourci_personnalise_est_conserve()
    {
        var source = new FakeShortcutSource().Dossier(Build.Racine);
        var configuration = Build.Configuration(
            customs: [Build.Custom(@"C:\Windows\notepad.exe", text: "Notes", argument: @"C:\notes.txt")]);

        var entries = Build.Composer(source).ComposeRoot(configuration);
        var entree = entries.OfType<GroupEntry>().Single().Children.OfType<LaunchEntry>().Single();

        Assert.That(entree.Target.Argument, Is.EqualTo(@"C:\notes.txt"));
    }

    [Test]
    [Category("SPEC-MENU-001")]
    public void Un_fichier_du_dossier_surveille_se_lance_par_son_chemin_complet()
    {
        var source = new FakeShortcutSource().Dossier(Build.Racine, fichiers: ["notepad.exe"]);

        var entries = Build.Composer(source).ComposeRoot(Build.Configuration());
        var entree = entries.OfType<LaunchEntry>().Single();

        Assert.Multiple(() =>
        {
            Assert.That(entree.Target.Path, Is.EqualTo(Build.Dans("notepad.exe")));
            Assert.That(entree.Target.Argument, Is.Null);
        });
    }
}
