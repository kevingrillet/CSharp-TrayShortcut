# language: fr
Fonctionnalité: Langue et cycle de vie de l'application

  Tray Shortcut est une application résidente : pas de fenêtre principale, pas de console,
  une icône et un menu. Cela change deux choses — la langue doit se choisir sans écran de
  réglages compliqué, et les erreurs doivent laisser une trace, puisque personne n'est là
  pour les lire au moment où elles surviennent.

  @SPEC-UI-LANG-001
  Plan du Scénario: Suivre la langue de Windows
    Etant donné que Camille laisse le réglage de langue sur « suivre Windows »
    Et que la langue d'interface de son poste est « <culture> »
    Quand elle ouvre le menu
    Alors l'interface est en « <langue> »

    Exemples:
      | culture | langue   |
      | en      | anglais  |
      | en-US   | anglais  |
      | en-GB   | anglais  |
      | fr      | français |
      | fr-CA   | français |
      | de-DE   | français |
      | ja-JP   | français |

  @SPEC-UI-LANG-001
  Scénario: Le français est le repli
    Etant donné un poste dont la langue d'interface est inconnue de l'application
    Quand Camille ouvre le menu
    Alors l'interface est en français, langue de référence du projet
    Et aucun libellé ne manque

  @SPEC-UI-LANG-001
  Scénario: Un choix explicite l'emporte
    Etant donné un poste en français
    Quand Camille choisit l'anglais
    Alors l'interface passe en anglais
    Mais ses dates et ses nombres restent au format de son poste

  @SPEC-UI-LANG-002
  Scénario: Les deux langues restent complètes
    Quand une formulation est ajoutée dans une langue
    Alors l'absence de sa traduction fait échouer les tests
    Et Camille ne peut donc pas tomber sur un identifiant technique dans son menu

  @SPEC-UI-LANG-002
  Scénario: Une formulation manquante n'efface pas le menu
    Etant donné une clé de texte sans formulation
    Quand le menu est affiché
    Alors l'entrée montre l'identifiant technique
    Mais l'icône de la zone de notification ne disparaît pas

  @SPEC-APP-001
  Scénario: Une seule icône
    Etant donné que Tray Shortcut tourne déjà
    Quand Camille le relance par un double-clic distrait
    Alors la seconde instance se termine aussitôt
    Et une seule icône reste dans la zone de notification
    Mais aucun message ne la dérange

  @SPEC-APP-001
  Scénario: Deux utilisateurs sur la même machine
    Etant donné que Camille et Alice sont connectées simultanément sur le même poste
    Quand chacune lance Tray Shortcut
    Alors chacune a son icône et sa propre configuration

  @SPEC-APP-002
  Scénario: Une erreur imprévue laisse une trace exploitable
    Quand une erreur qu'aucun traitement n'avait prévue survient
    Alors un rapport horodaté est écrit dans le dossier de données
    Et Camille est prévenue, avec le chemin de ce rapport
    Et deux erreurs de la même seconde ne s'écrasent pas

  @SPEC-APP-002
  Scénario: Un rapport impossible à écrire ne masque pas l'erreur
    Etant donné un disque plein
    Quand une erreur imprévue survient
    Alors Camille est prévenue quand même, sans chemin de rapport
    Et l'erreur d'origine n'est pas remplacée par un échec d'écriture

  @SPEC-APP-003
  Scénario: Le journal explique ce qui a été ignoré
    Etant donné un dossier illisible et un raccourci périmé dans le dossier surveillé
    Quand Camille ouvre le menu puis consulte le journal
    Alors elle y trouve la raison de chaque élément écarté
    Et un raccourci périmé y est noté discrètement, une écriture refusée fortement
    Mais le journal ne dépasse jamais une taille bornée

  @SPEC-APP-003
  Scénario: Un journal indisponible n'empêche pas de travailler
    Etant donné un dossier de données verrouillé
    Quand l'application démarre
    Alors elle renonce au journal
    Et non au démarrage
