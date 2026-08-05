# Intégration continue

Ce document décrit les pipelines du dépôt, les runners qu'ils exigent, et comment rejouer
exactement les mêmes contrôles sur un poste de développement.

---

## 1. Vue d'ensemble

| Fichier | Déclencheur | Rôle |
| --- | --- | --- |
| `.github/workflows/ci.yml` | `push` et `pull_request` sur `main` / `master` | Compilation Release, tests unitaires, contrôle de mise en forme |
| `.github/workflows/release.yml` | poussée d'un tag `v*` | Publication `win-x64`, archive ZIP, release GitHub |
| `.github/workflows/codeql-analysis.yml` | planifié et sur `push` | Analyse statique de sécurité |
| `.github/workflows/links.yml` | planifié | Vérification des liens de la documentation |
| `.github/dependabot.yml` | hebdomadaire (lundi 06:00, Europe/Paris) | Mises à jour des paquets NuGet et des actions |
| `.github/workflows/dependabot-auto-merge.yml` | sur PR Dependabot | Fusion automatique des montées de version mineures validées par la CI |

---

## 2. Ce que la CI vérifie

Quatre contrôles, ni plus ni moins :

1. **`dotnet restore`** — la restauration passe avec la seule source déclarée dans `NuGet.Config`
   (nuget.org). Ce contrôle attrape le classique « ça restaure chez moi » dû à un flux d'entreprise
   configuré uniquement en local.
2. **`dotnet build -c Release`** — la solution compile en Release. Aucun paramètre n'est ajouté pour
   neutraliser les avertissements : la solution étant configurée en `TreatWarningsAsErrors`, **le
   moindre avertissement fait échouer la CI**.
3. **`dotnet format --verify-no-changes`** — le code respecte le `.editorconfig` (indentation, fins
   de ligne, espaces, tri des `using`). Cette vérification tourne dans une tâche séparée : un rouge
   ici signifie « reformate », pas « le code est cassé ».
4. **`dotnet test -c Release`** — les 92 tests NUnit du domaine et de la couche application passent.
   Le rapport `.trx` est publié en artefact, y compris — et surtout — quand les tests échouent.

Parmi ces 92 tests, quatre sont des **garde-fous de documentation** plutôt que de comportement : ils
comparent les étiquettes des scénarios Gherkin aux catégories des tests, dans les deux sens, et
vérifient la parité des deux langues. Une documentation qui dérive fait donc rougir la CI.

Ce que la CI **ne** vérifie **pas** : le rendu du menu WinForms, l'extraction d'icônes, le mutex
d'instance unique, l'écriture dans le dossier de données. Ces couches n'ont pas de tests
automatisés ; elles ne sont que compilées. La liste exhaustive, avec la façon de les vérifier à la
main, est dans [`TRACEABILITE.md`](TRACEABILITE.md) § Zones sans test automatisé.

### Découpage en tâches

Deux tâches parallèles, toutes deux sur `windows-latest` :

- `build-et-tests` : restore → build → test → artefact `resultats-tests`
- `mise-en-forme` : restore → `dotnet format --verify-no-changes`

---

## 3. Runner nécessaire : `windows-latest`, obligatoire

La solution ne compile **que** sur Windows :

| Projet | Cible | Compile sur Linux ? |
| --- | --- | --- |
| `CSharpTrayShortcut.Domain` | `net9.0` | oui |
| `CSharpTrayShortcut.Application` | `net9.0` | oui |
| `CSharpTrayShortcut.Tests` | `net9.0` | oui |
| `CSharpTrayShortcut.Infrastructure` | `net9.0-windows` | non (COM, shell) |
| `CSharpTrayShortcut.Ui` | `net9.0-windows` | non (WinForms, `System.Drawing`) |

Le cœur métier resterait compilable et testable sur Linux, mais **aucune tâche ne l'exploite** :
un vert partiel se ferait passer pour un vert complet. Si le besoin s'en présentait — dépanner sans
runner Windows —, la tâche devrait être manuelle et signalée comme partielle, à la façon du mode
dégradé de `CSharp-ForgeWatcher`.

Le SDK est installé par `actions/setup-dotnet@v4` avec `dotnet-version: 9.0.x`. Une version
**9.0.200 minimum** est requise : le fichier solution est au format `.slnx`, que les SDK antérieurs
ne savent pas lire.

---

## 4. Reproduire la CI en local

Prérequis : SDK .NET 9 (>= 9.0.200) sur Windows. À exécuter depuis la racine du dépôt.

```powershell
# 1. Restauration (mêmes sources que la CI)
dotnet restore CSharp-TrayShortcut.slnx

# 2. Compilation Release — doit finir avec 0 avertissement
dotnet build CSharp-TrayShortcut.slnx --no-restore -c Release

# 3. Contrôle de mise en forme (ne modifie rien, sort en code 2 s'il y a des écarts)
dotnet format CSharp-TrayShortcut.slnx --verify-no-changes --no-restore

# 4. Tests unitaires + rapport TRX au même endroit que la CI
dotnet test CSharp-TrayShortcut.slnx --no-build -c Release --logger trx --results-directory TestResults
```

