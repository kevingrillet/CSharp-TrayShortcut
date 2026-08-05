# SPEC-LAUNCH — Lancement

> Un identifiant par comportement, couvert par au moins un test portant la catégorie
> correspondante, et par un scénario Gherkin dans [`../features/`](../features/).

Un menu se construit à un instant et se clique à un autre. Entre les deux, le fichier peut avoir
été déplacé, le partage réseau déconnecté, l'application désinstallée. Ces specs disent ce qui
se passe alors.

---

## SPEC-LAUNCH-001 — Ouvrir un élément

*Étant donné* une entrée de menu dont la cible mène quelque part
*Quand* l'utilisateur clique dessus
*Alors* la cible est ouverte par le shell de Windows, avec son argument s'il y en a un, et
l'application continue de tourner.

Règles :

1. C'est le **shell** qui ouvre, pas un lancement direct de processus : un document ouvre son
   application associée, un dossier ouvre l'explorateur, une adresse ouvre le navigateur.
2. L'argument est transmis tel quel. Un argument vide et un argument absent sont équivalents.
3. Le menu se referme, l'application reste résidente.
4. Un chemin qui désigne à la fois un fichier et un dossier existants est traité comme un
   **fichier**.

## SPEC-LAUNCH-002 — Cible disparue ou refusée

*Étant donné* une entrée dont la cible n'existe plus, ou que le système refuse d'ouvrir
*Quand* l'utilisateur clique dessus
*Alors* **rien ne se passe** : aucune fenêtre d'erreur, aucun plantage, l'icône de la zone de
notification reste en place. L'incident est consigné au journal (SPEC-APP-003).

Règles :

1. La cible est réexaminée **au clic**, et non seulement à la construction du menu.
2. Une cible disparue n'est même pas soumise au shell.
3. Un refus du système — aucune application associée, exécutable bloqué par une stratégie de
   groupe, blocage par l'antivirus — est consigné avec sa cause, sans remonter.

Cette spec existe parce que la version antérieure levait une exception depuis le gestionnaire de
clic : elle remontait au gestionnaire d'exceptions non gérées, qui écrivait un rapport et
**fermait l'application**. Un clic sur une entrée périmée faisait donc disparaître l'icône.

## SPEC-LAUNCH-003 — Ce qui constitue une cible valide

*Étant donné* le chemin d'une entrée de menu
*Quand* on détermine si elle mène quelque part
*Alors* trois natures sont acceptées — un **fichier** existant, un **dossier** existant, ou une
**adresse** dont le schéma figure dans la liste des schémas autorisés — et tout le reste est
traité comme disparu.

Règles :

1. Les schémas autorisés sont `http`, `https` et `mailto`.
2. Le schéma **`file` est délibérément exclu**, pour deux raisons cumulées : il n'apporte rien
   qu'un chemin nu ne fasse déjà, et un chemin Windows (`D:\parti.exe`) comme un chemin UNC
   (`\\serveur\partage`) constitue une adresse `file:` syntaxiquement valide. L'admettre
   classerait tout chemin disparu en cible valide, et viderait SPEC-LAUNCH-002 de son sens.
3. Tout autre schéma est refusé — `ftp:`, `javascript:`, `ms-settings:` et le reste. Passer un
   schéma quelconque au shell reviendrait à laisser un fichier de configuration déclencher
   n'importe quel gestionnaire de protocole installé.
4. L'existence sur le disque est testée **avant** l'interprétation comme adresse, sans quoi tout
   chemin local serait classé comme adresse.

Le README a toujours annoncé « lien ou exécutable à lancer » : seul le contrôle d'existence de
fichier empêchait les adresses de fonctionner. Cette spec entérine le comportement documenté
([ADR-0005](../adr/0005-cibles-de-lancement.md)).
