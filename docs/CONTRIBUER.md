# Contribuer à Tray Shortcut

Ce guide décrit **comment on travaille dans ce dépôt**. Il tient en une règle : un comportement se
spécifie, puis se raconte, puis se teste, puis s'implémente — jamais l'inverse.

Pour l'architecture et le *pourquoi* des choix, voir [`SDD.md`](SDD.md) et [`adr/`](adr/). Pour
l'usage de l'application, le [`README`](../README.md).

---

## 1. La démarche : spec → Gherkin → test → code

| Étape | Où | Ce qu'on y écrit |
|---|---|---|
| 1. **Spécifier** | `docs/specs/SPEC-*.md` | une section `## SPEC-XXX-0NN` en *Étant donné / Quand / Alors*, suivie d'une liste « Règles » numérotée pour les cas limites |
| 2. **Raconter** | `docs/features/*.feature` | le même comportement en Gherkin français, chaque scénario taggé `@SPEC-XXX-0NN` |
| 3. **Tester** | `tests/CSharpTrayShortcut.Tests/` | un test NUnit qui échoue, portant `[Category("SPEC-XXX-0NN")]` |
| 4. **Implémenter** | `src/…` | le minimum pour rendre le test vert |
| 5. **Tracer** | `docs/TRACEABILITE.md`, `CHANGELOG.md` | la ligne spec → test, et l'entrée utilisateur |

L'identifiant de spec est le fil qui relie les cinq étapes. Il permet de rejouer un comportement
précis :

```powershell
dotnet test CSharp-TrayShortcut.slnx --filter TestCategory=SPEC-MENU-003
```

