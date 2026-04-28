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

// Parsing des arguments : on accepte l'option `--exercice <debut>:<fin>` en
// plus du chemin positionnel obligatoire. Toute autre option déclenche EX_USAGE.
if (!TryParseArgs(args, out var path, out var exercice, out var argsError))
{
    Console.Error.WriteLine($"{FecCheckInfo.ProductName} : {argsError}");
    Console.Error.WriteLine("Utilisez --help pour afficher l'usage.");
    return 64; // EX_USAGE
}

try
{
    var report = FecValidator.Validate(path, exercice);
    PrintReport(path, report, exercice);
    return MapVerdictToExitCode(report.Verdict);
}
catch (FileNotFoundException ex)
{
    Console.Error.WriteLine($"{FecCheckInfo.ProductName} : fichier introuvable — {ex.FileName}.");
    return 3; // Erreur d'exécution.
}
catch (UnauthorizedAccessException ex)
{
    Console.Error.WriteLine($"{FecCheckInfo.ProductName} : accès refusé — {ex.Message}");
    return 3;
}
catch (IOException ex)
{
    Console.Error.WriteLine($"{FecCheckInfo.ProductName} : erreur d'I/O — {ex.Message}");
    return 3;
}

static void PrintUsage()
{
    Console.WriteLine($"{FecCheckInfo.ProductName} — validateur de Fichier des Écritures Comptables (FEC).");
    Console.WriteLine();
    Console.WriteLine("Usage :");
    Console.WriteLine("  fec-check [--exercice <debut>:<fin>] <chemin-vers-fec>");
    Console.WriteLine();
    Console.WriteLine("Options :");
    Console.WriteLine("  -h, --help                       Affiche cette aide.");
    Console.WriteLine("      --version                    Affiche la version.");
    Console.WriteLine("      --exercice <debut>:<fin>     Période d'exercice contre laquelle valider EcritureDate (règle C05).");
    Console.WriteLine("                                   Format : YYYY-MM-DD:YYYY-MM-DD (bornes incluses).");
    Console.WriteLine("                                   Ex : --exercice 2024-01-01:2024-12-31");
    Console.WriteLine();
    Console.WriteLine("Codes de retour :");
    Console.WriteLine("  0   Conforme.");
    Console.WriteLine("  1   Conforme avec avertissements.");
    Console.WriteLine("  2   Non conforme.");
    Console.WriteLine("  3   Erreur d'exécution.");
    Console.WriteLine("  64  Usage incorrect.");
    Console.WriteLine();
    Console.WriteLine($"Version : {FecCheckInfo.Version} — règles couvertes : Famille A (format) + Famille B (cohérence comptable) + Famille C (cohérence temporelle).");
    Console.WriteLine("Documentation : https://github.com/RezDevOps/fec-check");
}

static bool TryParseArgs(
    string[] args,
    out string path,
    out ExercicePeriod? exercice,
    out string error)
{
    path = string.Empty;
    exercice = null;
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
