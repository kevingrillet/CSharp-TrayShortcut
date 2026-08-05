---
name: verifier-avant-commit
description: "Dérouler les contrôles avant de committer : les quatre commandes de la CI, les scripts de cohérence entre documentation et code, et la liste de ce qui ne doit jamais entrer dans le dépôt. À utiliser à la fin de toute modification."
---

# Vérifier avant de committer

La checklist de fond est dans [`docs/CONTRIBUER.md`](../../../docs/CONTRIBUER.md) §4. **Ne pas la
recopier.** Cette fiche donne les **commandes** et les contrôles que la CI ne fait pas.

## 1. Ce que fait la CI — à jouer d'abord

```powershell
dotnet restore CSharp-TrayShortcut.slnx
dotnet build   CSharp-TrayShortcut.slnx -c Release
dotnet format  CSharp-TrayShortcut.slnx --verify-no-changes
dotnet test    CSharp-TrayShortcut.slnx
```

La tâche VS Code **« tout vérifier »** enchaîne les trois dernières. Correction automatique du
format : `dotnet format CSharp-TrayShortcut.slnx`.

`TreatWarningsAsErrors` est actif : un `dotnet build` qui passe garantit **0 avertissement**. Ne
jamais ajouter de paramètre pour contourner un avertissement ; s'il doit vraiment être toléré, le
désactiver **localement** par `#pragma warning disable` avec un commentaire justifiant l'exception.

Quatre des tests sont des garde-fous de documentation. S'ils tombent, le message dit quoi faire :

| Test qui tombe | Ce qui manque |
|---|---|
| `Chaque_scenario_Gherkin_renvoie_a_une_spec_reellement_testee` | le test correspondant à un scénario |
| `Chaque_spec_couverte_par_un_test_est_illustree_par_un_scenario` | le scénario `@SPEC-…` correspondant à une catégorie |
| `La_liste_blanche_ne_contient_aucune_entree_devenue_inutile` | une ligne à supprimer de `VerificationManuelleOuAVenir` |
| `Chaque_cle_declaree_est_formulee_dans_les_deux_langues` | une formulation dans `Strings.resx` ou `Strings.en.resx` |

## 2. Ce que la CI ne vérifie pas

### Fins de ligne des fichiers non-C#

`dotnet format` **ne voit que le C#**. Un `.md` ou un `.feature` écrit en LF passe sa vérification et
n'apparaîtra qu'au premier `git diff` d'un collègue.

```powershell
# Fichiers versionnés contenant du LF nu, hors ceux qui doivent en avoir
Get-ChildItem -Recurse -File -Include *.md,*.feature,*.json,*.yml,*.resx |
    Where-Object { $_.FullName -notmatch '\\(bin|obj|\.git|publish|TestResults)\\' } |
    Where-Object {
        $octets = [System.IO.File]::ReadAllBytes($_.FullName)
        $lf = 0; $crlf = 0
        for ($i = 0; $i -lt $octets.Length; $i++) {
            if ($octets[$i] -eq 10) { if ($i -gt 0 -and $octets[$i - 1] -eq 13) { $crlf++ } else { $lf++ } }
        }
        $lf -gt 0
    } |
    Select-Object -ExpandProperty FullName
```

Exceptions **légitimes**, à ne pas « corriger » : les `.sh` doivent être en LF (un CRLF dans un
shebang casse l'exécution sous Git Bash, dont dépendent les dispatchers `free.sh` du workspace) et
les `.ps1` en UTF-8 **avec BOM** (Windows PowerShell 5.1 lit un `.ps1` sans BOM comme de l'ANSI).

### Cohérence documentation ↔ code

```powershell
# Toute spec citée dans docs/specs a-t-elle une ligne dans TRACEABILITE.md ?
$specs = Select-String -Path docs\specs\*.md -Pattern '^#+\s+(SPEC-[A-Z-]+-\d+)' |
    ForEach-Object { $_.Matches[0].Groups[1].Value } | Sort-Object -Unique
$traces = Select-String -Path docs\TRACEABILITE.md -Pattern '(SPEC-[A-Z-]+-\d+)' |
    ForEach-Object { $_.Matches } | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
Compare-Object $specs $traces | Format-Table -AutoSize
```

```powershell
# Un ADR référencé existe-t-il ? (liens cassés dans la doc)
Select-String -Path docs\*.md, docs\**\*.md -Pattern 'adr/(\d{4}-[a-z0-9-]+)\.md' |
    ForEach-Object { $_.Matches[0].Groups[1].Value } | Sort-Object -Unique |
    Where-Object { -not (Test-Path "docs\adr\$_.md") }
```

### Balayages d'architecture

Les trois `Select-String` du skill [`respecter-architecture`](../respecter-architecture/SKILL.md) §3.
Chacun doit ne rien rendre.

## 3. Ce qui ne doit jamais entrer dans le dépôt

```powershell
git status --short
git diff --cached --stat
```

| À vérifier | Pourquoi |
|---|---|
| aucun `bin/`, `obj/`, `publish/`, `TestResults/` | régénérables ; couverts par `.gitignore`, mais un `git add -f` distrait passe |
| aucun `config.json` | contient les chemins du poste de travail ; il vit dans `%APPDATA%\TrayShortcut` |
| aucun chemin absolu de poste de travail dans le code ou la doc | `D:\Users\kevin\…` dans un exemple de README est une fuite d'information et vieillit mal |
| aucun `log.txt` ni `crash-*.txt` | données locales |

Le dépôt ne manipule aucun secret : l'application ne contacte aucun réseau et ne stocke aucun
identifiant. Il n'y a donc rien à chercher de ce côté, ce qui n'est pas une raison d'y ajouter un
jeton dans un exemple.

## 4. Nettoyer avant de mesurer

Si un doute subsiste sur un build incrémental — un test qui passe alors qu'il ne devrait pas, un
fichier supprimé qui semble encore présent :

```powershell
.\scripts\nettoyer.ps1 -WhatIf   # liste ce qui serait supprimé
.\scripts\nettoyer.ps1           # dotnet clean + suppression physique
```

Puis rejouer le §1 en entier.

## 5. Le journal des modifications

`CHANGELOG.md` se remplit **en même temps** que le code, pas au moment de publier. Une entrée est
formulée **côté utilisateur** : ce qu'il peut faire, ou ce qui ne le gêne plus. « Refactorisation de
`MenuComposer` » n'a rien à y faire ; « les raccourcis vers une adresse web fonctionnent enfin »
oui.

Catégories employées : *Ajouté*, *Modifié*, *Corrigé*, *Supprimé*, *Sécurité*. Toute entrée qui
correspond à une spec la cite par son identifiant.

## 6. Le commit

Identité git : celle du workspace, appliquée en `--local` par `../../init-git.sh` (voir
`../../AGENTS.md`). **Ne jamais modifier le `--global`** de la machine.

Messages en anglais, style conventionnel (`feat:`, `fix:`, `refactor:`, `docs:`, `build:`, `ci:`) —
c'est ce que montre l'historique du dépôt. Le corps du message, s'il y en a un, peut être en
français.
