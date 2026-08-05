# ADR-0004 — Interface bilingue : des clés dans le domaine, des `.resx` dans l'application

* **Statut** : accepté
* **Contexte** : l'application ne parlait qu'anglais, et cet anglais était en dur dans le code —
  `"Customs"`, `"Refresh"`, `"Edit"`, `"Exit"`, `"Please enter folder path"`, `"Save"`,
  `"Delete row"`, `"Show File"`. Une quinzaine de littéraux, dispersés entre la classe du menu,
  la fenêtre d'édition et son fichier de concepteur. Le dépôt, lui, est documenté en français.

## Décision 1 — Les couches basses émettent des clés, pas des phrases

Un message destiné à l'utilisateur est un `TextRef` : une **clé** et ses **arguments**
(`Domain/Text/TextRef.cs`). Le domaine et les règles disent *ce qu'il faut afficher*,
l'interface dit *dans quelle langue*.

Concrètement, `MenuComposer` produit une entrée portant `TextRef.Of(TextKeys.Menu.Customs)`, et
non la chaîne « Raccourcis personnalisés ». Ce que cela change au-delà de la traduction :

* les tests vérifient **quel** message est produit plutôt que son libellé, ce qui les rend
  indifférents à une reformulation ;
* le domaine ne porte plus de présentation, ce que l'architecture proscrit
  ([ADR-0001](0001-quatre-couches-pour-mille-lignes.md)).

Un argument peut être lui-même un `TextRef`, pour qu'un fragment facultatif se compose sans
imposer à toutes les langues la même découpe de phrase.

| Ce qui est traduit | Ce qui ne l'est pas |
|---|---|
| Menu, fenêtre d'édition, invites, messages d'erreur | `log.txt` — outil de diagnostic ; le localiser complique le support sans rien apporter |
| Intitulés des commandes et des sections | Le nom du produit, « Tray Shortcut » |
| Messages de validation de configuration | Messages d'`ArgumentException` : ils visent le développeur |
| — | Documentation, specs, scénarios Gherkin, noms de tests : documentation interne, pas une surface produit |
| — | Le message d'erreur fatale (SPEC-APP-002) : à ce stade, le catalogue est peut-être ce qui a échoué |

## Décision 2 — Les formulations vivent dans des `.resx`, dans la couche application

`Text/Strings.resx` (français, langue neutre déclarée par `Directory.Build.props`) et
`Text/Strings.en.resx` (anglais, assembly satellite). C'est le format que tout outil de
traduction sait lire, et le repli de culture — `fr-CA` vers `fr` vers la langue neutre — est
assuré par `ResourceManager` sans code de notre part.

Le catalogue est dans **`Application`** et non dans `Ui` : les tests ne référencent que `Domain`
et `Application`, et c'est ce qui permet de le mettre sous garde-fou. Les clés, elles, sont dans
`Domain` (`TextKeys`) — le seul endroit que les quatre couches voient.

L'accès reste **par clé** plutôt que par classe fortement typée, parce qu'une partie des clés se
déduit d'une énumération (`TextKeys.MenuCommandLabel`). Le filet est ailleurs : les clés sont des
constantes, et trois tests de garde vérifient la parité des deux langues, la présence d'une
formulation pour chaque clé déclarée — trouvée par réflexion, donc sans liste à maintenir — et le
fait que les deux catalogues rendent bien des textes différents, ce qui détecterait un assembly
satellite absent.

## Décision 3 — Le format des nombres et des dates ne suit pas la langue

Seule `CurrentUICulture` est alignée sur la langue choisie ; `CurrentCulture` reste celle du
poste. Quelqu'un qui lit l'interface en anglais depuis un poste français attend toujours ses
dates en jour/mois. Aligner les deux aurait été plus simple à écrire et plus surprenant à
l'usage.

## Conséquences

* **Le français devient la langue par défaut**, y compris sur un poste anglophone qui n'aurait
  pas `en` comme langue d'interface. C'est un changement visible par rapport à l'ancienne version,
  entièrement anglaise ; il est cohérent avec le reste du dépôt, et le réglage permet de forcer
  l'anglais.
* **Le pluriel n'est pas géré.** `.resx` et `string.Format` n'ont pas d'ICU MessageFormat. Aucun
  message actuel n'en a besoin ; si cela vient, c'est un utilitaire à écrire, pas un changement de
  format.
* **Un changement de langue ne repeint pas les fenêtres ouvertes.** WinForms compose ses libellés
  à la construction ; le menu, reconstruit à chaque ouverture, suit immédiatement, la fenêtre
  d'édition à sa prochaine ouverture (SPEC-CFG-004, règle 3).
* **Ajouter une langue** consiste à ajouter un `Strings.<culture>.resx` et une position au
  réglage : aucun code de présentation à toucher.
