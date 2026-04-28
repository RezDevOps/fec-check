// © 2026 Rudy Rezaire / RezDevOps. Licence MIT — voir LICENSE.

using static RezDevOps.FecCheck.Core.FecCheckInfo;

namespace RezDevOps.FecCheck.Core;

/// <summary>
/// Catalogue exhaustif des règles de validation FEC. Source de vérité unique
/// côté code, miroir du tableau de <c>docs/regles.md</c>. Toute règle
/// implémentée doit y être enregistrée pour être visible dans le rapport.
/// </summary>
/// <remarks>
/// Familles couvertes par jalon :
/// <list type="bullet">
/// <item><description>J1 (v0.1.0) : Famille A — A01 à A07.</description></item>
/// <item><description>J2 (v0.2.0) : Famille B — B01 à B06.</description></item>
/// <item><description>J3 (v0.3.0) : Famille C — C01 à C08 (ajoutées plus tard).</description></item>
/// </list>
/// </remarks>
public static class Rules
{
    // --- Famille A — Conformité de format (J1) -----------------------------

    /// <summary>A01 — Encodage du fichier dans l'ensemble {ASCII, ISO-8859-15, UTF-8} (BOM toléré).</summary>
    public static readonly Rule A01 = new(
        Id: "A01",
        Famille: RuleFamily.Format,
        Severity: Severity.Bloquante,
        Libelle: "Encodage du fichier dans {ASCII, ISO-8859-15, UTF-8} (BOM toléré).",
        Source: "A. 47 A-1 LPF, BOI-CF-IOR-60-40-20");

    /// <summary>A02 — Séparateur de champs (tabulation ou pipe), cohérent dans tout le fichier.</summary>
    public static readonly Rule A02 = new(
        Id: "A02",
        Famille: RuleFamily.Format,
        Severity: Severity.Bloquante,
        Libelle: "Séparateur de champs : tabulation \\t ou pipe |, cohérent dans tout le fichier.",
        Source: "A. 47 A-1 LPF, BOI-CF-IOR-60-40-20");

    /// <summary>A03 — Présence de l'en-tête (1<sup>re</sup> ligne) avec les 18 noms de colonnes attendus.</summary>
    public static readonly Rule A03 = new(
        Id: "A03",
        Famille: RuleFamily.Format,
        Severity: Severity.Bloquante,
        Libelle: "Présence de l'en-tête avec les 18 noms de colonnes attendus.",
        Source: "A. 47 A-1 LPF");

    /// <summary>A04 — Ordre exact des 18 colonnes dans l'en-tête.</summary>
    public static readonly Rule A04 = new(
        Id: "A04",
        Famille: RuleFamily.Format,
        Severity: Severity.Bloquante,
        Libelle: "Ordre exact des 18 colonnes dans l'en-tête.",
        Source: "A. 47 A-1 LPF");

    /// <summary>A05 — Toute ligne de données contient exactement 18 champs.</summary>
    public static readonly Rule A05 = new(
        Id: "A05",
        Famille: RuleFamily.Format,
        Severity: Severity.Erreur,
        Libelle: "Toute ligne de données contient exactement 18 champs (pas tronquée, pas surnuméraire).",
        Source: "A. 47 A-1 LPF");

    /// <summary>A06 — Fin de ligne CRLF ou LF, cohérente dans tout le fichier.</summary>
    public static readonly Rule A06 = new(
        Id: "A06",
        Famille: RuleFamily.Format,
        Severity: Severity.Avertissement,
        Libelle: "Fin de ligne CRLF ou LF, cohérente dans tout le fichier.",
        Source: "BOI-CF-IOR-60-40-20");

    /// <summary>A07 — Champs obligatoires non vides (JournalCode, EcritureNum, EcritureDate, CompteNum, EcritureLib + Debit ou Credit).</summary>
    public static readonly Rule A07 = new(
        Id: "A07",
        Famille: RuleFamily.Format,
        Severity: Severity.Erreur,
        Libelle: "Champs obligatoires non vides : JournalCode, EcritureNum, EcritureDate, CompteNum, EcritureLib, et soit Debit soit Credit non nul.",
        Source: "A. 47 A-1 LPF");

    // --- Famille B — Cohérence comptable (J2) ------------------------------

    /// <summary>B01 — Pour chaque couple (JournalCode, EcritureNum), somme Debit = somme Credit.</summary>
    public static readonly Rule B01 = new(
        Id: "B01",
        Famille: RuleFamily.Accounting,
        Severity: Severity.Erreur,
        Libelle: "Pour chaque couple (JournalCode, EcritureNum), somme Debit = somme Credit.",
        Source: "Principe de la partie double, A. 47 A-1 LPF");

    /// <summary>B02 — Somme globale Debit du fichier = somme globale Credit.</summary>
    public static readonly Rule B02 = new(
        Id: "B02",
        Famille: RuleFamily.Accounting,
        Severity: Severity.Erreur,
        Libelle: "Somme globale Debit du fichier = somme globale Credit.",
        Source: "Principe de la partie double");

    /// <summary>B03 — Format numérique des montants : séparateur décimal cohérent, pas de séparateur de milliers, 0 à 4 décimales tolérées.</summary>
    public static readonly Rule B03 = new(
        Id: "B03",
        Famille: RuleFamily.Accounting,
        Severity: Severity.Erreur,
        Libelle: "Format numérique des montants : séparateur décimal cohérent (, ou .) sur tout le fichier, pas de séparateur de milliers, 0 à 4 décimales tolérées.",
        Source: "A. 47 A-1 LPF");

