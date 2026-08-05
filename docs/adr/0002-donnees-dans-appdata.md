# ADR-0002 — Configuration et journaux dans le dossier de données de l'utilisateur

* **Statut** : accepté
* **Contexte** : la configuration vivait dans `Configurations\config.json`, copié à côté de
  l'exécutable par une règle `CopyToOutputDirectory`. Les rapports de plantage s'écrivaient
  eux aussi à côté de l'exécutable, via `AppContext.BaseDirectory`.

## Le problème

Un dossier sous `Program Files` n'est pas accessible en écriture à un utilisateur ordinaire.
L'ancien emplacement avait donc trois conséquences, dont la dernière est la plus gênante :

1. l'application ne fonctionnait correctement que depuis un dossier utilisateur — un dossier
   personnel, un bureau, une clé USB ;
2. deux comptes Windows sur la même machine partageaient la même configuration, chacun écrasant
   celle de l'autre ;
3. **l'enregistrement échouait en silence.** `File.WriteAllText` levait une exception non
   traitée, qui remontait au gestionnaire d'exceptions non gérées, qui écrivait un rapport — dans
   le même dossier inaccessible, donc pas du tout — et fermait l'application. L'utilisateur
   voyait son icône disparaître au moment d'enregistrer, sans explication.

Remplacer le dossier d'installation lors d'une mise à jour effaçait par ailleurs la
configuration.

## Décision

Tout ce qui est écrit va dans `%APPDATA%\TrayShortcut\` :

| Fichier | Rôle |
|---|---|
| `config.json` | configuration (SPEC-CFG-001) |
| `log.txt`, `log.txt.1` | journal, avec une génération de rotation (SPEC-APP-003) |
| `crash-<horodatage>-<discriminant>.txt` | rapport d'erreur imprévue (SPEC-APP-002) |

Ce qui est **livré** avec l'application — les fichiers `.ico` fournis — reste à côté de
l'exécutable, dans un sous-dossier `Icons\`, et n'est jamais écrit.

## Conséquences

* La configuration survit au remplacement du dossier d'installation.
* Chaque compte Windows a la sienne, ce qui est aussi ce qu'attend SPEC-APP-001 pour deux
  utilisateurs connectés simultanément.
* L'application fonctionne installée n'importe où, `Program Files` compris.
* Une configuration désignant une icône par son seul nom continue de fonctionner : ce nom est
  cherché parmi les icônes livrées (SPEC-ICON-002, règle 2).
* **Aucune migration n'est faite.** Un `config.json` laissé à côté d'un ancien exécutable est
  ignoré, et l'application demande le dossier à surveiller comme au premier démarrage. Écrire une
  migration pour un fichier de cinq réglages, dont le principal se resaisit en trois clics, ne
  valait pas le code à maintenir ensuite.
* Le dossier de données peut lui-même être indisponible — profil itinérant en panne, disque
  plein. Dans ce cas on renonce au journal, jamais au démarrage (SPEC-APP-003, règle 3).
