// © 2026 Rudy Rezaire / RezDevOps. Licence MIT — voir LICENSE.

using System.Text;
using FluentAssertions;
using Xunit;

namespace RezDevOps.FecCheck.Core.Tests;

/// <summary>
/// Tests du <see cref="ReportFileWriter"/>. On vérifie l'encodage UTF-8 sans
/// BOM, la création de répertoires parents et l'écrasement d'un fichier
/// pré-existant.
/// </summary>
public sealed class ReportFileWriterTests : IDisposable
{
    private readonly string _tempDir;

    public ReportFileWriterTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(),
            "fec-check-tests-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    private static ReportEnvironment SimpleEnv() =>
        new(
            ProductName: FecCheckInfo.ProductName,
            ProductVersion: FecCheckInfo.Version,
            FilePath: TestFixtures.Conforme,
            GeneratedAt: new DateTimeOffset(2026, 4, 28, 14, 30, 0, TimeSpan.Zero),
            Exercice: null);

    [Fact]
    public void WriteJson_EcritUtf8SansBom()
    {
        var report = FecValidator.Validate(TestFixtures.Conforme);
        var path = Path.Combine(_tempDir, "rapport.json");

        ReportFileWriter.WriteJson(path, report, SimpleEnv());

        var bytes = File.ReadAllBytes(path);
        bytes.Length.Should().BeGreaterThan(0);
        bytes.Take(3).Should().NotEqual(new byte[] { 0xEF, 0xBB, 0xBF },
            "le rapport JSON ne doit pas démarrer par un BOM UTF-8");

        var content = Encoding.UTF8.GetString(bytes);
        content.Should().Contain("\"schemaVersion\": 1");
    }

    [Fact]
    public void WriteMarkdown_EcritUtf8SansBom()
    {
        var report = FecValidator.Validate(TestFixtures.Conforme);
        var path = Path.Combine(_tempDir, "rapport.md");

        ReportFileWriter.WriteMarkdown(path, report, SimpleEnv());

        var bytes = File.ReadAllBytes(path);
        bytes.Take(3).Should().NotEqual(new byte[] { 0xEF, 0xBB, 0xBF });

        var content = Encoding.UTF8.GetString(bytes);
        content.Should().Contain("# Rapport d'analyse FEC");
    }

    [Fact]
    public void WriteJson_CreeRepertoiresParentsManquants()
    {
        var report = FecValidator.Validate(TestFixtures.Conforme);
        var nested = Path.Combine(_tempDir, "a", "b", "c", "rapport.json");

        ReportFileWriter.WriteJson(nested, report, SimpleEnv());

        File.Exists(nested).Should().BeTrue();
    }

    [Fact]
    public void WriteJson_EcraseFichierExistant()
    {
        var report = FecValidator.Validate(TestFixtures.Conforme);
        var path = Path.Combine(_tempDir, "rapport.json");
        File.WriteAllText(path, "ancien contenu obsolète");

        ReportFileWriter.WriteJson(path, report, SimpleEnv());

        var content = File.ReadAllText(path);
        content.Should().NotContain("ancien contenu obsolète");
        content.Should().Contain("\"schemaVersion\"");
    }
}
