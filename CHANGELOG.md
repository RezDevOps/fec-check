# Changelog

Toutes les modifications notables de ce projet sont documentées dans ce fichier.

Le format suit [Keep a Changelog](https://keepachangelog.com/fr/1.1.0/) et le projet adhère au [versionnement sémantique](https://semver.org/lang/fr/).

## [Non publié]

### À venir
- **J5 (`v1.0.0`)** — Pipeline de release multi-OS, premiers binaires self-contained publiés (Windows x64, Linux x64, macOS).

## [0.4.0] — 2026-04-28

Jalon **J4** clos : les sorties fichier sont opérationnelles. À ce stade,
`fec-check` peut produire un rapport Markdown destiné à un dirigeant non-tech
**et** un rapport JSON v1 versionné, exploitable par un consommateur tiers
(script, back-office RezDevOps, futur web upload). Les deux sorties sont
indépendantes et combinables.

### Ajouté
- Nouveau composant Core `JsonReportWriter` : sérialise un `ValidationReport`
  en JSON conforme au schéma `v1` (champ racine `schemaVersion: 1`),
  source-genéré via `System.Text.Json` (AOT-friendly, cf. cadrage §6.1).
- Nouveau composant Core `MarkdownReportWriter` : produit un rapport
  pédagogique structuré (verdict, caractéristiques, synthèse par famille,
  détail par anomalie groupé par famille, pied de page avec liens documentation).
- Nouveau composant Core `ReportFileWriter` : helpers d'écriture vers fichier
  en UTF-8 sans BOM, fin de ligne LF forcée, création des répertoires parents.
- Nouveau type public `ReportEnvironment` (record) : porte les métadonnées
  d'environnement (chemin du fichier source, horodatage, version produit,
  exercice) que le Core ne connaît pas par construction.
- CLI : option `--output-md <chemin>` qui écrit le rapport Markdown finalisé.
- CLI : option `--output-json <chemin>` qui écrit le rapport JSON v1.
- Console : section « Synthèse » ajoutée (compteurs par sévérité et par
  famille) avant la liste détaillée des anomalies.
- `docs/json-schema.md` : contrat figé du JSON v1 (toute évolution non
  additive incrémentera `schemaVersion`).
- `docs/rapport-exemple.md` : exemple de rapport Markdown produit par l'outil
  sur un FEC fictif (cf. cadrage §7.2).
- 16 tests xUnit additionnels :
  - `JsonReportWriterTests` (7 tests) : `schemaVersion`, camelCase, omission
    `null`, cohérence synthèse / anomalies, exposition exercice.
  - `MarkdownReportWriterTests` (7 tests) : verdict en bandeau, sections par
    famille présentes uniquement si non vides, pied de page, fins de ligne LF.
  - `ReportFileWriterTests` (4 tests, fixtures temporaires) : UTF-8 sans BOM,
    création des répertoires parents, écrasement d'un fichier existant.
- `global.json` à la racine du repo : épingle la bande SDK `.NET 8.0.x`
  (`rollForward: latestFeature`) pour des builds reproductibles avant la
  migration vers .NET 10 LTS prévue fin 2026 (Data Context §3.1).

### Modifié
- `FecCheckInfo.Version` passe de `0.3.0` à `0.4.0`.
- `Program.cs` : parser d'arguments étendu (`--output-md`, `--output-json`),
  bloc d'aide `--help` réécrit pour documenter les nouveaux flags. Si une
  écriture de rapport fichier échoue, l'erreur est tracée sur `stderr` et le
  code de retour devient `3` ; le verdict reste affiché en console.
- Aide console : la mention du flag `--output-md` / `--output-json` apparaît
  en plus de `--exercice`.

### Dépendances
- Aucune nouvelle dépendance NuGet ajoutée. La sérialisation JSON s'appuie
  uniquement sur `System.Text.Json` (BCL .NET 8) en mode source generation.

## [0.3.0] — 2026-04-28

