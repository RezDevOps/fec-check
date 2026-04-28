// © 2026 Rudy Rezaire / RezDevOps. Licence MIT — voir LICENSE.

using System.Globalization;
using System.Text;
using RezDevOps.FecCheck.Core;

// La sortie console doit afficher correctement les accents français sur tous
// les OS, y compris Windows où la code page par défaut peut être autre que UTF-8.
Console.OutputEncoding = Encoding.UTF8;

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    PrintUsage();
    return 0;
}

if (args[0] is "--version")
{
    Console.WriteLine($"{FecCheckInfo.ProductName} {FecCheckInfo.Version}");
    return 0;
}

// Parsing des arguments : chemin positionnel obligatoire, options
// --exercice / --output-md / --output-json acceptées, ordre libre.
if (!TryParseArgs(args, out var path, out var exercice, out var outputMd, out var outputJson, out var argsError))
{
    Console.Error.WriteLine($"{FecCheckInfo.ProductName} : {argsError}");
    Console.Error.WriteLine("Utilisez --help pour afficher l'usage.");
    return 64; // EX_USAGE
}

ValidationReport report;
try
{
    report = FecValidator.Validate(path, exercice);
}
catch (FileNotFoundException ex)
{
    Console.Error.WriteLine($"{FecCheckInfo.ProductName} : fichier introuvable — {ex.FileName}.");
    return 3;
}
catch (UnauthorizedAccessException ex)
{
    Console.Error.WriteLine($"{FecCheckInfo.ProductName} : accès refusé — {ex.Message}");
    return 3;
}
catch (IOException ex)
{
    Console.Error.WriteLine($"{FecCheckInfo.ProductName} : erreur d'I/O en lecture — {ex.Message}");
    return 3;
}

var environment = new ReportEnvironment(
    ProductName: FecCheckInfo.ProductName,
    ProductVersion: FecCheckInfo.Version,
    FilePath: path,
    GeneratedAt: DateTimeOffset.UtcNow,
    Exercice: exercice);

PrintReport(path, report, exercice);

// Les écritures fichier viennent après l'affichage console : si elles
// échouent, le verdict reste visible à l'utilisateur. On ne masque pas le
// verdict par un code de retour 3 : on affiche un message d'erreur sur stderr
// et on garde le code de retour fonctionnel (0/1/2).
if (outputMd is not null && !TryWriteReport(outputMd, "Markdown",
        path => ReportFileWriter.WriteMarkdown(path, report, environment)))
{
    return 3;
}

if (outputJson is not null && !TryWriteReport(outputJson, "JSON",
        path => ReportFileWriter.WriteJson(path, report, environment)))
{
    return 3;
}

return MapVerdictToExitCode(report.Verdict);

// ----------------------------------------------------------------------------
// Helpers locaux
// ----------------------------------------------------------------------------

static void PrintUsage()
{
    Console.WriteLine($"{FecCheckInfo.ProductName} — validateur de Fichier des Écritures Comptables (FEC).");
    Console.WriteLine();
    Console.WriteLine("Usage :");
    Console.WriteLine("  fec-check [OPTIONS] <chemin-vers-fec>");
    Console.WriteLine();
    Console.WriteLine("Options :");
    Console.WriteLine("  -h, --help                       Affiche cette aide.");
    Console.WriteLine("      --version                    Affiche la version.");
    Console.WriteLine("      --exercice <debut>:<fin>     Période d'exercice contre laquelle valider EcritureDate (règle C05).");
    Console.WriteLine("                                   Format : YYYY-MM-DD:YYYY-MM-DD (bornes incluses).");
    Console.WriteLine("                                   Ex : --exercice 2024-01-01:2024-12-31");
    Console.WriteLine("      --output-md <chemin>         Écrit le rapport Markdown finalisé (UTF-8 sans BOM, fin de ligne LF).");
    Console.WriteLine("      --output-json <chemin>       Écrit le rapport JSON v1 (schéma figé, schemaVersion=1).");
    Console.WriteLine("                                   Les deux flags sont indépendants et combinables.");
    Console.WriteLine();
    Console.WriteLine("Codes de retour :");
    Console.WriteLine("  0   Conforme.");
    Console.WriteLine("  1   Conforme avec avertissements.");
    Console.WriteLine("  2   Non conforme.");
    Console.WriteLine("  3   Erreur d'exécution (lecture du FEC ou écriture d'un rapport).");
    Console.WriteLine("  64  Usage incorrect.");
    Console.WriteLine();
    Console.WriteLine($"Version : {FecCheckInfo.Version} — règles couvertes : Famille A (format) + Famille B (cohérence comptable) + Famille C (cohérence temporelle).");
    Console.WriteLine("Documentation : https://github.com/RezDevOps/fec-check");
}

