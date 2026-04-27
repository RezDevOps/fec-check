# Fixtures FEC pathologiques — Famille B (cohérence comptable)

Chacun de ces fichiers contient **une seule anomalie** par rapport au FEC
de référence (`tests/fixtures/conforme/fec-minimal-conforme.txt`), conçue
pour déclencher la règle de la Famille B correspondante. Cela facilite
l'attribution exacte d'un finding à un fichier source dans les tests.

| Fichier | Règle visée | Anomalie injectée |
|---|---|---|
| `B01-ecriture-desequilibree.txt` | B01 (Erreur) | Deux écritures localement déséquilibrées (`AC0001` et `VE0001`) qui se compensent : équilibre **global** préservé, mais somme Debit ≠ somme Credit par couple `(JournalCode, EcritureNum)`. Conçu pour déclencher B01 sans déclencher B02. |
| `B02-total-global-desequilibre.txt` | B02 (Erreur) | Une seule écriture déséquilibrée (`AC0001`, écart 200 €) : la somme globale Debit ≠ somme globale Credit. Déclenche aussi B01 sur cette même écriture (attendu). |
| `B03-format-numerique-invalide.txt` | B03 (Erreur) | Un montant à 5 décimales (`1000,00000`) sur la ligne 2 : parsable comme `decimal` (donc pas de bruit B01/B02), mais hors du motif strict 0–4 décimales. |
| `B04-debit-et-credit-non-nuls.txt` | B04 (Avertissement) | Une ligne supplémentaire (écriture `OD0001`, ligne 10) avec `Debit = Credit = 150,00` : auto-équilibrée (pas de B01/B02), mais Débit *et* Crédit non nuls sur la même ligne. |
| `B05-compaux-num-sans-lib.txt` | B05 (Erreur) | `CompAuxNum = "DURAND"` rempli mais `CompAuxLib` vide sur la ligne 4 (l'écriture fournisseur). |
| `B06-compaux-sur-compte-non-tiers.txt` | B06 (Avertissement) | `CompAuxNum = "MARTIN"` attaché à un compte `707000` (ventes), qui n'est pas un compte de tiers (PCG : racine `4` attendue). |

## Convention

Sauf mention contraire :

- Encodage : UTF-8 sans BOM.
- Séparateur : tabulation `\t`.
- Fin de ligne : LF.
- Période : exercice 2026.
- Tous les autres aspects (format, colonnes, dates) sont conformes — seule
  la règle visée est violée. Cela vaut aussi pour les fixtures qui en
  déclenchent plusieurs « par effet domino » (B02 implique souvent B01) :
  le test associé n'assert que sur le finding visé.

## Régénération

Toutes les fixtures de ce dossier sont générées par transformation `awk`
du FEC conforme. Pour les régénérer après modification du conforme :

```sh
SRC=tests/fixtures/conforme/fec-minimal-conforme.txt
DST=tests/fixtures/non-conforme/comptable

# B01 : déséquilibres locaux compensés (équilibre global préservé)
awk 'BEGIN{FS=OFS="\t"} NR==4 {$13="1000,00"} NR==6 {$13="2200,00"} {print}' \
    "$SRC" > "$DST/B01-ecriture-desequilibree.txt"

# B02 : déséquilibre global
awk 'BEGIN{FS=OFS="\t"} NR==4 {$13="1000,00"} {print}' \
    "$SRC" > "$DST/B02-total-global-desequilibre.txt"

# B03 : montant à 5 décimales
awk 'BEGIN{FS=OFS="\t"} NR==2 {$12="1000,00000"} {print}' \
    "$SRC" > "$DST/B03-format-numerique-invalide.txt"

# B04 : écriture additionnelle avec Débit ET Crédit non nuls
awk 'BEGIN{FS=OFS="\t"}
{print}
END {print "OD","Operations Diverses","OD0001","20260301","606300","Petites fournitures","","","OD-2026-001","20260301","Reglement direct OD","150,00","150,00","","","20260301","",""}' \
    "$SRC" > "$DST/B04-debit-et-credit-non-nuls.txt"

# B05 : CompAuxNum rempli, CompAuxLib vidé
awk 'BEGIN{FS=OFS="\t"} NR==4 {$8=""} {print}' \
    "$SRC" > "$DST/B05-compaux-num-sans-lib.txt"

# B06 : CompAuxNum attaché à un compte non-tiers
awk 'BEGIN{FS=OFS="\t"} NR==6 {$7="MARTIN"; $8="MARTIN SARL"} {print}' \
    "$SRC" > "$DST/B06-compaux-sur-compte-non-tiers.txt"
```
