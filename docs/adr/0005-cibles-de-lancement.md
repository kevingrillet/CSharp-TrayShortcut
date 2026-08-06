# ADR-0005 — Ce qu'on accepte de lancer

* **Statut** : accepté
* **Contexte** : le README annonce depuis toujours, pour le réglage `Path` d'un raccourci
  personnalisé, « lien ou exécutable à lancer » / « path or executable to launch ». Le code, lui,
  commençait par `if (!File.Exists(path)) return;`. Une adresse web n'a donc jamais fonctionné,
  ni un dossier. La documentation promettait une fonctionnalité que le contrôle d'existence
  interdisait.

  Deuxième problème, plus sérieux, dans le même gestionnaire de clic :

  ```csharp
  if (sender is not CustomToolStripMenuItem)
      throw new ApplicationException(...);
  if (string.IsNullOrWhiteSpace((sender as CustomToolStripMenuItem).Path))
      throw new ApplicationException("Path is required.");
  ```

  Ces exceptions étaient levées **depuis un gestionnaire d'événement WinForms**. Elles
  remontaient donc au gestionnaire d'exceptions non gérées, qui écrivait un rapport de plantage et
  fermait l'application. Un raccourci mal renseigné, cliqué par erreur, faisait disparaître
  l'icône de la zone de notification.

## Décision 1 — Une cible impossible n'est pas constructible

`LaunchTarget.TryCreate` refuse un chemin vide et rend `null`. Un `LaunchTarget` qui existe porte
toujours un chemin exploitable, et le code de lancement n'a plus à s'en assurer. Les deux
exceptions ci-dessus disparaissent : il n'y a plus d'état à valider au clic, et une ligne de
configuration à moitié remplie est simplement absente du menu (SPEC-MENU-005, règle 1).

## Décision 2 — Trois natures de cible, une liste blanche de schémas

`LaunchService.Inspect` reconnaît un **fichier**, un **dossier**, ou une **adresse** dont le
schéma est autorisé — et rien d'autre (SPEC-LAUNCH-003). Cela tient la promesse du README.

Les schémas autorisés sont `http`, `https` et `mailto`. Une liste blanche et non « tout ce que
`Uri.TryCreate` accepte » : passer un schéma arbitraire à `UseShellExecute` revient à laisser un
fichier de configuration déclencher n'importe quel gestionnaire de protocole installé sur le
poste — `ms-settings:`, un protocole applicatif tiers, voire un gestionnaire vulnérable. Le
bénéfice serait nul, la surface d'attaque non.

### Pourquoi `file:` est exclu

C'est le point subtil, découvert en écrivant le test. `Uri.TryCreate(@"D:\parti.exe",
UriKind.Absolute, out _)` **réussit** : un chemin Windows, comme un chemin UNC, constitue une
adresse `file:` syntaxiquement valide. Admettre ce schéma dans la liste blanche aurait donc
classé **tout chemin disparu** en cible valide, et vidé SPEC-LAUNCH-002 de son sens : la cible
aurait été soumise au shell, qui aurait échoué, au lieu d'être écartée en amont.

Les chemins locaux doivent être jugés sur leur existence, pas sur leur syntaxe. Le schéma
n'apporte par ailleurs rien qu'un chemin nu ne fasse déjà. Corollaire : l'existence sur le disque
est testée **avant** l'interprétation comme adresse.

## Décision 3 — Un échec de lancement se rapporte, il ne se lève pas

`IProcessLauncher.Launch` rend un booléen. Un refus du shell — aucune application associée,
exécutable bloqué par une stratégie de groupe, blocage par l'antivirus — est consigné au journal
avec sa cause, et l'application continue.

## Conséquences

* Une adresse web ou de courriel devient un raccourci personnalisé valide. C'est une
  **fonctionnalité nouvelle** de l'utilisateur, même si le README la décrivait déjà.
* Un dossier aussi, ce qui ouvre l'explorateur au bon endroit.
* Un clic sur une entrée périmée ne ferme plus l'application.
* Un chemin UNC est jugé sur son existence : injoignable, il est écarté sans bruit — ce qui est le
  comportement voulu pour un partage déconnecté.
* **Coût assumé** : ajouter un schéma demande de modifier la liste blanche et son test. C'est
  voulu : chaque schéma admis est une décision de sécurité, pas un réglage.
