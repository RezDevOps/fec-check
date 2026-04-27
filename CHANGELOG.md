# Changelog

Toutes les modifications notables de ce projet sont documentées dans ce fichier.

Le format suit [Keep a Changelog](https://keepachangelog.com/fr/1.1.0/) et le projet adhère au [versionnement sémantique](https://semver.org/lang/fr/).

## [Non publié]

### À venir
- **J3 (`v0.3.0`)** — Famille C : cohérence temporelle (format des dates `AAAAMMJJ`, écritures dans la période d'exercice, `ValidDate >= EcritureDate`, chronologie par journal, signalement des écritures non validées).
- **J4 (`v0.4.0`)** — Rapport Markdown finalisé, rapport JSON (schéma versionné), affinage des codes de retour processus.
- **J5 (`v1.0.0`)** — Pipeline de release multi-OS, premiers binaires self-contained publiés (Windows x64, Linux x64, macOS).

## [0.2.0] — 2026-04-27

Jalon **J2** clos : la Famille B (cohérence comptable) est entièrement
opérationnelle. À ce stade, `fec-check <chemin>` détecte treize types
d'anomalies (sept de format en Famille A, six de cohérence comptable en
Famille B) et retourne un verdict + un code de retour processus
exploitable en CI.

### Ajouté
- Règles B01 à B06 (Famille B — Cohérence comptable) implémentées et couvertes par tests :
  - **B01** (Erreur) — Équilibre Débit/Crédit par couple (`JournalCode`, `EcritureNum`). Une seule entrée de rapport par écriture déséquilibrée, citant les sommes et l'écart.
  - **B02** (Erreur) — Équilibre global du fichier (somme Débit = somme Crédit).
  - **B03** (Erreur) — Format numérique des montants : 0 à 4 décimales tolérées, pas de séparateur de milliers, séparateur décimal (`,` ou `.`) cohérent sur tout le fichier (la convention est fixée par la première occurrence).
  - **B04** (Avertissement) — Mutuelle exclusion `Debit`/`Credit` non nuls sur la même ligne.
  - **B05** (Erreur) — `CompAuxNum` et `CompAuxLib` remplis ensemble ou tous deux vides.
  - **B06** (Avertissement) — Un compte auxiliaire implique un `CompteNum` commençant par `'4'` (compte de tiers PCG).
- Nouvelle classe interne `AccountingContext` qui porte l'état inter-lignes (agrégateur d'écritures pour B01, accumulateurs globaux pour B02, tracker de séparateur décimal pour B03). Empreinte mémoire en *O(nombre d'écritures distinctes)*, compatible avec la cible §6.3 du cadrage.
- 6 fixtures pathologiques (`tests/fixtures/non-conforme/comptable/`), une par règle, avec README expliquant l'anomalie injectée et la procédure de régénération `awk`.
- 6 tests xUnit B01-B06 sur la pattern existante (verdict + au moins le finding attendu sur la bonne ligne).

### Modifié
- `FecCheckInfo.Version` passe de `0.1.0` à `0.2.0`.
- Catalogue `Rules.All` étendu : 13 règles désormais (A01-A07 + B01-B06).
- `DataLineValidator.Validate(...)` accepte désormais `string[] fields` plutôt que `string line` : le découpage est fait une seule fois au niveau de `FecValidator` et partagé avec `AccountingContext` (perf §6.3).
- CLI : la ligne d'aide « règles couvertes » mentionne maintenant les Familles A (format) et B (cohérence comptable).
- `RulesCatalogTests` : sévérités B01-B06 ajoutées au théorème de cohérence avec `docs/regles.md` ; les asserts par-famille sont scindés (Famille A `Format`, Famille B `Accounting`).

### Dépendances
- Aucune nouvelle dépendance NuGet ajoutée. La Famille B est implémentée
  avec la BCL .NET 8 uniquement (`System.Globalization`,
  `System.Text.RegularExpressions`).

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
