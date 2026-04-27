# Changelog

Toutes les modifications notables de ce projet sont documentées dans ce fichier.

Le format suit [Keep a Changelog](https://keepachangelog.com/fr/1.1.0/) et le projet adhère au [versionnement sémantique](https://semver.org/lang/fr/).

## [Non publié]

### À venir
- **J2 (`v0.2.0`)** — Famille B : cohérence comptable (équilibres débit/crédit par écriture et global, format numérique des montants, mutuelle exclusion débit/crédit, comptes auxiliaires).
- **J3 (`v0.3.0`)** — Famille C : cohérence temporelle (format des dates `AAAAMMJJ`, écritures dans la période d'exercice, `ValidDate >= EcritureDate`, chronologie par journal, signalement des écritures non validées).
- **J4 (`v0.4.0`)** — Rapport Markdown finalisé, rapport JSON (schéma versionné), affinage des codes de retour processus.
- **J5 (`v1.0.0`)** — Pipeline de release multi-OS, premiers binaires self-contained publiés (Windows x64, Linux x64, macOS).

## [0.1.0] — 2026-04-27

Jalon **J1** clos : la Famille A (conformité de format) est entièrement
opérationnelle. À ce stade, `fec-check <chemin>` détecte sept types
d'anomalies de format et retourne un verdict + un code de retour processus
exploitable en CI.

### Ajouté
- Règles A01 à A07 (Famille A — Format) implémentées et couvertes par tests :
  - **A01** (Bloquante) — Encodage du fichier dans `{ASCII, ISO-8859-15, UTF-8}`, BOM toléré.
  - **A02** (Bloquante) — Séparateur de champs cohérent (tabulation `\t` ou pipe `|`).
  - **A03** (Bloquante) — Présence de l'en-tête avec les 18 noms de colonnes attendus.
  - **A04** (Bloquante) — Ordre exact des 18 colonnes dans l'en-tête.
  - **A05** (Erreur) — Toute ligne de données contient exactement 18 champs.
  - **A06** (Avertissement) — Fin de ligne CRLF ou LF cohérente sur tout le fichier.
  - **A07** (Erreur) — Champs obligatoires non vides + au moins un de `Debit` / `Credit` non nul.
- API publique stable du Core : `FecValidator`, `ValidationReport`, `Finding`,
  `Rule`, `Rules`, `Verdict`, `Severity`, `DetectedEncoding`, `DetectedLineEnding`.
- Lecture en streaming byte-level (`FecLineReader`) — empreinte mémoire stable
  indépendamment de la taille du fichier (cf. cadrage §6.3).
- CLI : résumé console en français, codes de retour `0` / `1` / `2` / `3` / `64`.
- 7 fixtures pathologiques minimales (`tests/fixtures/non-conforme/format/`),
  une par règle, avec README expliquant l'anomalie injectée.
- Tests xUnit couvrant catalogue de règles, détecteurs unitaires, fixture
  conforme et chacune des 7 fixtures pathologiques.

### Modifié
- `FecCheckInfo.Version` passe de `0.0.0` à `0.1.0`.
- `Program.cs` : remplace le message « version de cadrage (J0) » par l'appel
  effectif au validateur et l'impression d'un rapport console structuré.

### Dépendances
- Ajout du package Microsoft officiel `System.Text.Encoding.CodePages`
  (8.0.0) sur `RezDevOps.FecCheck.Core` pour le décodage ISO-8859-15 hors
  Windows. Justification dans le `README.md`. Aucune autre dépendance NuGet
  tierce, conformément au cadrage §6.1.

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
