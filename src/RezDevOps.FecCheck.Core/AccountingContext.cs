// © 2026 Rudy Rezaire / RezDevOps. Licence MIT — voir LICENSE.

using System.Globalization;

namespace RezDevOps.FecCheck.Core;

/// <summary>
/// Porte l'état nécessaire aux règles de la Famille B qui s'évaluent à
/// l'échelle du fichier (et non d'une seule ligne) :
/// <list type="bullet">
/// <item><description><see cref="Rules.B01"/> — équilibre Débit/Crédit par couple (<c>JournalCode</c>, <c>EcritureNum</c>) ;</description></item>
/// <item><description><see cref="Rules.B02"/> — équilibre Débit/Crédit global du fichier ;</description></item>
/// <item><description><see cref="Rules.B03"/> — cohérence du séparateur décimal entre tous les montants du fichier (la validation par-ligne du format numérique reste dans <see cref="DataLineValidator"/>).</description></item>
/// </list>
/// </summary>
/// <remarks>
/// Cette classe est instanciée une fois par appel à <see cref="FecValidator.Validate(Stream)"/>.
/// Elle reçoit chaque ligne de données via <see cref="Observe"/> pendant la
/// boucle de lecture en streaming, puis émet ses findings finaux via
/// <see cref="EmitFinalFindings"/> après la dernière ligne. La consommation
/// mémoire est <em>O(nombre d'écritures distinctes)</em>, pas O(nombre de
/// lignes) — ce qui reste compatible avec la cible §6.3 du cadrage : un FEC
/// de 100 Mo contient typiquement quelques dizaines de milliers d'écritures,
/// soit moins de 5 Mo d'agrégats.
/// </remarks>
internal sealed class AccountingContext
{
    // Indices des champs utilisés (parallèles à FecHeader.ExpectedColumns).
    private const int IdxJournalCode = 0;
    private const int IdxEcritureNum = 2;
    private const int IdxDebit = 11;
    private const int IdxCredit = 12;

    private readonly Dictionary<EcritureKey, EcritureAggregate> _ecritures = new();

    private decimal _totalDebit;
    private decimal _totalCredit;

    /// <summary>
    /// Séparateur décimal observé pour la première fois dans le fichier
    /// (<c>','</c> ou <c>'.'</c>). <c>null</c> tant qu'aucun montant ne l'a
    /// fixé (les montants entiers sans décimale n'établissent pas la convention).
    /// </summary>
    private char? _premierSeparateurDecimal;

    /// <summary>Numéro de la ligne où <see cref="_premierSeparateurDecimal"/> a été fixé, pour traçabilité dans les messages.</summary>
    private long _ligneOuSeparateurFixe;

    /// <summary>
    /// Observe une ligne de données : alimente les agrégats B01/B02 et
    /// vérifie B03 (cohérence séparateur décimal) au passage. La validation
    /// par-ligne du format numérique strict (B03 forme) reste dans
    /// <see cref="DataLineValidator"/> — ici on ne traite que la cohérence
    /// inter-lignes.
    /// </summary>
    /// <param name="fields">Champs déjà découpés de la ligne (longueur ≥ 13 attendue ; tolère plus court).</param>
    /// <param name="lineNumber">Numéro de ligne 1-indexé pour les messages d'anomalie.</param>
    /// <param name="sink">Accumulateur de findings : reçoit les anomalies B03 (cohérence séparateur).</param>
    public void Observe(string[] fields, long lineNumber, List<Finding> sink)
    {
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(sink);

        var debitRaw = IdxDebit < fields.Length ? fields[IdxDebit] : string.Empty;
        var creditRaw = IdxCredit < fields.Length ? fields[IdxCredit] : string.Empty;

        // B03 — cohérence du séparateur décimal sur tout le fichier.
        ObserveSeparatorConsistency(debitRaw, "Debit", lineNumber, sink);
        ObserveSeparatorConsistency(creditRaw, "Credit", lineNumber, sink);

        // B01 + B02 — agrégats. Un montant non parsable est ignoré côté
        // sommes (B03 par-ligne dans DataLineValidator l'aura signalé) :
        // l'inclure fausserait l'équilibre rapporté à l'utilisateur.
        var debit = TryParseAmount(debitRaw);
        var credit = TryParseAmount(creditRaw);

        _totalDebit += debit;
        _totalCredit += credit;

        var journalCode = IdxJournalCode < fields.Length ? fields[IdxJournalCode] : string.Empty;
        var ecritureNum = IdxEcritureNum < fields.Length ? fields[IdxEcritureNum] : string.Empty;

        // On n'agrège pas les lignes dont l'identifiant d'écriture est
        // inutilisable (déjà signalé par A07) — sinon une « écriture vide »
        // bidon agrège plusieurs lignes orphelines et produit un faux B01.
        if (string.IsNullOrWhiteSpace(journalCode) || string.IsNullOrWhiteSpace(ecritureNum))
        {
            return;
        }

        var key = new EcritureKey(journalCode, ecritureNum);
        if (!_ecritures.TryGetValue(key, out var aggregate))
        {
            aggregate = new EcritureAggregate(PremiereLigne: lineNumber);
            _ecritures[key] = aggregate;
        }

        _ecritures[key] = aggregate with
        {
            SommeDebit = aggregate.SommeDebit + debit,
            SommeCredit = aggregate.SommeCredit + credit,
        };
    }

