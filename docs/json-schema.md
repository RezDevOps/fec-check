<!-- © 2026 Rudy Rezaire / RezDevOps — Document figé. Voir LICENSE. -->

# Schéma JSON du rapport `fec-check` — version 1

> Schéma figé à partir de `v0.4.0` (avril 2026).
> Source de vérité du contrat exposé par l'option `--output-json`.
> Toute modification non additive entraîne l'incrément de `schemaVersion`.

## 1. Posture

Le rapport JSON est conçu pour être **stable dans le temps**. Un script qui parse `fec-check` aujourd'hui doit pouvoir parser une version ultérieure sans casser, tant que la racine annonce `schemaVersion: 1`.

Trois règles d'évolution :

1. **Ajout d'un champ optionnel** — n'incrémente pas `schemaVersion`. Les consommateurs anciens ignorent le champ inconnu, les nouveaux l'utilisent.
2. **Ajout d'une valeur d'énumération** — n'incrémente pas `schemaVersion`. Les consommateurs doivent prévoir une branche par défaut (les énumérations sont listées plus bas avec leur date d'introduction).
3. **Renommage, suppression, changement de type** — incrémente `schemaVersion` à `2`. Les consommateurs `v1` qui croisent un document `v2` doivent refuser de continuer.

## 2. Structure générale

```jsonc
{
  "schemaVersion": 1,
  "outil": { "nom": "fec-check", "version": "1.0.0" },
  "genereLe": "2026-04-29T14:30:00Z",
  "fichier": {
    "chemin": "/chemin/vers/le/fichier.txt",
    "encodage": "UTF-8",
    "separateur": "tabulation",
    "finDeLigne": "LF",
    "lignesLues": 9
  },
  "exercice": { "debut": "2024-01-01", "fin": "2024-12-31" },
  "verdict": "CONFORME",
  "codeRetour": 0,
  "synthese": {
    "totalAnomalies": 0,
    "parSeverite": { "avertissement": 0, "erreur": 0, "bloquante": 0 },
    "parFamille":  { "format": 0, "comptable": 0, "temporel": 0 }
  },
  "anomalies": []
}
```

Tous les noms de champs sont en `camelCase`. Les chaînes vides ne sont jamais utilisées comme « absent » : un champ optionnel absent n'est pas écrit (cf. `chemin`, `separateur`, `contexte`, `ligne`, et l'objet `exercice` lui-même). Cette omission est imposée par `JsonIgnoreCondition.WhenWritingNull` côté writer.

## 3. Champs racine

### 3.1 `schemaVersion` (requis, entier)

Numéro de version du schéma. Vaut `1` depuis `v0.4.0` (figé à `1` pour toute la branche `v1.x`). Doit être lu en tout premier par les consommateurs pour décider s'ils savent traiter le document.

### 3.2 `outil` (requis, objet)

| Sous-champ | Type | Description |
|---|---|---|
| `nom` | string | Nom du produit. Stable : `"fec-check"`. |
| `version` | string | Version SemVer du produit (ex : `"1.0.0"`). |

### 3.3 `genereLe` (requis, string ISO-8601)

Horodatage UTC de génération du rapport, au format `YYYY-MM-DDThh:mm:ssZ`. Toujours en UTC, suffixe `Z` obligatoire.

### 3.4 `fichier` (requis, objet)

Caractéristiques effectives du FEC analysé telles que vues par le validateur.

| Sous-champ | Type | Description |
|---|---|---|
| `chemin` | string \| absent | Chemin tel que fourni à la CLI. Absent si l'analyse a été faite sur flux non identifié. |
| `encodage` | string | Énumération : `"UTF-8"`, `"UTF-8 (avec BOM)"`, `"ISO-8859-15"`, `"non reconnu"`. |
| `separateur` | string \| absent | Énumération : `"tabulation"`, `"pipe"`. Absent si non détecté. |
| `finDeLigne` | string | Énumération : `"CRLF"`, `"LF"`, `"mixte"`, `"aucune"`. |
| `lignesLues` | number (entier) | Nombre total de lignes lues, en-tête comprise. |

### 3.5 `exercice` (optionnel, objet)

Présent uniquement si l'utilisateur a fourni `--exercice` à la CLI. Quand absent, la règle C05 n'a pas été évaluée.

| Sous-champ | Type | Description |
|---|---|---|
| `debut` | string `YYYY-MM-DD` | Borne inférieure incluse. |
| `fin` | string `YYYY-MM-DD` | Borne supérieure incluse. |

### 3.6 `verdict` (requis, string)

Énumération exposée pour la lisibilité humaine et l'agrégation : `"CONFORME"`, `"CONFORME_AVEC_AVERTISSEMENTS"`, `"NON_CONFORME"`.

### 3.7 `codeRetour` (requis, entier)

Mappe directement le verdict sur le code de retour processus :

| Verdict | `codeRetour` |
|---|---|
| `CONFORME` | `0` |
| `CONFORME_AVEC_AVERTISSEMENTS` | `1` |
| `NON_CONFORME` | `2` |

