// © 2026 Rudy Rezaire / RezDevOps. Licence MIT — voir LICENSE.

using FluentAssertions;
using Xunit;

namespace RezDevOps.FecCheck.Core.Tests;

/// <summary>
/// Tests du <see cref="MarkdownReportWriter"/>. Le rapport Markdown est destiné
/// à être lu par un dirigeant TPE/PME non-tech : les assertions s'attachent
/// donc au fond (présence des blocs structurants, exactitude des libellés)
/// plutôt qu'à la forme exacte (espacements, ordre des cellules d'un tableau).
/// </summary>
public sealed class MarkdownReportWriterTests
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
    public void Serialize_FixtureConforme_AfficheVerdictConformeEtAucuneAnomalie()
    {
        var report = FecValidator.Validate(TestFixtures.Conforme);

        var md = MarkdownReportWriter.Serialize(report, FrozenEnv(TestFixtures.Conforme));

        md.Should().Contain("# Rapport d'analyse FEC");
        md.Should().Contain("## Verdict — CONFORME");
        md.Should().Contain("Aucune anomalie détectée");
        md.Should().NotContain("## Anomalies détectées",
            "le rapport conforme n'expose pas la section Anomalies");
    }

    [Fact]
    public void Serialize_FixtureA01_AfficheVerdictNonConformeEtSectionFamilleA()
    {
        var report = FecValidator.Validate(TestFixtures.A01_EncodageUtf16);

        var md = MarkdownReportWriter.Serialize(report, FrozenEnv(TestFixtures.A01_EncodageUtf16));

        md.Should().Contain("## Verdict — NON CONFORME");
        md.Should().Contain("## Anomalies détectées");
        md.Should().Contain("Famille A — Conformité de format");
        md.Should().Contain("#### A01 —");
    }

    [Fact]
    public void Serialize_AvecExercice_AfficheLesBornes()
    {
        var report = FecValidator.Validate(TestFixtures.Conforme);
        var exercice = ExercicePeriod.Create(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31));

        var md = MarkdownReportWriter.Serialize(report, FrozenEnv(TestFixtures.Conforme, exercice));

        md.Should().Contain("du 2024-01-01 au 2024-12-31");
    }

    [Fact]
    public void Serialize_SansExercice_AfficheNotePasdEvalC05()
    {
        var report = FecValidator.Validate(TestFixtures.Conforme);

        var md = MarkdownReportWriter.Serialize(report, FrozenEnv(TestFixtures.Conforme));

        md.Should().Contain("règle C05 non évaluée");
    }

    [Fact]
    public void Serialize_AvecAnomaliesDeFamillesDifferentes_GroupeParFamille()
    {
        // Cette fixture ne déclenche que des erreurs de Famille C, mais on
        // vérifie le mécanisme de groupement (au moins une section par famille
        // présente, pas de section vide).
        var report = FecValidator.Validate(TestFixtures.C05_HorsPeriodeExercice,
            ExercicePeriod.Create(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31)));

        var md = MarkdownReportWriter.Serialize(
            report,
            FrozenEnv(TestFixtures.C05_HorsPeriodeExercice,
                ExercicePeriod.Create(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31))));

        md.Should().Contain("Famille C — Cohérence temporelle");
        md.Should().NotContain("Famille A — Conformité de format",
            "aucune anomalie de format n'est attendue sur cette fixture, la section ne doit pas apparaître");
    }

    [Fact]
    public void Serialize_FooterContientLesLiensDocs()
    {
        var report = FecValidator.Validate(TestFixtures.Conforme);

        var md = MarkdownReportWriter.Serialize(report, FrozenEnv(TestFixtures.Conforme));

        md.Should().Contain(MarkdownReportWriter.DocsRulesUrl);
        md.Should().Contain(MarkdownReportWriter.DocsJsonSchemaUrl);
        md.Should().Contain("licence MIT");
    }

    [Fact]
    public void Serialize_FinDeLignes_EstLfPourPortabilite()
    {
        var report = FecValidator.Validate(TestFixtures.Conforme);

        var md = MarkdownReportWriter.Serialize(report, FrozenEnv(TestFixtures.Conforme));

        md.Should().NotContain("\r\n",
            "le rapport Markdown est généré en LF pour rester déterministe quel que soit l'OS");
    }
}
