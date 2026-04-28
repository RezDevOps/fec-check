# Règles de validation `fec-check`

> Liste exhaustive des règles que `fec-check` vérifie ou prévoit de vérifier dans le MVP.
> Chaque règle a un identifiant stable (cité dans le code et dans les rapports), une source réglementaire et un jalon de livraison.
> Cette liste est la **source de vérité** pour la traçabilité réglementaire de l'outil.

## Conventions

- **Identifiant** : `<Famille><Numéro>` — par exemple `A01`, `B03`, `C02`.
- **Sévérité** : `Bloquante` (empêche la suite de l'analyse), `Erreur` (rapportée, fichier non conforme), `Avertissement` (rapporté, fichier conforme avec réserve).
- **Source** : référence réglementaire ou doctrinale exigible.
- **Jalon** : le tag à partir duquel la règle est *prévue* d'être couverte.
- **État** : `✓ vX.Y.Z` si la règle est implémentée et couverte par tests dans la version indiquée ; `—` si la règle est encore à venir.

---

## Famille A — Conformité de format

Couche déterministe, prérequis à toute validation sémantique.

| ID | Règle | Sévérité | Source | Jalon | État |
|---|---|---|---|---|---|
| A01 | Encodage du fichier dans `{ASCII, ISO-8859-15, UTF-8}` (BOM toléré) | Bloquante | A. 47 A-1 LPF, BOI-CF-IOR-60-40-20 | J1 | ✓ v0.1.0 |
| A02 | Séparateur de champs : tabulation `\t` ou pipe `\|`, cohérent dans tout le fichier | Bloquante | A. 47 A-1 LPF, BOI-CF-IOR-60-40-20 | J1 | ✓ v0.1.0 |
| A03 | Présence de l'en-tête (première ligne) avec les 18 noms de colonnes attendus | Bloquante | A. 47 A-1 LPF | J1 | ✓ v0.1.0 |
| A04 | Ordre exact des 18 colonnes dans l'en-tête | Bloquante | A. 47 A-1 LPF | J1 | ✓ v0.1.0 |
| A05 | Toute ligne de données contient exactement 18 champs (pas tronquée, pas surnuméraire) | Erreur | A. 47 A-1 LPF | J1 | ✓ v0.1.0 |
| A06 | Fin de ligne CRLF ou LF, cohérente dans tout le fichier | Avertissement | BOI-CF-IOR-60-40-20 | J1 | ✓ v0.1.0 |
| A07 | Champs obligatoires non vides : `JournalCode`, `EcritureNum`, `EcritureDate`, `CompteNum`, `EcritureLib`, et soit `Debit` soit `Credit` non nul | Erreur | A. 47 A-1 LPF | J1 | ✓ v0.1.0 |

## Famille B — Cohérence comptable

Cœur métier : ce qu'un vérificateur regarde en premier.

| ID | Règle | Sévérité | Source | Jalon | État |
|---|---|---|---|---|---|
| B01 | Pour chaque couple (`JournalCode`, `EcritureNum`), somme `Debit` = somme `Credit` | Erreur | Principe de la partie double, A. 47 A-1 LPF | J2 | ✓ v0.2.0 |
| B02 | Somme globale `Debit` du fichier = somme globale `Credit` | Erreur | Principe de la partie double | J2 | ✓ v0.2.0 |
| B03 | Format numérique des montants : séparateur décimal cohérent (`,` ou `.`) sur tout le fichier, pas de séparateur de milliers, 0 à 4 décimales tolérées | Erreur | A. 47 A-1 LPF | J2 | ✓ v0.2.0 |
| B04 | Mutuelle exclusion `Debit`/`Credit` sur une même ligne (l'un des deux est zéro), sauf cas explicitement documenté | Avertissement | Pratique comptable standard | J2 | ✓ v0.2.0 |
| B05 | Si `CompAuxNum` est rempli, alors `CompAuxLib` doit l'être aussi (et inversement) | Erreur | A. 47 A-1 LPF | J2 | ✓ v0.2.0 |
| B06 | Si `CompAuxNum` est rempli, alors `CompteNum` commence par `4` (compte de tiers — racines 401, 411, 421, 425, etc.) | Avertissement | PCG | J2 | ✓ v0.2.0 |

## Famille C — Cohérence temporelle

Vérifie l'irréversibilité de la comptabilité.

| ID | Règle | Sévérité | Source | Jalon | État |
|---|---|---|---|---|---|
| C01 | `EcritureDate` au format `AAAAMMJJ` strict (8 chiffres, date valide) | Erreur | A. 47 A-1 LPF | J3 | ✓ v0.3.0 |
| C02 | `PieceDate` au format `AAAAMMJJ` strict si rempli | Erreur | A. 47 A-1 LPF | J3 | ✓ v0.3.0 |
| C03 | `ValidDate` au format `AAAAMMJJ` strict si rempli | Erreur | A. 47 A-1 LPF | J3 | ✓ v0.3.0 |
| C04 | `DateLet` au format `AAAAMMJJ` strict si rempli | Erreur | A. 47 A-1 LPF | J3 | ✓ v0.3.0 |
| C05 | Toutes les `EcritureDate` dans la période d'exercice déclarée (option `--exercice`) | Erreur | BOI-CF-IOR-60-40-20 | J3 | ✓ v0.3.0 |
| C06 | `ValidDate` postérieure ou égale à `EcritureDate` quand les deux sont remplies | Erreur | Doctrine fiscale | J3 | ✓ v0.3.0 |
| C07 | Numérotation chronologique des écritures **validées** au sein d'un même journal (croissance de `EcritureDate` selon `EcritureNum` parmi les écritures avec `ValidDate` non vide) | Erreur | BOI-CF-IOR-60-40-20 (irréversibilité) | J3 | ✓ v0.3.0 |
| C08 | Signalement des écritures sans `ValidDate` (non validées) | Avertissement | BOI-CF-IOR-60-40-20 | J3 | ✓ v0.3.0 |

---

## Hors MVP — reporté v2 ou plus

Pour mémoire, à ne pas implémenter dans le MVP :

- Validation des numéros de compte selon le PCG (longueur, racines autorisées, exceptions par secteur d'activité).
- Cohérences sémantiques fines : TVA collectée/déductible, lettrage croisé, contreparties standards (exemple : un achat doit avoir une contrepartie 401 ou banque), écritures de clôture.
- Format XML du FEC.
- Correction automatique des anomalies (par construction : `fec-check` rapporte, ne modifie pas).

---

## Sources réglementaires

| Référence | URL |
|---|---|
| Article L. 47 A-I du LPF | https://www.legifrance.gouv.fr/ |
| Article A. 47 A-1 du LPF | https://www.legifrance.gouv.fr/ |
| Arrêté du 29 juillet 2013 | https://www.legifrance.gouv.fr/ |
| BOI-CF-IOR-60-40-10 | https://bofip.impots.gouv.fr/bofip/3372-PGP |
| BOI-CF-IOR-60-40-20 | https://bofip.impots.gouv.fr/bofip/3373-PGP |
| BOI-CF-IOR-60-40-30 | https://bofip.impots.gouv.fr/bofip/3374-PGP |

> Les liens directs vers chaque article et BOFiP seront vérifiés au début de chaque jalon (le BOFiP est mis à jour par publication ; un lien permanent peut bouger).
