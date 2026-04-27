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
/// <item><description>J2 (v0.2.0) : Famille B — B01 à B06 (ajoutées plus tard).</description></item>
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

    /// <summary>
    /// Toutes les règles connues du catalogue, dans l'ordre de leur identifiant.
    /// Utilisable pour générer la documentation, la liste --list-rules du CLI,
    /// et pour s'assurer qu'aucun <see cref="Rule.Id"/> n'est dupliqué (test).
    /// </summary>
    public static IReadOnlyList<Rule> All { get; } = new[]
    {
        A01, A02, A03, A04, A05, A06, A07,
    };
}
