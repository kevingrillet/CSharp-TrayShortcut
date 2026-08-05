# ADR-0006 — Cache des icônes : la clé est une règle, l'image est une ressource

* **Statut** : accepté
* **Contexte** : [ADR-0003](0003-icone-source-et-non-image.md) avait séparé la décision (*quelle*
  icône) de la fabrication (*quelle* image), et confié les images au rendu du menu, qui les
  libérait à chaque reconstruction. Correct, mais coûteux à deux titres.

  D'abord, la clé de réutilisation était l'`IconSource` **entière**, chaîne de replis comprise, et
  le cache ne vivait que le temps d'un rendu. Chaque *Actualiser* refabriquait donc tout.

  Ensuite, et surtout : l'`IconSource` d'un fichier contient son **chemin**. Trente fichiers PDF
  dans un dossier donnaient trente clés distinctes, donc trente appels à
  `Icon.ExtractAssociatedIcon`, donc trente images — **identiques**. Sur un partage réseau, chacun
  de ces appels est un aller-retour.

## L'observation qui décide

`Icon.ExtractAssociatedIcon` ne fait pas la même chose selon le fichier :

| Fichier | Ce que rend Windows | Réutilisable par |
|---|---|---|
| `rapport.pdf`, `notes.txt` | l'icône associée au **type de fichier** | son **extension** |
| `app.exe`, `shell32.dll`, `logo.ico` | l'icône **portée par le fichier** | son **chemin** |

C'est une propriété du système, pas un détail d'implémentation : elle mérite donc d'être une
règle, à sa place et testée.

## Décision 1 — La clé de cache est une décision de la couche application

`Application.Menu.IconCachePolicy` produit un `IconCacheKey` (domaine) :

* **document** → clé par extension normalisée en minuscules ;
* **exécutable** → clé par chemin **et empreinte du fichier** (date de dernière écriture, taille) ;
* **icône désignée explicitement** → clé par chemin, toujours : deux `.ico` différents partagent
  leur extension mais pas leur image ;
* **fichier sans extension** → traité comme un exécutable. Se tromper par excès de prudence coûte
  une fabrication de plus ; l'inverse afficherait la mauvaise image.

La liste des extensions « à icône propre » est courte et fermée (`.exe`, `.dll`, `.ico`, `.cpl`,
`.scr`, `.msc`, `.ocx`). Elle est délibérément statique : la question « ce fichier porte-t-il sa
propre icône ? » n'a pas de réponse bon marché sous Windows, et l'interroger coûterait ce que le
cache économise. `.lnk` n'y figure pas — un raccourci est déjà résolu vers sa cible avant
d'arriver là (SPEC-ICON-003), c'est donc l'extension de la cible qui est examinée.

La clé porte sur un **maillon** de la chaîne, plus sur la chaîne entière : deux configurations
différentes qui se replient sur l'icône livrée avec l'application la partagent désormais.

L'empreinte demande une méthode de plus sur le port `IShortcutSource` :
`GetFileStamp(path)`. C'est une lecture de **métadonnées**, pas de contenu — bien moins chère que
l'extraction qu'elle permet d'éviter.

## Décision 2 — L'image appartient au rendu d'icônes, plus au rendu du menu

`Ui.Icons.IconRenderer` détient le cache, pour la durée de vie de l'application. `MenuRenderer`
demande une image et l'affiche ; il n'en libère plus aucune. Un pan entier de sa logique
disparaît, et avec lui la dernière trace du problème que
[ADR-0003](0003-icone-source-et-non-image.md) avait entrepris de régler.

### Éviction aux frontières de rendu, et nulle part ailleurs

C'est le point délicat. Libérer une image qu'un menu vivant affiche encore la ferait peindre
après destruction. L'éviction n'a donc lieu que dans `BeginRender()`, à l'instant précis où le
menu précédent est abandonné — jamais pendant la construction paresseuse d'un sous-menu, qui
survient plus tard.

Un compteur de génération marque les images servies par chaque menu. Au-delà de 512 entrées,
celles qui n'ont pas servi lors des **deux** derniers menus sont libérées : les images du menu qui
vient de s'achever sont épargnées, car elles resserviront immédiatement si la configuration n'a
pas changé.

La borne existe pour qu'une arborescence de plusieurs milliers d'éléments, parcourue au fil des
heures, n'épuise pas les handles graphiques du processus. Elle est large : un dossier de
raccourcis normal ne l'atteint jamais, et le cas où elle mord est celui où le cache sert le moins.

## Conséquences

* Un sous-dossier de trente documents coûte **deux** extractions au lieu de trente-cinq.
* *Actualiser* ne refabrique que ce qui a changé, au prix d'une lecture de métadonnées par
  exécutable.
* Une application mise à jour montre bien sa nouvelle icône, sans redémarrer Tray Shortcut.
* Les échecs sont mis en cache eux aussi : un fichier d'icône absent n'est pas retenté à chaque
  entrée qui le désigne.
* **Coût assumé** : une méthode de plus sur le port, une classe de règle, et une invariante à
  respecter — n'évincer qu'au début d'un rendu. Elle est écrite dans la documentation XML de
  `IconRenderer.BeginRender` et dans la fiche `respecter-architecture`, parce que la violer
  produirait un défaut d'affichage difficile à relier à sa cause.
* **Ce qui a été écarté** : un cache des énumérations de dossier. Obtenir l'empreinte d'un dossier
  demande de toute façon un aller-retour, et sur un dossier de dix éléments l'économie est nulle.
  Le gain n'apparaîtrait que sur de très grands dossiers distants — à reconsidérer si le cas se
  présente, mais pas avant.
