# ADR-0003 — Une icône est une source, pas une image

* **Statut** : accepté
* **Contexte** : la moitié de la complexité de l'ancien code portait sur les icônes.
  `IconHelpers` mêlait la décision (icône explicite ? extraction ? suivi d'un `.lnk` ?) et la
  fabrication (`new Icon(path)`, `Icon.ExtractAssociatedIcon`, `ToBitmap`). Comme ces méthodes
  rendaient des `Bitmap`, la décision était inséparable de `System.Drawing`, donc intestable — et
  la libération des ressources graphiques devenait le problème de tout le monde.

  Le symptôme le plus parlant tenait en une signature :

  ```csharp
  private static void DisposeMenuImages(ToolStripItemCollection items, Image skip)
  ```

  Ce paramètre `skip` existait parce que tous les dossiers partageaient une même instance
  d'image : il fallait parcourir récursivement l'arbre du menu en épargnant celle-là. Un
  traitement particulier, facile à oublier, dont l'oubli fuit des handles GDI.

## Décision — séparer « quelle icône » de « quelle image »

Le domaine porte un `IconSource` : une **manière d'obtenir** une image (aucune, un fichier
`.ico`, une extraction), un chemin, et un **repli**.

```csharp
IconSource.FromIconFile(configuration.PathFolderIcon)
    .Or(IconSource.FromIconFile(TrayShortcutConfiguration.DefaultFolderIcon))
```

* la **décision** vit dans `Application.Menu.IconSourceResolver` — trois règles, quinze tests,
  aucun écran ;
* la **fabrication** vit dans `Ui.Icons.IconRenderer` — seule classe du dépôt à connaître
  `System.Drawing`, et qui ne décide rien : elle descend la chaîne et prend la première image
  obtenue.

Le repli fait partie de la source, et non du rendu : « l'icône configurée, sinon celle livrée »
est une décision, pas un détail d'affichage.

## Ce que cela règle, au-delà de la testabilité

**Le paramètre `skip` disparaît.** `IconSource` étant un `record`, son égalité est structurelle :
tous les dossiers ont la même source, donc la même clé de cache. Le rendu fabrique une image par
source distincte, **retient tout ce qu'il a fabriqué**, et libère l'ensemble à la reconstruction
suivante. Deux entrées peuvent partager une image sans risque, puisque c'est le rendu qui en est
propriétaire et non les éléments de menu. Il n'y a plus de cas particulier, donc plus de cas
particulier à oublier.

## Conséquences

* Les règles d'icône se vérifient en une ligne d'assertion sur `Kind` et `Path`.
* La chaîne de replis est elle-même vérifiable : `icone.Chain()` rend la liste des tentatives.
* Une source vide ne rallonge jamais la chaîne — `None.Or(x)` vaut `x`, `x.Or(None)` vaut `x` —,
  de sorte que la chaîne ne contient que des tentatives réellement utiles.
* Le comportement du rendu lui-même (un `.ico` tronqué, un fichier verrouillé) reste hors des
  tests automatisés : c'est SPEC-ICON-004, inscrit dans la liste des zones vérifiées à la main.
* **Coût assumé** : une indirection de plus entre « je veux une icône » et « voici un Bitmap ».
  Elle se paie une fois, à la lecture ; le `skip` se payait à chaque modification du menu.
