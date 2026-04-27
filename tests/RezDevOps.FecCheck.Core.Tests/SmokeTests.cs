// © 2026 Rudy Rezaire / RezDevOps. Licence MIT — voir LICENSE.

using FluentAssertions;
using Xunit;

namespace RezDevOps.FecCheck.Core.Tests;

/// <summary>
/// Tests fumée du jalon J0 : on s'assure que la solution compile, que
/// les projets sont liés correctement et que le pipeline xUnit tourne.
/// Les tests métier arrivent au J1.
/// </summary>
public sealed class SmokeTests
{
    [Fact]
    public void ProductName_DoitEtreFecCheck()
    {
        FecCheckInfo.ProductName.Should().Be("fec-check");
    }

    [Fact]
    public void Version_DoitSuivreSemver()
    {
        // J0 = 0.0.0 (cadrage seulement). Le format doit rester semver.
        FecCheckInfo.Version.Should().MatchRegex(@"^\d+\.\d+\.\d+(-[\w\.]+)?$");
    }

    [Theory]
    [InlineData(FecCheckInfo.RuleFamily.Format)]
    [InlineData(FecCheckInfo.RuleFamily.Accounting)]
    [InlineData(FecCheckInfo.RuleFamily.Temporal)]
    public void TroisFamillesDeRegles_SontDefinies(FecCheckInfo.RuleFamily famille)
    {
        // Les trois familles du cadrage §4.1 doivent exister dès J0,
        // même si aucune règle n'est encore implémentée.
        Enum.IsDefined(famille).Should().BeTrue();
    }
}
