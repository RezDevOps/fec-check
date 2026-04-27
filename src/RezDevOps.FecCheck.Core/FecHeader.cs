// © 2026 Rudy Rezaire / RezDevOps. Licence MIT — voir LICENSE.

namespace RezDevOps.FecCheck.Core;

/// <summary>
/// Source de vérité pour la liste des 18 colonnes obligatoires d'un FEC,
/// dans l'ordre exact requis par l'A. 47 A-1 LPF. Toute déviation déclenche
/// les règles <see cref="Rules.A03"/> (présence) ou <see cref="Rules.A04"/> (ordre).
/// </summary>
internal static class FecHeader
{
    /// <summary>Numéro de la ligne d'en-tête dans un FEC : par convention, la ligne 1.</summary>
    public const long HeaderLineNumber = 1;

    /// <summary>Nombre de colonnes obligatoires d'un FEC.</summary>
    public const int ExpectedColumnCount = 18;

    /// <summary>
    /// Noms des 18 colonnes attendues, dans l'ordre exact prescrit par
    /// l'A. 47 A-1 LPF. Toute modification ici doit être répercutée dans
    /// <c>docs/regles.md</c> et <c>tests/fixtures/conforme/</c>.
    /// </summary>
    public static IReadOnlyList<string> ExpectedColumns { get; } = new[]
    {
        "JournalCode",
        "JournalLib",
        "EcritureNum",
        "EcritureDate",
        "CompteNum",
        "CompteLib",
        "CompAuxNum",
        "CompAuxLib",
        "PieceRef",
        "PieceDate",
        "EcritureLib",
        "Debit",
        "Credit",
        "EcritureLet",
        "DateLet",
        "ValidDate",
        "Montantdevise",
        "Idevise",
    };

    /// <summary>
    /// Valide la ligne d'en-tête contre la liste attendue. Émet zéro, un ou
    /// deux <see cref="Finding"/> selon la nature de l'écart :
    /// <list type="bullet">
    /// <item><description><see cref="Rules.A03"/> si le nombre ou l'ensemble des noms diffère.</description></item>
    /// <item><description><see cref="Rules.A04"/> si l'ensemble est correct mais l'ordre incorrect.</description></item>
    /// </list>
    /// </summary>
    /// <param name="headerLine">Ligne d'en-tête déjà décodée, sans la fin de ligne.</param>
    /// <param name="separator">Séparateur de champs détecté dans la même ligne.</param>
    public static IReadOnlyList<Finding> Validate(string headerLine, char separator)
    {
        ArgumentNullException.ThrowIfNull(headerLine);

        var findings = new List<Finding>();
        var fields = headerLine.Split(separator);

        // 1. Bon nombre de colonnes ?
        if (fields.Length != ExpectedColumnCount)
        {
            findings.Add(new Finding(
                Rule: Rules.A03,
                LineNumber: HeaderLineNumber,
                Message: $"En-tête FEC invalide : {fields.Length} colonne(s) trouvée(s), {ExpectedColumnCount} attendues.",
                Contexte: headerLine));
            return findings;
        }

        // 2. L'ensemble des noms (peu importe l'ordre) correspond-il ?
        var expectedSet = new HashSet<string>(ExpectedColumns, StringComparer.Ordinal);
        var receivedSet = new HashSet<string>(fields, StringComparer.Ordinal);

        var missing = expectedSet.Except(receivedSet).ToList();
        var unexpected = receivedSet.Except(expectedSet).ToList();

        if (missing.Count > 0 || unexpected.Count > 0)
        {
            var details = new List<string>();
            if (missing.Count > 0)
            {
                details.Add($"manquante(s) : {string.Join(", ", missing)}");
            }

            if (unexpected.Count > 0)
            {
                details.Add($"inattendue(s) : {string.Join(", ", unexpected)}");
            }

            findings.Add(new Finding(
                Rule: Rules.A03,
                LineNumber: HeaderLineNumber,
                Message: $"Colonnes de l'en-tête non conformes — {string.Join(" ; ", details)}.",
                Contexte: headerLine));
            return findings;
        }

        // 3. Ensemble correct, ordre correct ?
        var mismatches = new List<string>();
        for (var i = 0; i < ExpectedColumnCount; i++)
        {
            if (!string.Equals(fields[i], ExpectedColumns[i], StringComparison.Ordinal))
            {
                mismatches.Add($"position {i + 1} : « {fields[i]} » au lieu de « {ExpectedColumns[i]} »");
            }
        }

        if (mismatches.Count > 0)
        {
            findings.Add(new Finding(
                Rule: Rules.A04,
                LineNumber: HeaderLineNumber,
                Message: $"Ordre des colonnes incorrect — {mismatches.Count} colonne(s) mal placée(s) : {string.Join(" ; ", mismatches)}.",
                Contexte: headerLine));
        }

        return findings;
    }
}
