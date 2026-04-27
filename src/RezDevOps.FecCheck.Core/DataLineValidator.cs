// © 2026 Rudy Rezaire / RezDevOps. Licence MIT — voir LICENSE.

using System.Globalization;

namespace RezDevOps.FecCheck.Core;

/// <summary>
/// Valide les règles de format applicables à chaque ligne de données d'un FEC :
/// <see cref="Rules.A05"/> (exactement 18 champs) et <see cref="Rules.A07"/>
/// (champs obligatoires non vides, et au moins un montant Débit ou Crédit non nul).
/// </summary>
/// <remarks>
/// Le validateur reçoit un <see cref="List{Finding}"/> en accumulateur pour
/// éviter d'allouer une liste par ligne de FEC : un fichier client peut faire
/// des centaines de milliers de lignes, et la perf cible §6.3 du cadrage
/// vise &lt; 10 s sur 100 Mo.
///
/// Note J1/J2 : la vérification numérique stricte (deux décimales, pas de
/// séparateur de milliers, séparateur décimal cohérent) est de la responsabilité
/// de la règle <see cref="Rule.Source"/> B03 et arrivera au jalon J2. À J1 on se
/// contente d'un parsing tolérant pour décider de la nullité du montant.
/// </remarks>
internal static class DataLineValidator
{
    // Indices des champs obligatoires dans la ligne (parallèle à FecHeader.ExpectedColumns).
    private const int IdxJournalCode = 0;
    private const int IdxEcritureNum = 2;
    private const int IdxEcritureDate = 3;
    private const int IdxCompteNum = 4;
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
    /// Valide une ligne de données et ajoute les <see cref="Finding"/> détectés
    /// dans <paramref name="sink"/>. La méthode ne court-circuite pas : si A05
    /// est violée (mauvais nombre de champs), A07 est tout de même évaluée sur
    /// les champs effectivement présents — un FEC tronqué reste informatif.
    /// </summary>
    public static void Validate(string line, char separator, long lineNumber, List<Finding> sink)
    {
        ArgumentNullException.ThrowIfNull(line);
        ArgumentNullException.ThrowIfNull(sink);

        var fields = line.Split(separator);

        // A05 — exactement 18 champs.
        if (fields.Length != FecHeader.ExpectedColumnCount)
        {
            sink.Add(new Finding(
                Rule: Rules.A05,
                LineNumber: lineNumber,
                Message:
                    $"Ligne {lineNumber} : {fields.Length} champ(s) trouvé(s), "
                    + $"{FecHeader.ExpectedColumnCount} attendus.",
                Contexte: line));
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
                    Contexte: line));
            }
        }

        // A07 (suite) — au moins un de Debit ou Credit non nul.
        var debit = IdxDebit < fields.Length ? fields[IdxDebit] : string.Empty;
        var credit = IdxCredit < fields.Length ? fields[IdxCredit] : string.Empty;

        if (!IsNonZeroAmount(debit) && !IsNonZeroAmount(credit))
        {
            sink.Add(new Finding(
                Rule: Rules.A07,
                LineNumber: lineNumber,
                Message:
                    $"Ligne {lineNumber} : ni Debit ni Credit n'a de montant non nul "
                    + "(au moins un des deux est requis).",
                Contexte: line));
        }
    }

    /// <summary>
    /// Détermine si un champ représente un montant non nul, avec une tolérance
    /// pragmatique sur le format à J1 : le séparateur décimal peut être <c>,</c>
    /// ou <c>.</c>, et un champ non parsable est considéré comme « non nul »
    /// (la validation stricte du format numérique relève de B03 au jalon J2).
    /// </summary>
    private static bool IsNonZeroAmount(string field)
    {
        var s = field.Trim();
        if (s.Length == 0)
        {
            return false;
        }

        // Normalisation séparateur décimal pour parsing invariant.
        var normalized = s.Replace(',', '.');
        if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
        {
            return amount != 0m;
        }

        // Champ non vide et non parsable : on laisse B03 (J2) qualifier le format.
        // À J1 on considère que l'utilisateur a voulu mettre un montant, donc « non nul ».
        return true;
    }
}
