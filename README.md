# CSharp-TrayShortcut

Application Windows résidente qui **présente le contenu d'un dossier sous forme de menu** dans la
zone de notification, et **lance ce qu'on y clique**. Elle remplace les barres d'outils
personnalisables de la barre des tâches, disparues avec Windows 11.

```
┌─ Tray Shortcut ────────────────────────────┐
│  Bureautique                        ▸      │
│  Développement                      ▸      │
│  Éditeurs                           ▸      │
│  notepad                                   │
├────────────────────────────────────────────┤
│  Raccourcis personnalisés           ▸      │
├────────────────────────────────────────────┤
│  Actualiser                                │
│  Modifier…                                 │
│  Quitter                                   │
└────────────────────────────────────────────┘
```

*Interface en français ou en anglais. Lecture seule : l'application n'écrit jamais dans le dossier
surveillé.*

---

## Sommaire

1. [Ce que ça fait](#ce-que-ça-fait)
2. [Démarrage rapide](#démarrage-rapide)
3. [Configuration](#configuration)
4. [Comment ça marche](#comment-ça-marche)
5. [Architecture du code](#architecture-du-code)
6. [Tests et documentation vivante](#tests-et-documentation-vivante)
7. [Contribuer](#contribuer)
8. [Publier une version](#publier-une-version)
9. [Dépannage](#dépannage)
10. [Documentation de conception](#documentation-de-conception)
11. [Limites connues](#limites-connues)
12. [Licence](#licence)

---

## Ce que ça fait

| Fonction | Détail |
|---|---|
| **Dérouler un dossier en menu** | Les sous-dossiers deviennent des sous-menus, à la profondeur qu'on veut |
| **Lancer d'un clic** | Par le shell de Windows : chaque fichier s'ouvre avec son application associée |
| **Reconnaître les icônes** | Un raccourci `.lnk` affiche l'icône de **sa cible**, pas une flèche générique |
| **Raccourcis personnalisés** | Des entrées vers des cibles hors du dossier surveillé, avec argument facultatif |
| **Dossiers et adresses** | Un raccourci peut viser un dossier, ou une adresse `http`, `https`, `mailto` |
| **Ouverture instantanée** | Un seul niveau est lu à la fois — un partage réseau ne ralentit pas le démarrage |
| **Deux langues** | Français, anglais, ou selon Windows |

## Démarrage rapide

**Prérequis** : Windows 10 ou 11, et le [runtime .NET 9 Desktop (x64)](https://dotnet.microsoft.com/download/dotnet/9.0).
La version autonome n'en a pas besoin.

1. Télécharger l'archive de la [dernière version](../../releases/latest) et la décompresser où vous
   voulez.
2. Lancer `TrayShortcut.exe`. Une icône apparaît dans la zone de notification — éventuellement
   derrière la flèche des icônes masquées.
3. Au premier démarrage, l'application demande **quel dossier surveiller**. Choisissez celui où vous
   rangez vos raccourcis.
4. Clic droit sur l'icône : le menu déroule votre dossier.

Depuis les sources :

```powershell
dotnet run --project src/CSharpTrayShortcut.Ui
```

## Configuration

Tout se règle dans `%APPDATA%\TrayShortcut\config.json`. Les raccourcis personnalisés se modifient
depuis le menu (*Modifier…*) ; le reste s'édite à la main. **Aucun redémarrage n'est nécessaire** :
*Actualiser* relit tout.

| Réglage | Rôle |
|---|---|
| `Path` | Dossier dont le contenu devient le menu |
| `ShowRootFiles` | Afficher les fichiers situés à la racine du dossier. Absent = `true` |
| `PathFolderIcon` | Icône affichée devant chaque dossier (`.ico`). Un nom seul désigne une icône livrée |
| `PathTrayIcon` | Icône de la zone de notification (`.ico`) |
| `Language` | `System` (suit Windows), `French`, `English` |
| `CustomShortcuts` | Entrées supplémentaires, regroupées dans leur propre section |

Chaque raccourci personnalisé porte quatre champs :

| Champ | Rôle |
|---|---|
| `Path` | Ce qu'il faut ouvrir : fichier, dossier, ou adresse. **Sans lui, la ligne est ignorée** |
| `Text` | Intitulé affiché. Absent, le nom du fichier visé en tient lieu |
| `Argument` | Argument passé au lancement |
| `Image` | Icône `.ico` à afficher. Absent, celle de la cible est extraite |

### Exemple

```json
{
  "Path": "D:\\Toolbar",
  "ShowRootFiles": true,
  "PathFolderIcon": "folder_w11.ico",
  "PathTrayIcon": "tray-shortcut.ico",
  "Language": "System",
  "CustomShortcuts": [
    {
      "Text": "Notepad++",
      "Path": "C:\\Program Files\\Notepad++\\notepad++.exe"
    },
    {
      "Text": "Mes notes",
      "Path": "C:\\Windows\\notepad.exe",
      "Argument": "D:\\notes.txt"
    },
    {
      "Text": "Wiki de l'équipe",
      "Path": "https://example.org/wiki",
      "Image": "wiki.ico"
    },
    {
      "Text": "Projets",
      "Path": "D:\\Projets"
    }
  ]
}
```

Les icônes livrées avec l'application (`folder_w10.ico`, `folder_w11.ico`, `tray-shortcut.ico`) se
désignent par leur seul nom. Toute autre icône demande un chemin complet.

## Comment ça marche

À chaque ouverture du menu :

1. la configuration est relue — un fichier absent ou abîmé donne des valeurs par défaut ;
2. la langue est alignée sur le réglage et sur Windows ;
3. si le dossier surveillé manque, le sélecteur de dossier s'ouvre — et si vous annulez, le menu se
   limite aux trois commandes plutôt que d'insister ;
4. le **premier niveau seulement** est énuméré ; chaque sous-dossier est lu à sa première ouverture ;
5. au clic, la cible est réexaminée puis confiée au shell.

Entre deux ouvertures de menu, **rien ne tourne** : pas de minuterie, pas de surveillance de dossier,
pas de thread de fond.

Un dossier auquel vous n'avez pas droit, ou un lecteur réseau déconnecté, apparaît vide et n'empêche
rien d'autre de fonctionner.

## Architecture du code

Quatre projets, dépendances tournées vers l'intérieur
([ADR-0001](docs/adr/0001-quatre-couches-pour-mille-lignes.md)) :

```
src/
  CSharpTrayShortcut.Domain/          MenuEntry, LaunchTarget, IconSource, TextKeys — aucune dépendance
  CSharpTrayShortcut.Application/     MenuComposer, IconSourceResolver, LaunchService, PORTS
  CSharpTrayShortcut.Infrastructure/  disque, IShellLink (COM), processus, JSON, journal
  CSharpTrayShortcut.Ui/              WinForms, rendu du menu, images, composition
tests/
  CSharpTrayShortcut.Tests/           NUnit
```

`Domain` et `Application` ciblent `net9.0` et non `net9.0-windows` : un `using
System.Windows.Forms` y est une **erreur de compilation**. La règle d'architecture est donc vérifiée
par le compilateur, pas par la relecture.

Le menu est décrit comme une **donnée** — une liste de `MenuEntry` — que la présentation traduit en
éléments WinForms. C'est ce qui rend l'ordre, la présence des entrées et le choix des icônes
vérifiables sans écran.

## Tests et documentation vivante

```powershell
dotnet test CSharp-TrayShortcut.slnx

# Rejouer un comportement précis
dotnet test CSharp-TrayShortcut.slnx --filter TestCategory=SPEC-MENU-003
```

92 tests, une centaine de millisecondes, sans écran ni disque. Chaque comportement porte un
identifiant qui relie sa **spec** ([`docs/specs/`](docs/specs/)), son **scénario Gherkin**
([`docs/features/`](docs/features/)), son **test** et sa ligne de
[traçabilité](docs/TRACEABILITE.md).

Quatre des tests sont des garde-fous de documentation : ils échouent si un scénario cite une spec que
plus aucun test ne vérifie, si une spec testée n'est racontée par aucun scénario, ou si une
formulation manque dans l'une des deux langues. La documentation ne peut donc pas dériver en silence.

## Contribuer

La démarche est **spec → Gherkin → test → code**, détaillée dans
[`docs/CONTRIBUER.md`](docs/CONTRIBUER.md). Le dépôt fournit des *skills* et des *subagents* de
relecture pour Claude Code dans [`.claude/`](.claude/).

Avant de pousser, la tâche VS Code **« tout vérifier »**, ou :

```powershell
dotnet build  CSharp-TrayShortcut.slnx -c Release   # 0 avertissement exigé
dotnet format CSharp-TrayShortcut.slnx --verify-no-changes
dotnet test   CSharp-TrayShortcut.slnx
```

## Publier une version

```powershell
.\scripts\publier.ps1              # version légère (runtime .NET 9 Desktop requis)
.\scripts\publier.ps1 -Autonome    # exécutable unique, aucun runtime à installer
```

Sur GitHub, pousser un tag `v*` déclenche la publication complète — voir
[`docs/CI.md`](docs/CI.md) §7.

## Dépannage

| Symptôme | Piste |
|---|---|
| Aucune icône n'apparaît | Elle est peut-être derrière la flèche des icônes masquées de la zone de notification |
| Le menu ne contient que trois commandes | Le dossier surveillé est absent ou inaccessible. *Modifier…* affiche le chemin du fichier de configuration |
| Un sous-dossier paraît vide | Droits de lecture, ou lecteur réseau déconnecté. La raison est dans `%APPDATA%\TrayShortcut\log.txt` |
| Un clic ne fait rien | La cible a disparu, ou le système a refusé de l'ouvrir. Voir le journal |
| Une icône manque | Fichier `.ico` invalide ou introuvable ; l'entrée reste cliquable |
| L'application s'est fermée seule | Un rapport `crash-*.txt` se trouve dans `%APPDATA%\TrayShortcut\` |
| Deux icônes après une mise à jour | Une ancienne version tourne encore ; quittez-la par son menu |

Le dossier de données se réinitialise en supprimant `%APPDATA%\TrayShortcut\config.json`.

## Documentation de conception

* [SDD](docs/SDD.md) — conception d'ensemble : contexte, exigences, patrons, extensibilité, risques
* [Spécifications](docs/specs/) — les comportements, un identifiant par comportement
* [Scénarios Gherkin](docs/features/) — les mêmes, racontés du point de vue de l'utilisateur
* [Traçabilité](docs/TRACEABILITE.md) — spec → test, et les zones vérifiées à la main
* [Contribuer](docs/CONTRIBUER.md) — la démarche et les conventions
* [Intégration continue](docs/CI.md) — pipelines et reproduction en local
* [Décisions (ADR)](docs/adr/) — les cinq choix structurants et leur pourquoi

## Limites connues

* **Un seul dossier surveillé** à la fois.
* **Pas de raccourci clavier global** pour ouvrir le menu.
* **Pas de démarrage automatique avec Windows** intégré : à faire par un raccourci dans le dossier
  `shell:startup`.
* **Pas de recherche** dans le menu.
* L'application **ne modifie jamais** le dossier surveillé : elle lit et lance.
* Le rendu du menu, l'extraction d'icônes et le mutex d'instance unique **n'ont pas de tests
  automatisés** — la liste et le mode de vérification manuelle sont dans
  [`docs/TRACEABILITE.md`](docs/TRACEABILITE.md).

## Licence

```text
/*
 * ----------------------------------------------------------------------------
 * "LICENCE BEERWARE" (Révision 42):
 * kevingrillet a créé ce fichier. Tant que vous conservez cet avertissement,
 * vous pouvez faire ce que vous voulez de ce truc. Si on se rencontre un jour et
 * que vous pensez que ce truc vaut le coup, vous pouvez me payer une bière en
 * retour. Poul-Henning Kamp
 * ----------------------------------------------------------------------------
 */
```
