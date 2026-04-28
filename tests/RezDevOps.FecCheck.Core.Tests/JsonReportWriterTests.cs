// © 2026 Rudy Rezaire / RezDevOps. Licence MIT — voir LICENSE.

using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace RezDevOps.FecCheck.Core.Tests;

/// <summary>
/// Tests du <see cref="JsonReportWriter"/> — vérifient que le schéma JSON v1
/// est produit conformément au contrat figé dans <c>docs/json-schema.md</c>.
/// </summary>
/// <remarks>
/// Les assertions parsent la sortie en <see cref="JsonDocument"/> plutôt que
/// de comparer chaîne à chaîne pour ne pas être fragiles au formatage
/// (espaces, ordre des champs).
/// </remarks>
public sealed class JsonReportWriterTests
{
    private static readonly DateTimeOffset FrozenTime =
        new(2026, 4, 28, 14, 30, 0, TimeSpan.Zero);

    private static ReportEnvironment FrozenEnv(string? path = null, ExercicePeriod? exercice = null) =>
        new(
            ProductName: FecCheckInfo.ProductName,
            ProductVersion: FecCheckInfo.Version,
            FilePath: path,
            GeneratedAt: FrozenTime,
            Exercice: exercice);

    [Fact]
    public void Serialize_FixtureConforme_ProduitSchemaV1_VerdictConforme()
    {
        var report = FecValidator.Validate(TestFixtures.Conforme);

        var json = JsonReportWriter.Serialize(report, FrozenEnv(TestFixtures.Conforme));
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("schemaVersion").GetInt32().Should().Be(1);
        root.GetProperty("verdict").GetString().Should().Be("CONFORME");
        root.GetProperty("codeRetour").GetInt32().Should().Be(0);
        root.GetProperty("anomalies").GetArrayLength().Should().Be(0);

        var outil = root.GetProperty("outil");
        outil.GetProperty("nom").GetString().Should().Be("fec-check");
        outil.GetProperty("version").GetString().Should().Be(FecCheckInfo.Version);

        root.GetProperty("genereLe").GetString().Should().Be("2026-04-28T14:30:00Z");
    }

    [Fact]
    public void Serialize_FixtureA01_ContientAnomalieAvecRegleEtSeverite()
    {
        var report = FecValidator.Validate(TestFixtures.A01_EncodageUtf16);

        var json = JsonReportWriter.Serialize(report, FrozenEnv(TestFixtures.A01_EncodageUtf16));
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("verdict").GetString().Should().Be("NON_CONFORME");
        root.GetProperty("codeRetour").GetInt32().Should().Be(2);

        var anomalies = root.GetProperty("anomalies");
        anomalies.GetArrayLength().Should().Be(1);

        var first = anomalies[0];
        var regle = first.GetProperty("regle");
        regle.GetProperty("id").GetString().Should().Be("A01");
        regle.GetProperty("famille").GetString().Should().Be("format");
        regle.GetProperty("severite").GetString().Should().Be("bloquante");
        regle.GetProperty("source").GetString().Should().NotBeNullOrEmpty();
        regle.GetProperty("libelle").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Serialize_AvecExercice_ExposeBornesIso()
    {
        var report = FecValidator.Validate(TestFixtures.Conforme);
        var exercice = ExercicePeriod.Create(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31));

        var json = JsonReportWriter.Serialize(report, FrozenEnv(TestFixtures.Conforme, exercice));
        using var doc = JsonDocument.Parse(json);

        var ex = doc.RootElement.GetProperty("exercice");
        ex.GetProperty("debut").GetString().Should().Be("2024-01-01");
        ex.GetProperty("fin").GetString().Should().Be("2024-12-31");
    }

    [Fact]
    public void Serialize_SansExercice_OmitChampExercice()
    {
        var report = FecValidator.Validate(TestFixtures.Conforme);

        var json = JsonReportWriter.Serialize(report, FrozenEnv(TestFixtures.Conforme));
        using var doc = JsonDocument.Parse(json);

        doc.RootElement.TryGetProperty("exercice", out _).Should().BeFalse(
            "le champ exercice doit être omis quand il n'a pas été fourni (DefaultIgnoreCondition WhenWritingNull)");
    }

    [Fact]
    public void Serialize_SyntheseEstCoherenteAvecLesAnomalies()
    {
        var report = FecValidator.Validate(TestFixtures.B01_EcritureDesequilibree);

        var json = JsonReportWriter.Serialize(report, FrozenEnv());
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var synth = root.GetProperty("synthese");
        synth.GetProperty("totalAnomalies").GetInt32().Should().Be(report.Findings.Count);

        var parSeverite = synth.GetProperty("parSeverite");
        var avert = parSeverite.GetProperty("avertissement").GetInt32();
        var erreur = parSeverite.GetProperty("erreur").GetInt32();
        var bloquante = parSeverite.GetProperty("bloquante").GetInt32();
        (avert + erreur + bloquante).Should().Be(report.Findings.Count);

        var parFamille = synth.GetProperty("parFamille");
        var format = parFamille.GetProperty("format").GetInt32();
        var comptable = parFamille.GetProperty("comptable").GetInt32();
        var temporel = parFamille.GetProperty("temporel").GetInt32();
        (format + comptable + temporel).Should().Be(report.Findings.Count);
    }

    [Fact]
    public void Serialize_CamelCase_EstAppliqueeATousLesChamps()
    {
        var report = FecValidator.Validate(TestFixtures.Conforme);

        var json = JsonReportWriter.Serialize(report, FrozenEnv(TestFixtures.Conforme));

        // Tous les noms de champs sont en camelCase, jamais en PascalCase.
        json.Should().Contain("\"schemaVersion\"");
        json.Should().Contain("\"genereLe\"");
        json.Should().Contain("\"codeRetour\"");
        json.Should().NotContain("\"SchemaVersion\"");
        json.Should().NotContain("\"GenereLe\"");
    }

    [Fact]
    public void Serialize_FichierBlock_RenseignéAvecCharacteristiquesDetectees()
    {
        var report = FecValidator.Validate(TestFixtures.Conforme);

        var json = JsonReportWriter.Serialize(report, FrozenEnv(TestFixtures.Conforme));
        using var doc = JsonDocument.Parse(json);

        var fichier = doc.RootElement.GetProperty("fichier");
        fichier.GetProperty("encodage").GetString().Should().Be("UTF-8");
        fichier.GetProperty("separateur").GetString().Should().Be("tabulation");
        fichier.GetProperty("finDeLigne").GetString().Should().Be("LF");
        fichier.GetProperty("lignesLues").GetInt64().Should().Be(9);
        fichier.GetProperty("chemin").GetString().Should().EndWith("fec-minimal-conforme.txt");
    }
}
