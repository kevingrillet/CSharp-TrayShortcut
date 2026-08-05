#!/usr/bin/env bash
# Supprime tout ce qui est régénérable : bin/, obj/, publish/ et TestResults/.
# Le prochain build relancera un restore complet. À lancer de n'importe où :  ./clean.sh
#
# Équivalent bash de scripts/nettoyer.ps1, gardé pour les dispatchers « free.sh » du
# workspace (voir ../AGENTS.md). La version PowerShell fait en plus un « dotnet clean »
# et sait simuler avec -WhatIf.
set -euo pipefail

# La racine du dépôt est le dossier contenant ce script.
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "Nettoyage de bin/, obj/, publish/ et TestResults/ sous : $ROOT"

count=0
while IFS= read -r -d '' dir; do
    rm -rf "$dir"
    echo "  supprimé : ${dir#"$ROOT"/}"
    count=$((count + 1))
done < <(find "$ROOT" \
    -type d -name .git -prune -o \
    -type d \( -name bin -o -name obj -o -name publish -o -name TestResults \) -prune -print0)

echo "Terminé. $count dossier(s) supprimé(s)."
