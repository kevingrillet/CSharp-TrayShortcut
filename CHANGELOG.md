# Journal des modifications

Toutes les évolutions notables de Tray Shortcut sont consignées ici.

Le format suit [Keep a Changelog](https://keepachangelog.com/fr/1.1.0/) et le versionnage suit
[SemVer](https://semver.org/lang/fr/). Les catégories utilisées sont *Ajouté*, *Modifié*,
*Corrigé*, *Supprimé*, *Sécurité*.

> **Convention du dépôt** : toute modification de comportement visible par l'utilisateur ajoute une
> ligne dans `[Non publié]`, en même temps que la spec et le scénario Gherkin correspondants. La
> checklist de `.github/pull_request_template.md` le rappelle.

> **Publier une version** : la section `[Non publié]` porte le numéro annoncé par `<Version>` dans
> `Directory.Build.props` — **1.0.0** aujourd'hui. Publier, c'est renommer cette section en
> `[1.0.0] — <date>`, ouvrir une nouvelle section `[Non publié]`, monter `<Version>` au numéro
> suivant, puis pousser le tag `v1.0.0` : c'est ce tag qui déclenche `release.yml` et qui fixe la
> version du binaire livré.

## [Non publié]

Refonte complète de la structure du projet. Le comportement visible change peu ; ce qui change,
c'est que l'application ne disparaît plus quand quelque chose se passe mal, et qu'elle fonctionne
installée n'importe où.

### Ajouté

- **Les icônes du menu sont réutilisées au lieu d'être refabriquées** (`SPEC-ICON-004`,
  [ADR-0006](docs/adr/0006-cache-des-icones.md)). Windows attribue à un document l'icône de son
  type : un sous-dossier de trente PDF coûte désormais **deux** extractions au lieu de
  trente-cinq — ce qui se sent surtout sur un dossier réseau. Les images survivent à un
  *Actualiser*, et sont invalidées dès qu'un fichier change, de sorte qu'une application mise à
  jour montre bien sa nouvelle icône.
- **Un fichier de configuration abîmé est mis de côté et recréé** (`SPEC-CFG-001`, règle 7).
  L'ancien contenu est conservé sous un nom horodaté `.invalide` — une accolade oubliée ne fait
  plus perdre le travail —, et un fichier valide reprend sa place, prêt à être édité. Une lecture
  qui échoue pour une autre raison (verrou momentané, droits) ne déplace rien.
- **Un raccourci personnalisé peut viser un dossier ou une adresse web** (`SPEC-LAUNCH-003`,
  [ADR-0005](docs/adr/0005-cibles-de-lancement.md)). Un dossier s'ouvre dans l'explorateur, une
  adresse `http`, `https` ou `mailto` dans l'application associée. Le README l'annonçait depuis
  toujours — « lien ou exécutable à lancer » — mais un contrôle d'existence de fichier l'interdisait.
- **Interface en français ou en anglais**, ou selon Windows (`SPEC-UI-LANG-001`,
  [ADR-0004](docs/adr/0004-multilingue.md)). Réglage `Language` : `System`, `French`, `English`.
- **Un sous-dossier vide le dit** au lieu d'ouvrir un sous-menu vide (`SPEC-MENU-003`).
- **Un journal d'exécution** dans `%APPDATA%\TrayShortcut\log.txt`, avec rotation à 1 Mo
  (`SPEC-APP-003`). Il répond à la seule question qu'on se pose sur cette application : qu'est-ce qui
  a été ignoré, et pourquoi.
- **Raccourcis clavier** dans la fenêtre d'édition : `Ctrl+S` pour enregistrer, `Ctrl+Suppr` pour
  supprimer la ligne courante.
- **Une suite de tests unitaires**, une documentation de conception (`docs/`) et une intégration
  continue qui compile, teste et vérifie la mise en forme.

### Modifié

- **La configuration vit désormais dans `%APPDATA%\TrayShortcut\config.json`**
  ([ADR-0002](docs/adr/0002-donnees-dans-appdata.md)), et non plus à côté de l'exécutable.
  L'application fonctionne donc installée sous `Program Files`, la configuration survit au
  remplacement du dossier d'installation, et deux comptes Windows ont chacun la leur.
  **Aucune migration n'est faite** : l'ancien `config.json` est ignoré, et le dossier à surveiller
  est redemandé au premier démarrage.
- **Le dossier à surveiller se choisit par le sélecteur de dossier de Windows**, non par une saisie
  libre (`SPEC-CFG-002`).
- **La section des raccourcis personnalisés s'intitule « Raccourcis personnalisés »** au lieu de
  « Customs », et les commandes du menu sont traduites.
- **L'ordre du menu tient compte des accents** : « audio », « Éditeurs », « Zip » — et il est le même
  sur toutes les machines (`SPEC-MENU-002`).
- **Un raccourci personnalisé sans intitulé prend le nom du fichier visé** au lieu de s'afficher sans
  texte (`SPEC-MENU-002`).
- **Le fichier de configuration s'ouvre dans l'éditeur associé aux fichiers `.json`**, et non dans le
  Bloc-notes imposé.
- **Les rapports d'erreur imprévue vont dans le dossier de données**, et leur chemin est affiché à
  l'utilisateur (`SPEC-APP-002`). Ils étaient auparavant écrits à côté de l'exécutable, et
  l'application disparaissait sans rien dire.
- Cible portée de .NET 8 à **.NET 9**. Le poste doit avoir le runtime .NET 9 Desktop (x64).
- L'icône livrée `icon.ico` est renommée `tray-shortcut.ico`. Une configuration désignant
  `icon.ico` retombe sur l'icône par défaut, qui est ce même fichier : l'affichage est inchangé.
- Dependabot passe d'un rythme quotidien à hebdomadaire, et surveille désormais aussi les paquets
  NuGet.

### Corrigé

- **Un clic sur une entrée dont la cible a disparu ne ferme plus l'application**
  (`SPEC-LAUNCH-002`). Le gestionnaire de clic levait une exception, qui remontait au gestionnaire
  d'exceptions non gérées, qui écrivait un rapport et terminait le processus : l'icône disparaissait
  sur un clic malheureux.
- **Annuler la demande de dossier ne bloque plus l'application** (`SPEC-CFG-002`, règle 3). L'invite
  était réaffichée indéfiniment ; il ne restait que le gestionnaire de tâches pour en sortir.
- **Un enregistrement de configuration qui échoue est signalé** au lieu de fermer l'application
  (`SPEC-CFG-001`, règle 5). C'était le cas de toute installation dans un dossier protégé.
- **Une coupure pendant l'enregistrement ne détruit plus la configuration existante** : l'écriture
  passe par un fichier temporaire (`SPEC-CFG-001`, règle 4).
- **Un fichier de configuration abîmé n'empêche plus le démarrage** : les valeurs par défaut prennent
  le relais et l'incident est journalisé (`SPEC-CFG-001`, règle 3).
- **Le réglage `Language` se lit sous la forme documentée** — `"French"` et non `1`
  (`SPEC-CFG-001`, règle 6). Écrit en clair comme l'indique le README, il faisait rejeter la
  configuration **entière** : le dossier surveillé était oublié avec le reste, et l'application
  redemandait un dossier à chaque démarrage. Les noms de réglages sont par ailleurs insensibles à la
  casse, et un réglage inconnu — écrit par une version plus récente — est ignoré au lieu de tout
  invalider.
- **La dernière cellule saisie dans la fenêtre d'édition n'est plus perdue** à l'enregistrement
  (`SPEC-CFG-003`, règle 2).
- **Plus de fuite de ressources graphiques en actualisant** : les images appartiennent au rendu
  d'icônes, qui les borne et les libère ([ADR-0003](docs/adr/0003-icone-source-et-non-image.md),
  [ADR-0006](docs/adr/0006-cache-des-icones.md)).
- **Un réglage laissé vide n'est plus écrit comme `null`** dans le fichier de configuration, qui
  reste lisible à l'œil.

### Sécurité

- **Les schémas d'adresse lançables sont une liste blanche** de trois entrées — `http`, `https`,
  `mailto` (`SPEC-LAUNCH-003`, règle 3). Un fichier de configuration ne peut pas déclencher un
  gestionnaire de protocole arbitraire du poste.

### Supprimé

- Le fichier `CodeMaid.config` et l'ancienne solution `.sln` au format Visual Studio 2022, remplacée
  par `CSharp-TrayShortcut.slnx`.
- La classe `CustomToolStripMenuItem` : le menu est désormais décrit comme une donnée
  (`Domain/Menu/MenuEntry.cs`) que la présentation traduit.
