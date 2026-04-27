# Fixtures FEC conformes

FEC d'exemple **conformes** à la norme A. 47 A-1 LPF. Servent de base aux tests positifs.

## `fec-minimal-conforme.txt`

Le plus petit FEC plausible que l'on puisse écrire à la main et qui satisfait toutes les règles A, B, C du MVP.

- **Encodage** : UTF-8 (sans BOM).
- **Séparateur** : tabulation (`\t`).
- **Fin de ligne** : LF.
- **Période** : exercice 2026 (du 01/01/2026 au 31/12/2026).
- **Écritures** : 3 écritures équilibrées sur 3 journaux différents (`AC` Achats, `VE` Ventes, `BQ` Banque), totalisant 8 lignes de données + 1 ligne d'en-tête.

### Détail des écritures

| Journal | EcritureNum | Description | Total |
|---|---|---|---|
| `AC` | `AC0001` | Facture fournisseur DURAND HT 1000 € + TVA 20 % | 1 200,00 € |
| `VE` | `VE0001` | Facture client MARTIN HT 2000 € + TVA 20 % | 2 400,00 € |
| `BQ` | `BQ0001` | Virement de paiement fournisseur DURAND | 1 200,00 € |

Total débit = total crédit = **4 800,00 €**.

### Cas d'usage en test

Cette fixture doit produire :
- Code de retour `0` (conforme).
- Aucune anomalie remontée par les familles A, B, C.
- Un rapport Markdown avec un verdict « **Conforme** » et zéro entrée d'erreur.
