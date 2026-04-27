// © 2026 Rudy Rezaire / RezDevOps. Licence MIT — voir LICENSE.

namespace RezDevOps.FecCheck.Core.Tests;

/// <summary>
/// Pointe vers les fixtures FEC copiées dans le dossier de sortie de tests
/// par le csproj (cf. <c>RezDevOps.FecCheck.Core.Tests.csproj</c>, ItemGroup
/// « Fixtures FEC »). Permet aux tests de s'exécuter quel que soit le
/// répertoire de travail courant (CI, IDE, dotnet test, etc.).
/// </summary>
internal static class TestFixtures
{
    /// <summary>FEC minimal conforme — référence pour les tests positifs.</summary>
    public static string Conforme => Path("conforme", "fec-minimal-conforme.txt");

    /// <summary>Fixture pathologique pour la règle <see cref="Rules.A01"/> (encodage UTF-16).</summary>
    public static string A01_EncodageUtf16 => Path("non-conforme", "format", "A01-encodage-utf16.txt");

    /// <summary>Fixture pour <see cref="Rules.A02"/> (mélange tabulation/pipe).</summary>
    public static string A02_SeparateurMixte => Path("non-conforme", "format", "A02-separateur-mixte.txt");

    /// <summary>Fixture pour <see cref="Rules.A03"/> (en-tête à 17 colonnes).</summary>
    public static string A03_EnteteColonnesManquantes => Path("non-conforme", "format", "A03-entete-colonnes-manquantes.txt");

    /// <summary>Fixture pour <see cref="Rules.A04"/> (Debit/Credit permutés en en-tête).</summary>
    public static string A04_EnteteOrdreFaux => Path("non-conforme", "format", "A04-entete-ordre-faux.txt");

    /// <summary>Fixture pour <see cref="Rules.A05"/> (ligne de données tronquée).</summary>
    public static string A05_LigneTronquee => Path("non-conforme", "format", "A05-ligne-tronquee.txt");

    /// <summary>Fixture pour <see cref="Rules.A06"/> (mélange CRLF/LF).</summary>
    public static string A06_EolMixte => Path("non-conforme", "format", "A06-eol-mixte.txt");

    /// <summary>Fixture pour <see cref="Rules.A07"/> (champ JournalCode vide).</summary>
    public static string A07_ChampObligatoireVide => Path("non-conforme", "format", "A07-champ-obligatoire-vide.txt");

    private static string Path(params string[] parts) =>
        System.IO.Path.Combine(
            new[] { AppContext.BaseDirectory, "fixtures" }
                .Concat(parts)
                .ToArray());
}
