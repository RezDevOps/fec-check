// © 2026 Rudy Rezaire / RezDevOps. Licence MIT — voir LICENSE.

using System.Globalization;
using System.Text.RegularExpressions;

namespace RezDevOps.FecCheck.Core;

/// <summary>
/// Valide les règles applicables ligne par ligne (sans état inter-lignes) :
/// <list type="bullet">
/// <item><description><see cref="Rules.A05"/> — exactement 18 champs ;</description></item>
/// <item><description><see cref="Rules.A07"/> — champs obligatoires non vides + au moins un montant Débit/Crédit non nul ;</description></item>
/// <item><description><see cref="Rules.B03"/> — forme par-ligne du montant : motif <c>^-?\d+([,.]\d{0,4})?$</c>, sans séparateur de milliers ni notation scientifique. La cohérence du séparateur décimal entre les lignes est gérée par <see cref="AccountingContext"/>.</description></item>
/// <item><description><see cref="Rules.B04"/> — mutuelle exclusion <c>Debit</c>/<c>Credit</c> non nuls sur la même ligne (avertissement) ;</description></item>
/// <item><description><see cref="Rules.B05"/> — <c>CompAuxNum</c> et <c>CompAuxLib</c> remplis ensemble ou tous les deux vides ;</description></item>
/// <item><description><see cref="Rules.B06"/> — un compte auxiliaire implique un <c>CompteNum</c> commençant par <c>'4'</c> (compte de tiers).</description></item>
/// </list>
/// </summary>
/// <remarks>
/// Le validateur reçoit un <see cref="List{Finding}"/> en accumulateur pour
/// éviter d'allouer une liste par ligne de FEC : un fichier client peut faire
/// des centaines de milliers de lignes, et la perf cible §6.3 du cadrage
/// vise &lt; 10 s sur 100 Mo.
/// </remarks>
internal static class DataLineValidator
{
    // Indices des champs (parallèles à FecHeader.ExpectedColumns).
    private const int IdxJournalCode = 0;
    private const int IdxEcritureNum = 2;
    private const int IdxEcritureDate = 3;
    private const int IdxCompteNum = 4;
    private const int IdxCompAuxNum = 6;
    private const int IdxCompAuxLib = 7;
    private const int IdxEcritureLib = 10;
    private const int IdxDebit = 11;
    private const int IdxCredit = 12;

    private static readonly (int Index, string Nom)[] ChampsObligatoiresNonVides =
    {
        (IdxJournalCode, "JournalCode"),
        (IdxEcritureNum, "EcritureNum"),
        (IdxEcritureDate, "EcritureDate"),
        (IdxCompteNum, "CompteNum"),
        (IdxEcritureLib, "EcritureLib"),
    };

