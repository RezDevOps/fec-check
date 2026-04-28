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

    /// <summary>Fixture pour <see cref="Rules.B01"/> (deux écritures localement déséquilibrées, équilibre global préservé).</summary>
    public static string B01_EcritureDesequilibree => Path("non-conforme", "comptable", "B01-ecriture-desequilibree.txt");

    /// <summary>Fixture pour <see cref="Rules.B02"/> (déséquilibre global du fichier).</summary>
    public static string B02_TotalGlobalDesequilibre => Path("non-conforme", "comptable", "B02-total-global-desequilibre.txt");

    /// <summary>Fixture pour <see cref="Rules.B03"/> (montant à 5 décimales).</summary>
    public static string B03_FormatNumeriqueInvalide => Path("non-conforme", "comptable", "B03-format-numerique-invalide.txt");

    /// <summary>Fixture pour <see cref="Rules.B04"/> (Débit et Crédit non nuls sur la même ligne).</summary>
    public static string B04_DebitEtCreditNonNuls => Path("non-conforme", "comptable", "B04-debit-et-credit-non-nuls.txt");

    /// <summary>Fixture pour <see cref="Rules.B05"/> (CompAuxNum rempli, CompAuxLib vide).</summary>
    public static string B05_CompAuxNumSansLib => Path("non-conforme", "comptable", "B05-compaux-num-sans-lib.txt");

    /// <summary>Fixture pour <see cref="Rules.B06"/> (CompAuxNum attaché à un compte non-tiers).</summary>
    public static string B06_CompAuxSurCompteNonTiers => Path("non-conforme", "comptable", "B06-compaux-sur-compte-non-tiers.txt");

    /// <summary>Fixture pour <see cref="Rules.C01"/> (EcritureDate au mauvais format).</summary>
    public static string C01_EcritureDateInvalide => Path("non-conforme", "temporel", "C01-ecriture-date-invalide.txt");

    /// <summary>Fixture pour <see cref="Rules.C02"/> (PieceDate au mauvais format).</summary>
    public static string C02_PieceDateInvalide => Path("non-conforme", "temporel", "C02-piece-date-invalide.txt");

    /// <summary>Fixture pour <see cref="Rules.C03"/> (ValidDate au mauvais format).</summary>
    public static string C03_ValidDateInvalide => Path("non-conforme", "temporel", "C03-valid-date-invalide.txt");

    /// <summary>Fixture pour <see cref="Rules.C04"/> (DateLet au mauvais format).</summary>
    public static string C04_DateLetInvalide => Path("non-conforme", "temporel", "C04-date-let-invalide.txt");

    /// <summary>Fixture pour <see cref="Rules.C05"/> (EcritureDate hors période d'exercice).</summary>
    public static string C05_HorsPeriodeExercice => Path("non-conforme", "temporel", "C05-hors-periode-exercice.txt");

    /// <summary>Fixture pour <see cref="Rules.C06"/> (ValidDate antérieure à EcritureDate).</summary>
    public static string C06_ValidationAnterieureEcriture => Path("non-conforme", "temporel", "C06-validation-anterieure-ecriture.txt");

    /// <summary>Fixture pour <see cref="Rules.C07"/> (chronologie cassée dans un journal).</summary>
    public static string C07_ChronologieCassee => Path("non-conforme", "temporel", "C07-chronologie-cassee.txt");

    /// <summary>Fixture pour <see cref="Rules.C08"/> (deux écritures sans ValidDate).</summary>
    public static string C08_EcrituresNonValidees => Path("non-conforme", "temporel", "C08-ecritures-non-validees.txt");

    private static string Path(params string[] parts) =>
        System.IO.Path.Combine(
            new[] { AppContext.BaseDirectory, "fixtures" }
                .Concat(parts)
                .ToArray());
}