static bool TryParseArgs(
    string[] args,
    out string path,
    out ExercicePeriod? exercice,
    out string? outputMd,
    out string? outputJson,
    out string error)
{
    path = string.Empty;
    exercice = null;
    outputMd = null;
    outputJson = null;
    error = string.Empty;
    string? pathFound = null;

    for (var i = 0; i < args.Length; i++)
    {
        var a = args[i];
        if (a is "--exercice")
        {
            if (i + 1 >= args.Length)
            {
                error = "option --exercice : argument manquant (format attendu : YYYY-MM-DD:YYYY-MM-DD).";
                return false;
            }

            i++;
            if (!TryParseExercice(args[i], out exercice, out var exerciceError))
            {
                error = $"option --exercice : {exerciceError}";
                return false;
            }
        }
        else if (a is "--output-md")
        {
            if (i + 1 >= args.Length)
            {
                error = "option --output-md : argument manquant (chemin du fichier de sortie).";
                return false;
            }

            i++;
            if (string.IsNullOrWhiteSpace(args[i]))
            {
                error = "option --output-md : chemin vide.";
                return false;
            }

            outputMd = args[i];
        }
        else if (a is "--output-json")
        {
            if (i + 1 >= args.Length)
            {
                error = "option --output-json : argument manquant (chemin du fichier de sortie).";
                return false;
            }

            i++;
            if (string.IsNullOrWhiteSpace(args[i]))
            {
                error = "option --output-json : chemin vide.";
                return false;
            }

            outputJson = args[i];
        }
        else if (a.Length > 0 && a[0] == '-')
        {
            error = $"option « {a} » non reconnue.";
            return false;
        }
        else
        {
            if (pathFound is not null)
            {
                error = "un seul chemin de fichier est accepté.";
                return false;
            }

            pathFound = a;
        }
    }

    if (pathFound is null)
    {
        error = "chemin du FEC manquant.";
        return false;
    }

    path = pathFound;
    return true;
}

