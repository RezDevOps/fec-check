<!-- Exemple de rapport produit par fec-check sur un FEC fictif. Format identique à la sortie de `--output-md`. -->

# Rapport d'analyse FEC

**Outil** : fec-check 0.4.0
**Généré le** : 2026-04-28T14:30:00Z
**Fichier analysé** : `exports/fec-2024-sarl-lumieres-du-marais.txt`

## Verdict — NON CONFORME

> **4 anomalies** détectées : 3 erreurs, 1 avertissement.

## Caractéristiques du fichier

| Propriété      | Valeur |
|----------------|--------|
| Encodage       | UTF-8 |
| Séparateur     | tabulation |
| Fin de ligne   | mixte |
| Lignes lues    | 1287 |
| Exercice       | du 2024-01-01 au 2024-12-31 |

## Synthèse par famille de règles

| Famille | Anomalies |
|---------|-----------|
| Famille A — Conformité de format | 1 |
| Famille B — Cohérence comptable | 2 |
| Famille C — Cohérence temporelle | 1 |

## Anomalies détectées

### Famille A — Conformité de format

#### A06 — Fin de ligne CRLF ou LF, cohérente dans tout le fichier. (Avertissement)

- **Source** : BOI-CF-IOR-60-40-20
- **Emplacement** : fichier (anomalie globale)
- **Message** : Le fichier mélange plusieurs conventions de fin de ligne (CRLF, LF, CR). Une convention unique est attendue sur l'ensemble du fichier.

### Famille B — Cohérence comptable

#### B01 — Pour chaque couple (JournalCode, EcritureNum), somme Debit = somme Credit. (Erreur)

- **Source** : Principe de la partie double, A. 47 A-1 LPF
- **Emplacement** : ligne 412
- **Message** : Écriture (VT, 0042) : somme Debit = 1200,00, somme Credit = 1100,00, écart = 100,00.

```text
VT	0042	20240315	411DUPONT	Client Dupont	411DUPONT	Client Dupont	FA2024-042	20240315	Vente prestation	1200,00	0,00		20240320	20240320	0,00	
```

#### B05 — Si CompAuxNum est rempli, alors CompAuxLib doit l'être aussi (et inversement). (Erreur)

- **Source** : A. 47 A-1 LPF
- **Emplacement** : ligne 605
- **Message** : CompAuxNum « 411MARTIN » renseigné mais CompAuxLib vide.

```text
VT	0058	20240522	411000	Compte clients	411MARTIN		FA2024-058	20240522	Vente marchandise	0,00	840,00		20240601	20240601	0,00	
```

### Famille C — Cohérence temporelle

#### C05 — Toutes les EcritureDate dans la période d'exercice déclarée (option --exercice). (Erreur)

- **Source** : BOI-CF-IOR-60-40-20
- **Emplacement** : ligne 980
- **Message** : EcritureDate 20250115 hors de l'exercice 2024-01-01 / 2024-12-31.

```text
AC	0091	20250115	606300	Achat fournitures de bureau			FA2025-001	20250115	Fournitures janvier	0,00	62,40		20250120	20250120	0,00	
```

---

## Pour aller plus loin

- Liste exhaustive des règles : <https://github.com/RezDevOps/fec-check/blob/main/docs/regles.md>
- Schéma JSON (contrat figé v1) : <https://github.com/RezDevOps/fec-check/blob/main/docs/json-schema.md>
- Texte officiel : Article A. 47 A-1 du Livre des procédures fiscales, BOI-CF-IOR-60-40-20.

> *Rapport généré par `fec-check`, utilitaire libre publié par RezDevOps sous licence MIT. Aucune donnée n'est transmise sur le réseau ; l'analyse est 100 % locale.*
