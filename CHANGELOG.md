# Changelog

Toutes les modifications notables de ce projet sont documentées dans ce fichier.

Le format suit [Keep a Changelog](https://keepachangelog.com/fr/1.1.0/) et le projet adhère au [versionnement sémantique](https://semver.org/lang/fr/).

## [Non publié]

### À venir
- **J1 (`v0.1.0`)** — Famille A : conformité de format (encodage, séparateur, 18 colonnes obligatoires, fin de ligne, lignes tronquées).
- **J2 (`v0.2.0`)** — Famille B : cohérence comptable (équilibres débit/crédit par écriture et global, format numérique des montants, mutuelle exclusion débit/crédit, comptes auxiliaires).
- **J3 (`v0.3.0`)** — Famille C : cohérence temporelle (format des dates `AAAAMMJJ`, écritures dans la période d'exercice, `ValidDate >= EcritureDate`, chronologie par journal, signalement des écritures non validées).
- **J4 (`v0.4.0`)** — Rapport Markdown finalisé, rapport JSON (schéma versionné), codes de retour processus.
- **J5 (`v1.0.0`)** — Pipeline de release multi-OS, premiers binaires self-contained publiés (Windows x64, Linux x64, macOS).

## [0.0.0] — 2026-04-27

### Ajouté
- Création du repo `fec-check` (jalon J0).
- Solution `fec-check.sln` avec trois projets :
  - `RezDevOps.FecCheck.Core` (bibliothèque, sans I/O).
  - `RezDevOps.FecCheck.Cli` (exécutable, nom de commande `fec-check`).
  - `RezDevOps.FecCheck.Core.Tests` (xUnit + FluentAssertions, tests fumée du J0).
- Cible `net8.0` LTS, `Nullable enable`, `TreatWarningsAsErrors`.
- `Directory.Build.props` à la racine pour les propriétés MSBuild communes.
- `.editorconfig` aligné sur les conventions Microsoft pour C#.
- `.gitignore` standard .NET.
- `LICENSE` MIT au nom de Rudy Rezaire / RezDevOps.
- `README.md` pédagogique en français : pourquoi, cadre réglementaire, périmètre MVP, hors périmètre, posture, usage cible, architecture, feuille de route.
- `docs/regles.md` — squelette de la liste exhaustive des règles à implémenter avec leur source réglementaire.
- `tests/fixtures/conforme/fec-minimal-conforme.txt` — premier FEC d'exemple conforme (3 écritures équilibrées sur 3 journaux).
- `.github/workflows/build.yml` — CI minimale : restore + build + tests sur Ubuntu.

### Notes
- Aucune logique de validation n'est encore implémentée. Le binaire `fec-check` imprime sa version et son aide ; toute autre invocation retourne `64` (`EX_USAGE`).
- Repo public dès le premier commit par choix de transparence (cf. cadrage §8).
