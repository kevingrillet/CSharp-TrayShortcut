# SDD — Software Design Document — Tray Shortcut

> Document de conception. Il décrit **quoi** et **pourquoi**.
> Les comportements testables sont dans [`specs/`](specs/), la correspondance spec → test dans
> [`TRACEABILITE.md`](TRACEABILITE.md).

| | |
|---|---|
| **Version** | 1.0 |
| **Statut** | Implémenté |
| **Plateforme** | Windows 10 / 11, .NET 9 |
| **Type** | Application de bureau résidente (zone de notification) |

---

## 1. Contexte et problème

Les barres d'outils personnalisables de la barre des tâches Windows ont disparu avec Windows 11.
Elles permettaient d'épingler un dossier et d'en dérouler le contenu en menu : le moyen le plus
direct d'atteindre une vingtaine d'outils rangés à la main, sans les épingler un par un ni
encombrer le bureau.

Conséquence observée : on ouvre l'explorateur, on navigue jusqu'au dossier, on double-clique. Une
dizaine de fois par jour, pour un geste qui en demandait deux.

## 2. Objectif

Une application légère, résidente dans la zone de notification, qui **présente le contenu d'un
dossier sous forme de menu** et **lance ce qu'on y clique**, en respectant l'organisation en
sous-dossiers que l'utilisateur a déjà faite.

### 2.1 Exigences fonctionnelles

| # | Exigence | Spec |
|---|---|---|
| EF-1 | Dérouler en menu l'arborescence d'un dossier choisi | SPEC-MENU-001, SPEC-MENU-003 |
| EF-2 | Lancer un élément d'un clic, par son application associée | SPEC-LAUNCH-001 |
| EF-3 | Reconnaître les éléments à leur icône, celle de leur cible pour un raccourci | SPEC-ICON-001, SPEC-ICON-003 |
| EF-4 | Ajouter des raccourcis vers des cibles hors du dossier surveillé | SPEC-MENU-005 |
| EF-5 | Passer un argument à un raccourci personnalisé | SPEC-LAUNCH-001 |
| EF-6 | Lancer aussi un dossier ou une adresse web | SPEC-LAUNCH-003 |
| EF-7 | Choisir de masquer les fichiers situés à la racine | SPEC-MENU-001 |
| EF-8 | Personnaliser l'icône des dossiers et celle de la zone de notification | SPEC-ICON-002 |
| EF-9 | Modifier les raccourcis personnalisés depuis une petite fenêtre | SPEC-CFG-003 |
| EF-10 | Prendre en compte un changement de configuration sans redémarrer | SPEC-CFG-004 |
| EF-11 | Interface en français ou en anglais, ou selon Windows | SPEC-UI-LANG-001 |

### 2.2 Exigences non fonctionnelles

| # | Exigence | Décision de conception |
|---|---|---|
| ENF-1 | Le menu s'ouvre instantanément, même sur un partage réseau | Énumération d'un niveau à la fois, à la demande — SPEC-MENU-003 |
| ENF-2 | Un dossier inaccessible ne casse pas le reste | Tolérance concentrée dans l'adaptateur de système de fichiers — SPEC-MENU-004 |
| ENF-3 | L'application ne disparaît jamais sur un clic malheureux | Aucune exception depuis un gestionnaire d'événement — SPEC-LAUNCH-002, [ADR-0005](adr/0005-cibles-de-lancement.md) |
| ENF-4 | Fonctionner installée n'importe où, `Program Files` compris | Données dans `%APPDATA%` — [ADR-0002](adr/0002-donnees-dans-appdata.md) |
| ENF-5 | Aucune fuite de ressources graphiques à l'usage | Le rendu possède les images et les libère — [ADR-0003](adr/0003-icone-source-et-non-image.md) |
| ENF-6 | Empreinte mémoire et CPU négligeables | Aucune fenêtre au démarrage, aucun sondage, aucun travail hors ouverture de menu |
| ENF-7 | Comportements vérifiables | Cœur sans dépendance Windows, testé unitairement — [ADR-0001](adr/0001-quatre-couches-pour-mille-lignes.md) |

