# fec-check

> Validateur de **Fichier des Écritures Comptables** (FEC) pour les TPE et PME françaises.
> Ligne de commande, déterministe, sans réseau, en français.
> Statut : **v1.0.0 — 21 règles, binaires multi-OS Native AOT (J5)**. Cf. [`CHANGELOG.md`](CHANGELOG.md).

---

## Pourquoi cet outil

Toute entreprise française qui tient sa comptabilité de façon informatisée doit, en cas de contrôle fiscal, produire un FEC conforme à la norme imposée par l'administration. Le format est strict : **18 colonnes ordonnées**, séparateur tabulation ou pipe, encodage ASCII / ISO-8859-15 / UTF-8, équilibres comptables vérifiés, dates au format `AAAAMMJJ`.

Les ERP du segment TPE/PME (Sage, EBP, Cegid PME, Dolibarr, Odoo) génèrent un FEC qui est *presque* conforme. Le dirigeant le découvre au mauvais moment — au début du contrôle, sous pression, expert-comptable indisponible.

`fec-check` vise ce moment-là : **valider à froid, en amont, sur son poste, en cinq minutes**, et obtenir un rapport actionnable qui dit ligne par ligne ce qui ne va pas et pourquoi.

L'outil ne remplace pas [Test Compta Demat](https://www.economie.gouv.fr/) (l'outil officiel de la DGFiP). Il complète : ligne de commande, scriptable, messages d'erreur explicites en français, code source ouvert et auditable.

## Cadre réglementaire

Toutes les règles implémentées citent leur source. Les références canoniques :

| Référence | Objet |
|---|---|
| Article L. 47 A-I du LPF | Obligation de remise du FEC en cas de contrôle |
| Article A. 47 A-1 du LPF | Normes techniques (format plat ou XML, structure des 18 colonnes) |
| Arrêté du 29 juillet 2013 | Modification de l'article A. 47 A-1 (norme actuelle) |
| BOI-CF-IOR-60-40-10 | Présentation et représentation de la comptabilité |
| BOI-CF-IOR-60-40-20 | Format du fichier des écritures comptables |
| BOI-CF-IOR-60-40-30 | Mise en œuvre de traitements informatiques |

