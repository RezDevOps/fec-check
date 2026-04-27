# Fixtures FEC pathologiques — Famille A (format)

Chacun de ces fichiers contient **une seule anomalie** par rapport au FEC
de référence (`tests/fixtures/conforme/fec-minimal-conforme.txt`), conçue
pour déclencher la règle de la Famille A correspondante. Cela facilite
l'attribution exacte d'un finding à un fichier source dans les tests.

| Fichier | Règle visée | Anomalie injectée |
|---|---|---|
| `A01-encodage-utf16.txt` | A01 (Bloquante) | Encodage UTF-16 LE avec BOM (hors ensemble autorisé). |
| `A02-separateur-mixte.txt` | A02 (Bloquante) | Une ligne de données utilise `\|` alors que l'en-tête utilise `\t`. |
| `A03-entete-colonnes-manquantes.txt` | A03 (Bloquante) | En-tête avec 17 colonnes au lieu de 18 (la dernière, `Idevise`, est absente). |
| `A04-entete-ordre-faux.txt` | A04 (Bloquante) | Les 18 colonnes attendues sont présentes mais `Debit` et `Credit` sont permutés. |
| `A05-ligne-tronquee.txt` | A05 (Erreur) | Une ligne de données ne contient que 17 champs. |
| `A06-eol-mixte.txt` | A06 (Avertissement) | L'en-tête se termine en CRLF, les lignes de données en LF. |
| `A07-champ-obligatoire-vide.txt` | A07 (Erreur) | Une ligne de données a un `JournalCode` vide. |

## Convention

Sauf mention contraire :

- Encodage : UTF-8 sans BOM.
- Séparateur : tabulation `\t`.
- Fin de ligne : LF.
- Période : exercice 2026.

Chaque fixture est volontairement la plus petite possible : 1 ligne d'en-tête
+ 2 à 3 lignes de données. Les écritures sont équilibrées comptablement
pour ne pas créer de faux positifs sur les Familles B/C qui viendront aux
jalons J2/J3.

## Régénération

`A01-encodage-utf16.txt` et `A06-eol-mixte.txt` sont des fichiers binaires
qui ne peuvent pas être édités directement. Pour les régénérer après
modification :

```sh
# A01 — UTF-16 LE avec BOM
python3 -c "
content = open('tests/fixtures/conforme/fec-minimal-conforme.txt').read()
with open('tests/fixtures/non-conforme/format/A01-encodage-utf16.txt', 'wb') as f:
    f.write(b'\xff\xfe')
    f.write(content.encode('utf-16-le'))
"

# A06 — mélange CRLF (en-tête) + LF (données)
# Voir le script generate-A06.sh dans ce dossier.
```
