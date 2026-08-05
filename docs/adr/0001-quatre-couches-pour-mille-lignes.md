# ADR-0001 — Quatre couches pour un millier de lignes

* **Statut** : accepté
* **Contexte** : l'application tenait en 997 lignes réparties sur 11 fichiers d'un projet
  unique, organisés par nature technique — `Entities/`, `Forms/`, `Helpers/`. Aucun test
  n'existait, et aucun n'était possible : la classe centrale de 259 lignes énumérait le disque,
  extrayait des icônes via `System.Drawing`, construisait des `ToolStripMenuItem`, gérait la
  libération de handles GDI et lançait des processus — le tout dans les mêmes méthodes. Vérifier
  « les dossiers viennent avant les fichiers » aurait demandé un écran, un disque et un shell.

## Décision — découper en Domain / Application / Infrastructure / Ui

Quatre projets, dépendances tournées vers l'intérieur :

| Couche | Référence | Cible | Contient |
|---|---|---|---|
| `Domain` | **rien** | `net9.0` | entités, objets-valeur, clés de textes |
| `Application` | `Domain` | `net9.0` | cas d'usage, règles, **ports** |
| `Infrastructure` | `Application` | `net9.0-windows` | disque, COM, processus, JSON, journal |
| `Ui` | `Application` + `Infrastructure` | `net9.0-windows` | WinForms, images, composition |

## Pourquoi quatre et pas deux

L'objection est réelle : quatre projets pour mille lignes, c'est plus de fichiers de projet que
de classes dans certains dossiers. Trois raisons l'emportent.

**Le cœur devient testable, et il ne l'était pas du tout.** 92 tests couvrent la composition du
menu, l'ordre d'affichage, le choix des icônes, la validation de configuration et le lancement.
Ils s'exécutent en une centaine de millisecondes, sans écran ni disque. Ce n'est pas un gain
théorique : les deux anomalies décrites en [ADR-0005](0005-cibles-de-lancement.md) et dans
SPEC-CFG-002 ont été trouvées en écrivant ces tests.

**La cible de compilation fait le travail de police.** `Application` cible `net9.0` et non
`net9.0-windows` : un `using System.Windows.Forms` y devient une **erreur de compilation**, pas
une remarque en relecture. La règle d'architecture est vérifiée par l'outil, gratuitement, à
chaque build.

**Il y a réellement quatre natures de code ici.** L'interopérabilité COM avec `IShellLink`, le
dossier de données de l'utilisateur, l'extraction d'icônes et WinForms ne sont pas la même chose
que « quelle icône montrer » ou « dans quel ordre trier ». La frontière la plus rentable est
celle entre décider et dessiner, détaillée en
[ADR-0003](0003-icone-source-et-non-image.md).

## Conséquences

* Le menu est décrit comme une **donnée** (`IReadOnlyList<MenuEntry>`) que l'interface traduit en
  `ToolStripItem`. C'est ce qui rend l'ordre et la présence des entrées vérifiables.
* Une seule classe du dépôt connaît `System.Drawing`, une seule connaît COM, une seule connaît le
  système de fichiers. Chacune est remplaçable par un double de test.
* La classe centrale passe de 259 lignes mêlant cinq responsabilités à un enchaînement d'une
  centaine de lignes qui n'en porte aucune règle.
* Remplacer WinForms par WPF, ou passer en service Windows, se limiterait au projet `Ui`.
* **Coût assumé** : un ajout de fonctionnalité touche parfois trois projets (un port, un
  adaptateur, une règle). Le skill `respecter-architecture` indique où va quoi.
