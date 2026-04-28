// © 2026 Rudy Rezaire / RezDevOps. Licence MIT — voir LICENSE.

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RezDevOps.FecCheck.Core;

/// <summary>
/// Sérialise un <see cref="ValidationReport"/> en JSON conforme au schéma v1
/// figé dans <c>docs/json-schema.md</c>. Sortie indentée, en UTF-8 sans BOM,
/// déterministe pour un même couple (rapport, environnement).
/// </summary>
/// <remarks>
/// <para>
/// Le contrat de schéma est versionné via le champ <c>schemaVersion</c> à la
/// racine, posé à <c>1</c> pour <c>v0.4.0</c>. Toute évolution non additive
/// déclenchera l'incrément à <c>2</c> ; les évolutions additives (nouveau
/// champ optionnel) restent en <c>1</c>.
/// </para>
/// <para>
/// La sérialisation passe par une <see cref="JsonSerializerContext"/> source-generated
/// pour rester AOT-friendly (cf. cadrage §6.1 — option AOT). Aucune dépendance
/// NuGet tierce n'est introduite : System.Text.Json est embarqué par la BCL .NET 8.
/// </para>
/// </remarks>
public static class JsonReportWriter
{
    /// <summary>
    /// Version du schéma JSON exposé en racine du document. Incrémentée
    /// uniquement en cas de breaking change (renommage / suppression de champ,
    /// changement de typage). Les ajouts purement additifs restent compatibles
    /// avec les consommateurs déclarant <c>schemaVersion == 1</c>.
    /// </summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>
    /// Sérialise <paramref name="report"/> en JSON et écrit le résultat dans
    /// <paramref name="writer"/>. Le writer est laissé ouvert ; sa disposition
    /// reste à l'appelant.
    /// </summary>
    /// <param name="report">Rapport à sérialiser.</param>
    /// <param name="environment">Métadonnées d'environnement (chemin, version, exercice…).</param>
    /// <param name="writer">Writer de sortie (UTF-8 sans BOM recommandé).</param>
    public static void Write(ValidationReport report, ReportEnvironment environment, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(writer);

        var dto = BuildDto(report, environment);
        var json = JsonSerializer.Serialize(dto, FecCheckJsonContext.Default.JsonReportDocument);
        writer.Write(json);
    }

    /// <summary>
    /// Surcharge qui retourne directement le texte JSON. Utile aux tests et aux
    /// consommateurs programmatiques qui n'ont pas besoin d'un flux.
    /// </summary>
    public static string Serialize(ValidationReport report, ReportEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(environment);

        var dto = BuildDto(report, environment);
        return JsonSerializer.Serialize(dto, FecCheckJsonContext.Default.JsonReportDocument);
    }

    // ------------------------------------------------------------------------
    // Construction du DTO
    // ------------------------------------------------------------------------

