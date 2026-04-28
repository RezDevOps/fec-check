// © 2026 Rudy Rezaire / RezDevOps. Licence MIT — voir LICENSE.

namespace RezDevOps.FecCheck.Core;

/// <summary>
/// Point d'entrée public de la bibliothèque. Orchestre la validation d'un FEC
/// en streaming : détection d'encodage, lecture ligne par ligne, application
/// des règles des Familles A (J1), B (J2) et C (J3). L'option <c>--exercice</c>
/// est portée jusqu'ici via <see cref="ExercicePeriod"/>.
/// </summary>
/// <remarks>
/// Cette classe est sans état : elle expose des méthodes statiques. Aucun I/O
/// n'est fait par la bibliothèque hors de la lecture du flux d'entrée — la
/// gestion de fichiers, des arguments CLI et des sorties est laissée à la
/// couche <c>RezDevOps.FecCheck.Cli</c> (cf. cadrage §6.2).
/// </remarks>
public static class FecValidator
{
    /// <summary>
    /// Valide un FEC à partir de son chemin sur disque, sans contrainte
    /// d'exercice (la règle <see cref="Rules.C05"/> n'est pas évaluée).
    /// Surcharge de compatibilité avec l'API J1/J2.
    /// </summary>
    /// <param name="filePath">Chemin absolu ou relatif vers le FEC à analyser.</param>
    /// <exception cref="FileNotFoundException">Si le fichier n'existe pas.</exception>
    /// <exception cref="UnauthorizedAccessException">Si le fichier n'est pas lisible.</exception>
    public static ValidationReport Validate(string filePath) =>
        Validate(filePath, exercice: null);

