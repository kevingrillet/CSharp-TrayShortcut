# Scénarios Gherkin — documentation vivante

Ce dossier contient la traduction des spécifications de [`../specs/`](../specs/) en scénarios
Gherkin francophones.

## À quoi ça sert

Ces fichiers sont **de la documentation, pas des tests exécutables**. Aucun runner BDD ne les
joue (voir plus bas). Ils servent à trois choses :

1. **Décrire les comportements du point de vue de l'utilisateur.** Les specs sont écrites pour
   être précises ; les scénarios sont écrits pour être lus — y compris par quelqu'un qui n'ouvrira
   jamais le code. On y parle de menus, de dossiers et de raccourcis, jamais de classes, de
   `ToolStripMenuItem` ou de handles GDI.
2. **Servir de point de départ à une évolution.** Avant de coder, on écrit le scénario : s'il est
   difficile à formuler simplement, c'est en général que le comportement demandé est mal cadré.
3. **Rester vérifiablement en phase avec les tests.** Des tests de garde échouent si un scénario
   parle d'une spec que plus aucun test ne couvre, si une spec couverte par un test n'est
   illustrée par aucun scénario, ou si la liste des exemptions contient une entrée devenue inutile
   (voir [`FeatureCoverageTests.cs`](../../tests/CSharpTrayShortcut.Tests/Features/FeatureCoverageTests.cs)).

La persona est toujours la même : **Camille** utilise l'application, **Alice** est sa collègue,
le dossier surveillé d'exemple est `D:\Toolbar` et ses sous-dossiers `Bureautique` et
`Développement`.

## Les fichiers

| Fichier | Contenu | Specs illustrées |
|---|---|---|
| [`menu.feature`](menu.feature) | Ce qu'on trouve dans le menu, dans quel ordre, et à quel moment c'est calculé | `SPEC-MENU-001` à `SPEC-MENU-005` |
| [`icones.feature`](icones.feature) | Quelle icône est montrée, et ce qui se passe quand elle manque | `SPEC-ICON-001` à `SPEC-ICON-004`, `SPEC-UI-ICON-001` |
| [`lancement.feature`](lancement.feature) | Ce qui s'ouvre au clic, et ce qui se passe quand la cible a disparu | `SPEC-LAUNCH-001` à `SPEC-LAUNCH-003` |
| [`configuration.feature`](configuration.feature) | Emplacement du fichier, dossier manquant, fenêtre d'édition, prise d'effet | `SPEC-CFG-001` à `SPEC-CFG-004` |
| [`application.feature`](application.feature) | Langue de l'interface, instance unique, erreurs imprévues, journal | `SPEC-UI-LANG-001`, `-002`, `SPEC-APP-001` à `SPEC-APP-003` |

Chaque fichier reste volontairement court : un scénario par comportement observable, une
quinzaine au maximum par fichier. Les variantes d'un même comportement (schémas d'adresse,
résolution de la langue) sont regroupées dans un `Plan du Scénario` plutôt que dupliquées.

## Convention d'étiquettes

Chaque scénario porte en étiquette l'identifiant de la spec qu'il illustre :

```gherkin
  @SPEC-MENU-003
  Scénario: Le contenu d'un sous-dossier est lu à son ouverture
```

C'est la même étiquette que la catégorie portée par les tests NUnit :

```csharp
[Test]
[Category("SPEC-MENU-003")]
public void Le_contenu_dun_sous_dossier_est_construit_a_son_ouverture()
```

Un scénario peut porter plusieurs étiquettes lorsqu'il illustre l'articulation de deux specs.

## Retrouver le test qui vérifie un scénario

```powershell
dotnet test CSharp-TrayShortcut.slnx --filter TestCategory=SPEC-MENU-003
```

Pour retrouver le code du test plutôt que l'exécuter, une recherche de `SPEC-MENU-003` dans
`tests/` suffit. La correspondance spec → fichier de test est aussi récapitulée dans
[`../TRACEABILITE.md`](../TRACEABILITE.md).

## Pourquoi pas Reqnroll (ou un autre runner BDD) ?

Ces scénarios ne sont pas branchés sur des *step definitions*. C'est un choix, pour l'instant :

- **Le coût dépasse le bénéfice à cette échelle.** L'application est un utilitaire de bureau de
  mille lignes, maintenu par une personne. Les tests NUnit existants sont déjà lisibles et nommés
  en français ; les doubler d'une couche de phrases et de définitions de pas ajouterait un étage
  d'indirection à maintenir sans rien vérifier de plus.
- **La granularité n'est pas la même.** Un scénario décrit un comportement utilisateur ; les
  tests, eux, descendent au niveau de la règle, ce qui permet des messages d'échec précis.
- **Une partie des scénarios n'est pas automatisable** en l'état : rendu des menus WinForms,
  extraction d'icônes, mutex nommé, écriture dans le dossier de données. Les rendre exécutables
  demanderait un harnais d'interface disproportionné (voir la section « Zones sans test
  automatisé » de [`../TRACEABILITE.md`](../TRACEABILITE.md)).

**Évolution possible.** Si le projet gagne des contributeurs, brancher
[Reqnroll](https://reqnroll.net/) est la suite naturelle : les fichiers de ce dossier sont déjà du
Gherkin valide, en français, avec `# language: fr` en tête.

## Ajouter ou modifier un scénario

1. Écrire ou compléter la spec dans [`../specs/`](../specs/) — c'est elle qui fait foi.
2. Écrire le scénario ici, étiqueté avec l'identifiant de la spec.
3. Écrire le test NUnit correspondant avec la catégorie de même nom.
4. Si le comportement n'est pas automatisable, ajouter son identifiant à la liste documentée en
   tête de
   [`FeatureCoverageTests.cs`](../../tests/CSharpTrayShortcut.Tests/Features/FeatureCoverageTests.cs),
   en expliquant pourquoi.
