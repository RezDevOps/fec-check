# Fixtures FEC pathologiques — Famille C (cohérence temporelle)

Chacun de ces fichiers contient **une seule anomalie** ciblée pour
déclencher la règle de la Famille C correspondante, en restant aussi
silencieux que possible sur les autres familles. Les écritures sont toutes
équilibrées (B01 vert, B02 vert), les formats numériques sont stricts
(B03 vert), et les autres règles A/B sont satisfaites — seule la règle
visée est violée. Cela facilite l'attribution exacte d'un finding à un
fichier source dans les tests.

| Fichier | Règle visée | Anomalie injectée |
|---|---|---|
| `C01-ecriture-date-invalide.txt` | C01 (Erreur) | `EcritureDate = "2024-01-15"` (avec tirets) au lieu du format `AAAAMMJJ` strict, sur les deux lignes de l'écriture `AC0001`. |
| `C02-piece-date-invalide.txt` | C02 (Erreur) | `PieceDate = "20240230"` (30 février : date impossible du calendrier) sur les deux lignes. |
| `C03-valid-date-invalide.txt` | C03 (Erreur) | `ValidDate = "31-12-2024"` (mauvais format) sur les deux lignes. La présence du champ (même invalide) suffit à considérer l'écriture comme « validée » côté C08, qui ne déclenche donc pas. |
| `C04-date-let-invalide.txt` | C04 (Erreur) | `DateLet = "2024/01/20"` (slash au lieu de format `AAAAMMJJ`). `EcritureLet` rempli pour rendre la situation cohérente. |
| `C05-hors-periode-exercice.txt` | C05 (Erreur) | `EcritureDate = 20231231` — date valide mais hors de la période d'exercice **2024-01-01 → 2024-12-31**. Cette fixture ne déclenche C05 qu'avec l'option `--exercice 2024-01-01:2024-12-31` ; sans option, le fichier est conforme. |
| `C06-validation-anterieure-ecriture.txt` | C06 (Erreur) | `ValidDate = 20240110` antérieure à `EcritureDate = 20240115` sur les deux lignes : une écriture ne peut être validée avant d'être passée. |
| `C07-chronologie-cassee.txt` | C07 (Erreur) | Deux écritures validées dans le journal `AC` : `AC0001` (`20240315`) puis `AC0002` (`20240210`). La numérotation est croissante mais la date recule — violation d'irréversibilité. |
| `C08-ecritures-non-validees.txt` | C08 (Avertissement) | Deux écritures équilibrées sans `ValidDate` (champ vide sur toutes les lignes) : déclenche un finding C08 agrégé. |

## Convention

Sauf mention contraire :

- Encodage : UTF-8 sans BOM.
- Séparateur : tabulation `\t`.
- Fin de ligne : LF.
- Période : exercice 2024 (cohérent avec l'option `--exercice 2024-01-01:2024-12-31` utilisée par les tests C05).
- Tous les autres aspects (format, colonnes, montants équilibrés, dates
  parsables sauf pour la règle C0x visée) sont conformes — seule la
  règle visée est violée.

## Régénération

Toutes les fixtures de ce dossier sont produites par le script
`scripts/gen-c-fixtures.sh` (à conserver dans le repo si la régénération
devient récurrente). À J3, elles ont été générées à la main une seule
fois ; on régénérera via script si une refonte du format FEC ou un
ajustement systématique d'écritures-types s'impose en J4+.