À noter : `3` (erreur d'exécution) et `64` (usage incorrect) ne sont pas exposés ici puisqu'ils ne génèrent pas de rapport JSON.

### 3.8 `synthese` (requis, objet)

Compteurs agrégés. Le total `synthese.totalAnomalies` est égal à `anomalies.length`, et la somme des valeurs de `parSeverite` est égale à `totalAnomalies` (idem `parFamille`).

| Sous-champ | Type | Description |
|---|---|---|
| `totalAnomalies` | number | Nombre total d'anomalies. |
| `parSeverite.avertissement` | number | Anomalies de sévérité avertissement (n'invalident pas). |
| `parSeverite.erreur` | number | Anomalies de sévérité erreur (verdict non conforme). |
| `parSeverite.bloquante` | number | Anomalies bloquantes (analyse interrompue, verdict non conforme). |
| `parFamille.format` | number | Anomalies de la Famille A (format). |
| `parFamille.comptable` | number | Anomalies de la Famille B (cohérence comptable). |
| `parFamille.temporel` | number | Anomalies de la Famille C (cohérence temporelle). |

### 3.9 `anomalies` (requis, tableau)

Liste ordonnée des anomalies dans l'ordre de leur découverte par le validateur. Tableau vide si le verdict est `CONFORME`.

#### Élément `anomalies[i]`

| Sous-champ | Type | Description |
|---|---|---|
| `regle` | objet | Définition de la règle violée (cf. ci-dessous). |
| `ligne` | number \| absent | Numéro de ligne 1-indexé (la ligne 1 est l'en-tête). Absent si la règle s'applique au fichier dans son ensemble. |
| `message` | string | Description précise de l'anomalie en français, à destination d'un dirigeant non-tech. |
| `contexte` | string \| absent | Extrait du fichier (ligne brute) à présenter à l'utilisateur. Absent si non pertinent. |

#### Élément `anomalies[i].regle`

| Sous-champ | Type | Description |
|---|---|---|
| `id` | string | Identifiant stable au format `<Famille><NN>` (ex : `"A01"`, `"B03"`, `"C07"`). |
| `famille` | string | Énumération : `"format"`, `"comptable"`, `"temporel"`. |
| `severite` | string | Énumération : `"avertissement"`, `"erreur"`, `"bloquante"`. |
| `libelle` | string | Libellé court de la règle, en français. |
| `source` | string | Référence réglementaire ou doctrinale (ex : `"A. 47 A-1 LPF"`, `"BOI-CF-IOR-60-40-20"`, `"PCG"`). |

## 4. Exemple complet (rapport non conforme abrégé)

```json
{
  "schemaVersion": 1,
  "outil": { "nom": "fec-check", "version": "1.0.0" },
  "genereLe": "2026-04-29T14:30:00Z",
  "fichier": {
    "chemin": "exports/fec-2024.txt",
    "encodage": "UTF-8",
    "separateur": "tabulation",
    "finDeLigne": "CRLF",
    "lignesLues": 1287
  },
  "exercice": { "debut": "2024-01-01", "fin": "2024-12-31" },
  "verdict": "NON_CONFORME",
  "codeRetour": 2,
  "synthese": {
    "totalAnomalies": 2,
    "parSeverite": { "avertissement": 0, "erreur": 2, "bloquante": 0 },
    "parFamille":  { "format": 0, "comptable": 1, "temporel": 1 }
  },
  "anomalies": [
    {
      "regle": {
        "id": "B01",
        "famille": "comptable",
        "severite": "erreur",
        "libelle": "Pour chaque couple (JournalCode, EcritureNum), somme Debit = somme Credit.",
        "source": "Principe de la partie double, A. 47 A-1 LPF"
      },
      "ligne": 412,
      "message": "Écriture (VT, 0042) : somme Debit = 1200,00, somme Credit = 1100,00, écart = 100,00.",
      "contexte": "VT\t0042\t..."
    },
    {
      "regle": {
        "id": "C05",
        "famille": "temporel",
        "severite": "erreur",
        "libelle": "Toutes les EcritureDate dans la période d'exercice déclarée (option --exercice).",
        "source": "BOI-CF-IOR-60-40-20"
      },
      "ligne": 980,
      "message": "EcritureDate 20250115 hors de l'exercice 2024-01-01 / 2024-12-31."
    }
  ]
}
```

## 5. Énumérations — historique des valeurs

| Champ | Valeurs `v1` | Introduit en |
|---|---|---|
| `verdict` | `CONFORME`, `CONFORME_AVEC_AVERTISSEMENTS`, `NON_CONFORME` | v0.4.0 |
| `fichier.encodage` | `UTF-8`, `UTF-8 (avec BOM)`, `ISO-8859-15`, `non reconnu` | v0.4.0 |
| `fichier.separateur` | `tabulation`, `pipe` | v0.4.0 |
| `fichier.finDeLigne` | `CRLF`, `LF`, `mixte`, `aucune` | v0.4.0 |
| `regle.famille` | `format`, `comptable`, `temporel` | v0.4.0 |
| `regle.severite` | `avertissement`, `erreur`, `bloquante` | v0.4.0 |
| `regle.id` | `A01..A07`, `B01..B06`, `C01..C08` | v0.4.0 |

Les futures règles ajoutées au catalogue pourront étendre `regle.id` sans déclencher de bump de `schemaVersion`. Les consommateurs doivent traiter les identifiants comme des chaînes opaques et ne jamais en hardcoder l'enveloppe complète.

## 6. Validation par un consommateur

Recette minimale pour un script qui veut savoir si un FEC est conforme :

```bash
fec-check --output-json /tmp/r.json fec.txt
jq -r '.verdict' /tmp/r.json
```

Pour récupérer les règles violées, par sévérité :

```bash
jq '[.anomalies[] | select(.regle.severite == "erreur") | .regle.id] | unique' /tmp/r.json
```

## 7. Limites connues

- Le champ `genereLe` est en UTC, jamais en heure locale. Un consommateur qui veut afficher l'heure locale doit faire la conversion lui-même.
- L'ordre des anomalies dans `anomalies` reflète l'ordre de détection du validateur, pas un tri par sévérité ni par ligne. Un consommateur qui veut un tri spécifique doit le faire lui-même.
- Aucun champ de hash / signature n'est exposé pour l'instant : si vous archivez le rapport pour valeur probante, calculez vous-même un SHA-256 du fichier émis.