Sources : [legifrance.gouv.fr](https://www.legifrance.gouv.fr/) et [bofip.impots.gouv.fr](https://bofip.impots.gouv.fr/).

La norme est stable depuis 2014, dernière mise à jour majeure du BOFiP en 2017 sur le format. Aucune réforme annoncée à 12 mois.

## Ce que l'outil vérifie (cible MVP v1.0.0)

Trois familles de règles, dans l'ordre d'exécution. Si une famille échoue de manière bloquante, les suivantes ne sont pas exécutées.

**A. Conformité de format** — encodage, séparateur, présence et ordre des 18 colonnes obligatoires (`JournalCode`, `JournalLib`, `EcritureNum`, `EcritureDate`, `CompteNum`, `CompteLib`, `CompAuxNum`, `CompAuxLib`, `PieceRef`, `PieceDate`, `EcritureLib`, `Debit`, `Credit`, `EcritureLet`, `DateLet`, `ValidDate`, `Montantdevise`, `Idevise`), fin de ligne, pas de ligne tronquée.

**B. Cohérence comptable** — équilibre débit/crédit par écriture (somme par `EcritureNum` dans un même `JournalCode`), équilibre global du fichier, format numérique des montants, mutuelle exclusion débit/crédit sur une même ligne, cohérence des comptes auxiliaires.

**C. Cohérence temporelle** — dates au format `AAAAMMJJ`, `EcritureDate` dans la période d'exercice, `ValidDate` postérieure ou égale à `EcritureDate`, numérotation chronologique des écritures validées, signalement des écritures non validées.

Le détail exhaustif des règles, avec leur source réglementaire et un exemple de violation, est dans [`docs/regles.md`](docs/regles.md).

## Ce que l'outil **ne fait pas**

Garde-fous explicites pour ne pas refaire l'erreur classique du « petit utilitaire qui devient une usine à gaz » :

- **Pas de correction automatique** des anomalies. `fec-check` rapporte, ne modifie pas.
- **Pas de validation des numéros de compte selon le PCG** au MVP (longueur, racines, exceptions secteur). Reportée v2.
- **Pas de vérification des cohérences sémantiques fines** (TVA collectée/déductible, lettrage, contreparties standards). Hors périmètre TPE/PME, reporté v2 ou v3.
- **Pas de format XML** au MVP (variante prévue par la norme mais minoritaire en pratique). Reporté v2.
- **Pas d'interface graphique**, pas de drag-and-drop web, pas de mode interactif. Pas avant un retour utilisateur réel sur le CLI.
- **Pas d'IA, pas de LLM, pas de heuristique floue.** Toute règle est déterministe et citée. C'est un point d'identité, pas un détail.

## Posture

- **Souveraineté** — norme purement française, **aucune dépendance** à un service tiers, aucun appel réseau, aucune télémétrie, exécution 100 % locale.
- **Déterminisme** — même fichier d'entrée = même rapport de sortie, à l'octet près. Reproductible.
- **Pédagogie** — chaque message d'erreur explique *quoi*, *où*, *pourquoi* (avec citation), *comment corriger*.
- **Sobriété** — dépendances NuGet réduites au minimum. Si une dépendance devient nécessaire, elle est argumentée dans la section [Dépendances](#dépendances) ci-dessous.

## Dépendances

À v0.1.0, **une seule** dépendance NuGet est ajoutée à la bibliothèque `Core`, en plus de la BCL .NET 8 :

| Package | Version | Émetteur | Justification |
|---|---|---|---|
| `System.Text.Encoding.CodePages` | `8.0.0` | Microsoft (officiel) | Le runtime .NET sur Linux et macOS ne charge pas par défaut la code page `28605` (ISO-8859-15 / Latin-9), pourtant exigée par la norme FEC. Ce package, publié par Microsoft, expose la code page de manière portable. Le runtime Windows l'inclut déjà nativement. |

Les projets `Cli` et `Tests` n'ajoutent aucune dépendance tierce supplémentaire (xUnit + FluentAssertions côté tests uniquement). Cette posture sera maintenue aux jalons suivants : toute nouvelle dépendance sera justifiée ici.

## Usage

Le binaire valide les 21 règles des Familles A, B et C, imprime un résumé console en français, et peut générer en plus un rapport Markdown finalisé (lecture humaine) et/ou un rapport JSON v1 (consommation programmatique).

```bash
# Validation simple, résumé console uniquement
fec-check chemin/vers/mon-fec.txt

# Validation avec contrôle de la période d'exercice (règle C05)
fec-check --exercice 2024-01-01:2024-12-31 chemin/vers/mon-fec.txt

# Génération d'un rapport Markdown lisible par un dirigeant
fec-check --output-md rapport.md chemin/vers/mon-fec.txt

# Génération d'un rapport JSON pour intégration CI / script
fec-check --output-json rapport.json chemin/vers/mon-fec.txt

# Combinaison : exercice + Markdown + JSON
fec-check --exercice 2024-01-01:2024-12-31 \
          --output-md rapport.md \
          --output-json rapport.json \
          chemin/vers/mon-fec.txt

# Aide
fec-check --help

# Version
fec-check --version
```

Les fichiers de rapport sont écrits en UTF-8 sans BOM, fin de ligne LF, quel que soit l'OS. Les répertoires parents sont créés si nécessaire ; un fichier existant est écrasé sans avertissement.

**Exemple sur la fixture conforme livrée avec le repo** :

```text
$ fec-check tests/fixtures/conforme/fec-minimal-conforme.txt
Fichier analysé : tests/fixtures/conforme/fec-minimal-conforme.txt
Encodage détecté : UTF-8 (sans BOM)
Séparateur détecté : tabulation
Fin de ligne : LF
Lignes lues : 9

Verdict : CONFORME (aucune anomalie de format détectée).
```

**Exemple sur une fixture pathologique** (ligne tronquée à 17 champs) :

```text
$ fec-check tests/fixtures/non-conforme/format/A05-ligne-tronquee.txt
[…]
Verdict : NON CONFORME (1 anomalie détectée).
  - [A05] Ligne 3 : 17 champs au lieu de 18 attendus.
```

**Codes de retour processus** (utilisables en CI / script) :

| Code | Signification |
|---|---|
| `0` | Conforme |
| `1` | Conforme avec avertissements |
| `2` | Non conforme |
| `3` | Erreur d'exécution |
| `64` | Usage incorrect (`EX_USAGE`) |

Un exemple complet de rapport Markdown est publié dans [`docs/rapport-exemple.md`](docs/rapport-exemple.md). Le contrat figé du rapport JSON est documenté dans [`docs/json-schema.md`](docs/json-schema.md) (`schemaVersion: 1`).

## Installation

À partir de **`v1.0.0`** (J5), des binaires **self-contained Native AOT** sont publiés à chaque tag dans les [Releases GitHub](https://github.com/RezDevOps/fec-check/releases). **Aucune installation de .NET requise** sur le poste.

| Plateforme | Archive | Binaire |
|---|---|---|
| Windows 10/11 x64 | `fec-check-<version>-win-x64.zip` | `fec-check.exe` |
| Linux x64 (Ubuntu, Debian, RHEL, …) | `fec-check-<version>-linux-x64.tar.gz` | `fec-check` |
| macOS Apple Silicon (M1/M2/M3/M4) | `fec-check-<version>-osx-arm64.tar.gz` | `fec-check` |

Les binaires Native AOT pèsent quelques mégaoctets et démarrent en quelques millisecondes (pas de JIT runtime embarqué). macOS Intel et Linux ARM ne sont pas distribués par défaut au MVP — ouvrez une issue si nécessaire.

### Vérifier l'intégrité et l'origine

À côté des archives, chaque release publie :

- `SHA256SUMS` — empreintes SHA-256 de tous les artefacts ;
- `SHA256SUMS.sig` — signature [sigstore](https://www.sigstore.dev/) (cosign keyless via OIDC GitHub) ;
- `fec-check-<version>-sbom.cdx.json` — SBOM [CycloneDX](https://cyclonedx.org/) pour audit de la chaîne de dépendances.

```bash
# 1. Vérifier l'intégrité (toutes plateformes, sha256sum natif sous Linux/macOS,
#    `Get-FileHash` ou `certutil -hashfile` sous Windows).
sha256sum -c SHA256SUMS

# 2. Vérifier l'origine (cosign keyless, prouve que SHA256SUMS a bien été
#    signé par le workflow GitHub Actions de RezDevOps/fec-check).
cosign verify-blob \
  --bundle SHA256SUMS.sig \
  --certificate-identity-regexp '^https://github.com/RezDevOps/fec-check/' \
  --certificate-oidc-issuer https://token.actions.githubusercontent.com \
  SHA256SUMS
```

Aucune clé privée n'est détenue par RezDevOps : les signatures sont ancrées au workflow GitHub Actions du repo (transparency log [Rekor](https://docs.sigstore.dev/logging/overview/)). Voir [`SECURITY.md`](SECURITY.md) pour la politique de signalement.

### Compilation depuis les sources

Pour les contributeurs ou un environnement où le binaire publié ne convient pas :

```bash
git clone https://github.com/RezDevOps/fec-check.git
cd fec-check
dotnet build -c Release
dotnet run --project src/RezDevOps.FecCheck.Cli -- --help

# Pour produire le binaire Native AOT localement (RID adapté à votre poste) :
dotnet publish src/RezDevOps.FecCheck.Cli \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -o ./out
./out/fec-check --version
```

Prérequis : SDK .NET 8 LTS, plus l'outillage natif requis par AOT :

- **Linux** — `clang` + `zlib` (paquets `clang` + `zlib1g-dev` sous Debian/Ubuntu).
- **Windows** — Visual Studio Build Tools avec workload C++.
- **macOS** — Xcode Command Line Tools **et** [LLVM via Homebrew](https://formulae.brew.sh/formula/llvm) pour fournir `llvm-objcopy` (Apple ne livre pas `objcopy`, sans lequel le strip des symboles AOT échoue) :

  ```sh
  brew install llvm
  echo 'export PATH="$(brew --prefix llvm)/bin:$PATH"' >> ~/.zshrc
  exec zsh
  ```

La version SDK est épinglée par le `global.json` à la racine (bande `8.0.x`, `rollForward: latestFeature`).

## Architecture

Découpage en deux assemblages, voulu pour ne pas peindre dans un coin :

- **`RezDevOps.FecCheck.Core`** — bibliothèque pure, sans I/O. Prend un flux en entrée, émet un rapport. Testable à 100 %. Réutilisable par d'autres outils RezDevOps.
- **`RezDevOps.FecCheck.Cli`** — exécutable mince qui parse les arguments, ouvre le fichier, appelle le Core, écrit les rapports. Aucune logique métier.

```
fec-check/
├── src/
│   ├── RezDevOps.FecCheck.Core/        # bibliothèque, logique pure
│   └── RezDevOps.FecCheck.Cli/         # exécutable, I/O et arguments
├── tests/
│   ├── RezDevOps.FecCheck.Core.Tests/  # xUnit + FluentAssertions
│   └── fixtures/                       # FEC d'exemple (conformes et pathologiques)
├── docs/
│   ├── regles.md                       # règles implémentées et leurs sources
│   ├── json-schema.md                  # contrat du rapport JSON v1 (figé)
│   └── rapport-exemple.md              # sortie type Markdown
├── .github/workflows/                  # CI
├── Directory.Build.props               # propriétés MSBuild communes
├── fec-check.sln
├── CHANGELOG.md
├── LICENSE
└── README.md
```

## Performance cible

100 Mo de FEC en moins de 10 secondes sur un poste TPE/PME standard (i5 8e gen, SSD), empreinte mémoire stable indépendamment de la taille du fichier (lecture en streaming, jamais de chargement intégral en mémoire).

## Statut et feuille de route

| Jalon | Contenu | État |
|---|---|---|
| **J0** | Repo, README, structure, CI minimale, LICENSE, fixture conforme | livré (`v0.0.0`) |
| **J1** | Famille A — conformité de format | livré (`v0.1.0`) |
| **J2** | Famille B — cohérence comptable | livré (`v0.2.0`) |
| **J3** | Famille C — cohérence temporelle | livré (`v0.3.0`) |
| **J4** | Rapport Markdown finalisé, rapport JSON v1, codes de retour | livré (`v0.4.0`) |
| **J5** | Pipeline release multi-OS, binaires AOT, SBOM, sigstore | **livré (`v1.0.0`)** |

Voir [`CHANGELOG.md`](CHANGELOG.md) pour le détail commit par commit.

## Contribuer

Le repo est public dès le premier commit, par choix : c'est aussi un journal de bord et un exemple de la qualité de travail attendue dans une mission RezDevOps.

À ce stade, les contributions externes ne sont pas encore organisées (`CONTRIBUTING.md` arrivera quand une première PR externe se présentera). Les retours sont en revanche bienvenus via les [Issues GitHub](https://github.com/RezDevOps/fec-check/issues).

## Licence

[MIT](LICENSE). Permissive, lisible, standard. Cohérent avec la posture RezDevOps : on travaille en clair, on ne cherche pas à enfermer.

## Auteur

Rudy Rezaire — [RezDevOps](https://github.com/RezDevOps). Audit & développement de solutions data sur mesure pour TPE et PME françaises.