### 2.3 Hors périmètre

Créer, déplacer ou supprimer des fichiers (l'application ne fait que lire et lancer) ; surveiller
plusieurs dossiers à la fois ; raccourcis clavier globaux ; démarrage automatique avec Windows ;
recherche dans le menu ; thème clair / sombre (le menu suit celui de Windows sans code de notre
part).

---

## 3. Vue d'ensemble

```
                    ┌──────────────────────────── Windows ────────────────────────────┐
                    │  Zone de notification          Shell (ShellExecute)             │
                    │        ▲                            ▲                           │
                    └────────┼────────────────────────────┼───────────────────────────┘
                             │ icône + menu               │ clic → application associée
                    ┌────────┴────────────────────────────┴───────────────────────────┐
                    │  CSharpTrayShortcut.Ui (WinForms)                               │
                    │  TrayApplicationContext · MenuRenderer · IconRenderer            │
                    │  EditForm · TextService · racine de composition (DI)             │
                    └────────────────────────────┬───────────────────────────────────┘
                                                 │ appelle
                    ┌────────────────────────────┴───────────────────────────────────┐
                    │  CSharpTrayShortcut.Application                                  │
                    │  MenuComposer (cas d'usage « composer le menu »)                 │
                    │  IconSourceResolver · LaunchService · TextCatalogue              │
                    │  PORTS : IShortcutSource, IShortcutTargetResolver,               │
                    │          IProcessLauncher, IConfigurationStore                   │
                    └────────────────────────────┬───────────────────────────────────┘
                             implémente les ports │            utilise
                    ┌────────────────────────────┴───────────────────────────────────┐
                    │  CSharpTrayShortcut.Infrastructure                               │
                    │  DirectoryShortcutSource · ShellLinkTargetResolver (COM)         │
                    │  ShellProcessLauncher · JsonConfigurationStore · FileLogger      │
                    └────────────────────────────┬───────────────────────────────────┘
                                                 │
                    ┌────────────────────────────┴───────────────────────────────────┐
                    │  CSharpTrayShortcut.Domain — MenuEntry, LaunchTarget,            │
                    │  IconSource, MenuCommand, TextRef, TextKeys (aucune dép.)        │
                    └────────────────────────────────────────────────────────────────┘
```

**Règle de dépendance** : les flèches de référence pointent toujours vers l'intérieur. `Domain` ne
référence rien. `Application` ne référence que `Domain` — et cible `net9.0`, ce qui fait d'un
`using System.Windows.Forms` une erreur de compilation. Conséquence pratique : **on teste toutes
les règles sans Windows, sans disque et sans shell**.

---

## 4. Fonctionnement — l'ouverture du menu

1. **Relire la configuration** (`IConfigurationStore.Load`). Un fichier absent ou abîmé donne des
   valeurs par défaut (SPEC-CFG-001).
2. **Aligner la langue** sur le réglage et sur Windows (SPEC-UI-LANG-001).
3. **Valider le dossier surveillé.** S'il manque, le sélecteur de dossier s'ouvre ; si
   l'utilisateur annule, on continue avec un menu réduit (SPEC-CFG-002).
4. **Appliquer l'icône de la zone de notification** et son info-bulle (SPEC-ICON-002).
5. **Composer le menu** (`MenuComposer.ComposeRoot`) : sous-dossiers du premier niveau, fichiers de
   la racine, section des raccourcis personnalisés, commandes. Une liste de `MenuEntry` — aucune
   image, aucun `ToolStripItem`.
6. **Rendre** (`MenuRenderer`) : traduire chaque entrée en élément WinForms, fabriquer les images
   une fois par source distincte, libérer celles du rendu précédent.
7. **À l'ouverture d'un sous-dossier**, énumérer ce dossier et le rendre, une seule fois
   (SPEC-MENU-003).
8. **Au clic**, réexaminer la cible et la confier au shell (SPEC-LAUNCH-001 à -003).

Rien ne tourne entre deux ouvertures de menu : pas de minuterie, pas de surveillance de dossier,
pas de thread de fond.

---

## 5. Décisions de conception structurantes

### 5.1 Le menu est une donnée

```csharp
public abstract record MenuEntry;
public sealed record FolderEntry(string Label, string Path, IconSource Icon) : MenuEntry;
public sealed record LaunchEntry(string Label, LaunchTarget Target, IconSource Icon) : MenuEntry;
public sealed record SeparatorEntry : MenuEntry;
public sealed record GroupEntry(TextRef Label, IReadOnlyList<MenuEntry> Children) : MenuEntry;
public sealed record CommandEntry(TextRef Label, MenuCommand Command) : MenuEntry;
```

Hiérarchie fermée, cinq formes exhaustives. C'est ce qui rend vérifiable « les dossiers viennent
avant les fichiers », « la section n'apparaît que s'il y a des raccourcis » ou « les commandes sont
toujours là » — sans instancier de fenêtre. Le rendu les traite par filtrage de motif, sans branche
« autre » utile.

### 5.2 Patrons utilisés et pourquoi

| Patron | Où | Raison |
|---|---|---|
| **Ports & Adapters** | `Application/Abstractions` ↔ `Infrastructure` | Testabilité ; remplaçabilité du disque, du shell et de COM |
| **Repository** | `IConfigurationStore` | Isoler la persistance JSON — c'est ce qui a permis [ADR-0002](adr/0002-donnees-dans-appdata.md) sans toucher une règle |
| **Value Object** | `LaunchTarget`, `IconSource`, `MenuEntry` | Égalité structurelle ; un `LaunchTarget` qui existe est toujours exploitable |
| **Chaîne de replis** | `IconSource.Or` / `Chain` | Exprimer « ceci, sinon cela » comme une donnée vérifiable plutôt qu'un `??` enfoui dans le rendu |
| **Options** | `TrayShortcutConfiguration` + `Validate` | Configuration validée en un point, avec un prédicat injecté |
| **Composite** | `GroupEntry` | Une section de menu se traite comme une entrée |
| **Command** | `MenuCommand` + `Action<MenuCommand>` | Le menu décrit ce qui est proposé, la présentation décide ce que ça fait |

### 5.3 Modèle de configuration

`%APPDATA%\TrayShortcut\config.json` :

```
TrayShortcutConfiguration
├─ Path            : string?    dossier surveillé
├─ PathFolderIcon  : string?    icône des dossiers, à défaut celle livrée
├─ PathTrayIcon    : string?    icône de la zone de notification, idem
├─ ShowRootFiles   : bool?      absent = true (compatibilité)
├─ Language        : System | French | English
└─ CustomShortcuts : [ { Text?, Path?, Argument?, Image? } ]
```

`bool?` et non `bool` pour `ShowRootFiles` : distinguer « absent du fichier » de « explicitement
faux » est ce qui préserve le comportement des configurations écrites avant l'apparition du
réglage.

### 5.4 Coût d'une ouverture de menu

Une énumération de dossier pour le niveau racine, plus une par sous-dossier réellement ouvert.
L'extraction d'icônes est le poste dominant — c'est la raison pour laquelle l'énumération est
paresseuse, et celle pour laquelle les images sont mises en cache
([ADR-0006](adr/0006-cache-des-icones.md)).

Le cache réutilise une image selon ce que Windows fait réellement : un **document** reçoit
l'icône de son **type**, un **exécutable** porte la sienne. Pour un sous-dossier de 30 PDF et
5 fichiers texte, cela fait **2** extractions et non 35.

| Situation | Énumérations | Extractions d'icônes |
|---|---|---|
| Ouverture initiale, 8 dossiers + 5 fichiers à la racine | 1 | ≤ 5, et 1 pour l'icône de dossier partagée |
| Dépliage d'un sous-dossier de 30 documents de 2 types | 1 | 2 |
| Dépliage d'un sous-dossier de 12 exécutables distincts | 1 | 12 — irréductible, chacun porte son image |
| *Actualiser* sans changement | 1 par dossier réouvert | 0 ; une lecture de métadonnées par exécutable |

Les images survivent à un *Actualiser* et sont invalidées par l'empreinte du fichier (date, taille)
pour celles qui dépendent de lui. Leur nombre est borné à 512, avec éviction **aux seules
frontières de rendu** — libérer une image qu'un menu affiche encore la ferait peindre après
destruction.

Un cache des énumérations de dossier a été envisagé et écarté : obtenir l'empreinte d'un dossier
demande de toute façon un aller-retour, et l'économie est nulle en dessous de quelques dizaines
d'éléments.

---

## 6. Sécurité

* L'application ne fait que **lire et lancer** : aucune écriture hors de son dossier de données.
* Les schémas d'adresse lançables sont une **liste blanche** de trois entrées
  ([ADR-0005](adr/0005-cibles-de-lancement.md)) : un fichier de configuration ne peut pas
  déclencher un gestionnaire de protocole arbitraire.
* Aucun secret n'est manipulé, aucun réseau n'est contacté.
* `config.json` contient des chemins de poste de travail : il est hors du dépôt, dans le dossier de
  données de l'utilisateur.

## 7. Journalisation et diagnostic

`%APPDATA%\TrayShortcut\log.txt` (rotation à 1 Mo, une génération de sauvegarde). Le journal
répond à une seule question, la seule qu'on se pose sur cette application : **qu'est-ce qui a été
ignoré, et pourquoi**. Les rapports d'erreur imprévue sont dans le même dossier
(SPEC-APP-002).

## 8. Extensibilité — scénarios anticipés

| Besoin futur | Geste |
|---|---|
| Nouvelle commande dans le menu | Une valeur dans `MenuCommand`, sa formulation dans les deux `.resx`, un cas dans `TrayApplicationContext.Execute`. Le test de couverture des clés impose la formulation |
| Nouvelle forme d'entrée de menu | Un `record` dans `MenuEntry.cs` et un cas dans `MenuRenderer.Create` — qui échoue explicitement si on l'oublie |
| Surveiller plusieurs dossiers | `Path` devient une liste ; `ComposeRoot` boucle. Aucune autre règle ne change |
| Filtrer par extension | Un prédicat dans `MenuComposer.ComposeDirectory` |
| Nouvelle langue | Un `Strings.<culture>.resx` et une position au réglage ([ADR-0004](adr/0004-multilingue.md)) |
| Interface WPF ou WinUI | Seule la couche `Ui` change ; `MenuEntry` est déjà indépendant de WinForms |
| Autre source que le disque | Une implémentation de `IShortcutSource` |

## 9. Risques connus et parades

| Risque | Parade en place |
|---|---|
| Partage réseau lent ou déconnecté | Énumération paresseuse (SPEC-MENU-003) et tolérance (SPEC-MENU-004) |
| Cible disparue entre la construction et le clic | Réexamen au clic (SPEC-LAUNCH-002) |
| Fuite de ressources graphiques à force d'actualiser | Le rendu possède les images ([ADR-0003](adr/0003-icone-source-et-non-image.md)) |
| Installation sous `Program Files` | Données dans `%APPDATA%` ([ADR-0002](adr/0002-donnees-dans-appdata.md)) |
| Configuration abîmée à la main | Valeurs par défaut et journal (SPEC-CFG-001) |
| Dossier surveillé jamais configuré | Menu réduit mais utilisable (SPEC-CFG-002, règle 3) |

## 10. Décisions consignées (ADR)

* [ADR-0001 — Quatre couches pour un millier de lignes](adr/0001-quatre-couches-pour-mille-lignes.md)
* [ADR-0002 — Configuration et journaux dans le dossier de données de l'utilisateur](adr/0002-donnees-dans-appdata.md)
* [ADR-0003 — Une icône est une source, pas une image](adr/0003-icone-source-et-non-image.md)
* [ADR-0004 — Interface bilingue : des clés dans le domaine, des `.resx` dans l'application](adr/0004-multilingue.md)
* [ADR-0005 — Ce qu'on accepte de lancer](adr/0005-cibles-de-lancement.md)
* [ADR-0006 — Cache des icônes : la clé est une règle, l'image est une ressource](adr/0006-cache-des-icones.md)
