# language: fr
Fonctionnalité: Contenu du menu de la zone de notification

  Camille a rassemblé dans un dossier « Toolbar » les outils qu'elle ouvre plusieurs fois
  par jour, rangés en sous-dossiers. Elle veut y accéder d'un clic droit dans la zone de
  notification, sans ouvrir l'explorateur.

  Ce qu'elle attend du menu est simple : qu'il ressemble à son dossier, qu'il s'ouvre
  instantanément, et qu'un dossier auquel elle n'a pas droit ne l'empêche pas d'utiliser
  le reste.

  Contexte:
    Etant donné que Camille surveille le dossier « D:\Toolbar »
    Et que ce dossier contient les sous-dossiers « Bureautique » et « Développement »
    Et qu'il contient aussi le fichier « notepad.exe » à sa racine

  @SPEC-MENU-001
  Scénario: Le menu reproduit le dossier surveillé
    Quand Camille ouvre le menu de la zone de notification
    Alors elle voit d'abord les sous-dossiers, puis les fichiers de la racine
    Et ensuite les commandes « Actualiser », « Modifier… » et « Quitter »
    Et un séparateur isole ces commandes du reste

  @SPEC-MENU-001
  Scénario: Les commandes sont toujours là, même quand rien d'autre ne l'est
    Etant donné que Camille n'a configuré aucun dossier à surveiller
    Quand elle ouvre le menu
    Alors elle voit uniquement les trois commandes
    Et elle peut donc corriger sa configuration ou quitter l'application
    Mais sans elles, il ne lui resterait que le gestionnaire de tâches

  @SPEC-MENU-001
  Scénario: Masquer les fichiers de la racine
    Etant donné que Camille a désactivé l'affichage des fichiers de la racine
    Quand elle ouvre le menu
    Alors « notepad.exe » n'apparaît plus
    Mais les sous-dossiers restent visibles
    Et les fichiers situés dans les sous-dossiers restent visibles, eux aussi

  @SPEC-MENU-001
  Scénario: Une configuration écrite avant l'apparition du réglage garde son comportement
    Etant donné un fichier de configuration où le réglage des fichiers de la racine est absent
    Quand Camille ouvre le menu
    Alors les fichiers de la racine sont affichés, comme avant l'apparition du réglage

  @SPEC-MENU-002
  Scénario: L'ordre est celui qu'attend une lectrice francophone
    Etant donné que le dossier surveillé contient les sous-dossiers « Zip », « Éditeurs » et « audio »
    Quand Camille ouvre le menu
    Alors ils apparaissent dans l'ordre « audio », « Éditeurs », « Zip »
    Et non dans l'ordre des codes de caractères, qui rejetterait « Éditeurs » après « Zip »
    Et cet ordre est le même sur toutes les machines, quelle que soit leur langue

  @SPEC-MENU-002
  Scénario: Un fichier est présenté sans son extension
    Etant donné que le dossier surveillé contient « Notepad++.lnk » et « rapport.docx »
    Quand Camille ouvre le menu
    Alors elle lit « Notepad++ » et « rapport »

  @SPEC-MENU-003
  Scénario: Le menu s'ouvre sans parcourir toute l'arborescence
    Etant donné que « Bureautique » contient lui-même plusieurs niveaux de sous-dossiers
    Quand Camille ouvre le menu
    Alors seul le premier niveau du dossier surveillé a été lu
    Et l'icône apparaît sans attendre, même sur un partage réseau

  @SPEC-MENU-003
  Scénario: Le contenu d'un sous-dossier est lu à son ouverture
    Quand Camille déplie « Bureautique »
    Alors son contenu est lu à ce moment-là
    Et il est conservé jusqu'au prochain « Actualiser »

  @SPEC-MENU-003
  Scénario: Un sous-dossier vide le dit
    Etant donné un sous-dossier « Archives » qui ne contient rien
    Quand Camille le déplie
    Alors elle lit une entrée inerte indiquant que le dossier est vide
    Et non un sous-menu vide où le clic ne mènerait nulle part

  @SPEC-MENU-004
  Scénario: Un dossier interdit n'emporte pas le menu
    Etant donné un sous-dossier « Interdit » dont Camille n'a pas les droits de lecture
    Quand elle ouvre le menu puis déplie « Interdit »
    Alors « Interdit » figure bien dans le menu, son inaccessibilité n'étant connue qu'à l'ouverture
    Et son contenu apparaît vide
    Et tout le reste du menu fonctionne normalement
    Mais aucune fenêtre d'erreur ne s'affiche

  @SPEC-MENU-004
  Scénario: Un dossier surveillé devenu illisible laisse l'application utilisable
    Etant donné que le lecteur réseau contenant le dossier surveillé est déconnecté
    Quand Camille ouvre le menu
    Alors elle voit les trois commandes et peut actualiser une fois le lecteur revenu

  @SPEC-MENU-005
  Scénario: Les raccourcis personnalisés forment leur propre section
    Etant donné que Camille a déclaré un raccourci « Notepad++ » vers un exécutable hors du dossier surveillé
    Quand elle ouvre le menu
    Alors une section « Raccourcis personnalisés » regroupe ce raccourci
    Et un séparateur la sépare du contenu du dossier surveillé

  @SPEC-MENU-005
  Scénario: Aucune section vide
    Etant donné que Camille n'a déclaré aucun raccourci personnalisé
    Quand elle ouvre le menu
    Alors aucune section « Raccourcis personnalisés » n'apparaît
    Et aucun séparateur ne reste orphelin

  @SPEC-MENU-005
  Scénario: Une ligne à moitié remplie est ignorée
    Etant donné un raccourci personnalisé dont le chemin est resté vide
    Quand Camille ouvre le menu
    Alors ce raccourci n'apparaît pas
    Et les raccourcis complets, eux, apparaissent normalement

  @SPEC-MENU-005
  Scénario: Les raccourcis personnalisés sont classés entre eux
    Etant donné les raccourcis « Zip » et « Archivage »
    Quand Camille ouvre la section des raccourcis personnalisés
    Alors « Archivage » précède « Zip »

  @SPEC-MENU-002
  Scénario: Un raccourci personnalisé sans intitulé prend le nom de sa cible
    Etant donné un raccourci personnalisé vers « notepad++.exe », sans intitulé saisi
    Quand Camille ouvre la section des raccourcis personnalisés
    Alors l'entrée s'intitule « notepad++ »
    Et elle n'est donc jamais affichée sans texte
