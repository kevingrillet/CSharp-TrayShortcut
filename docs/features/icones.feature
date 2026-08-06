# language: fr
Fonctionnalité: Icônes du menu

  Un menu de raccourcis sans icônes n'est qu'une liste de mots : Camille reconnaît ses
  outils à leur image bien avant d'avoir lu leur nom. C'est ce qui rend le sujet moins
  cosmétique qu'il n'y paraît.

  Deux questions sont distinguées partout : quelle icône montrer — une décision — et
  comment en fabriquer l'image — du dessin.

  Contexte:
    Etant donné que Camille surveille le dossier « D:\Toolbar »

  @SPEC-ICON-001
  Scénario: L'icône d'un outil est celle de l'outil
    Etant donné que le dossier surveillé contient « notepad.exe »
    Quand Camille ouvre le menu
    Alors l'entrée affiche l'icône du fichier lui-même

  @SPEC-ICON-001
  Scénario: Une icône choisie explicitement gagne toujours
    Etant donné un raccourci personnalisé vers « notepad.exe » auquel Camille a associé « monicone.ico »
    Quand elle ouvre la section des raccourcis personnalisés
    Alors c'est « monicone.ico » qui est affichée, et non l'icône de l'exécutable
    Et ce choix n'est pas remis en cause, même si le fichier est momentanément indisponible

  @SPEC-ICON-001
  Scénario: Un fichier disparu ne reçoit pas d'image de remplacement
    Etant donné un raccourci personnalisé vers un exécutable qui n'existe plus
    Quand Camille ouvre le menu
    Alors l'entrée s'affiche sans image
    Mais aucune image générique n'est mise à sa place, qui laisserait croire que la cible existe

  @SPEC-ICON-003
  Scénario: Un raccourci Windows montre ce qu'il pointe
    Etant donné que le dossier surveillé contient le raccourci « Word.lnk » vers « WINWORD.EXE »
    Quand Camille ouvre le menu
    Alors l'entrée affiche l'icône de Word
    Et non l'image générique commune à tous les raccourcis

  @SPEC-ICON-003
  Scénario: Un raccourci abîmé se replie sur sa propre icône
    Etant donné un raccourci « Perime.lnk » dont la cible est illisible ou a disparu
    Quand Camille ouvre le menu
    Alors l'entrée affiche l'icône du raccourci lui-même
    Et la construction du menu n'est pas interrompue

  @SPEC-ICON-003
  Scénario: L'extension est reconnue quelle qu'en soit la casse
    Etant donné un raccourci nommé « Word.LNK »
    Quand Camille ouvre le menu
    Alors sa cible est suivie comme pour « Word.lnk »

  @SPEC-ICON-002
  Scénario: L'icône de dossier configurée précède celle fournie
    Etant donné que Camille a choisi « folder_w11.ico » comme icône de dossier
    Quand elle ouvre le menu
    Alors ses dossiers portent cette icône
    Et l'icône livrée avec l'application reste le repli si celle-ci ne peut pas être chargée

  @SPEC-ICON-002
  Scénario: Sans choix, l'icône livrée suffit
    Etant donné que Camille n'a choisi aucune icône de dossier
    Quand elle ouvre le menu
    Alors ses dossiers portent l'icône livrée avec l'application

  @SPEC-ICON-002
  Scénario: Une icône fournie se désigne par son seul nom
    Etant donné que Camille écrit « folder_w11.ico » sans chemin dans sa configuration
    Quand elle ouvre le menu
    Alors l'icône est trouvée parmi celles livrées avec l'application
    Et un chemin complet, lui, est pris tel quel

  @SPEC-ICON-002
  Scénario: L'icône de la zone de notification suit la même règle
    Etant donné que Camille a choisi une icône personnelle pour la zone de notification
    Quand l'application démarre
    Alors cette icône est employée, avec repli sur celle livrée

  @SPEC-ICON-004
  Scénario: Le repli est tenté dans l'ordre
    Etant donné une icône configurée dont le fichier est tronqué
    Quand le menu est affiché
    Alors l'application passe au repli sans se plaindre
    Et si aucun candidat ne donne d'image, l'entrée s'affiche sans icône
    Et cet échec est retenu, pour ne pas retenter à chaque entrée qui désigne le même fichier

  @SPEC-ICON-004
  Scénario: Un dossier de documents ne coûte qu'une poignée d'icônes
    Etant donné un sous-dossier contenant trente fichiers PDF et cinq fichiers texte
    Quand Camille le déplie
    Alors deux icônes seulement sont fabriquées, une par type de document
    Et non trente-cinq, Windows attribuant à un document l'icône de son type

  @SPEC-ICON-004
  Scénario: Deux applications différentes gardent des icônes différentes
    Etant donné un dossier contenant « alpha.exe » et « beta.exe »
    Quand Camille l'ouvre
    Alors chaque exécutable affiche sa propre icône
    Car un exécutable porte son image, à la différence d'un document

  @SPEC-ICON-004
  Scénario: Mettre à jour une application change son icône
    Etant donné un raccourci vers une application dont l'icône est déjà affichée
    Quand cette application est mise à jour et que Camille actualise
    Alors la nouvelle icône apparaît
    Car l'image retenue est invalidée dès que la date ou la taille du fichier change

  @SPEC-ICON-004
  Scénario: Actualiser réutilise ce qui n'a pas changé
    Etant donné un menu de plusieurs dizaines d'entrées, déjà affiché
    Quand Camille actualise
    Alors les icônes des fichiers inchangés ne sont pas refabriquées
    Et le menu réapparaît sans délai perceptible

  @SPEC-ICON-004
  Scénario: Actualiser ne consomme pas de ressources graphiques indéfiniment
    Etant donné une arborescence de plusieurs milliers d'éléments, parcourue au fil du temps
    Quand le nombre d'images retenues dépasse la borne
    Alors celles qui n'ont pas servi lors des deux derniers menus sont libérées
    Mais jamais pendant qu'un menu les affiche encore

  @SPEC-UI-ICON-001
  Scénario: L'application est reconnaissable
    Quand Camille voit l'application dans l'explorateur ou dans la zone de notification
    Alors elle porte l'icône livrée avec elle
    Et l'info-bulle indique le nom du produit suivi du dossier surveillé
    Mais elle est tronquée à 63 caractères, faute de quoi Windows ne l'afficherait pas du tout