    /// <summary>
    /// Valide un FEC à partir de son chemin sur disque. Ouvre le fichier en
    /// lecture partagée et délègue à <see cref="Validate(Stream, ExercicePeriod?)"/>.
    /// </summary>
    /// <param name="filePath">Chemin absolu ou relatif vers le FEC à analyser.</param>
    /// <param name="exercice">Période d'exercice pour la règle C05, ou <c>null</c> pour ne pas l'évaluer.</param>
    /// <exception cref="FileNotFoundException">Si le fichier n'existe pas.</exception>
    /// <exception cref="UnauthorizedAccessException">Si le fichier n'est pas lisible.</exception>
    public static ValidationReport Validate(string filePath, ExercicePeriod? exercice)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("FEC introuvable.", filePath);
        }

        using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            FileOptions.SequentialScan);

        return Validate(stream, exercice);
    }

    /// <summary>
    /// Valide un FEC à partir d'un flux, sans contrainte d'exercice (la règle
    /// <see cref="Rules.C05"/> n'est pas évaluée). Surcharge de compatibilité
    /// avec l'API J1/J2.
    /// </summary>
    /// <param name="input">Flux du FEC, seekable.</param>
    public static ValidationReport Validate(Stream input) =>
        Validate(input, exercice: null);

    /// <summary>
    /// Valide un FEC à partir d'un flux ouvert en lecture, seekable. Le flux
    /// n'est pas disposé par la méthode — la responsabilité reste à l'appelant.
    /// </summary>
    /// <param name="input">Flux du FEC, seekable.</param>
    /// <param name="exercice">Période d'exercice pour la règle C05, ou <c>null</c> pour ne pas l'évaluer.</param>
    public static ValidationReport Validate(Stream input, ExercicePeriod? exercice)
    {
        ArgumentNullException.ThrowIfNull(input);

        var findings = new List<Finding>();

        // -- A01 — détection d'encodage --------------------------------------
        var (detectedEncoding, encoding, _) = EncodingDetector.Detect(input);

        if (detectedEncoding == DetectedEncoding.Inconnu || encoding is null)
        {
            findings.Add(new Finding(
                Rule: Rules.A01,
                LineNumber: null,
                Message:
                    "Encodage du fichier non reconnu. Les encodages autorisés sont "
                    + "ASCII, UTF-8 (BOM toléré) et ISO-8859-15.",
                Contexte: null));

            return new ValidationReport(
                Verdict: Verdict.NonConforme,
                Findings: findings,
                EncodageDetecte: DetectedEncoding.Inconnu,
                SeparateurDetecte: null,
                FinDeLigneDetectee: DetectedLineEnding.Aucune,
                LignesLues: 0);
        }

        // -- Lecture ligne par ligne en streaming ----------------------------
        using var reader = new FecLineReader(input, encoding, leaveOpen: true);

        if (!reader.TryReadLine(out var headerLine))
        {
            findings.Add(new Finding(
                Rule: Rules.A03,
                LineNumber: null,
                Message: "Fichier vide : aucune ligne d'en-tête trouvée.",
                Contexte: null));

            return new ValidationReport(
                Verdict: Verdict.NonConforme,
                Findings: findings,
                EncodageDetecte: detectedEncoding,
                SeparateurDetecte: null,
                FinDeLigneDetectee: reader.GetDetectedLineEnding(),
                LignesLues: 0);
        }

        // -- A02 (en-tête) — détection du séparateur -------------------------
        var separator = SeparatorDetector.Detect(headerLine);
        if (separator is null)
        {
            findings.Add(new Finding(
                Rule: Rules.A02,
                LineNumber: FecHeader.HeaderLineNumber,
                Message:
                    "Aucun séparateur de champs reconnu dans l'en-tête. "
                    + "Attendu : tabulation \\t ou pipe |, en usage exclusif.",
                Contexte: headerLine));

            return new ValidationReport(
                Verdict: Verdict.NonConforme,
                Findings: findings,
                EncodageDetecte: detectedEncoding,
                SeparateurDetecte: null,
                FinDeLigneDetectee: reader.GetDetectedLineEnding(),
                LignesLues: reader.LineNumber);
        }

        // -- A03 / A04 — validation de l'en-tête -----------------------------
        var headerFindings = FecHeader.Validate(headerLine, separator.Value);
        findings.AddRange(headerFindings);

        // Si l'en-tête est cassée au point que la sévérité Bloquante est
        // atteinte, on ne tente pas l'analyse des lignes de données : on ne
        // saurait pas comment les interpréter.
        if (headerFindings.Any(f => f.Rule.Severity == Severity.Bloquante))
        {
            // On consomme tout de même le reste du flux pour finaliser A06.
            DrainRemainingLines(reader);

            return new ValidationReport(
                Verdict: ValidationReport.ComputeVerdict(findings),
                Findings: findings,
                EncodageDetecte: detectedEncoding,
                SeparateurDetecte: separator,
                FinDeLigneDetectee: reader.GetDetectedLineEnding(),
                LignesLues: reader.LineNumber);
        }

        // -- A02 (lignes de données) + A05 + A07 + Famille B -----------------
        var alternativeSeparator = separator.Value == SeparatorDetector.Tabulation
            ? SeparatorDetector.Pipe
            : SeparatorDetector.Tabulation;

        var accounting = new AccountingContext();
        var temporal = new TemporalContext(exercice);

        while (reader.TryReadLine(out var line))
        {
            // A02 : la ligne semble utiliser le séparateur opposé.
            if (LooksLikeWrongSeparator(line, separator.Value, alternativeSeparator))
            {
                findings.Add(new Finding(
                    Rule: Rules.A02,
                    LineNumber: reader.LineNumber,
                    Message:
                        $"Ligne {reader.LineNumber} : séparateur incohérent avec l'en-tête "
                        + $"(« {Describe(separator.Value)} » attendu, « {Describe(alternativeSeparator)} » trouvé).",
                    Contexte: line));

                // On n'évalue pas A05/A07/Famille B/C sur une ligne au mauvais
                // séparateur : les findings seraient redondants et trompeurs.
                continue;
            }

            // Le split est fait une seule fois et partagé entre les trois
            // évaluateurs (par-ligne, comptable inter-lignes, temporel
            // inter-lignes), conformément à la contrainte de perf §6.3 du cadrage.
            var fields = line.Split(separator.Value);

            DataLineValidator.Validate(fields, line, reader.LineNumber, findings);
            accounting.Observe(fields, reader.LineNumber, findings);
            temporal.Observe(fields, reader.LineNumber, findings);
        }

        // -- Famille B agrégée — B01 (équilibre par écriture) + B02 (global) -
        accounting.EmitFinalFindings(findings);

        // -- Famille C agrégée — C05 (hors exercice) + C07 (chronologie) + C08 (non validées) --
        temporal.EmitFinalFindings(findings);

        // -- A06 — cohérence des fins de ligne -------------------------------
        var detectedEol = reader.GetDetectedLineEnding();
        if (detectedEol == DetectedLineEnding.Mixte)
        {
            findings.Add(new Finding(
                Rule: Rules.A06,
                LineNumber: null,
                Message:
                    "Le fichier mélange plusieurs conventions de fin de ligne (CRLF, LF, CR). "
                    + "Une convention unique est attendue sur l'ensemble du fichier.",
                Contexte: null));
        }

        return new ValidationReport(
            Verdict: ValidationReport.ComputeVerdict(findings),
            Findings: findings,
            EncodageDetecte: detectedEncoding,
            SeparateurDetecte: separator,
            FinDeLigneDetectee: detectedEol,
            LignesLues: reader.LineNumber);
    }

    private static bool LooksLikeWrongSeparator(string line, char expected, char alternative)
    {
        // Heuristique : la ligne ne contient aucun séparateur attendu et au moins
        // (ExpectedColumnCount - 1) occurrences de l'alternative — signal fort
        // d'une ligne au mauvais séparateur, distinct d'une simple troncation.
        var expectedCount = 0;
        var altCount = 0;
        foreach (var c in line)
        {
            if (c == expected)
            {
                expectedCount++;
            }
            else if (c == alternative)
            {
                altCount++;
            }
        }

        return expectedCount == 0 && altCount >= FecHeader.ExpectedColumnCount - 1;
    }

    private static void DrainRemainingLines(FecLineReader reader)
    {
        while (reader.TryReadLine(out _))
        {
            // Lecture seule pour finaliser les statistiques de fin de ligne (A06).
        }
    }

    private static string Describe(char separator) => separator switch
    {
        '\t' => "tabulation",
        '|' => "pipe |",
        _ => $"« {separator} »",
    };
}