    /// <summary>
    /// Motif strict d'un montant FEC : signe optionnel, au moins un chiffre
    /// avant le séparateur décimal, séparateur décimal optionnel <c>,</c> ou
    /// <c>.</c> suivi de 0 à 4 décimales. Refuse les séparateurs de milliers
    /// (espace, apostrophe, etc.), la notation scientifique, et plus de
    /// 4 décimales — cf. cadrage J2 (réponse Rudy : 0–4 décimales tolérées).
    /// </summary>
    private static readonly Regex AmountPattern =
        new(@"^-?\d+([,.]\d{0,4})?$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Valide une ligne de données et ajoute les <see cref="Finding"/> détectés
    /// dans <paramref name="sink"/>. La méthode ne court-circuite pas : si A05
    /// est violée (mauvais nombre de champs), les autres règles sont tout de
    /// même évaluées sur les champs effectivement présents — un FEC tronqué
    /// reste informatif.
    /// </summary>
    /// <param name="fields">Champs déjà découpés (le split est fait par <see cref="FecValidator"/> pour mutualiser avec <see cref="AccountingContext"/>).</param>
    /// <param name="rawLine">Ligne brute, pour servir de contexte aux findings.</param>
    /// <param name="lineNumber">Numéro de ligne 1-indexé.</param>
    /// <param name="sink">Accumulateur de findings.</param>
    public static void Validate(string[] fields, string rawLine, long lineNumber, List<Finding> sink)
    {
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(rawLine);
        ArgumentNullException.ThrowIfNull(sink);

        // A05 — exactement 18 champs.
        if (fields.Length != FecHeader.ExpectedColumnCount)
        {
            sink.Add(new Finding(
                Rule: Rules.A05,
                LineNumber: lineNumber,
                Message:
                    $"Ligne {lineNumber} : {fields.Length} champ(s) trouvé(s), "
                    + $"{FecHeader.ExpectedColumnCount} attendus.",
                Contexte: rawLine));
        }

        // A07 — champs obligatoires non vides.
        foreach (var (index, nom) in ChampsObligatoiresNonVides)
        {
            if (index >= fields.Length || string.IsNullOrWhiteSpace(fields[index]))
            {
                sink.Add(new Finding(
                    Rule: Rules.A07,
                    LineNumber: lineNumber,
                    Message: $"Ligne {lineNumber} : champ obligatoire « {nom} » vide ou absent.",
                    Contexte: rawLine));
            }
        }

        // Lecture défensive des champs utilisés par les règles suivantes.
        var debitRaw = IdxDebit < fields.Length ? fields[IdxDebit] : string.Empty;
        var creditRaw = IdxCredit < fields.Length ? fields[IdxCredit] : string.Empty;

        // B03 — format par-ligne des montants Débit et Crédit.
        ValidateAmountForm(debitRaw, "Debit", lineNumber, rawLine, sink);
        ValidateAmountForm(creditRaw, "Credit", lineNumber, rawLine, sink);

        // A07 (suite) — au moins un de Debit ou Credit non nul.
        var debit = TryParseAmount(debitRaw);
        var credit = TryParseAmount(creditRaw);
        var debitNonNul = debit is { } d && d != 0m;
        var creditNonNul = credit is { } c && c != 0m;

        // Si l'un des deux n'est pas parsable du tout (champ rempli mais format
        // invalide), on considère qu'il y a une intention de montant : A07 ne
        // doit pas se déclencher en plus de B03 sur la même ligne.
        var debitNonVideEtNonParsable = !string.IsNullOrWhiteSpace(debitRaw) && debit is null;
        var creditNonVideEtNonParsable = !string.IsNullOrWhiteSpace(creditRaw) && credit is null;

        if (!debitNonNul && !creditNonNul && !debitNonVideEtNonParsable && !creditNonVideEtNonParsable)
        {
            sink.Add(new Finding(
                Rule: Rules.A07,
                LineNumber: lineNumber,
                Message:
                    $"Ligne {lineNumber} : ni Debit ni Credit n'a de montant non nul "
                    + "(au moins un des deux est requis).",
                Contexte: rawLine));
        }

        // B04 — mutuelle exclusion Débit/Crédit (avertissement). On exige les
        // deux montants strictement positifs : un Débit non nul ET un Crédit
        // non nul sur la même ligne est inhabituel, sauf cas documenté.
        if (debitNonNul && creditNonNul)
        {
            sink.Add(new Finding(
                Rule: Rules.B04,
                LineNumber: lineNumber,
                Message:
                    $"Ligne {lineNumber} : Debit et Credit sont tous deux non nuls "
                    + $"(Debit = {Format(debit!.Value)}, Credit = {Format(credit!.Value)}). "
                    + "L'usage standard veut qu'une ligne porte soit un débit, soit un crédit.",
                Contexte: rawLine));
        }

        // B05 — cohérence CompAuxNum / CompAuxLib (XOR rempli interdit).
        var compAuxNum = IdxCompAuxNum < fields.Length ? fields[IdxCompAuxNum] : string.Empty;
        var compAuxLib = IdxCompAuxLib < fields.Length ? fields[IdxCompAuxLib] : string.Empty;
        var hasCompAuxNum = !string.IsNullOrWhiteSpace(compAuxNum);
        var hasCompAuxLib = !string.IsNullOrWhiteSpace(compAuxLib);

        if (hasCompAuxNum != hasCompAuxLib)
        {
            var rempli = hasCompAuxNum ? "CompAuxNum" : "CompAuxLib";
            var vide = hasCompAuxNum ? "CompAuxLib" : "CompAuxNum";
            sink.Add(new Finding(
                Rule: Rules.B05,
                LineNumber: lineNumber,
                Message:
                    $"Ligne {lineNumber} : « {rempli} » est rempli mais « {vide} » est vide. "
                    + "Les deux champs auxiliaires doivent être renseignés ensemble ou laissés vides ensemble.",
                Contexte: rawLine));
        }

        // B06 — un compte auxiliaire implique un compte de tiers (racine '4').
        if (hasCompAuxNum)
        {
            var compteNum = IdxCompteNum < fields.Length ? fields[IdxCompteNum].Trim() : string.Empty;
            if (compteNum.Length > 0 && compteNum[0] != '4')
            {
                sink.Add(new Finding(
                    Rule: Rules.B06,
                    LineNumber: lineNumber,
                    Message:
                        $"Ligne {lineNumber} : un compte auxiliaire (« {compAuxNum} ») est attaché "
                        + $"au compte « {compteNum} », qui n'est pas un compte de tiers (racine PCG attendue : 4xxxxx).",
                    Contexte: rawLine));
            }
        }
    }

    /// <summary>
    /// Vérifie qu'un montant respecte la forme attendue (B03 par-ligne).
    /// Un champ vide est ignoré (la règle de présence est A07, pas B03).
    /// </summary>
    private static void ValidateAmountForm(string raw, string nomChamp, long lineNumber, string rawLine, List<Finding> sink)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        var trimmed = raw.Trim();
        if (AmountPattern.IsMatch(trimmed))
        {
            return;
        }

        sink.Add(new Finding(
            Rule: Rules.B03,
            LineNumber: lineNumber,
            Message:
                $"Ligne {lineNumber} : champ « {nomChamp} » au format invalide « {trimmed} ». "
                + "Format attendu : chiffres avec séparateur décimal optionnel (, ou .), "
                + "0 à 4 décimales, sans séparateur de milliers ni notation scientifique.",
            Contexte: rawLine));
    }

    /// <summary>
    /// Parse un montant en restant tolérant : remplace <c>,</c> par <c>.</c>
    /// pour une lecture invariante. Retourne <c>null</c> si le champ est non
    /// vide mais non parsable — signalant à l'appelant que la forme est cassée
    /// (déjà ou sur le point d'être qualifiée par B03), pour qu'il évite d'en
    /// déduire à tort que le montant est nul.
    /// </summary>
    private static decimal? TryParseAmount(string raw)
    {
        var s = raw.Trim();
        if (s.Length == 0)
        {
            return 0m;
        }

        var normalized = s.Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
            ? amount
            : null;
    }

    private static string Format(decimal amount) =>
        amount.ToString("0.00", CultureInfo.InvariantCulture);
}
