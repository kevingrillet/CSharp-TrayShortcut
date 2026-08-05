# Objet de la modification

<!-- En une ou deux phrases : quel besoin cette PR couvre-t-elle ? -->

## Vérifications avant relecture

- [ ] `dotnet build -c Release` passe sans avertissement
- [ ] `dotnet test` passe (tous les tests, y compris les nouveaux)
- [ ] `dotnet format --verify-no-changes` ne signale rien
- [ ] Specs de `docs/specs/` et scénarios de `docs/features/` mis à jour si le comportement change
- [ ] `docs/TRACEABILITE.md` complété pour toute spec nouvelle ou renommée
- [ ] `CHANGELOG.md` mis à jour, formulé côté utilisateur
- [ ] Aucun secret ni chemin absolu de poste de travail commité (`config.json` compris)

## Points d'attention pour le relecteur

<!-- Choix discutables, dette assumée, zones à regarder en priorité. Supprimer si vide. -->
