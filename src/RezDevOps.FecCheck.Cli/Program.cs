// © 2026 Rudy Rezaire / RezDevOps. Licence MIT — voir LICENSE.

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

// Argument unique attendu à J1 : le chemin du FEC à analyser. Les options
// --output / --json arriveront au jalon J4 ; on les rejette explicitement
// pour ne pas laisser croire qu'elles sont silencieusement ignorées.
if (args[0].StartsWith('-'))
{
    Console.Error.WriteLine($"{FecCheckInfo.ProductName} : option « {args[0]} » non reconnue.");
    Console.Error.WriteLine("Utilisez --help pour afficher l'usage.");
    return 64; // EX_USAGE
}

var path = args[0];

try
{
    var report = FecValidator.Validate(path);
    PrintReport(path, report);
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
    Console.WriteLine("  fec-check <chemin-vers-fec>");
    Console.WriteLine();
    Console.WriteLine("Options :");
    Console.WriteLine("  -h, --help       Affiche cette aide.");
    Console.WriteLine("      --version    Affiche la version.");
    Console.WriteLine();
    Console.WriteLine("Codes de retour :");
    Console.WriteLine("  0   Conforme.");
    Console.WriteLine("  1   Conforme avec avertissements.");
    Console.WriteLine("  2   Non conforme.");
    Console.WriteLine("  3   Erreur d'exécution.");
    Console.WriteLine("  64  Usage incorrect.");
    Console.WriteLine();
    Console.WriteLine($"Version : {FecCheckInfo.Version} — règles couvertes : Famille A (format) + Famille B (cohérence comptable).");
    Console.WriteLine("Documentation : https://github.com/RezDevOps/fec-check");
}

static void PrintReport(string path, ValidationReport report)
{
    Console.WriteLine();
    Console.WriteLine($"{FecCheckInfo.ProductName} {FecCheckInfo.Version} — analyse de « {path} »");
    Console.WriteLine();

    Console.WriteLine("Caractéristiques du fichier :");
    Console.WriteLine($"  Encodage     : {DescribeEncoding(report.EncodageDetecte)}");
    Console.WriteLine($"  Séparateur   : {DescribeSeparator(report.SeparateurDetecte)}");
    Console.WriteLine($"  Fin de ligne : {DescribeLineEnding(report.FinDeLigneDetectee)}");
    Console.WriteLine($"  Lignes lues  : {report.LignesLues}");
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