static bool TryParseExercice(string raw, out ExercicePeriod? exercice, out string error)
{
    exercice = null;
    error = string.Empty;

    var parts = raw.Split(':');
    if (parts.Length != 2)
    {
        error = $"format invalide « {raw} », attendu : YYYY-MM-DD:YYYY-MM-DD.";
        return false;
    }

    if (!DateOnly.TryParseExact(parts[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var debut))
    {
        error = $"date de début invalide « {parts[0]} », format attendu YYYY-MM-DD.";
        return false;
    }

    if (!DateOnly.TryParseExact(parts[1], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fin))
    {
        error = $"date de fin invalide « {parts[1]} », format attendu YYYY-MM-DD.";
        return false;
    }

    if (debut > fin)
    {
        error = $"la date de début ({parts[0]}) est postérieure à la date de fin ({parts[1]}).";
        return false;
    }

    exercice = ExercicePeriod.Create(debut, fin);
    return true;
}

static bool TryWriteReport(string path, string label, Action<string> writeAction)
{
    try
    {
        writeAction(path);
        Console.WriteLine($"Rapport {label} écrit : {path}");
        return true;
    }
    catch (UnauthorizedAccessException ex)
    {
        Console.Error.WriteLine($"{FecCheckInfo.ProductName} : impossible d'écrire le rapport {label} — accès refusé sur « {path} » ({ex.Message}).");
        return false;
    }
    catch (DirectoryNotFoundException ex)
    {
        Console.Error.WriteLine($"{FecCheckInfo.ProductName} : impossible d'écrire le rapport {label} — répertoire introuvable pour « {path} » ({ex.Message}).");
        return false;
    }
    catch (IOException ex)
    {
        Console.Error.WriteLine($"{FecCheckInfo.ProductName} : impossible d'écrire le rapport {label} — erreur d'I/O sur « {path} » ({ex.Message}).");
        return false;
    }
}

static void PrintReport(string path, ValidationReport report, ExercicePeriod? exercice)
{
    Console.WriteLine();
    Console.WriteLine($"{FecCheckInfo.ProductName} {FecCheckInfo.Version} — analyse de « {path} »");
    Console.WriteLine();

    Console.WriteLine("Caractéristiques du fichier :");
    Console.WriteLine($"  Encodage     : {DescribeEncoding(report.EncodageDetecte)}");
    Console.WriteLine($"  Séparateur   : {DescribeSeparator(report.SeparateurDetecte)}");
    Console.WriteLine($"  Fin de ligne : {DescribeLineEnding(report.FinDeLigneDetectee)}");
    Console.WriteLine($"  Lignes lues  : {report.LignesLues}");
    if (exercice is not null)
    {
        Console.WriteLine(
            $"  Exercice     : du {exercice.Debut:yyyy-MM-dd} au {exercice.Fin:yyyy-MM-dd}");
    }
    else
    {
        Console.WriteLine("  Exercice     : non précisé (règle C05 non évaluée — utilisez --exercice <debut>:<fin> pour l'activer).");
    }

    Console.WriteLine();
    Console.WriteLine($"Verdict : {DescribeVerdict(report.Verdict)}");

    if (report.Findings.Count == 0)
    {
        Console.WriteLine("Aucune anomalie détectée.");
        return;
    }

    // Synthèse par famille — ajoutée en J4 pour donner un coup d'œil
    // synthétique avant la liste détaillée. Reste sobre, ne duplique pas
    // l'information du rapport Markdown.
    var (format, comptable, temporel) = CountByFamily(report.Findings);
    var (avert, erreur, bloquante) = CountBySeverity(report.Findings);
    Console.WriteLine();
    Console.WriteLine($"Synthèse : {report.Findings.Count} anomalie{(report.Findings.Count > 1 ? "s" : string.Empty)} ({DescribeSeverityCounts(avert, erreur, bloquante)}).");
    if (format > 0)
    {
        Console.WriteLine($"  Famille A (format)        : {format}");
    }

    if (comptable > 0)
    {
        Console.WriteLine($"  Famille B (comptabilité)  : {comptable}");
    }

    if (temporel > 0)
    {
        Console.WriteLine($"  Famille C (temporel)      : {temporel}");
    }

    Console.WriteLine();
    Console.WriteLine($"Anomalies détectées ({report.Findings.Count}) :");
    foreach (var f in report.Findings)
    {
        var location = f.LineNumber is { } n ? $"ligne {n}" : "fichier";
        Console.WriteLine($"  - {f.Rule.Id} [{f.Rule.Severity}] {location} : {f.Message}");
        Console.WriteLine($"        Source : {f.Rule.Source}");
    }

    Console.WriteLine();
    Console.WriteLine("Liste complète des règles : docs/regles.md du repo fec-check.");
}

static int MapVerdictToExitCode(Verdict v) => v switch
{
    Verdict.Conforme => 0,
    Verdict.ConformeAvecAvertissements => 1,
    Verdict.NonConforme => 2,
    _ => 2,
};

static string DescribeEncoding(DetectedEncoding e) => e switch
{
    DetectedEncoding.Utf8 => "UTF-8",
    DetectedEncoding.Utf8WithBom => "UTF-8 (avec BOM)",
    DetectedEncoding.Iso8859_15 => "ISO-8859-15",
    DetectedEncoding.Inconnu => "non reconnu",
    _ => e.ToString(),
};

static string DescribeSeparator(char? c) => c switch
{
    '\t' => "tabulation",
    '|' => "pipe |",
    null => "non détecté",
    _ => $"« {c} »",
};

static string DescribeLineEnding(DetectedLineEnding e) => e switch
{
    DetectedLineEnding.Crlf => "CRLF (Windows)",
    DetectedLineEnding.Lf => "LF (Unix)",
    DetectedLineEnding.Mixte => "mixte (incohérente)",
    DetectedLineEnding.Aucune => "aucune",
    _ => e.ToString(),
};

static string DescribeVerdict(Verdict v) => v switch
{
    Verdict.Conforme => "CONFORME",
    Verdict.ConformeAvecAvertissements => "CONFORME AVEC AVERTISSEMENTS",
    Verdict.NonConforme => "NON CONFORME",
    _ => v.ToString(),
};

static (int format, int comptable, int temporel) CountByFamily(IReadOnlyList<Finding> findings)
{
    var format = 0;
    var comptable = 0;
    var temporel = 0;
    foreach (var f in findings)
    {
        switch (f.Rule.Famille)
        {
            case FecCheckInfo.RuleFamily.Format: format++; break;
            case FecCheckInfo.RuleFamily.Accounting: comptable++; break;
            case FecCheckInfo.RuleFamily.Temporal: temporel++; break;
        }
    }

    return (format, comptable, temporel);
}

static (int avertissement, int erreur, int bloquante) CountBySeverity(IReadOnlyList<Finding> findings)
{
    var avert = 0;
    var erreur = 0;
    var bloquante = 0;
    foreach (var f in findings)
    {
        switch (f.Rule.Severity)
        {
            case Severity.Avertissement: avert++; break;
            case Severity.Erreur: erreur++; break;
            case Severity.Bloquante: bloquante++; break;
        }
    }

    return (avert, erreur, bloquante);
}

static string DescribeSeverityCounts(int avert, int erreur, int bloquante)
{
    var fragments = new List<string>(3);
    if (bloquante > 0)
    {
        fragments.Add($"{bloquante} bloquante{(bloquante > 1 ? "s" : string.Empty)}");
    }

    if (erreur > 0)
    {
        fragments.Add($"{erreur} erreur{(erreur > 1 ? "s" : string.Empty)}");
    }

    if (avert > 0)
    {
        fragments.Add($"{avert} avertissement{(avert > 1 ? "s" : string.Empty)}");
    }

    return string.Join(", ", fragments);
}
