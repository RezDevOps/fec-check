// © 2026 Rudy Rezaire / RezDevOps. Licence MIT — voir LICENSE.

using FluentAssertions;
using Xunit;

namespace RezDevOps.FecCheck.Core.Tests;

/// <summary>
/// Garde-fous sur le catalogue de règles. L'objectif est de détecter au plus
/// tôt les régressions de cohérence (ID dupliqué, source manquante, règle
/// orpheline) qui dégraderaient la traçabilité réglementaire.
/// </summary>
public sealed class RulesCatalogTests
{
    [Fact]
    public void All_ContientLesReglesDesFamillesAEtB_AJalonJ2()
    {
        Rules.All.Select(r => r.Id).Should().BeEquivalentTo(new[]
        {
            "A01", "A02", "A03", "A04", "A05", "A06", "A07",
            "B01", "B02", "B03", "B04", "B05", "B06",
        });
    }

    [Fact]
    public void All_FamilleA_EstFormat()
    {
        Rules.All.Where(r => r.Id.StartsWith('A'))
            .Should().OnlyContain(r => r.Famille == FecCheckInfo.RuleFamily.Format);
    }

    [Fact]
    public void All_FamilleB_EstAccounting()
    {
        Rules.All.Where(r => r.Id.StartsWith('B'))
            .Should().OnlyContain(r => r.Famille == FecCheckInfo.RuleFamily.Accounting);
    }

    [Fact]
    public void All_AucunIdDuplique()
    {
        var ids = Rules.All.Select(r => r.Id).ToList();
        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void All_ChaqueRegleACiteUneSourceReglementaire()
    {
        // Posture RezDevOps : aucune règle n'est implémentée sans citer
        // sa source. Garde-fou pour ne jamais perdre cette propriété.
        Rules.All.Should().OnlyContain(r => !string.IsNullOrWhiteSpace(r.Source));
    }

    [Fact]
    public void All_ChaqueRegleAUnLibelleNonVide()
    {
        Rules.All.Should().OnlyContain(r => !string.IsNullOrWhiteSpace(r.Libelle));
    }

    [Theory]
    [InlineData("A01", Severity.Bloquante)]
    [InlineData("A02", Severity.Bloquante)]
    [InlineData("A03", Severity.Bloquante)]
    [InlineData("A04", Severity.Bloquante)]
    [InlineData("A05", Severity.Erreur)]
    [InlineData("A06", Severity.Avertissement)]
    [InlineData("A07", Severity.Erreur)]
    [InlineData("B01", Severity.Erreur)]
    [InlineData("B02", Severity.Erreur)]
    [InlineData("B03", Severity.Erreur)]
    [InlineData("B04", Severity.Avertissement)]
    [InlineData("B05", Severity.Erreur)]
    [InlineData("B06", Severity.Avertissement)]
    public void Severite_AlignéeSurDocsRegles(string ruleId, Severity expected)
    {
        // Doit rester aligné avec docs/regles.md, qui est la source de vérité
        // documentaire pour la traçabilité réglementaire.
        var rule = Rules.All.Single(r => r.Id == ruleId);
        rule.Severity.Should().Be(expected);
    }
}
