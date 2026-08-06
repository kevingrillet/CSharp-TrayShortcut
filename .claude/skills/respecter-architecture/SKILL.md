---
name: respecter-architecture
description: "Décider dans quelle couche va un fichier, un `using`, une référence ou un appel technique, avant de l'écrire. À utiliser dès qu'on crée un fichier dans `src/`, qu'on ajoute un `using`, qu'on hésite entre « une règle » et « du rendu », ou qu'on s'apprête à toucher au disque, au shell, à COM, à `System.Drawing` ou à `Process.Start`."
---

# Respecter l'architecture

Quatre couches, dépendances vers l'intérieur ([ADR-0001](../../../docs/adr/0001-quatre-couches-pour-mille-lignes.md)).
Cette fiche répond à une seule question : **où va ce que je m'apprête à écrire**.

La règle de dépendance elle-même est dans [`docs/CONTRIBUER.md`](../../../docs/CONTRIBUER.md) §2.
**Ne pas la recopier.**

## 1. Le test qui tranche

> **Cette décision pourrait-elle être vérifiée par un test sans écran, sans disque et sans shell ?**

* **Oui** → c'est une règle : `Application` (ou `Domain` si c'est de la donnée pure).
* **Non** → c'est de la technique : `Infrastructure` si c'est un accès au monde extérieur, `Ui` si
  c'est du rendu.

Le piège classique est de confondre les deux parce qu'elles se touchent. Trois exemples réels de ce
dépôt :

| Ce qu'on veut faire | Décision (Application) | Technique |
|---|---|---|
| Montrer l'icône d'un `.lnk` | *quelle* icône : celle de la cible, sinon celle du raccourci — `IconSourceResolver` | *lire* la cible (COM) ; *fabriquer* l'image (`System.Drawing`) |
| Trier le menu | l'ordre, et lequel — `MenuComposer` | énumérer le dossier — `DirectoryShortcutSource` |
| Lancer un élément | ce qui constitue une cible valide — `LaunchService` | `Process.Start` — `ShellProcessLauncher` |

Si les deux moitiés se retrouvent dans la même méthode, la règle devient intestable : c'était
exactement l'état de l'ancienne classe de 259 lignes.

## 2. Table de décision

| Ce que j'écris | Couche | Dossier |
|---|---|---|
| Un type de donnée sans comportement (entité, objet-valeur, énumération) | `Domain` | `Shortcuts/`, `Menu/`, `Text/` |
| Une clé de texte | `Domain` | `Text/TextKeys.cs` |
| Une règle, un cas d'usage | `Application` | `Menu/`, `Launching/`, `Configuration/` |
| Une **interface** vers le monde extérieur | `Application` | `Abstractions/` |
| Une formulation destinée à l'utilisateur | `Application` | `Text/Strings.resx` **et** `Strings.en.resx` |
| Un accès disque, réseau, registre, COM, processus | `Infrastructure` | `FileSystem/`, `Shell/`, `Processes/`, `Persistence/`, `Logging/` |
| Un `Bitmap`, un `Icon`, un `ToolStripItem`, un `Form` | `Ui` | `Icons/`, `Tray/`, `Views/` |
| Un enregistrement de service | la couche concernée | `DependencyInjection/`, ou `Ui/Composition/` |

## 3. Balayages de contrôle

À lancer après toute modification dans `src/`. Chacun doit **ne rien rendre**.

```powershell
# Aucune trace de présentation hors de Ui
Select-String -Path src\CSharpTrayShortcut.Domain\*.cs, src\CSharpTrayShortcut.Application\*.cs `
    -Recurse -Pattern 'System\.Windows\.Forms|System\.Drawing'

# Aucun accès direct au monde extérieur hors de Infrastructure
Select-String -Path src\CSharpTrayShortcut.Domain\*.cs, src\CSharpTrayShortcut.Application\*.cs `
    -Recurse -Pattern 'Directory\.|File\.|Process\.Start|Registry|ComImport'

# Aucune phrase destinée à l'utilisateur en dehors des ressources
Select-String -Path src\CSharpTrayShortcut.Domain\*.cs, src\CSharpTrayShortcut.Application\*.cs `
    -Recurse -Pattern '"[A-ZÉÀ][a-zéèàêç ]{6,}"'
```

Le premier balayage est en principe redondant : `Application` cible `net9.0`, donc un
`using System.Windows.Forms` **ne compile pas**. Le garder coûte une seconde et attrape le cas où
quelqu'un aurait « corrigé » la cible du projet pour faire passer une erreur.

Exceptions légitimes au deuxième : `System.IO.Path` est de la manipulation de chaînes, sans accès
disque — `Path.GetFileName` et `Path.GetExtension` sont autorisés dans `Application`.

## 4. Pièges rencontrés dans ce dépôt

* **`Application` désigne deux choses.** La couche du dépôt et `System.Windows.Forms.Application`.
  Dans `Ui`, employer l'alias `WinFormsApplication`, comme `Program.cs` et
  `TrayApplicationContext.cs`. Ne jamais renommer la couche pour cette raison.
* **Un `record` scellé ne se convertit pas vers une interface COM.** La coclasse `ShellLink` de
  `ShellLinkTargetResolver` est délibérément non scellée : sur un type scellé, le compilateur connaît
  statiquement les interfaces implémentées et refuse la conversion (CS0030).
* **Une exception depuis un gestionnaire d'événement ferme l'application.** Elle remonte au
  gestionnaire d'exceptions non gérées, qui écrit un rapport et termine le processus. Rendre un
  booléen et journaliser ([ADR-0005](../../../docs/adr/0005-cibles-de-lancement.md)).
* **Qui possède une image ?** `IconRenderer` et son cache, jamais l'élément de menu — un
  `ToolStripItem` ne libère pas son image, et `MenuRenderer` n'en libère aucune
  ([ADR-0003](../../../docs/adr/0003-icone-source-et-non-image.md),
  [ADR-0006](../../../docs/adr/0006-cache-des-icones.md)). Un `Dispose` sur une image obtenue de
  `RenderBitmap` est un défaut : elle est partagée.
* **L'éviction du cache d'icônes n'a lieu que dans `BeginRender`.** C'est le seul instant où aucune
  image n'est référencée par un menu vivant. Libérer ailleurs — dans une expansion paresseuse, sur
  une minuterie, à l'ajout d'une entrée — ferait peindre une image détruite, avec une exception
  très loin de sa cause. Toute modification de `IconRenderer` doit préserver cette invariante.
* **Un commentaire XML ne peut pas contenir `--`.** Documenter une option de ligne de commande dans
  un `.csproj` provoque une erreur `MSB4025` illisible.

## 5. Ajouter un port

Quand une règle a besoin de quelque chose que la couche application ne peut pas faire :

1. déclarer l'interface dans `Application/Abstractions/`, avec un **contrat de tolérance
   explicite** : que rend-elle quand ça échoue ? Les quatre ports existants ne lèvent jamais — ils
   rendent une liste vide, `null`, ou `false` ;
2. l'implémenter dans `Infrastructure/`, en y concentrant l'absorption des exceptions ;
3. l'enregistrer dans `InfrastructureServiceCollectionExtensions` ;
4. écrire le double dans `tests/…/Doubles/`, et l'exposer par `Build.cs`.

Le contrat de tolérance est le point important : c'est ce qui fait qu'un dossier illisible ne
remonte pas jusqu'au menu (SPEC-MENU-004). Le décider au moment de déclarer l'interface, pas au
moment de l'implémenter.