    /// <summary>
    /// Émet les findings agrégés à émettre après lecture complète :
    /// un finding par écriture déséquilibrée (B01) puis, le cas échéant,
    /// un finding global pour le fichier (B02). Les écritures sont parcourues
    /// dans l'ordre de leur première apparition pour un rapport stable et
    /// reproductible.
    /// </summary>
    public void EmitFinalFindings(List<Finding> sink)
    {
        ArgumentNullException.ThrowIfNull(sink);

        // B01 — une entrée par écriture déséquilibrée, ordre = première ligne croissante.
        foreach (var (key, aggregate) in _ecritures.OrderBy(kv => kv.Value.PremiereLigne))
        {
            var ecart = aggregate.SommeDebit - aggregate.SommeCredit;
            if (ecart == 0m)
            {
                continue;
            }

            sink.Add(new Finding(
                Rule: Rules.B01,
                LineNumber: aggregate.PremiereLigne,
                Message:
                    $"Écriture « {key.JournalCode} / {key.EcritureNum} » déséquilibrée — "
                    + $"somme Debit = {Format(aggregate.SommeDebit)}, "
                    + $"somme Credit = {Format(aggregate.SommeCredit)}, "
                    + $"écart = {Format(ecart)}.",
                Contexte: null));
        }

        // B02 — équilibre global du fichier.
        var ecartGlobal = _totalDebit - _totalCredit;
        if (ecartGlobal != 0m)
        {
            sink.Add(new Finding(
                Rule: Rules.B02,
                LineNumber: null,
                Message:
                    "Le fichier est globalement déséquilibré — "
                    + $"somme Debit = {Format(_totalDebit)}, "
                    + $"somme Credit = {Format(_totalCredit)}, "
                    + $"écart = {Format(ecartGlobal)}.",
                Contexte: null));
        }
    }

    private void ObserveSeparatorConsistency(string raw, string nomChamp, long lineNumber, List<Finding> sink)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        var separator = DetectDecimalSeparator(raw);
        if (separator is null)
        {
            // Pas de séparateur visible (montant entier ou champ non parsable) :
            // ne fixe pas la convention, ne la viole pas. La validation de forme
            // est du ressort de DataLineValidator (B03 par-ligne).
            return;
        }

        if (_premierSeparateurDecimal is null)
        {
            _premierSeparateurDecimal = separator;
            _ligneOuSeparateurFixe = lineNumber;
            return;
        }

        if (separator != _premierSeparateurDecimal)
        {
            sink.Add(new Finding(
                Rule: Rules.B03,
                LineNumber: lineNumber,
                Message:
                    $"Ligne {lineNumber} : séparateur décimal incohérent dans le champ « {nomChamp} » "
                    + $"(« {separator} » trouvé, « {_premierSeparateurDecimal} » attendu — "
                    + $"convention fixée par la première occurrence ligne {_ligneOuSeparateurFixe}).",
                Contexte: raw));
        }
    }

    /// <summary>
    /// Détecte le séparateur décimal présent dans un champ montant.
    /// Retourne <c>null</c> si le champ ne contient ni <c>,</c> ni <c>.</c>,
    /// ou s'il en contient plusieurs (cas pathologique laissé à B03 par-ligne).
    /// </summary>
    private static char? DetectDecimalSeparator(string raw)
    {
        var s = raw.Trim();
        var hasComma = s.Contains(',');
        var hasDot = s.Contains('.');

        return (hasComma, hasDot) switch
        {
            (true, false) => ',',
            (false, true) => '.',
            _ => null, // ni l'un ni l'autre, ou les deux à la fois.
        };
    }

    /// <summary>
    /// Parse un montant en restant tolérant : remplace <c>,</c> par <c>.</c>
    /// pour une lecture invariante. Retourne <c>0m</c> sur champ vide ou non
    /// parsable — la qualification d'erreur est à la charge des règles de
    /// forme (B03 par-ligne dans <see cref="DataLineValidator"/>).
    /// </summary>
    private static decimal TryParseAmount(string raw)
    {
        var s = raw.Trim();
        if (s.Length == 0)
        {
            return 0m;
        }

        var normalized = s.Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
            ? amount
            : 0m;
    }

    private static string Format(decimal amount) =>
        amount.ToString("0.00", CultureInfo.InvariantCulture);

    private readonly record struct EcritureKey(string JournalCode, string EcritureNum);

    private sealed record EcritureAggregate(
        long PremiereLigne,
        decimal SommeDebit = 0m,
        decimal SommeCredit = 0m);
}
