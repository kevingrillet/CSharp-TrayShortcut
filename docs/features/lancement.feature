# language: fr
Fonctionnalité: Lancement d'un élément du menu

  Un menu se construit à un instant et se clique à un autre. Entre les deux, Camille a pu
  désinstaller un outil, déplacer un dossier, ou perdre son lecteur réseau.

  La règle qui gouverne toute cette fonctionnalité tient en une phrase : un clic qui ne mène
  nulle part ne doit rien casser. Dans les versions antérieures, il fermait l'application.

  Contexte:
    Etant donné que Camille surveille le dossier « D:\Toolbar »

  @SPEC-LAUNCH-001
  Scénario: Ouvrir un outil
    Etant donné une entrée pointant vers « notepad.exe »
    Quand Camille clique dessus
    Alors le Bloc-notes s'ouvre
    Et le menu se referme
    Et l'application reste dans la zone de notification

  @SPEC-LAUNCH-001
  Scénario: Ouvrir un document avec son argument
    Etant donné un raccourci personnalisé vers « notepad.exe » avec l'argument « D:\notes.txt »
    Quand Camille clique dessus
    Alors le Bloc-notes s'ouvre sur ce fichier

  @SPEC-LAUNCH-001
  Scénario: C'est Windows qui décide avec quoi ouvrir
    Etant donné une entrée pointant vers « rapport.docx »
    Quand Camille clique dessus
    Alors le document s'ouvre dans l'application qui lui est associée
    Et non dans une application choisie par Tray Shortcut

  @SPEC-LAUNCH-002
  Scénario: Une cible disparue ne fait rien
    Etant donné une entrée dont l'exécutable a été désinstallé depuis l'ouverture du menu
    Quand Camille clique dessus
    Alors rien ne se passe
    Et aucune fenêtre d'erreur n'apparaît
    Et l'icône reste dans la zone de notification
    Mais la raison est consignée dans le journal

  @SPEC-LAUNCH-002
  Scénario: Un refus du système est consigné, pas affiché
    Etant donné une entrée que la stratégie de groupe interdit d'exécuter
    Quand Camille clique dessus
    Alors rien ne se passe visiblement
    Et la cause est consignée dans le journal

  @SPEC-LAUNCH-002
  Scénario: Un chemin local disparu reste un chemin disparu
    Etant donné une entrée vers « D:\parti.exe », qui n'existe plus
    Quand Camille clique dessus
    Alors rien ne se passe
    Et le fait que ce chemin constitue une adresse « file: » syntaxiquement valide n'y change rien

  @SPEC-LAUNCH-003
  Scénario: Un dossier s'ouvre dans l'explorateur
    Etant donné un raccourci personnalisé vers le dossier « D:\Projets »
    Quand Camille clique dessus
    Alors l'explorateur s'ouvre sur ce dossier

  @SPEC-LAUNCH-003
  Plan du Scénario: Une adresse s'ouvre dans l'application associée
    Etant donné un raccourci personnalisé vers « <adresse> »
    Quand Camille clique dessus
    Alors l'application associée à ce genre d'adresse s'ouvre

    Exemples:
      | adresse                       |
      | https://example.org/wiki      |
      | http://intranet/outils        |
      | mailto:support@example.org    |

  @SPEC-LAUNCH-003
  Plan du Scénario: Les autres schémas d'adresse sont refusés
    Etant donné un raccourci personnalisé vers « <adresse> »
    Quand Camille clique dessus
    Alors rien ne se passe
    Et le schéma n'est pas transmis à Windows, faute de quoi un fichier de configuration
    pourrait déclencher n'importe quel gestionnaire de protocole installé

    Exemples:
      | adresse                |
      | ftp://serveur/fichier  |
      | javascript:alert(1)    |
      | ms-settings:privacy    |

  @SPEC-LAUNCH-001
  Scénario: Un nom porté à la fois par un fichier et un dossier désigne le fichier
    Etant donné un chemin « D:\Ambigu » qui existe à la fois comme fichier et comme dossier
    Quand Camille clique sur l'entrée correspondante
    Alors c'est le fichier qui est ouvert
