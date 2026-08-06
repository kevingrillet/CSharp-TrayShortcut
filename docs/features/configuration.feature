# language: fr
Fonctionnalité: Configuration

  Camille règle Tray Shortcut par une petite fenêtre, ou en éditant un fichier JSON à la
  main — les deux chemins doivent mener au même résultat.

  Ce qui compte ici : ne jamais refuser de démarrer à cause d'un fichier, ne jamais coincer
  l'utilisatrice dans une invite dont elle ne peut pas sortir, et ne jamais perdre une saisie.

  @SPEC-CFG-001
  Scénario: La configuration vit dans le dossier de données de l'utilisateur
    Quand Camille enregistre sa configuration
    Alors elle est écrite dans son propre dossier de données, hors du dossier d'installation
    Et l'application fonctionne donc même installée dans « Program Files »
    Et un autre compte Windows sur la même machine a sa propre configuration

  @SPEC-CFG-001
  Scénario: Un premier démarrage sans fichier
    Etant donné qu'aucune configuration n'existe encore
    Quand l'application démarre
    Alors elle part de valeurs par défaut exploitables
    Et elle demande à Camille quel dossier surveiller

  @SPEC-CFG-001
  Scénario: Un fichier abîmé ne bloque pas le démarrage
    Etant donné un fichier de configuration tronqué par un arrêt brutal
    Quand l'application démarre
    Alors elle repart de valeurs par défaut
    Et l'incident est consigné dans le journal
    Mais elle ne refuse jamais de démarrer pour cette raison

  @SPEC-CFG-001
  Scénario: Un fichier abîmé est mis de côté et recréé
    Etant donné que Camille a oublié une accolade en éditant sa configuration à la main
    Quand l'application démarre
    Alors son ancien contenu est conservé dans un fichier horodaté « .invalide »
    Et une configuration par défaut est recréée à la place
    Et elle retrouve donc un fichier valide à éditer, sans avoir perdu son travail

  @SPEC-CFG-001
  Scénario: Un fichier momentanément verrouillé n'est pas déplacé
    Etant donné un fichier de configuration parfaitement valide, mais verrouillé par un autre programme
    Quand l'application démarre
    Alors elle emploie les valeurs par défaut pour cette fois
    Mais elle ne met pas le fichier de côté, celui-ci n'étant pas en cause

  @SPEC-CFG-001
  Scénario: Une coupure pendant l'enregistrement ne détruit pas l'existant
    Etant donné une configuration déjà enregistrée
    Quand le poste s'éteint au milieu d'un enregistrement
    Alors l'ancienne configuration est toujours lisible au redémarrage
    Et non un fichier à moitié écrit

  @SPEC-CFG-001
  Scénario: Un enregistrement refusé est signalé
    Etant donné un dossier de données en lecture seule
    Quand Camille enregistre
    Alors elle est prévenue, avec le chemin du fichier concerné

  @SPEC-CFG-001
  Scénario: Le fichier s'écrit et se relit tel qu'il est documenté
    Etant donné que Camille écrit « "Language": "French" » à la main, comme le README l'indique
    Quand l'application démarre
    Alors le réglage est pris en compte
    Et le reste de sa configuration l'est aussi
    Mais si ce réglage avait dû s'écrire sous forme de nombre, l'objet entier aurait été rejeté
    Et Camille aurait perdu jusqu'à son dossier surveillé

  @SPEC-CFG-001
  Scénario: Un fichier écrit par une version plus récente reste lisible
    Etant donné un fichier contenant un réglage que cette version ne connaît pas
    Quand l'application démarre
    Alors le réglage inconnu est ignoré
    Et les réglages connus sont appliqués normalement

  @SPEC-CFG-001
  Scénario: La casse des noms de réglages est indifférente
    Etant donné un fichier où Camille a écrit « path » au lieu de « Path »
    Quand l'application démarre
    Alors le réglage est pris en compte

  @SPEC-CFG-002
  Scénario: Le dossier surveillé a disparu
    Etant donné une configuration désignant « D:\Toolbar », qui n'existe plus
    Quand l'application démarre
    Alors Camille est invitée à choisir un dossier
    Et le message nomme le dossier introuvable
    Et le choix se fait par le sélecteur de dossier de Windows, non par une saisie libre

  @SPEC-CFG-002
  Scénario: Annuler l'invite n'enferme pas l'utilisatrice
    Etant donné que l'application demande un dossier à surveiller
    Quand Camille annule
    Alors le menu se limite aux trois commandes
    Et l'application reste utilisable et peut être quittée normalement
    Mais l'invite n'est pas réaffichée indéfiniment

  @SPEC-CFG-002
  Scénario: Un dossier inaccessible est traité comme absent
    Etant donné un dossier surveillé sur un partage réseau déconnecté
    Quand l'application démarre
    Alors Camille est invitée à en choisir un autre
    Et la distinction entre « absent » et « inaccessible » ne lui est pas soumise, n'y pouvant rien

  @SPEC-CFG-003
  Scénario: Modifier ses raccourcis personnalisés
    Quand Camille active « Modifier… »
    Alors une grille présente ses raccourcis : intitulé, chemin, argument, icône
    Et elle peut ajouter, modifier et supprimer des lignes
    Et les colonnes sont ordonnées dans le sens de la saisie

  @SPEC-CFG-003
  Scénario: La dernière saisie n'est pas perdue
    Etant donné que Camille vient de taper dans une cellule sans en sortir
    Quand elle enregistre
    Alors la valeur tapée est bien prise en compte

  @SPEC-CFG-003
  Scénario: Le fichier reste propre
    Etant donné une grille où Camille a laissé une ligne sans chemin et vidé quelques cellules
    Quand elle enregistre
    Alors la ligne sans chemin n'est pas écrite
    Et les cellules vides sont absentes du fichier, plutôt qu'écrites comme chaînes vides
    Et ce nettoyage s'applique aussi à un fichier modifié à la main

  @SPEC-CFG-003
  Scénario: Ouvrir le fichier de configuration
    Quand Camille demande à voir le fichier
    Alors il s'ouvre dans l'éditeur qu'elle a associé aux fichiers JSON
    Et non dans un éditeur imposé par l'application

  @SPEC-CFG-003
  Scénario: Un enregistrement qui échoue ne fait pas perdre le travail
    Etant donné un dossier de données devenu inaccessible
    Quand Camille enregistre
    Alors la fenêtre reste ouverte, avec sa saisie intacte

  @SPEC-CFG-004
  Scénario: Actualiser reprend tout
    Etant donné que Camille a modifié le fichier de configuration à la main
    Quand elle active « Actualiser »
    Alors le dossier surveillé, la langue, les icônes et le menu sont relus
    Et aucun redémarrage n'est nécessaire

  @SPEC-CFG-004
  Scénario: Fermer la fenêtre d'édition actualise le menu
    Quand Camille ferme la fenêtre d'édition
    Alors le menu est reconstruit, qu'elle ait enregistré ou non

  @SPEC-CFG-004
  Scénario: Les sous-dossiers déjà ouverts sont réénumérés
    Etant donné que Camille avait déplié « Bureautique » et y a depuis ajouté un fichier
    Quand elle actualise puis déplie « Bureautique » de nouveau
    Alors le nouveau fichier apparaît
