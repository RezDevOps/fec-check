// © 2026 Rudy Rezaire / RezDevOps. Licence MIT — voir LICENSE.

using FluentAssertions;
using Xunit;

namespace RezDevOps.FecCheck.Core.Tests;

/// <summary>
/// Garde-fous sur les métadonnées statiques du produit. Évitent qu'un
/// renommage involontaire ou un format de version invalide ne passe en CI.
/// </summary>
public sealed class FecCheckInfoTests
{
    [Fact]
    public void ProductName_DoitEtreFecCheck()
    {
        FecCheckInfo.ProductName.Should().Be("fec-check");
    }

    [Fact]
    public void Version_DoitSuivreSemver()
    {
        FecCheckInfo.Version.Should().MatchRegex(@"^\d+\.\d+\.\d+(-[\w\.]+)?$");
    }

    [Theory]
    [InlineData(FecCheckInfo.RuleFamily.Format)]
    [InlineData(FecCheckInfo.RuleFamily.Accounting)]
    [InlineData(FecCheckInfo.RuleFamily.Temporal)]
    public void TroisFamillesDeRegles_SontDefinies(FecCheckInfo.RuleFamily famille)
    {
        // Les trois familles du cadrage §4.1 doivent rester définies, même
        // tant que B et C n'ont pas encore d'implémentation (J2/J3).
        Enum.IsDefined(famille).Should().BeTrue();
    }
}
