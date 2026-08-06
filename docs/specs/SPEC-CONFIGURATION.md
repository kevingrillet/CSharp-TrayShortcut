# SPEC-CFG — Configuration

> Un identifiant par comportement, couvert par au moins un test portant la catégorie
> correspondante, et par un scénario Gherkin dans [`../features/`](../features/).

Tout se règle dans un fichier JSON, modifiable par une petite fenêtre ou à la main. Ces specs
disent où il vit, ce qui se passe quand il est absent ou abîmé, et comment un changement prend
effet.

---

## SPEC-CFG-001 — Emplacement et tolérance

*Étant donné* une installation quelconque de l'application
*Quand* la configuration est lue ou écrite
*Alors* elle vit dans `%APPDATA%\TrayShortcut\config.json`, et une configuration absente ou
illisible donne des valeurs par défaut exploitables.

Règles :

1. Le dossier de données est créé au besoin. Il est propre à chaque compte Windows.
2. Un fichier **absent** donne une configuration par défaut : liste de raccourcis vide, fichiers
   de la racine affichés, langue suivant Windows.
3. Un fichier **abîmé** — édité à la main, tronqué par un arrêt brutal — donne la même
   configuration par défaut, et l'incident est consigné au journal. L'application ne refuse
   jamais de démarrer pour cette raison.
4. L'écriture passe par un fichier temporaire voisin, ensuite substitué à l'original : une
   coupure pendant l'enregistrement laisse l'ancienne configuration intacte plutôt qu'un fichier
   à moitié écrit.
5. Un échec d'écriture est signalé à l'utilisateur, avec le chemin concerné.
6. Le fichier est un **contrat lisible et modifiable à la main** : les noms de réglages sont
   insensibles à la casse, un réglage inconnu est ignoré — un fichier écrit par une version plus
   récente reste lisible —, et les réglages à choix fermé s'écrivent **en clair**
   (`"Language": "French"`), jamais sous forme de nombre. Un réglage absent n'est pas écrit
   comme `null`.
7. Un fichier dont le **contenu** est inexploitable est **mis de côté** sous un nom horodaté
   (`config.json.20260805-143012.invalide`), et une configuration par défaut est **recréée** à sa
   place. L'utilisateur retrouve donc un fichier valide à éditer, et son ancien contenu à côté.
   Une lecture qui échoue pour une autre raison — verrou momentané, droits, disque en panne — ne
   déplace **rien** : le fichier est probablement intact.

L'emplacement n'est pas anodin : les versions antérieures écrivaient à côté de l'exécutable, ce
qu'un utilisateur ordinaire ne peut pas faire sous `Program Files`
([ADR-0002](../adr/0002-donnees-dans-appdata.md)).

La règle 6 non plus. Une lecture JSON est **tout ou rien** : un seul réglage que le lecteur
refuse fait rejeter l'objet entier, donc retomber sur les valeurs par défaut, donc **oublier le
dossier surveillé**. Le coût d'un format trop strict n'est pas le réglage perdu, c'est toute la
configuration.

## SPEC-CFG-002 — Dossier surveillé manquant

*Étant donné* une configuration dont le dossier surveillé est absent, vide ou introuvable
*Quand* l'application démarre ou s'actualise
*Alors* elle demande à l'utilisateur de choisir un dossier, et enregistre son choix.

Règles :

1. Le message nomme le dossier fautif : « dossier introuvable » sans le chemin n'aide personne.
2. La demande passe par le sélecteur de dossier de Windows, non par une saisie libre : c'est le
   geste habituel, et cela supprime une classe entière de fautes de frappe.
3. **Si l'utilisateur annule, on n'insiste pas** : le menu se limite aux commandes
   (SPEC-MENU-001, règle 5) et l'application reste utilisable. La version antérieure réaffichait
   la même invite indéfiniment, et ne pouvait alors être arrêtée que par le gestionnaire de
   tâches.
4. Un dossier inaccessible est traité comme inexistant : la distinction n'apporterait rien à
   l'utilisateur, qui doit de toute façon en désigner un autre.

## SPEC-CFG-003 — Édition des raccourcis personnalisés

*Étant donné* la commande *Modifier…* du menu
*Quand* l'utilisateur l'active
*Alors* une fenêtre présente les raccourcis personnalisés dans une grille — intitulé, chemin,
argument, icône — où il peut ajouter, modifier et supprimer des lignes, puis enregistrer.

Règles :

1. Les colonnes sont ordonnées pour la saisie : intitulé, chemin, argument, icône.
2. La cellule en cours d'édition est validée avant l'enregistrement : sans cela, la dernière
   saisie serait perdue.
3. À l'enregistrement, les lignes **sans chemin** sont écartées et les cellules vides
   redeviennent absentes du fichier. Ce nettoyage appartient à l'enregistrement et non à la
   fenêtre, pour que le fichier reste propre même modifié à la main.
4. La fenêtre affiche le chemin du fichier de configuration, et permet de l'ouvrir dans
   l'éditeur associé aux fichiers `.json` — pas dans un éditeur imposé.
5. Un échec d'enregistrement laisse la fenêtre ouverte, avec son contenu : on ne perd pas la
   saisie.

## SPEC-CFG-004 — Prise d'effet

*Étant donné* une configuration modifiée, par la fenêtre d'édition ou à la main
*Quand* l'utilisateur active *Actualiser*, ou ferme la fenêtre d'édition
*Alors* tout est relu et reconstruit : dossier surveillé, langue, icônes, menu.

Règles :

1. Fermer la fenêtre d'édition déclenche une actualisation, qu'on ait enregistré ou non.
2. L'actualisation reconstruit le menu **entièrement** : les sous-dossiers déjà ouverts sont
   réénumérés à leur prochaine ouverture (SPEC-MENU-003, règle 2).
3. Un changement de langue s'applique au menu immédiatement ; une fenêtre déjà ouverte garde ses
   libellés jusqu'à sa prochaine ouverture.
4. L'application n'a jamais besoin d'être redémarrée pour prendre un réglage en compte.