    /// <summary>B04 — Mutuelle exclusion Debit/Credit sur une même ligne (l'un des deux est zéro).</summary>
    public static readonly Rule B04 = new(
        Id: "B04",
        Famille: RuleFamily.Accounting,
        Severity: Severity.Avertissement,
        Libelle: "Mutuelle exclusion Debit/Credit sur une même ligne (l'un des deux est zéro), sauf cas explicitement documenté.",
        Source: "Pratique comptable standard");

    /// <summary>B05 — Si CompAuxNum est rempli, alors CompAuxLib doit l'être aussi (et inversement).</summary>
    public static readonly Rule B05 = new(
        Id: "B05",
        Famille: RuleFamily.Accounting,
        Severity: Severity.Erreur,
        Libelle: "Si CompAuxNum est rempli, alors CompAuxLib doit l'être aussi (et inversement).",
        Source: "A. 47 A-1 LPF");

    /// <summary>B06 — Si CompAuxNum est rempli, alors CompteNum commence par '4' (compte de tiers).</summary>
    public static readonly Rule B06 = new(
        Id: "B06",
        Famille: RuleFamily.Accounting,
        Severity: Severity.Avertissement,
        Libelle: "Si CompAuxNum est rempli, alors CompteNum commence par '4' (compte de tiers — racines 401, 411, 421, 425, etc.).",
        Source: "PCG");

    // --- Famille C — Cohérence temporelle (J3) -----------------------------

    /// <summary>C01 — EcritureDate au format AAAAMMJJ strict (8 chiffres, date valide).</summary>
    public static readonly Rule C01 = new(
        Id: "C01",
        Famille: RuleFamily.Temporal,
        Severity: Severity.Erreur,
        Libelle: "EcritureDate au format AAAAMMJJ strict (8 chiffres, date valide).",
        Source: "A. 47 A-1 LPF");

    /// <summary>C02 — PieceDate au format AAAAMMJJ strict si rempli.</summary>
    public static readonly Rule C02 = new(
        Id: "C02",
        Famille: RuleFamily.Temporal,
        Severity: Severity.Erreur,
        Libelle: "PieceDate au format AAAAMMJJ strict si rempli.",
        Source: "A. 47 A-1 LPF");

    /// <summary>C03 — ValidDate au format AAAAMMJJ strict si rempli.</summary>
    public static readonly Rule C03 = new(
        Id: "C03",
        Famille: RuleFamily.Temporal,
        Severity: Severity.Erreur,
        Libelle: "ValidDate au format AAAAMMJJ strict si rempli.",
        Source: "A. 47 A-1 LPF");

    /// <summary>C04 — DateLet au format AAAAMMJJ strict si rempli.</summary>
    public static readonly Rule C04 = new(
        Id: "C04",
        Famille: RuleFamily.Temporal,
        Severity: Severity.Erreur,
        Libelle: "DateLet au format AAAAMMJJ strict si rempli.",
        Source: "A. 47 A-1 LPF");

    /// <summary>C05 — Toutes les EcritureDate dans la période d'exercice déclarée (option --exercice).</summary>
    public static readonly Rule C05 = new(
        Id: "C05",
        Famille: RuleFamily.Temporal,
        Severity: Severity.Erreur,
        Libelle: "Toutes les EcritureDate dans la période d'exercice déclarée (option --exercice).",
        Source: "BOI-CF-IOR-60-40-20");

    /// <summary>C06 — ValidDate postérieure ou égale à EcritureDate quand les deux sont remplies.</summary>
    public static readonly Rule C06 = new(
        Id: "C06",
        Famille: RuleFamily.Temporal,
        Severity: Severity.Erreur,
        Libelle: "ValidDate postérieure ou égale à EcritureDate quand les deux sont remplies.",
        Source: "Doctrine fiscale");

    /// <summary>C07 — Numérotation chronologique des écritures validées au sein d'un même journal.</summary>
    public static readonly Rule C07 = new(
        Id: "C07",
        Famille: RuleFamily.Temporal,
        Severity: Severity.Erreur,
        Libelle: "Numérotation chronologique des écritures validées au sein d'un même journal (croissance d'EcritureDate selon EcritureNum parmi les écritures avec ValidDate non vide).",
        Source: "BOI-CF-IOR-60-40-20 (irréversibilité)");

    /// <summary>C08 — Signalement des écritures sans ValidDate (non validées).</summary>
    public static readonly Rule C08 = new(
        Id: "C08",
        Famille: RuleFamily.Temporal,
        Severity: Severity.Avertissement,
        Libelle: "Signalement des écritures sans ValidDate (non validées).",
        Source: "BOI-CF-IOR-60-40-20");

    /// <summary>
    /// Toutes les règles connues du catalogue, dans l'ordre de leur identifiant.
    /// Utilisable pour générer la documentation, la liste --list-rules du CLI,
    /// et pour s'assurer qu'aucun <see cref="Rule.Id"/> n'est dupliqué (test).
    /// </summary>
    public static IReadOnlyList<Rule> All { get; } = new[]
    {
        A01, A02, A03, A04, A05, A06, A07,
        B01, B02, B03, B04, B05, B06,
        C01, C02, C03, C04, C05, C06, C07, C08,
    };
}
