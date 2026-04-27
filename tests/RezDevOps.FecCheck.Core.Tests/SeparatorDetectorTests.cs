// © 2026 Rudy Rezaire / RezDevOps. Licence MIT — voir LICENSE.

using FluentAssertions;
using Xunit;

namespace RezDevOps.FecCheck.Core.Tests;

public sealed class SeparatorDetectorTests
{
    [Fact]
    public void Detect_HeaderTabule_RetourneTabulation()
    {
        const string header = "JournalCode\tJournalLib\tEcritureNum";

        SeparatorDetector.Detect(header).Should().Be('\t');
    }

    [Fact]
    public void Detect_HeaderPipe_RetourneePipe()
    {
        const string header = "JournalCode|JournalLib|EcritureNum";

        SeparatorDetector.Detect(header).Should().Be('|');
    }

    [Fact]
    public void Detect_HeaderMixte_RetourneNull()
    {
        // Un header qui mélange les deux séparateurs est ambigu : la décision
        // est volontairement reportée aux règles A03/A04 sur la base d'autres signaux.
        const string header = "JournalCode\tJournalLib|EcritureNum";

        SeparatorDetector.Detect(header).Should().BeNull();
    }

    [Fact]
    public void Detect_HeaderSansAucunSeparateur_RetourneNull()
    {
        SeparatorDetector.Detect("JournalCodeJournalLib").Should().BeNull();
    }

    [Fact]
    public void Detect_HeaderVide_RetourneNull()
    {
        SeparatorDetector.Detect(string.Empty).Should().BeNull();
    }
}