    private static JsonReportDocument BuildDto(ValidationReport report, ReportEnvironment env)
    {
        var anomalies = new List<JsonAnomalyDto>(report.Findings.Count);
        foreach (var f in report.Findings)
        {
            anomalies.Add(new JsonAnomalyDto(
                Regle: new JsonRuleDto(
                    Id: f.Rule.Id,
                    Famille: SerializeFamille(f.Rule.Famille),
                    Severite: SerializeSeverite(f.Rule.Severity),
                    Libelle: f.Rule.Libelle,
                    Source: f.Rule.Source),
                Ligne: f.LineNumber,
                Message: f.Message,
                Contexte: f.Contexte));
        }

        var synth = ComputeSynthese(report.Findings);

        return new JsonReportDocument(
            SchemaVersion: CurrentSchemaVersion,
            Outil: new JsonToolDto(
                Nom: env.ProductName,
                Version: env.ProductVersion),
            GenereLe: env.GeneratedAt.ToUniversalTime()
                .ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
            Fichier: new JsonFileDto(
                Chemin: env.FilePath,
                Encodage: SerializeEncodage(report.EncodageDetecte),
                Separateur: SerializeSeparateur(report.SeparateurDetecte),
                FinDeLigne: SerializeFinDeLigne(report.FinDeLigneDetectee),
                LignesLues: report.LignesLues),
            Exercice: env.Exercice is null
                ? null
                : new JsonExerciceDto(
                    Debut: env.Exercice.Debut.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Fin: env.Exercice.Fin.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            Verdict: SerializeVerdict(report.Verdict),
            CodeRetour: VerdictToExitCode(report.Verdict),
            Synthese: synth,
            Anomalies: anomalies);
    }

    private static JsonSyntheseDto ComputeSynthese(IReadOnlyList<Finding> findings)
    {
        var avert = 0;
        var erreur = 0;
        var bloquante = 0;
        var format = 0;
        var comptable = 0;
        var temporel = 0;

        foreach (var f in findings)
        {
            switch (f.Rule.Severity)
            {
                case Severity.Avertissement: avert++; break;
                case Severity.Erreur: erreur++; break;
                case Severity.Bloquante: bloquante++; break;
            }

            switch (f.Rule.Famille)
            {
                case FecCheckInfo.RuleFamily.Format: format++; break;
                case FecCheckInfo.RuleFamily.Accounting: comptable++; break;
                case FecCheckInfo.RuleFamily.Temporal: temporel++; break;
            }
        }

        return new JsonSyntheseDto(
            TotalAnomalies: findings.Count,
            ParSeverite: new JsonSeveriteCountsDto(
                Avertissement: avert,
                Erreur: erreur,
                Bloquante: bloquante),
            ParFamille: new JsonFamilleCountsDto(
                Format: format,
                Comptable: comptable,
                Temporel: temporel));
    }

    // ------------------------------------------------------------------------
    // Sérialiseurs de chaînes — partagés avec le rapport Markdown via
    // <see cref="ReportLabels"/> pour parler le même vocabulaire.
    // ------------------------------------------------------------------------

    internal static string SerializeFamille(FecCheckInfo.RuleFamily f) => f switch
    {
        FecCheckInfo.RuleFamily.Format => "format",
        FecCheckInfo.RuleFamily.Accounting => "comptable",
        FecCheckInfo.RuleFamily.Temporal => "temporel",
        _ => f.ToString().ToLowerInvariant(),
    };

    internal static string SerializeSeverite(Severity s) => s switch
    {
        Severity.Avertissement => "avertissement",
        Severity.Erreur => "erreur",
        Severity.Bloquante => "bloquante",
        _ => s.ToString().ToLowerInvariant(),
    };

    internal static string SerializeVerdict(Verdict v) => v switch
    {
        Verdict.Conforme => "CONFORME",
        Verdict.ConformeAvecAvertissements => "CONFORME_AVEC_AVERTISSEMENTS",
        Verdict.NonConforme => "NON_CONFORME",
        _ => v.ToString().ToUpperInvariant(),
    };

    internal static string SerializeEncodage(DetectedEncoding e) => e switch
    {
        DetectedEncoding.Utf8 => "UTF-8",
        DetectedEncoding.Utf8WithBom => "UTF-8 (avec BOM)",
        DetectedEncoding.Iso8859_15 => "ISO-8859-15",
        DetectedEncoding.Inconnu => "non reconnu",
        _ => e.ToString(),
    };

    internal static string? SerializeSeparateur(char? c) => c switch
    {
        '\t' => "tabulation",
        '|' => "pipe",
        null => null,
        _ => c.ToString(),
    };

    internal static string SerializeFinDeLigne(DetectedLineEnding e) => e switch
    {
        DetectedLineEnding.Crlf => "CRLF",
        DetectedLineEnding.Lf => "LF",
        DetectedLineEnding.Mixte => "mixte",
        DetectedLineEnding.Aucune => "aucune",
        _ => e.ToString(),
    };

    internal static int VerdictToExitCode(Verdict v) => v switch
    {
        Verdict.Conforme => 0,
        Verdict.ConformeAvecAvertissements => 1,
        Verdict.NonConforme => 2,
        _ => 2,
    };
}

// ----------------------------------------------------------------------------
// DTOs internes — figés v1. La sérialisation source-générée plus bas pose
// les contraintes de naming (camelCase) et d'omission des nulls.
// ----------------------------------------------------------------------------

internal sealed record JsonReportDocument(
    int SchemaVersion,
    JsonToolDto Outil,
    string GenereLe,
    JsonFileDto Fichier,
    JsonExerciceDto? Exercice,
    string Verdict,
    int CodeRetour,
    JsonSyntheseDto Synthese,
    IReadOnlyList<JsonAnomalyDto> Anomalies);

internal sealed record JsonToolDto(string Nom, string Version);

internal sealed record JsonFileDto(
    string? Chemin,
    string Encodage,
    string? Separateur,
    string FinDeLigne,
    long LignesLues);

internal sealed record JsonExerciceDto(string Debut, string Fin);

internal sealed record JsonSyntheseDto(
    int TotalAnomalies,
    JsonSeveriteCountsDto ParSeverite,
    JsonFamilleCountsDto ParFamille);

internal sealed record JsonSeveriteCountsDto(int Avertissement, int Erreur, int Bloquante);

internal sealed record JsonFamilleCountsDto(int Format, int Comptable, int Temporel);

internal sealed record JsonAnomalyDto(
    JsonRuleDto Regle,
    long? Ligne,
    string Message,
    string? Contexte);

internal sealed record JsonRuleDto(
    string Id,
    string Famille,
    string Severite,
    string Libelle,
    string Source);

// ----------------------------------------------------------------------------
// Source generator — System.Text.Json AOT-friendly.
// ----------------------------------------------------------------------------

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(JsonReportDocument))]
internal sealed partial class FecCheckJsonContext : JsonSerializerContext
{
}