Familles d'identifiants : `SPEC-MENU` (composition du menu), `SPEC-ICON` (icônes),
`SPEC-LAUNCH` (lancement), `SPEC-CFG` (configuration), `SPEC-UI-LANG` (langue),
`SPEC-UI-ICON` (icône de l'application), `SPEC-APP` (cycle de vie du processus).

Un identifiant **ne se renumérote jamais** et ne se réutilise pas : il est cité dans les tests, les
scénarios, la traçabilité et le code.

Les fichiers `.feature` ne sont pas exécutés par un moteur : ils sont la formulation lisible de la
spec, et la preuve exécutable est le test NUnit qui porte le même identifiant. C'est pour cela que
le tag et la catégorie doivent être **strictement** identiques.

Un choix structurant (dépendance nouvelle, mécanisme de stockage, rupture de compatibilité) se
consigne dans un ADR : `docs/adr/000N-titre-court.md`, numérotation continue.

---

## 2. Architecture — la règle en une ligne

Les dépendances pointent **vers l'intérieur**, sans exception.

| Couche | Peut référencer | Cible | Contient |
|---|---|---|---|
| `src/CSharpTrayShortcut.Domain/` | **rien** | `net9.0` | entités, objets-valeur, énumérations, clés de textes |
| `src/CSharpTrayShortcut.Application/` | `Domain` + `Microsoft.Extensions.*.Abstractions` | `net9.0` | cas d'usage, règles, **ports** (`Abstractions/`), catalogue de textes |
| `src/CSharpTrayShortcut.Infrastructure/` | `Application` | `net9.0-windows` | disque, COM, processus, JSON, journal |
| `src/CSharpTrayShortcut.Ui/` | `Application` + `Infrastructure` | `net9.0-windows` | WinForms, images, racine de composition |
| `tests/CSharpTrayShortcut.Tests/` | `Domain` + `Application` | `net9.0` | NUnit, doubles dans `Doubles/` |

Ce que cette contrainte achète : les règles se testent sans Windows, sans disque et sans shell, et
la suite complète s'exécute en une fraction de seconde. La cible `net9.0` de `Application` en est le
gardien : un `using System.Windows.Forms` y est une **erreur de compilation**.

Interdits, avec leur remplacement :

| Interdit | À la place |
|---|---|
| accès au système de fichiers hors de `Infrastructure` | `IShortcutSource` |
| `System.Windows.Forms` / `System.Drawing` hors de `Ui` | remonter la décision dans `Application` ; pour une icône, produire un `IconSource` |
| `Process.Start` hors de `Infrastructure` | `IProcessLauncher`, via `LaunchService` |
| interopérabilité COM hors de `Infrastructure` | `IShortcutTargetResolver` |
| chemin de fichier en dur | `AppPaths` |
| phrase destinée à l'utilisateur hors des `.resx` | une clé dans `TextKeys`, un `TextRef` |
| **lever une exception depuis un gestionnaire d'événement** | rendre un booléen et journaliser — voir [ADR-0005](adr/0005-cibles-de-lancement.md) |

Ce dernier point n'est pas théorique : c'est ce qui faisait disparaître l'application sur un clic
malheureux.

---

## 3. Conventions de code

* **Français** : commentaires, documentation XML, messages d'erreur, libellés, noms de tests,
  entrées du journal des modifications. Les identifiants de code restent en anglais, comme le reste
  de l'écosystème .NET.
* **Documentation XML sur tout membre `public`** : `<summary>` qui dit *pourquoi*, `<remarks>` pour
  la décision de conception et la référence à la spec ou à l'ADR, `<inheritdoc />` sur une
  implémentation d'interface. `CS1591` étant neutralisé, le compilateur ne rappelle rien : c'est une
  discipline.
* **Commentaires** : ils expliquent une décision ou un piège, jamais ce que le code dit déjà.
* **Style** : `.editorconfig` fait loi — 4 espaces, **fins de ligne CRLF**, saut de ligne final,
  `namespace` de portée fichier, accolade sur une nouvelle ligne, champs privés en `_camelCase`.
* **Tests** : `[TestFixture]` sur une classe `sealed` ; `[Category("SPEC-…")]` sur la classe si elle
  ne couvre qu'une spec, sur chaque `[Test]` sinon ; noms de méthodes en français avec underscores
  décrivant le comportement attendu ; `Assert.That(...)` exclusivement, `Assert.Multiple` pour
  grouper ; **tout objet vient de `Doubles/Build.cs`**.

```csharp
[Test]
[Category("SPEC-MENU-003")]
public void Le_contenu_dun_sous_dossier_est_construit_a_son_ouverture()
```

Le jeu de tests minimal pour une règle nouvelle : cas nominal, cas limite qui l'exclut, et cas
dégradé (chemin vide, dossier illisible, cible disparue). Le message d'assertion dit **pourquoi** la
règle existe, pas ce qu'elle fait : c'est ce qu'on veut lire quand un test tombe deux ans plus tard.

Quatre garde-fous automatiques complètent cette discipline, dans
`tests/CSharpTrayShortcut.Tests/Features/FeatureCoverageTests.cs` et
`tests/CSharpTrayShortcut.Tests/Text/TextCatalogueTests.cs` : ils font échouer `dotnet test` si un
scénario cite une spec que plus aucun test ne vérifie, si une spec testée n'est racontée par aucun
scénario, si la liste des exemptions contient une entrée devenue inutile, ou si une clé de texte
n'est pas formulée dans les deux langues.

---

## 4. Checklist avant de committer

```powershell
dotnet restore CSharp-TrayShortcut.slnx
dotnet build CSharp-TrayShortcut.slnx -c Release
dotnet format CSharp-TrayShortcut.slnx --verify-no-changes
dotnet test CSharp-TrayShortcut.slnx
```

`TreatWarningsAsErrors` est **déjà** dans `Directory.Build.props` : tout `dotnet build` échoue au
premier avertissement, l'objectif de **0 avertissement** est donc tenu par la compilation
elle-même. Pour appliquer les corrections de format : `dotnet format CSharp-TrayShortcut.slnx`.

Attention, `dotnet format` **ne voit que le C#** : un `.md` ou un `.feature` écrit en LF passe sa
vérification. Le `.gitattributes` du dépôt (`* text=auto eol=CRLF`) rend le comportement
déterministe.

La tâche VS Code **« tout vérifier »** enchaîne les trois contrôles en une fois.

Puis :

- [ ] spec, scénario Gherkin, test et ligne de traçabilité en place pour chaque comportement
      nouveau ou modifié
- [ ] `CHANGELOG.md` complété, formulé côté utilisateur
- [ ] `README.md` à jour si le changement est visible
- [ ] règles de dépendance respectées (§2)
- [ ] aucun chemin absolu de poste de travail, aucun `config.json`
- [ ] aucun `bin/`, `obj/`, `publish/` ni `TestResults/` ajouté au dépôt

---

## 5. Ajouter une commande au menu — la version courte

1. `docs/specs/SPEC-MENU.md` — compléter SPEC-MENU-001, règle 5
2. `docs/features/menu.feature` — un scénario taggé
3. `tests/…/Menu/MenuComposerTests.cs` — le test, **avant** le code
4. `src/…/Domain/Menu/MenuCommand.cs` — la valeur (l'ordre de déclaration est l'ordre d'affichage)
5. `src/…/Application/Text/Strings.resx` **et** `Strings.en.resx` — la formulation, clé
   `Menu.<Valeur>`. Le test de couverture des clés échoue si l'une manque
6. `src/…/Ui/Tray/TrayApplicationContext.cs` — un cas dans `Execute`
7. `docs/TRACEABILITE.md`, `CHANGELOG.md`

`MenuComposer` n'est **pas** à modifier : il parcourt `Enum.GetValues<MenuCommand>()`.
