---
name: cadrer-un-comportement
description: "Transformer une demande floue en spécification rédigeable : choisir la famille et le numéro d'identifiant, dérouler l'interrogatoire qui produit la liste « Règles », et rendre un squelette *Étant donné / Quand / Alors* prêt à coller — ou le constat que la demande n'est pas cadrable en l'état. À utiliser **avant** d'écrire dans `docs/specs/`, dès qu'une demande arrive sous la forme « il faudrait aussi que… », « ça devrait faire… », « et si… »."
---

# Cadrer un comportement

Les autres fiches commencent une fois le comportement décidé : elles disent **comment** l'écrire, le
tester, l'implémenter. Celle-ci produit la décision.

Sa sortie est une section `## SPEC-XXX-0NN` prête à coller dans `docs/specs/`, avec sa liste
« Règles » complète — ou le constat argumenté que la demande n'est pas cadrable, et la question
précise à poser. Rien d'autre : ni scénario, ni test, ni code.

La démarche d'ensemble est dans [`docs/CONTRIBUER.md`](../../../docs/CONTRIBUER.md) §1.
**Ne pas la recopier.**

## 0. Est-ce bien une spec ?

Le test d'entrée tient en une phrase : **si on ne sait pas énoncer l'assertion qui échouerait, ce
n'est pas une spec.** Trois cas de renvoi immédiat :

| La demande porte sur | Ce n'est pas une spec, c'est | Où ça va |
|---|---|---|
| la façon dont c'est assemblé, ce que coûte une ouverture de menu | de la conception | `docs/SDD.md`, une section numérotée |
| un choix entre deux options crédibles | une décision | `docs/adr/000N-titre.md` |
| l'usage, un réglage à documenter | de l'aide | `README.md` |

Et deux renvois de périmètre, à trancher **avant** de rédiger :

* **écrire dans le système de fichiers** (créer, renommer, supprimer) est hors périmètre —
  `docs/SDD.md` §2.3. L'application lit et lance, rien de plus. Y toucher demande un ADR ;
* **lever une exception depuis un gestionnaire d'événement** est interdit par
  [ADR-0005](../../../docs/adr/0005-cibles-de-lancement.md). Si la demande dit « et sinon on affiche
  une erreur », reformuler en « on n'affiche rien et on journalise », ou justifier l'écart.

## 1. Choisir la famille et le numéro

| Famille | Ce qu'elle couvre | Fichier |
|---|---|---|
| `SPEC-MENU` | ce qu'on trouve dans le menu, dans quel ordre, à quel moment c'est calculé | `docs/specs/SPEC-MENU.md` |
| `SPEC-ICON` | quelle icône est montrée, et le repli | `docs/specs/SPEC-ICONES.md` |
| `SPEC-LAUNCH` | ce qui s'ouvre au clic, et ce qui constitue une cible valide | `docs/specs/SPEC-LANCEMENT.md` |
| `SPEC-CFG` | ce qui se règle, se valide, se persiste, et quand ça prend effet | `docs/specs/SPEC-CONFIGURATION.md` |
| `SPEC-UI-LANG` | langue de l'interface et emplacement des formulations | `docs/specs/SPEC-INTERFACE.md` |
| `SPEC-UI-ICON` | icône de l'application, info-bulle | `docs/specs/SPEC-ICONES.md` |
| `SPEC-APP` | comportement du processus : instance unique, erreurs, journal | `docs/specs/SPEC-INTERFACE.md` |

Un identifiant **ne se renumérote jamais** et ne se réutilise pas : il est cité dans les tests, les
scénarios, la traçabilité et le code. Le prochain libre, par famille :

```powershell
Select-String -Path docs\specs\*.md -Pattern '^#+\s+(SPEC-[A-Z-]+?)-(\d+)' |
    ForEach-Object { $_.Matches[0] } |
    Group-Object { $_.Groups[1].Value } |
    ForEach-Object { '{0}-{1:000}' -f $_.Name, (1 + ($_.Group | ForEach-Object { [int]$_.Groups[2].Value } | Measure-Object -Maximum).Maximum) }
```

Si aucune famille ne convient, c'est probablement que la demande relève du §0.

## 2. L'interrogatoire

Chaque question ci-dessous a déjà produit une règle numérotée dans une spec existante. Les dérouler
**toutes** : c'est leur exhaustivité qui fait la valeur de la fiche.

### Périmètre

| Question | Ce qu'elle produit | Précédent |
|---|---|---|
| Cela vaut-il pour la racine, les sous-dossiers, ou les deux ? | une garde explicite | `SPEC-MENU-001` §3 : le réglage des fichiers de la racine ne touche pas les sous-dossiers |
| Et si la donnée est absente du fichier de configuration ? | **systématiquement** : quelle valeur vaut l'absence, et pourquoi | `SPEC-MENU-001` §3 : l'absence vaut « oui », pour ne pas changer le comportement des configurations existantes |
| Et si le chemin concerné a disparu depuis l'ouverture du menu ? | une règle de réexamen au clic | `SPEC-LAUNCH-002` §1 |
| Quels cas *ressemblants* doivent rester muets ? | une règle d'exclusion par cas | `SPEC-MENU-005` §1 (ligne sans chemin), `SPEC-ICON-001` §3 (fichier absent) |

