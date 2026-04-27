// © 2026 Rudy Rezaire / RezDevOps. Licence MIT — voir LICENSE.

using RezDevOps.FecCheck.Core;

// J0 : binaire minimal qui imprime sa version et l'usage attendu.
// La logique de validation arrive aux jalons J1 (format), J2 (cohérence comptable),
// J3 (cohérence temporelle), et l'orchestration finale au J4.

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

// À J0, l'outil ne sait encore rien valider — il refuse poliment le travail
// au lieu de mentir sur son périmètre.
Console.Error.WriteLine($"{FecCheckInfo.ProductName} {FecCheckInfo.Version} — version de cadrage (J0).");
Console.Error.WriteLine("La validation arrive au jalon J1 (cf. CHANGELOG.md). En attendant : --help.");
return 64; // EX_USAGE — usage incorrect.

static void PrintUsage()
{
    Console.WriteLine($"{FecCheckInfo.ProductName} — validateur de Fichier des Écritures Comptables (FEC).");
    Console.WriteLine();
    Console.WriteLine("Usage :");
    Console.WriteLine("  fec-check <chemin-vers-fec> [--output rapport.md] [--json rapport.json]");
    Console.WriteLine();
    Console.WriteLine("Options :");
    Console.WriteLine("  -h, --help       Affiche cette aide.");
    Console.WriteLine("      --version    Affiche la version.");
    Console.WriteLine();
    Console.WriteLine("Codes de retour (cibles, non encore implémentés) :");
    Console.WriteLine("  0   Conforme.");
    Console.WriteLine("  1   Conforme avec avertissements.");
    Console.WriteLine("  2   Non conforme.");
    Console.WriteLine("  3   Erreur d'exécution.");
    Console.WriteLine("  64  Usage incorrect (EX_USAGE).");
    Console.WriteLine();
    Console.WriteLine("Documentation : https://github.com/RezDevOps/fec-check");
}
