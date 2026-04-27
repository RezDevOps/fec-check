// © 2026 Rudy Rezaire / RezDevOps. Licence MIT — voir LICENSE.

using System.Text;
using FluentAssertions;
using Xunit;

namespace RezDevOps.FecCheck.Core.Tests;

/// <summary>
/// Tests unitaires de la détection d'encodage. Indépendants des fixtures
/// disque pour rester rapides et lisibles ; l'intégration sur fichier réel
/// est couverte par <see cref="FecValidatorTests"/>.
/// </summary>
public sealed class EncodingDetectorTests
{
    public EncodingDetectorTests()
    {
        // Pour que GetEncoding(28605) fonctionne sur Linux/macOS dans les tests
        // sans dépendre de l'ordre d'exécution des suites.
        EncodingDetector.EnsureCodePagesRegistered();
    }

    [Fact]
    public void Detect_UTF8_PurAscii_RetourneUtf8_SansBom()
    {
        var bytes = Encoding.UTF8.GetBytes("JournalCode\tJournalLib\n");
        using var stream = new MemoryStream(bytes);

        var (detected, encoding, bomLength) = EncodingDetector.Detect(stream);

        detected.Should().Be(DetectedEncoding.Utf8);
        encoding.Should().NotBeNull();
        bomLength.Should().Be(0);
        stream.Position.Should().Be(0);
    }

    [Fact]
    public void Detect_UTF8_AvecCaracteresAccentues_RetourneUtf8()
    {
        var bytes = Encoding.UTF8.GetBytes("JournalCode\tÉcritureLib\n");
        using var stream = new MemoryStream(bytes);

        var (detected, _, _) = EncodingDetector.Detect(stream);

        detected.Should().Be(DetectedEncoding.Utf8);
    }

    [Fact]
    public void Detect_UTF8_AvecBom_RetourneUtf8WithBom_EtAvanceLeFlux()
    {
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        var content = Encoding.UTF8.GetBytes("JournalCode\n");
        using var stream = new MemoryStream(bom.Concat(content).ToArray());

        var (detected, encoding, bomLength) = EncodingDetector.Detect(stream);

        detected.Should().Be(DetectedEncoding.Utf8WithBom);
        encoding.Should().NotBeNull();
        bomLength.Should().Be(3);
        stream.Position.Should().Be(3, "le BOM doit être consommé pour ne pas polluer la ligne d'en-tête");
    }

    [Fact]
    public void Detect_UTF16_LE_AvecBom_RetourneInconnu()
    {
        var bom = new byte[] { 0xFF, 0xFE };
        var content = Encoding.Unicode.GetBytes("JournalCode\n");
        using var stream = new MemoryStream(bom.Concat(content).ToArray());

        var (detected, encoding, _) = EncodingDetector.Detect(stream);

        detected.Should().Be(DetectedEncoding.Inconnu);
        encoding.Should().BeNull();
    }

    [Fact]
    public void Detect_UTF16_BE_AvecBom_RetourneInconnu()
    {
        var bom = new byte[] { 0xFE, 0xFF };
        var content = Encoding.BigEndianUnicode.GetBytes("JournalCode\n");
        using var stream = new MemoryStream(bom.Concat(content).ToArray());

        var (detected, _, _) = EncodingDetector.Detect(stream);

        detected.Should().Be(DetectedEncoding.Inconnu);
    }

    [Fact]
    public void Detect_UTF16_LE_SansBom_RetourneInconnu_ViaHeuristiqueNul()
    {
        // ASCII en UTF-16 LE = beaucoup d'octets 0x00 (un sur deux).
        // L'heuristique de densité de NUL doit le repérer.
        var content = Encoding.Unicode.GetBytes("JournalCode\tJournalLib\tEcritureNum\n");
        using var stream = new MemoryStream(content);

        var (detected, _, _) = EncodingDetector.Detect(stream);

        detected.Should().Be(DetectedEncoding.Inconnu);
    }

    [Fact]
    public void Detect_ISO8859_15_RetourneIso_QuandUtf8Echoue()
    {
        // Octets ISO-8859-15 valides mais invalides en UTF-8 : 0xE9 isolé
        // ('é' en Latin-9) suivi d'octets ASCII. UTF-8 strict refuse, on
        // bascule sur ISO-8859-15.
        var bytes = new byte[]
        {
            (byte)'L', (byte)'i', (byte)'b', (byte)'\t',
            0xE9, // 'é' en ISO-8859-15
            (byte)'c', (byte)'r', (byte)'i', (byte)'t', (byte)'u', (byte)'r', (byte)'e',
        };
        using var stream = new MemoryStream(bytes);

        var (detected, encoding, _) = EncodingDetector.Detect(stream);

        detected.Should().Be(DetectedEncoding.Iso8859_15);
        encoding.Should().NotBeNull();
        encoding!.GetString(bytes).Should().Contain("Lib\técriture");
    }

    [Fact]
    public void Detect_FichierVide_RetourneUtf8()
    {
        // Convention : un fichier vide est techniquement valide en UTF-8 (0 byte).
        // Les règles A03 (en-tête manquant) prendront le relais après.
        using var stream = new MemoryStream(Array.Empty<byte>());

        var (detected, _, _) = EncodingDetector.Detect(stream);

        detected.Should().Be(DetectedEncoding.Utf8);
    }
}
