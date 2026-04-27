// © 2026 Rudy Rezaire / RezDevOps. Licence MIT — voir LICENSE.

using FluentAssertions;
using Xunit;

namespace RezDevOps.FecCheck.Core.Tests;

public sealed class FecHeaderTests
{
    private const string ConformeTab =
        "JournalCode\tJournalLib\tEcritureNum\tEcritureDate\tCompteNum\tCompteLib\t"
        + "CompAuxNum\tCompAuxLib\tPieceRef\tPieceDate\tEcritureLib\tDebit\tCredit\t"
        + "EcritureLet\tDateLet\tValidDate\tMontantdevise\tIdevise";

    [Fact]
    public void Validate_HeaderConforme_NeRemonteAucunFinding()
    {
        var findings = FecHeader.Validate(ConformeTab, '\t');

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Validate_HeaderA17Colonnes_RemonteA03()
    {
        var truncated = string.Join('\t', ConformeTab.Split('\t')[..17]);

        var findings = FecHeader.Validate(truncated, '\t');

        findings.Should().ContainSingle()
            .Which.Rule.Id.Should().Be("A03");
    }

    [Fact]
    public void Validate_HeaderA19Colonnes_RemonteA03()
    {
        var inflated = ConformeTab + "\tColonneEnTrop";

        var findings = FecHeader.Validate(inflated, '\t');

        findings.Should().ContainSingle()
            .Which.Rule.Id.Should().Be("A03");
    }

    [Fact]
    public void Validate_HeaderColonneRenommee_RemonteA03()
    {
        var renamed = ConformeTab.Replace("Idevise", "DeviseId");

        var findings = FecHeader.Validate(renamed, '\t');

        findings.Should().ContainSingle()
            .Which.Rule.Id.Should().Be("A03");
    }

    [Fact]
    public void Validate_HeaderDebitCreditPermutes_RemonteA04()
    {
        var cols = ConformeTab.Split('\t').ToArray();
        var debitIdx = Array.IndexOf(cols, "Debit");
        var creditIdx = Array.IndexOf(cols, "Credit");
        (cols[debitIdx], cols[creditIdx]) = (cols[creditIdx], cols[debitIdx]);
        var swapped = string.Join('\t', cols);

        var findings = FecHeader.Validate(swapped, '\t');

        findings.Should().ContainSingle()
            .Which.Rule.Id.Should().Be("A04");
    }

    [Fact]
    public void Validate_HeaderViaPipe_FonctionnePareil()
    {
        var pipeHeader = ConformeTab.Replace('\t', '|');

        var findings = FecHeader.Validate(pipeHeader, '|');

        findings.Should().BeEmpty();
    }
}
