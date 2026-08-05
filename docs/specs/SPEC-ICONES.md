# SPEC-ICON — Icônes

> Un identifiant par comportement, couvert par au moins un test portant la catégorie
> correspondante, et par un scénario Gherkin dans [`../features/`](../features/).

Ces specs séparent deux questions que l'ancienne version traitait ensemble : **quelle** icône
montrer, qui est une règle, et **comment** en fabriquer l'image, qui est du dessin. La première
se vérifie sans écran ; la seconde n'existe que dans la couche de présentation
([ADR-0003](../adr/0003-icone-source-et-non-image.md)).

---

## SPEC-ICON-001 — Icône d'un élément lançable

*Étant donné* une entrée de menu qui lance quelque chose
*Quand* son icône est déterminée
*Alors* c'est, dans l'ordre : l'icône **explicitement désignée** si elle existe, sinon celle
**extraite du fichier visé**, sinon **aucune**.

Règles :

1. Seul un raccourci personnalisé peut désigner une icône explicite (réglage `Image`) ; les
   fichiers du dossier surveillé n'en ont pas la possibilité.
2. Une icône explicitement désignée n'est **pas vérifiée** au moment de la décision : c'est le
   seul endroit où l'utilisateur a exprimé un choix, et le repli du rendu (SPEC-ICON-004) suffit
   si le fichier manque.
3. Un fichier visé **absent** donne « aucune icône », et non un repli sur une image générique :
   une entrée sans image reste lisible, une image trompeuse ne l'est pas.
4. Un chemin vide donne « aucune icône », sans erreur.

## SPEC-ICON-002 — Icône de dossier et icône de la zone de notification

*Étant donné* une configuration désignant, ou non, une icône de dossier et une icône de zone de
notification
*Quand* ces icônes sont déterminées
*Alors* chacune est une **chaîne de replis** : l'icône configurée d'abord, puis celle livrée
avec l'application.

Règles :

1. Tous les dossiers du menu partagent la **même** icône, quel que soit leur niveau.
2. Une icône peut être désignée par son **seul nom de fichier** lorsqu'elle est livrée avec
   l'application (`folder_w11.ico`) ; sinon par un chemin. Un nom seul est cherché parmi les
   icônes fournies, un chemin ne l'est pas.
3. Quand aucune icône n'est configurée, la chaîne ne comporte que celle livrée : une source vide
   ne rallonge jamais la chaîne.
4. Le repli s'applique aussi quand l'icône configurée existe mais n'est pas exploitable
   (fichier tronqué, format invalide) : c'est le rendu qui le constate (SPEC-ICON-004).

## SPEC-ICON-003 — Raccourcis Windows

*Étant donné* un fichier d'extension `.lnk` dans le dossier surveillé
*Quand* son icône est déterminée
*Alors* c'est celle de **la cible du raccourci**, et non celle du raccourci lui-même.

Règles :

1. L'extension est reconnue sans égard à la casse (`.LNK` comme `.lnk`).
2. Si la cible est illisible — raccourci abîmé, partage injoignable — ou si elle a **disparu**,
   on retombe sur l'icône du raccourci.
3. Cette lecture ne doit jamais faire échouer la construction du menu : une défaillance ne coûte
   qu'une icône.

Sans cette règle, tous les raccourcis d'un dossier afficheraient la même image générique, ce qui
retire au menu l'essentiel de sa lisibilité.

## SPEC-ICON-004 — Réutilisation et fabrication de l'image

*Étant donné* une source d'icône et sa chaîne de replis
*Quand* l'image est fabriquée pour l'affichage
*Alors* les sources sont tentées **dans l'ordre**, la première qui donne une image gagne, et
cette image est **réutilisée** partout où la même source réapparaît ; si aucune source n'y
parvient, l'entrée s'affiche sans image.

Règles :

1. Deux fichiers qui reçoivent de Windows **la même icône** ne coûtent qu'une seule fabrication.
   Windows attribue à un **document** l'icône associée à son **type** : trente fichiers PDF d'un
   dossier partagent donc une image, et une extraction. Un **exécutable** — `.exe`, `.dll`,
   `.ico`, `.cpl`, `.scr`, `.msc`, `.ocx` — porte au contraire sa propre icône, et se réutilise
   par son **chemin**. Un fichier sans extension est traité comme un exécutable : se tromper par
   excès de prudence ne coûte qu'une fabrication de plus.
2. Une icône désignée explicitement se réutilise par son **chemin**, jamais par son extension :
   deux fichiers `.ico` différents portent deux images différentes.
3. Une image réutilisée par chemin est **invalidée dès que le fichier change** — date de
   dernière écriture ou taille. Mettre à jour une application change donc bien son icône dans le
   menu. Une empreinte illisible vaut « je ne sais pas » et périme l'image, ce qui est le choix
   sûr.
4. La réutilisation porte sur un **maillon** de la chaîne de replis, pas sur la chaîne entière :
   deux configurations différentes qui se replient sur la même icône livrée la partagent.
5. Les images survivent à un *Actualiser*, mais leur nombre est **borné**. Au-delà de la borne,
   celles qui n'ont pas servi lors des deux derniers menus sont libérées — et jamais à un autre
   moment, sous peine de libérer une image qu'un menu affiche encore.
6. Un fichier d'icône invalide, tronqué ou verrouillé est traité comme une source qui ne donne
   rien : on passe au repli. Cet échec est retenu lui aussi, pour ne pas retenter à chaque
   entrée qui désigne le même fichier.

La règle 1 est ce qui rend une ouverture de menu bon marché : sans elle, un dossier de documents
provoquait autant d'appels au shell qu'il contenait de fichiers, pour produire des images
identiques ([ADR-0006](../adr/0006-cache-des-icones.md)).

Les règles 1 à 3 portent sur une décision et sont testées. La fabrication elle-même et
l'éviction dépendent de la bibliothèque de dessin de Windows : voir
[`../TRACEABILITE.md`](../TRACEABILITE.md), § Zones sans test automatisé.

## SPEC-UI-ICON-001 — Icône de l'application

*Étant donné* l'application installée
*Quand* l'utilisateur la voit dans l'explorateur, dans la barre des tâches ou dans la zone de
notification
*Alors* elle porte l'icône livrée avec elle, et l'info-bulle de la zone de notification indique
le nom du produit suivi du dossier surveillé.

Règles :

1. L'icône de l'exécutable et le repli de l'icône de la zone de notification sont le même
   fichier.
2. L'info-bulle est tronquée à 63 caractères : au-delà, Windows ne l'affiche pas du tout plutôt
   que de la couper.
