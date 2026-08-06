# SPEC-UI / SPEC-APP — Interface et cycle de vie

> Un identifiant par comportement, couvert par au moins un test portant la catégorie
> correspondante, et par un scénario Gherkin dans [`../features/`](../features/).

Deux familles ici : `SPEC-UI-LANG` pour la langue de l'interface, `SPEC-APP` pour le
comportement du processus lui-même. L'icône de l'application est traitée avec les autres icônes,
en [SPEC-UI-ICON-001](SPEC-ICONES.md#spec-ui-icon-001--icône-de-lapplication).

---

## SPEC-UI-LANG-001 — Choix de la langue

*Étant donné* le réglage `Language`, qui vaut *suivre Windows*, *français* ou *anglais*
*Quand* l'interface est composée
*Alors* elle emploie la langue demandée ; *suivre Windows* donne l'anglais si la langue
d'interface de Windows appartient à la famille anglaise, le français sinon.

Règles :

1. La famille est déterminée par le **code de langue à deux lettres** : `en`, `en-US` et `en-GB`
   donnent tous l'anglais, sans avoir à les énumérer.
2. Toute autre langue — allemand, japonais, ou une culture inconnue — donne le **français**,
   langue neutre du dépôt et seule à toujours disposer d'une formulation.
3. Un choix explicite ignore la langue de Windows.
4. Seuls les libellés suivent la langue choisie. Le format des nombres et des dates reste celui
   du poste : quelqu'un qui lit l'interface en anglais depuis un poste français attend toujours
   ses dates en jour/mois.

## SPEC-UI-LANG-002 — Où vivent les formulations

*Étant donné* n'importe quel message destiné à l'utilisateur
*Quand* on cherche sa formulation
*Alors* elle se trouve dans **un seul endroit** du dépôt : les fichiers de ressources de la
couche application. Partout ailleurs, un message est désigné par une **clé**.

Règles :

1. Les clés sont des **constantes** : une clé mal orthographiée est une erreur de compilation, et
   non un message manquant découvert par l'utilisateur.
2. Les deux langues portent **exactement** les mêmes clés. Une clé traduite d'un seul côté fait
   échouer les tests.
3. Chaque clé déclarée a une formulation dans les deux langues. Sinon, l'utilisateur verrait
   l'identifiant technique — « Menu.Refresh » — dans son menu.
4. Une clé inconnue est rendue telle quelle plutôt que de lever une exception : un intitulé
   technique dans un menu vaut mieux qu'une icône qui disparaît.
5. Un message peut recevoir un autre message en argument, pour qu'un fragment facultatif se
   compose sans imposer à toutes les langues la même découpe de phrase.
6. Le journal et les messages destinés au développeur ne sont pas traduits : les localiser
   compliquerait le support sans rien apporter.

Ajouter une langue consiste à ajouter un fichier de ressources et une position au réglage :
aucun code de présentation à toucher ([ADR-0004](../adr/0004-multilingue.md)).

## SPEC-APP-001 — Instance unique

*Étant donné* l'application déjà lancée
*Quand* l'utilisateur la relance
*Alors* la seconde instance se termine immédiatement, sans message, et une seule icône reste
dans la zone de notification.

Règles :

1. Le verrou est **local à la session Windows** : deux utilisateurs connectés simultanément sur
   la même machine ont chacun droit à son icône.
2. Aucun message n'est affiché : relancer par un double-clic distrait est banal, et une boîte de
   dialogue serait plus gênante que le silence.

Sans cette règle, chaque lancement ajouterait une icône, sans moyen de distinguer laquelle
appartient à quelle instance.

## SPEC-APP-002 — Erreur inattendue

*Étant donné* une erreur qu'aucun traitement n'a prévue
*Quand* elle survient
*Alors* le détail est écrit dans un rapport horodaté du dossier de données, l'utilisateur est
prévenu **avec le chemin du rapport**, puis l'application se termine.

Règles :

1. Le nom du rapport comporte l'horodatage et un discriminant : deux erreurs de la même seconde
   ne s'écrasent pas.
2. Le message n'est pas traduit : à ce stade, le catalogue de textes est précisément ce qui peut
   avoir échoué.
3. Un échec d'écriture du rapport ne doit jamais masquer l'erreur d'origine, seule information
   utile : l'utilisateur est alors prévenu sans chemin.

La version antérieure écrivait le rapport à côté de l'exécutable et disparaissait en silence :
l'icône s'évanouissait sans explication, et le rapport était introuvable pour qui ne connaissait
pas le dossier d'installation.

## SPEC-APP-003 — Journal

*Étant donné* une application résidente, sans console
*Quand* quelque chose est ignoré — dossier illisible, icône introuvable, cible disparue
*Alors* la raison est consignée dans `%APPDATA%\TrayShortcut\log.txt`.

Règles :

1. Rotation à 1 Mo, avec une seule génération conservée : assez pour couvrir plusieurs jours
   d'usage, borné pour ne jamais remplir un disque à l'insu de l'utilisateur.
2. Le journal n'est pas traduit.
3. Un journal indisponible — disque plein, dossier verrouillé — ne doit jamais empêcher
   l'application de fonctionner : on renonce au journal, pas au démarrage.
4. Ce qui est ordinaire est consigné discrètement (un raccourci périmé dans un dossier bien
   rempli), ce qui est anormal l'est fortement (une écriture refusée). Sans cette distinction, le
   journal serait noyé à chaque ouverture de menu.