La tâche VS Code **« tout vérifier »** enchaîne les étapes 2, 3 et 4.

Commandes utiles autour de ces quatre-là :

```powershell
# Corriger automatiquement tout ce que l'étape 3 signale
dotnet format CSharp-TrayShortcut.slnx

# Rejouer les tests d'une seule spec (les tests sont tagués par identifiant de spec)
dotnet test CSharp-TrayShortcut.slnx --no-build -c Release --filter "TestCategory=SPEC-MENU-003"

# Lancer l'application
dotnet run --project src/CSharpTrayShortcut.Ui

# Produire l'exécutable distribuable dans publish/
.\scripts\publier.ps1

# Version autonome, qui n'exige aucun runtime sur le poste cible
.\scripts\publier.ps1 -Autonome

# Repartir d'un dépôt propre (bin, obj, publish, TestResults) ; -WhatIf pour simuler
.\scripts\nettoyer.ps1
```

Sur Linux ou macOS, seule la partie multiplateforme est jouable :

```bash
dotnet test tests/CSharpTrayShortcut.Tests/CSharpTrayShortcut.Tests.csproj -c Release
```

---

## 5. Rapports de tests

`dotnet test --logger trx` produit un fichier `.trx` (format Visual Studio) dans `TestResults/`,
publié par `actions/upload-artifact@v4` sous le nom `resultats-tests`, conservé 14 jours, y compris
quand la tâche échoue.

---

## 6. Points d'attention connus

### Fins de ligne : CRLF attendu

Le `.editorconfig` impose `end_of_line = crlf`, et le `.gitattributes` du dépôt contient
`* text=auto eol=CRLF`. Le comportement de `dotnet format` est donc identique sur le runner et sur
un poste de développement, quelle que soit la configuration git locale.

Exception assumée : les `.sh` sont en LF (`.editorconfig`), car un CRLF dans un shebang casse
l'exécution sous Git Bash — ce dont dépendent les dispatchers `free.sh` du workspace.

Les `.ps1` sont en UTF-8 **avec BOM** : Windows PowerShell 5.1, celui qu'invoquent les tâches VS
Code, lit un `.ps1` sans BOM comme de l'ANSI et affiche les accents en charabia.

### Format de solution `.slnx`

`CSharp-TrayShortcut.slnx` utilise le nouveau format XML de solution, lu par le SDK .NET **à partir
de la version 9.0.200**. `dotnet restore`, `build`, `test` et `format` l'acceptent tous ; un SDK plus
ancien échoue dès la restauration.

Un piège vaut d'être signalé : un commentaire XML ne peut pas contenir `--`. Documenter une option
de ligne de commande dans un commentaire de `.csproj` ou de `.slnx` provoque une erreur `MSB4025`,
au message peu évocateur.

### Cache NuGet

`actions/cache@v4` sur `~/.nuget/packages`, clé calculée depuis `**/*.csproj`,
`Directory.Build.props` et `NuGet.Config`. Un changement de version de paquet crée une nouvelle
entrée ; `restore-keys` sert de repli.

---

## 7. Release et secrets

### Publier une version

```powershell
# Le tag doit commencer par « v » ; le workflow en déduit le numéro de version
git tag v1.0.0
git push origin v1.0.0
```

Le workflow `release.yml` enchaîne alors : restore → build Release → tests → `dotnet publish -r
win-x64 --self-contained false` → archive `TrayShortcut-v1.0.0-win-x64.zip` → création de la
release GitHub avec l'archive attachée. Le numéro du tag est injecté dans les assemblies via
`-p:Version`, pour que le binaire livré porte le même numéro que la release.

Un tag contenant un tiret (`v1.1.0-beta.1`) crée automatiquement une **préversion**.

L'archive est **framework-dependent** : légère, mais le poste cible doit avoir le **runtime .NET 9
Desktop (x64)**. Pour un exécutable autonome, remplacer `--self-contained false` par
`--self-contained true` dans `release.yml`, ou utiliser `.\scripts\publier.ps1 -Autonome` en local.

### Quels secrets faut-il configurer ?

**Aucun.** La release utilise le jeton `GITHUB_TOKEN` fourni automatiquement à chaque exécution ; le
workflow demande simplement le droit d'écriture correspondant :

```yaml
permissions:
  contents: write
```

Un secret ne serait nécessaire que pour aller au-delà : signature du binaire, publication sur un
dépôt externe, notification vers un outil tiers. Le cas échéant : dépôt → **Settings** → **Secrets
and variables** → **Actions**, et l'utiliser par `env:` dans le workflow, jamais en clair.
