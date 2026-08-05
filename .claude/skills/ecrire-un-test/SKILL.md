---
name: ecrire-un-test
description: "Choisir le patron de test qui correspond au sujet et réutiliser les doubles existants, avant d'écrire un test NUnit. À utiliser dès qu'on ajoute ou modifie un test dans `tests/`, ou qu'on se demande comment vérifier un comportement sans écran, sans disque et sans shell."
---

# Écrire un test

Les conventions de style — classe `sealed`, `Assert.That`, noms français — sont dans
[`docs/CONTRIBUER.md`](../../../docs/CONTRIBUER.md) §3. **Ne pas les recopier.** Cette fiche donne
les **patrons** et les **doubles**.

## 1. Tout objet vient de `Build`

`tests/CSharpTrayShortcut.Tests/Doubles/Build.cs` est le seul endroit qui construise des objets.
Un test qui appelle `new TrayShortcutConfiguration { … }` est un écart : ajouter un champ
obligatoire casserait alors trente tests.

```csharp
Build.Racine                                   // @"C:\Toolbar", le dossier surveillé de tous les tests
Build.Dans("notepad.exe")                      // @"C:\Toolbar\notepad.exe"
Build.Configuration(showRootFiles: false)      // configuration valide, un aspect surchargé
Build.Custom(@"C:\a.exe", text: "Outil")       // raccourci personnalisé
Build.Composer(source)                         // MenuComposer branché sur les doubles
Build.Icons(source, cibles)                    // IconSourceResolver
Build.Launcher(source, lanceur)                // LaunchService
Build.LibellesDossiers(entries)                // intitulés des FolderEntry, dans l'ordre
Build.LibellesLancables(entries)               // intitulés des LaunchEntry, dans l'ordre
```

Un test ne mentionne que ce qui le concerne : tout le reste vient des valeurs par défaut.

## 2. Les trois doubles

| Double | Remplace | Ce qu'il sait faire |
|---|---|---|
| `FakeShortcutSource` | le système de fichiers | `.Dossier(chemin, dossiers, fichiers)`, `.Fichier(chemin)`, `.Illisible(chemin)`, et `.Enumerations` pour compter les lectures |
| `FakeShortcutTargetResolver` | la lecture d'un `.lnk` par COM | `.Cible(raccourci, cible)` ; par défaut, aucun raccourci n'est résolvable |
| `FakeProcessLauncher` | `Process.Start` | `.Demandes` (ce qu'on lui a demandé), `.Accepte` (faux simule un refus du shell) |

`FakeShortcutSource` rend le contenu **dans l'ordre de déclaration**, jamais trié : c'est ce qui
permet de vérifier que le tri est bien l'œuvre de `MenuComposer` et non un effet de bord du système
de fichiers.

`.Illisible` déclare un dossier **existant mais dont la lecture rend vide** — le cas gênant : il
apparaît dans le menu, et son contenu est vide quand on l'ouvre.

## 3. Patrons par sujet

### Une règle de composition du menu

```csharp
[Test]
[Category("SPEC-MENU-00N")]
public void Le_comportement_attendu_en_francais_avec_underscores()
{
    var source = new FakeShortcutSource()
        .Dossier(Build.Racine, dossiers: ["Bureautique"], fichiers: ["notepad.exe"]);

    var entries = Build.Composer(source).ComposeRoot(Build.Configuration());

    Assert.That(Build.LibellesDossiers(entries), Is.EqualTo(new[] { "Bureautique" }));
}
```

Jeu minimal pour une règle nouvelle : **cas nominal**, **cas limite qui l'exclut**, **cas dégradé**
(chemin vide, dossier illisible, cible disparue).

### Une règle d'icône

On assert sur `Kind` et `Path`, jamais sur une image — c'est tout l'intérêt de
[ADR-0003](../../../docs/adr/0003-icone-source-et-non-image.md). Pour une chaîne de replis :

```csharp
Assert.That(
    icone.Chain().Select(source => source.Path),
    Is.EqualTo(new[] { "folder_w11.ico", TrayShortcutConfiguration.DefaultFolderIcon }));
```

### Un lancement

Vérifier **deux choses** : le verdict (`Launch` rend vrai ou faux) et ce qui a été demandé
(`lanceur.Demandes`). Une cible disparue ne doit pas même être soumise au lanceur.

### Une variante d'un même comportement

`[TestCase]` plutôt que trois tests copiés — schémas d'adresse, cultures, valeurs nulles :

```csharp
[TestCase(null)]
[TestCase("")]
[TestCase("   ")]
[Category("SPEC-ICON-001")]
public void Un_chemin_vide_na_pas_dicone(string? chemin)
```

### Une construction paresseuse

Compter les lectures, pas observer un effet :

```csharp
Assert.That(source.Enumerations, Is.EqualTo(1));
```

## 4. Le message d'assertion dit *pourquoi*

C'est la convention la plus utile du dépôt, et la plus facile à négliger. Le message ne répète pas
ce que l'assertion dit déjà ; il explique **pourquoi la règle existe** — ce qu'on veut lire quand le
test tombe deux ans plus tard.

```csharp
// Inutile
Assert.That(entries.OfType<CommandEntry>().Count(), Is.EqualTo(3), "Il doit y avoir 3 commandes.");

// Utile
Assert.That(
    entries.OfType<CommandEntry>().Count(),
    Is.EqualTo(3),
    "Sans les commandes, une configuration incomplète rendrait l'application impossible à "
    + "corriger et même à quitter.");
```

Grouper les assertions liées dans `Assert.Multiple` : sans lui, la première qui tombe masque les
suivantes, et on découvre les problèmes un par un.

## 5. La catégorie n'est pas optionnelle

`[Category("SPEC-…")]` sur la classe si elle ne couvre qu'une spec, sur chaque `[Test]` sinon.

Quatre garde-fous font échouer `dotnet test` si la discipline se relâche :

| Garde-fou | Ce qu'il attrape |
|---|---|
| `Chaque_scenario_Gherkin_renvoie_a_une_spec_reellement_testee` | un scénario dont le test a disparu |
| `Chaque_spec_couverte_par_un_test_est_illustree_par_un_scenario` | une catégorie sans scénario `@SPEC-…` |
| `La_liste_blanche_ne_contient_aucune_entree_devenue_inutile` | une exemption devenue mensongère |
| `Chaque_cle_declaree_est_formulee_dans_les_deux_langues` | une formulation oubliée d'un côté |

Ajouter un `[Category("SPEC-…")]` **oblige** donc à écrire le scénario Gherkin correspondant. Ce
n'est pas une négligence possible.

Si le comportement n'est pas automatisable, l'inscrire dans `VerificationManuelleOuAVenir` de
`FeatureCoverageTests.cs` **avec sa raison**, et compléter le tableau « Zones sans test automatisé »
de [`docs/TRACEABILITE.md`](../../../docs/TRACEABILITE.md). Cette liste est un aveu : elle doit
rétrécir, et le quatrième garde-fou vérifie qu'elle ne contient rien d'inutile.
