# Traçabilité : spec → scénario → test

Chaque comportement spécifié est illustré par un **scénario Gherkin** ([`features/`](features/))
et vérifié par au moins un **test** portant la catégorie de même identifiant.

```powershell
dotnet test CSharp-TrayShortcut.slnx --filter TestCategory=SPEC-MENU-003
```

Des tests de garde (`tests/CSharpTrayShortcut.Tests/Features/FeatureCoverageTests.cs`)
maintiennent ce tableau honnête : ils échouent si un scénario cite une spec sans test, si une spec
testée n'est illustrée par aucun scénario, ou si la liste blanche ci-dessous contient une entrée
devenue inutile.

## Composition du menu

| Spec | Comportement | Tests |
|---|---|---|
| SPEC-MENU-001 | Contenu du menu racine | `Menu/MenuComposerTests.cs` |
| SPEC-MENU-002 | Ordre et intitulés | `Menu/MenuComposerTests.cs` |
| SPEC-MENU-003 | Construction à la demande | `Menu/MenuComposerTests.cs` |
| SPEC-MENU-004 | Un dossier illisible n'emporte pas le menu | `Menu/MenuComposerTests.cs` |
| SPEC-MENU-005 | Raccourcis personnalisés | `Menu/MenuComposerTests.cs` |

## Icônes

| Spec | Comportement | Tests |
|---|---|---|
| SPEC-ICON-001 | Icône d'un élément lançable | `Menu/IconSourceResolverTests.cs` |
| SPEC-ICON-002 | Icône de dossier et de la zone de notification | `Menu/IconSourceResolverTests.cs` |
| SPEC-ICON-003 | Raccourcis Windows (`.lnk`) | `Menu/IconSourceResolverTests.cs` |
| SPEC-ICON-004 | Réutilisation et fabrication de l'image | `Menu/IconCachePolicyTests.cs` (règles 1 à 3 : clé de réutilisation) ; fabrication et éviction : voir § Zones sans test automatisé |
| SPEC-UI-ICON-001 | Icône de l'application et info-bulle | — (idem) ; code : `Ui/CSharpTrayShortcut.Ui.csproj`, `Ui/Tray/TrayApplicationContext.cs` |

## Lancement

| Spec | Comportement | Tests |
|---|---|---|
| SPEC-LAUNCH-001 | Ouvrir un élément | `Launching/LaunchServiceTests.cs` |
| SPEC-LAUNCH-002 | Cible disparue ou refusée | `Launching/LaunchServiceTests.cs` |
| SPEC-LAUNCH-003 | Ce qui constitue une cible valide | `Launching/LaunchServiceTests.cs` |

## Configuration

| Spec | Comportement | Tests |
|---|---|---|
| SPEC-CFG-001 | Emplacement et tolérance | `Configuration/ConfigurationTests.cs` (valeurs par défaut, format du fichier) ; disque : voir § Zones sans test automatisé |
| SPEC-CFG-002 | Dossier surveillé manquant | `Configuration/ConfigurationTests.cs` |
| SPEC-CFG-003 | Édition des raccourcis personnalisés | `Configuration/ConfigurationTests.cs` (normalisation) ; interface : `Ui/Views/EditForm.cs` |
| SPEC-CFG-004 | Prise d'effet | — (voir § Zones sans test automatisé) ; code : `Ui/Tray/TrayApplicationContext.cs` |

## Langue et cycle de vie

| Spec | Comportement | Tests |
|---|---|---|
| SPEC-UI-LANG-001 | Choix de la langue | `Text/LanguageResolverTests.cs` |
| SPEC-UI-LANG-002 | Où vivent les formulations | `Text/TextCatalogueTests.cs` |
| SPEC-APP-001 | Instance unique | — (voir § Zones sans test automatisé) ; code : `Ui/Program.cs` |
| SPEC-APP-002 | Erreur inattendue | — (idem) ; code : `Ui/Program.cs` |
| SPEC-APP-003 | Journal | — (idem) ; code : `Infrastructure/Logging/FileLoggerProvider.cs` |

---

## Zones sans test automatisé

Ces specs sont documentées et implémentées, mais aucun test unitaire ne porte leur catégorie.
Chaque entrée figure dans la liste `VerificationManuelleOuAVenir` de `FeatureCoverageTests.cs`,
avec sa raison. **Cette liste est un aveu, pas une commodité : elle doit rétrécir.**

| Spec | Pourquoi | Comment c'est vérifié |
|---|---|---|
| SPEC-CFG-004 | Rechargement complet à chaud, orchestré autour d'un `NotifyIcon`. Les règles enchaînées (validation, composition, icônes, langue) sont couvertes séparément. | Lancer l'application, modifier `config.json` à la main, *Actualiser* |
| SPEC-ICON-004 (règles 4 et 5 seulement) | La **clé de réutilisation** est testée ; la fabrication par `System.Drawing` et l'éviction du cache demandent un écran et un fichier `.ico` réellement tronqué. | Désigner une icône inexistante puis un fichier texte renommé en `.ico` : le menu doit rester lisible. Actualiser vingt fois de suite : les handles graphiques du processus ne doivent pas croître |
| SPEC-UI-ICON-001 | Icône de l'exécutable et info-bulle de la zone de notification : rendu Windows. | Regarder l'icône dans l'explorateur, survoler celle de la zone de notification |
| SPEC-APP-001 | Mutex nommé, donc comportement du processus. | Lancer l'application deux fois : une seule icône |
| SPEC-APP-002 | Gestionnaire d'exceptions non gérées et écriture d'un rapport. | Vérifié lors des modifications de `Program.cs` ; un rapport doit apparaître dans `%APPDATA%\TrayShortcut` |
| SPEC-APP-003 | Écriture sur le disque réel et rotation à 1 Mo. | Consulter `log.txt` après avoir ouvert un menu contenant un dossier illisible |

### Ce qu'il serait raisonnable de couvrir ensuite

Deux de ces six lignes sont à portée sans harnais d'interface :

* **SPEC-CFG-001 côté disque** et **SPEC-APP-003** deviendraient testables en extrayant un port de
  système de fichiers pour `JsonFileStore` et `FileLoggerProvider`, comme `IShortcutSource` l'a
  fait pour l'énumération. Le rapport coût / bénéfice est correct : ce sont les deux endroits où
  une défaillance est silencieuse.
* **SPEC-CFG-004** deviendrait testable en extrayant de `TrayApplicationContext` l'enchaînement
  « relire, valider, composer » dans un service de la couche application, ne laissant à la
  présentation que l'application du résultat.

Les quatre autres — rendu, icônes, mutex, gestionnaire d'exceptions — resteront vérifiées à la
main : les rendre automatiques demanderait un harnais disproportionné pour un utilitaire de cette
taille.