Ce dernier point est le plus rentable : dans ce dépôt, beaucoup de règles sont des exclusions.
Formuler chacune comme un futur nom de test — `Un_raccourci_sans_chemin_est_ignore_en_silence`.

### Collision avec l'existant

| Question | Ce qu'elle produit | Précédent |
|---|---|---|
| Quelle spec voisine décrit **le même fait** sous un autre nom ? | une règle de priorité | `SPEC-LAUNCH-003` §4 : le disque est testé avant l'adresse |
| Y a-t-il un ordre entre deux règles, et lequel gagne ? | l'ordre explicite, avec sa raison | `SPEC-ICON-001` : l'icône explicite prime toujours |

### Ce que ça coûte

| Question | Si la réponse est « non » |
|---|---|
| La donnée est-elle déjà dans la configuration ? | un champ à ajouter à `TrayShortcutConfiguration` — et décider ce que vaut son absence (voir ci-dessus) |
| La donnée est-elle déjà rapportée par un port existant ? | une méthode de plus sur un port, avec son **contrat de tolérance** → skill [`respecter-architecture`](../respecter-architecture/SKILL.md) §5 |
| Cela peut-il se décider sans écran, sans disque et sans shell ? | si non : une ligne dans « Zones sans test automatisé » de `docs/TRACEABILITE.md` **avec sa raison et son mode de vérification**, plus l'identifiant dans `VerificationManuelleOuAVenir` |
| Cela ajoute-t-il une énumération de dossier ou une extraction d'icône par ouverture de menu ? | chiffrer le surcoût comme `docs/SDD.md` §5.4 — l'ouverture doit rester instantanée sur un partage réseau (ENF-1) |
| Le comportement est-il traduisible ? | une clé dans `TextKeys` et **deux** formulations ; un test de garde échoue sinon |

### Sortie observable

| Question | Ce qu'elle produit |
|---|---|
| Que voit exactement l'utilisateur ? | les éléments cités nommément dans le *Alors* — « le menu est correct » ne se teste pas utilement |
| Que se passe-t-il quand ça échoue ? | **systématiquement** : rien d'affiché, une trace au journal, l'application qui continue (`SPEC-MENU-004` §3, `SPEC-LAUNCH-002`) |
| Faut-il actualiser pour que ça prenne effet ? | une ligne dans `SPEC-CFG-004` |

## 3. Rendre le squelette

```markdown
## SPEC-XXX-0NN — <le fait, du point de vue de l'utilisateur>

*Étant donné* <l'état de départ, configuration et contenu du dossier compris>
*Quand* <le geste ou l'événement — un seul déclencheur>
*Alors* <ce que l'utilisateur voit, nommément>.

Règles :

1. <chaque réponse restrictive du §2, une par ligne, avec l'identifiant de la spec qui prend le
   relais quand il y en a une>
2. <ce que vaut l'absence de la donnée dans le fichier de configuration>
3. <ce qui se passe quand l'accès échoue : rien d'affiché, une trace au journal>
```

Vocabulaire imposé : le **dossier surveillé** est celui que désigne `Path` ; la **racine** est son
premier niveau ; un **raccourci personnalisé** est une entrée déclarée dans la configuration. Jamais
de nom de classe, de type WinForms ni de format de fichier dans une spec.

Relecture à voix haute avant de coller : un *Quand* qui contient « ou » cache deux déclencheurs, un
*Alors* qui promet deux effets cache deux specs.

## 4. Quand rendre la main plutôt qu'écrire

Ces signaux disent que la demande n'est pas mûre. Poser **une** question précise vaut mieux qu'une
spec que le code contredira :

* le *Quand* suppose de surveiller le dossier en continu — l'application ne fait rien entre deux
  ouvertures de menu (`docs/SDD.md` §4), et changer cela est un ADR ;
* la demande suppose d'écrire dans le dossier surveillé : hors périmètre (§0) ;
* le comportement dépend d'un réglage qui n'existe pas encore — décider d'abord si on l'ajoute
  (`SPEC-CFG`) ;
* personne ne sait dire ce que l'utilisateur devrait voir : le besoin est « ce serait pratique », pas
  un comportement.

Formuler alors l'hypothèse retenue **explicitement** et poursuivre ce qui n'en dépend pas — famille,
numéro, règles déjà tranchées.

## 5. Après le cadrage

L'ordre reste **spec → scénario → test → code**, et le scénario Gherkin n'est pas optionnel :
`FeatureCoverageTests` fait échouer `dotnet test` dès qu'une catégorie `[Category("SPEC-…")]` n'a pas
son tag `@SPEC-…` dans `docs/features/`.

Enchaîner sur [`ecrire-un-test`](../ecrire-un-test/SKILL.md), puis le code, puis
[`verifier-avant-commit`](../verifier-avant-commit/SKILL.md).
