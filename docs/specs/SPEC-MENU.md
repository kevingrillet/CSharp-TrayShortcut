# SPEC-MENU — Composition du menu

> Un identifiant par comportement, couvert par au moins un test portant la catégorie
> correspondante, et par un scénario Gherkin dans [`../features/`](../features/).

Le menu de la zone de notification est le produit de deux choses : le **contenu d'un dossier
surveillé** et une **liste de raccourcis déclarés à la main**. Ces specs disent ce qu'on y
trouve, dans quel ordre, et à quel moment c'est calculé.

Vocabulaire : le **dossier surveillé** est celui que désigne le réglage `Path` ; la **racine**
est son premier niveau ; un **raccourci personnalisé** est une entrée déclarée dans la
configuration, indépendante du dossier surveillé.

---

## SPEC-MENU-001 — Contenu du menu racine

*Étant donné* un dossier surveillé accessible
*Quand* l'utilisateur ouvre le menu de la zone de notification
*Alors* il voit, dans cet ordre : les sous-dossiers du premier niveau, les fichiers de la
racine, puis — s'il y en a — un séparateur et la section des raccourcis personnalisés, puis un
séparateur et les trois commandes *Actualiser*, *Modifier…*, *Quitter*.

Règles :

1. Les sous-dossiers précèdent toujours les fichiers, à tous les niveaux.
2. Seul le **premier niveau** est parcouru à ce moment-là (SPEC-MENU-003).
3. Les fichiers de la racine n'apparaissent que si le réglage `ShowRootFiles` le permet.
   **L'absence du réglage vaut « oui »** : une configuration écrite avant son apparition ne
   change pas de comportement. Le réglage ne concerne que la racine, jamais les sous-dossiers.
4. La section des raccourcis personnalisés est absente s'il n'y en a aucun d'exploitable
   (SPEC-MENU-005) — pas de section vide, pas de séparateur orphelin.
5. Les trois commandes sont présentes **en toutes circonstances**, y compris quand le dossier
   surveillé est absent, vide ou illisible. Sans elles, une mauvaise configuration rendrait
   l'application impossible à corriger et même à quitter autrement que par le gestionnaire de
   tâches.
6. Un fichier de la racine se lance par son chemin complet, sans argument.

## SPEC-MENU-002 — Ordre et intitulés

*Étant donné* un dossier contenant des éléments aux noms variés
*Quand* le menu est construit
*Alors* dossiers et fichiers sont ordonnés par nom, sans égard à la casse ni aux accents ;
un fichier est intitulé par son nom **sans son extension**.

Règles :

1. L'ordre est celui qu'attend un lecteur francophone — « audio », « Éditeurs », « Zip » — et
   non l'ordre des points de code, qui rejetterait « Éditeurs » après « Zip ». Il est en
   revanche **indépendant de la culture du poste**, pour que deux machines affichent le même
   menu et que les tests soient reproductibles.
2. Les sous-dossiers gardent leur nom tel quel, extension comprise si le dossier en a une.
3. Les raccourcis personnalisés sont ordonnés entre eux par leur intitulé affiché, selon la
   même règle.
4. Un raccourci personnalisé sans intitulé prend le **nom du fichier visé**, sans extension.
   Sans ce repli, l'entrée s'afficherait sans texte et serait impossible à cliquer sciemment.

## SPEC-MENU-003 — Construction à la demande

*Étant donné* un dossier surveillé contenant une arborescence profonde
*Quand* le menu racine est construit
*Alors* **une seule énumération** est demandée — celle de la racine ; le contenu d'un
sous-dossier n'est lu qu'à sa première ouverture.

Règles :

1. Un sous-dossier affiche sa flèche d'ouverture avant d'avoir été lu : on ne sait pas encore
   s'il contient quelque chose.
2. Le contenu construit est conservé jusqu'au prochain *Actualiser*, qui repart d'un menu neuf
   et donc d'une nouvelle énumération.
3. Un sous-dossier ouvert et vide affiche une entrée inerte *(dossier vide)*, jamais un
   sous-menu vide dans lequel le clic ne mènerait nulle part.

Sans cette règle, ouvrir l'application sur un partage réseau imposerait plusieurs secondes
d'attente avant l'apparition de l'icône, pour parcourir des dossiers que l'utilisateur
n'ouvrira jamais.

## SPEC-MENU-004 — Un dossier illisible n'emporte pas le menu

*Étant donné* un dossier dont la lecture échoue — droits refusés, lecteur réseau déconnecté,
dossier supprimé depuis la construction du menu
*Quand* le menu est construit, ou que ce dossier est ouvert
*Alors* ce dossier apparaît quand même dans son parent, son contenu est **vide**, et le reste
du menu est intact.

Règles :

1. Le dossier reste visible : son inaccessibilité n'est connue qu'à l'ouverture.
2. Si c'est la **racine** qui est illisible, le menu se réduit aux commandes (SPEC-MENU-001,
   règle 5) et reste utilisable.
3. Aucune fenêtre d'erreur : l'incident est consigné au journal (SPEC-APP-003), pas soumis à
   l'utilisateur, qui n'y peut rien sur le moment.

## SPEC-MENU-005 — Raccourcis personnalisés

*Étant donné* des raccourcis déclarés dans la configuration
*Quand* le menu est construit
*Alors* ils forment une section à part, sous l'intitulé *Raccourcis personnalisés*, séparée du
contenu du dossier surveillé.

Règles :

1. Un raccourci **sans chemin** est ignoré en silence : c'est une ligne à moitié remplie dans
   la fenêtre d'édition, et une entrée qui ne ferait rien au clic vaut moins que pas d'entrée.
2. L'argument déclaré est transmis au lancement (SPEC-LAUNCH-001).
3. Le chemin d'un raccourci personnalisé n'est pas restreint au dossier surveillé : c'est même
   sa raison d'être.