Jalon **J3** clos : la Famille C (cohérence temporelle) est entièrement
opérationnelle. À ce stade, `fec-check <chemin>` détecte vingt-et-un
types d'anomalies (sept de format, six de cohérence comptable, huit de
cohérence temporelle) et accepte une option `--exercice` pour valider la
période d'exercice.

### Ajouté
- Règles C01 à C08 (Famille C — Cohérence temporelle) implémentées et couvertes par tests :
  - **C01** (Erreur) — `EcritureDate` au format `AAAAMMJJ` strict (8 chiffres formant une date valide du calendrier).
  - **C02** (Erreur) — `PieceDate` au format `AAAAMMJJ` strict si rempli.
  - **C03** (Erreur) — `ValidDate` au format `AAAAMMJJ` strict si rempli.
  - **C04** (Erreur) — `DateLet` au format `AAAAMMJJ` strict si rempli.
  - **C05** (Erreur) — Toute `EcritureDate` doit appartenir à la période d'exercice fournie via l'option `--exercice` ; règle non évaluée si l'option n'est pas fournie.
  - **C06** (Erreur) — `ValidDate` postérieure ou égale à `EcritureDate` quand les deux sont remplies.
  - **C07** (Erreur) — Au sein d'un journal, les écritures validées (`ValidDate` non vide) doivent avoir une `EcritureDate` croissante quand on les trie par `EcritureNum` (lex).
  - **C08** (Avertissement) — Finding agrégé recensant les écritures sans `ValidDate` (avec échantillon des 10 premières).
- Nouveau type public `ExercicePeriod` (record) exposé par le Core, avec fabrique `Create(debut, fin)` qui rejette les bornes incohérentes et méthode `Contains(date)` (bornes incluses).
- Nouvelle classe interne `TemporalContext` qui porte l'état nécessaire aux règles C05/C07/C08 (agrégateur d'écritures avec `EcritureDate` parsable + drapeau `AAuMoinsUneLigneValidee`). Empreinte mémoire en *O(nombre d'écritures distinctes)*, miroir de `AccountingContext`.
- Nouvelle classe interne `FecDateParser` : parseur strict `AAAAMMJJ` partagé par `DataLineValidator` (C01-C04, C06) et `TemporalContext` (C05, C07).
- 8 fixtures pathologiques (`tests/fixtures/non-conforme/temporel/`), une par règle, avec README expliquant l'anomalie injectée.
- 11 tests xUnit C01-C08 + couplages (conforme avec exercice, sans exercice, invariants `ExercicePeriod`).
- Option CLI `--exercice <debut>:<fin>` au format `YYYY-MM-DD:YYYY-MM-DD` (séparateur `:`, bornes incluses). Le rapport indique explicitement quand C05 n'a pas été évaluée faute d'option.

### Modifié
- `FecCheckInfo.Version` passe de `0.2.0` à `0.3.0`.
- Catalogue `Rules.All` étendu : 21 règles désormais (A01-A07 + B01-B06 + C01-C08).
- `FecValidator` expose désormais 4 surcharges publiques : `Validate(string)`, `Validate(string, ExercicePeriod?)`, `Validate(Stream)`, `Validate(Stream, ExercicePeriod?)`. Les surcharges sans `ExercicePeriod` délèguent à celles avec `null` (compatibilité ascendante J1/J2 préservée).
- CLI : la ligne d'aide « règles couvertes » mentionne maintenant les Familles A, B et C ; le bloc de caractéristiques du fichier affiche soit la période fournie, soit la mention « non précisé (règle C05 non évaluée — utilisez --exercice <debut>:<fin> pour l'activer). ».
- `RulesCatalogTests` : sévérités C01-C08 ajoutées au théorème d'alignement avec `docs/regles.md` ; nouveau test `All_FamilleC_EstTemporal`.

### Dépendances
- Aucune nouvelle dépendance NuGet ajoutée. La Famille C est implémentée
  avec la BCL .NET 8 uniquement (`System.Globalization` pour `DateOnly.TryParseExact`).

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
