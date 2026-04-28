// © 2026 Rudy Rezaire / RezDevOps. Licence MIT — voir LICENSE.

using FluentAssertions;
using Xunit;

namespace RezDevOps.FecCheck.Core.Tests;

/// <summary>
/// Tests d'intégration de bout en bout sur les fixtures versionnées du repo.
/// Pour chaque règle de la Famille A, une fixture dédiée valide qu'au moins
/// le finding attendu est remonté ; pour la fixture conforme, l'analyse
/// doit retourner zéro anomalie.
/// </summary>
public sealed class FecValidatorTests
{
    // ----- Fichier conforme : zéro finding ---------------------------------

    [Fact]
    public void Validate_FixtureConforme_RetourneVerdictConforme_SansFindings()
    {
        var report = FecValidator.Validate(TestFixtures.Conforme);

        report.Verdict.Should().Be(Verdict.Conforme);
        report.Findings.Should().BeEmpty();

        report.EncodageDetecte.Should().Be(DetectedEncoding.Utf8);
        report.SeparateurDetecte.Should().Be('\t');
        report.FinDeLigneDetectee.Should().Be(DetectedLineEnding.Lf);
        report.LignesLues.Should().Be(9, "8 lignes de données + 1 ligne d'en-tête");
    }

    // ----- A01 — Encodage --------------------------------------------------

    [Fact]
    public void Validate_FixtureA01Utf16_RemonteA01_EtNonConforme()
    {
        var report = FecValidator.Validate(TestFixtures.A01_EncodageUtf16);

        report.Verdict.Should().Be(Verdict.NonConforme);
        report.Findings.Should().ContainSingle(f => f.Rule.Id == "A01");
        report.EncodageDetecte.Should().Be(DetectedEncoding.Inconnu);
    }

    // ----- A02 — Séparateur ------------------------------------------------

    [Fact]
    public void Validate_FixtureA02SeparateurMixte_RemonteA02_AuMoinsUneFois()
    {
        var report = FecValidator.Validate(TestFixtures.A02_SeparateurMixte);

        report.Verdict.Should().Be(Verdict.NonConforme);
        report.Findings.Should().Contain(f => f.Rule.Id == "A02");
    }

    // ----- A03 — En-tête à 17 colonnes -------------------------------------

    [Fact]
    public void Validate_FixtureA03ColonnesManquantes_RemonteA03_EtArreteAnalyse()
    {
        var report = FecValidator.Validate(TestFixtures.A03_EnteteColonnesManquantes);

        report.Verdict.Should().Be(Verdict.NonConforme);
        report.Findings.Should().Contain(f => f.Rule.Id == "A03");

        // A03 est Bloquante : on ne doit pas avoir de findings A05/A07 sur
        // les lignes de données qui suivent.
        report.Findings.Should().NotContain(f => f.Rule.Id == "A05" || f.Rule.Id == "A07");
    }

    // ----- A04 — Ordre des colonnes ----------------------------------------

    [Fact]
    public void Validate_FixtureA04OrdreFaux_RemonteA04()
    {
        var report = FecValidator.Validate(TestFixtures.A04_EnteteOrdreFaux);

        report.Verdict.Should().Be(Verdict.NonConforme);
        report.Findings.Should().Contain(f => f.Rule.Id == "A04");
    }

    // ----- A05 — Ligne de données tronquée ---------------------------------

    [Fact]
    public void Validate_FixtureA05LigneTronquee_RemonteA05_SurLaBonneLigne()
    {
        var report = FecValidator.Validate(TestFixtures.A05_LigneTronquee);

        report.Verdict.Should().Be(Verdict.NonConforme);
        report.Findings.Should()
            .Contain(f => f.Rule.Id == "A05" && f.LineNumber == 3);
    }

    // ----- A06 — Mélange CRLF / LF -----------------------------------------

    [Fact]
    public void Validate_FixtureA06EolMixte_RemonteA06_EtVerdictAvertissement()
    {
        var report = FecValidator.Validate(TestFixtures.A06_EolMixte);

        // A06 est un Avertissement, pas une Erreur : le verdict doit refléter
        // que le fichier reste conforme avec réserve.
        report.Verdict.Should().Be(Verdict.ConformeAvecAvertissements);
        report.Findings.Should().ContainSingle()
            .Which.Rule.Id.Should().Be("A06");

        report.FinDeLigneDetectee.Should().Be(DetectedLineEnding.Mixte);
    }

    // ----- A07 — Champ obligatoire vide ------------------------------------

    [Fact]
    public void Validate_FixtureA07ChampVide_RemonteA07_SurLaBonneLigne()
    {
        var report = FecValidator.Validate(TestFixtures.A07_ChampObligatoireVide);

        report.Verdict.Should().Be(Verdict.NonConforme);
        report.Findings.Should()
            .Contain(f => f.Rule.Id == "A07" && f.LineNumber == 3);
    }

    // ----- B01 — Équilibre par écriture ------------------------------------

    [Fact]
    public void Validate_FixtureB01EcritureDesequilibree_RemonteDeuxB01_SansB02()
    {
        var report = FecValidator.Validate(TestFixtures.B01_EcritureDesequilibree);

        report.Verdict.Should().Be(Verdict.NonConforme);

        // Deux écritures localement déséquilibrées (AC0001 et VE0001),
        // équilibre global préservé : pas de B02.
        report.Findings.Where(f => f.Rule.Id == "B01").Should().HaveCount(2);
        report.Findings.Should().NotContain(f => f.Rule.Id == "B02");

        // Le finding B01 cite la première ligne où l'écriture apparaît.
        report.Findings.Should().Contain(f => f.Rule.Id == "B01" && f.LineNumber == 2);
        report.Findings.Should().Contain(f => f.Rule.Id == "B01" && f.LineNumber == 5);
    }

    // ----- B02 — Équilibre global ------------------------------------------

    [Fact]
    public void Validate_FixtureB02DesequilibreGlobal_RemonteB02_EtB01SurEcritureFautive()
    {
        var report = FecValidator.Validate(TestFixtures.B02_TotalGlobalDesequilibre);

        report.Verdict.Should().Be(Verdict.NonConforme);

        // B02 est global : LineNumber null.
        report.Findings.Should().ContainSingle(f => f.Rule.Id == "B02")
            .Which.LineNumber.Should().BeNull();

        // L'écriture qui crée le déséquilibre global est aussi déséquilibrée localement.
        report.Findings.Should().Contain(f => f.Rule.Id == "B01" && f.LineNumber == 2);
    }

    // ----- B03 — Format numérique invalide ---------------------------------

    [Fact]
    public void Validate_FixtureB03FormatInvalide_RemonteB03_SurLaBonneLigne_SansBruitB01B02()
    {
        var report = FecValidator.Validate(TestFixtures.B03_FormatNumeriqueInvalide);

        report.Verdict.Should().Be(Verdict.NonConforme);
        report.Findings.Should().Contain(f => f.Rule.Id == "B03" && f.LineNumber == 2);

        // Le montant 1000,00000 reste parsable comme 1000m, donc l'agrégat
        // d'écriture reste équilibré : ni B01 ni B02 ne doivent se déclencher.
        report.Findings.Should().NotContain(f => f.Rule.Id == "B01" || f.Rule.Id == "B02");
    }

    // ----- B04 — Mutuelle exclusion Débit/Crédit ---------------------------

    [Fact]
    public void Validate_FixtureB04DebitEtCreditNonNuls_RemonteB04_AvertissementSeulement()
    {
        var report = FecValidator.Validate(TestFixtures.B04_DebitEtCreditNonNuls);

        // L'écriture ajoutée (OD0001) est auto-équilibrée (D=C=150) → pas
        // d'erreur, juste un avertissement B04 sur la ligne 10.
        report.Verdict.Should().Be(Verdict.ConformeAvecAvertissements);
        report.Findings.Should().ContainSingle()
            .Which.Should().Match<Finding>(f => f.Rule.Id == "B04" && f.LineNumber == 10);
    }

    // ----- B05 — Cohérence CompAuxNum / CompAuxLib --------------------------

    [Fact]
    public void Validate_FixtureB05CompAuxNumSansLib_RemonteB05_SurLaBonneLigne()
    {
        var report = FecValidator.Validate(TestFixtures.B05_CompAuxNumSansLib);

        report.Verdict.Should().Be(Verdict.NonConforme);
        report.Findings.Should().ContainSingle()
            .Which.Should().Match<Finding>(f => f.Rule.Id == "B05" && f.LineNumber == 4);
    }

    // ----- B06 — Compte auxiliaire sur compte non-tiers --------------------

    [Fact]
    public void Validate_FixtureB06CompAuxSurCompteNonTiers_RemonteB06_AvertissementSeulement()
    {
        var report = FecValidator.Validate(TestFixtures.B06_CompAuxSurCompteNonTiers);

        // Avertissement seul : le fichier reste conforme avec réserve.
        report.Verdict.Should().Be(Verdict.ConformeAvecAvertissements);
        report.Findings.Should().ContainSingle()
            .Which.Should().Match<Finding>(f => f.Rule.Id == "B06" && f.LineNumber == 6);
    }

    // ----- C01 — EcritureDate au mauvais format ----------------------------

    [Fact]
    public void Validate_FixtureC01EcritureDateInvalide_RemonteC01_VerdictNonConforme()
    {
        var report = FecValidator.Validate(TestFixtures.C01_EcritureDateInvalide);

        report.Verdict.Should().Be(Verdict.NonConforme);
        report.Findings.Should().Contain(f => f.Rule.Id == "C01");

        // EcritureDate non parsable ⇒ C05/C07 ne déclenchent pas pour cette
        // écriture (champ requis cassé). C08 ne déclenche pas car ValidDate
        // est rempli sur les deux lignes.
        report.Findings.Should().NotContain(f => f.Rule.Id == "C05");
        report.Findings.Should().NotContain(f => f.Rule.Id == "C07");
        report.Findings.Should().NotContain(f => f.Rule.Id == "C08");
    }

    // ----- C02 — PieceDate au mauvais format -------------------------------

    [Fact]
    public void Validate_FixtureC02PieceDateInvalide_RemonteC02_VerdictNonConforme()
    {
        var report = FecValidator.Validate(TestFixtures.C02_PieceDateInvalide);

        report.Verdict.Should().Be(Verdict.NonConforme);
        report.Findings.Should().Contain(f => f.Rule.Id == "C02" && f.LineNumber == 2);
        report.Findings.Should().NotContain(f => f.Rule.Id == "C01");
    }

    // ----- C03 — ValidDate au mauvais format -------------------------------

    [Fact]
    public void Validate_FixtureC03ValidDateInvalide_RemonteC03_VerdictNonConforme()
    {
        var report = FecValidator.Validate(TestFixtures.C03_ValidDateInvalide);

        report.Verdict.Should().Be(Verdict.NonConforme);
        report.Findings.Should().Contain(f => f.Rule.Id == "C03" && f.LineNumber == 2);

        // ValidDate non vide (même invalide) ⇒ écriture vue comme « validée »
        // côté C08 ; C08 ne doit pas déclencher.
        report.Findings.Should().NotContain(f => f.Rule.Id == "C08");
    }

    // ----- C04 — DateLet au mauvais format ---------------------------------

    [Fact]
    public void Validate_FixtureC04DateLetInvalide_RemonteC04_VerdictNonConforme()
    {
        var report = FecValidator.Validate(TestFixtures.C04_DateLetInvalide);

        report.Verdict.Should().Be(Verdict.NonConforme);
        report.Findings.Should().Contain(f => f.Rule.Id == "C04" && f.LineNumber == 2);
    }

    // ----- C05 — Période d'exercice ----------------------------------------

    [Fact]
    public void Validate_FixtureC05HorsPeriode_AvecExerciceFourni_RemonteC05()
    {
        var exercice = ExercicePeriod.Create(
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 12, 31));

        var report = FecValidator.Validate(TestFixtures.C05_HorsPeriodeExercice, exercice);

        report.Verdict.Should().Be(Verdict.NonConforme);
        report.Findings.Should().ContainSingle(f => f.Rule.Id == "C05");
    }

    [Fact]
    public void Validate_FixtureC05HorsPeriode_SansExercice_NeRemontePasC05()
    {
        // Sans option --exercice, C05 n'est pas évaluée — le fichier doit
        // être conforme (toutes les autres règles sont satisfaites).
        var report = FecValidator.Validate(TestFixtures.C05_HorsPeriodeExercice);

        report.Findings.Should().NotContain(f => f.Rule.Id == "C05");
        report.Verdict.Should().Be(Verdict.Conforme);
    }

    // ----- C06 — ValidDate antérieure à EcritureDate -----------------------

    [Fact]
    public void Validate_FixtureC06ValidationAnterieureEcriture_RemonteC06_VerdictNonConforme()
    {
        var report = FecValidator.Validate(TestFixtures.C06_ValidationAnterieureEcriture);

        report.Verdict.Should().Be(Verdict.NonConforme);
        report.Findings.Should().Contain(f => f.Rule.Id == "C06" && f.LineNumber == 2);
        report.Findings.Should().Contain(f => f.Rule.Id == "C06" && f.LineNumber == 3);
    }

    // ----- C07 — Chronologie cassée dans un journal ------------------------

    [Fact]
    public void Validate_FixtureC07ChronologieCassee_RemonteC07_VerdictNonConforme()
    {
        var report = FecValidator.Validate(TestFixtures.C07_ChronologieCassee);

        report.Verdict.Should().Be(Verdict.NonConforme);
        // Une seule paire (AC0001, AC0002) en violation ⇒ un seul finding C07.
        report.Findings.Where(f => f.Rule.Id == "C07").Should().HaveCount(1);

        // Les deux écritures sont validées ⇒ C08 ne déclenche pas.
        report.Findings.Should().NotContain(f => f.Rule.Id == "C08");
    }

    // ----- C08 — Écritures non validées ------------------------------------

    [Fact]
    public void Validate_FixtureC08EcrituresNonValidees_RemonteC08_AvertissementAgrege()
    {
        var report = FecValidator.Validate(TestFixtures.C08_EcrituresNonValidees);

        // C08 seul ⇒ verdict Avertissement.
        report.Verdict.Should().Be(Verdict.ConformeAvecAvertissements);
        var c08 = report.Findings.Should().ContainSingle(f => f.Rule.Id == "C08").Subject;
        c08.Message.Should().Contain("2 écriture(s) sans ValidDate");
        c08.Message.Should().Contain("AC/AC0001").And.Contain("AC/AC0002");
    }

    // ----- Conforme + --exercice : aucun finding C05 ------------------------

    [Fact]
    public void Validate_FixtureConforme_AvecExerciceCouvrant_RestePropre()
    {
        var exercice = ExercicePeriod.Create(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31));

        var report = FecValidator.Validate(TestFixtures.Conforme, exercice);

        report.Verdict.Should().Be(Verdict.Conforme);
        report.Findings.Should().BeEmpty();
    }

    // ----- ExercicePeriod — invariants -------------------------------------

    [Fact]
    public void ExercicePeriod_Create_RejetteDebutPosterieurFin()
    {
        var act = () => ExercicePeriod.Create(
            new DateOnly(2024, 12, 31),
            new DateOnly(2024, 1, 1));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ExercicePeriod_Contains_BornesIncluses()
    {
        var p = ExercicePeriod.Create(
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 12, 31));

        p.Contains(new DateOnly(2024, 1, 1)).Should().BeTrue();
        p.Contains(new DateOnly(2024, 12, 31)).Should().BeTrue();
        p.Contains(new DateOnly(2023, 12, 31)).Should().BeFalse();
        p.Contains(new DateOnly(2025, 1, 1)).Should().BeFalse();
    }

    // ----- Erreurs d'I/O ---------------------------------------------------

    [Fact]
    public void Validate_FichierInexistant_LeveFileNotFoundException()
    {
        var fakePath = Path.Combine(AppContext.BaseDirectory, "fixtures", "introuvable.txt");

        var act = () => FecValidator.Validate(fakePath);

        act.Should().Throw<FileNotFoundException>();
    }

    // ----- Verdict.ComputeVerdict — logique pure ---------------------------

    [Fact]
    public void ComputeVerdict_AucunFinding_RetourneConforme()
    {
        ValidationReport.ComputeVerdict(Array.Empty<Finding>())
            .Should().Be(Verdict.Conforme);
    }

    [Fact]
    public void ComputeVerdict_AvertissementSeul_RetourneConformeAvecAvertissements()
    {
        var f = new Finding(Rules.A06, null, "test");

        ValidationReport.ComputeVerdict(new[] { f })
            .Should().Be(Verdict.ConformeAvecAvertissements);
    }

    [Fact]
    public void ComputeVerdict_AvertissementPlusErreur_RetourneNonConforme()
    {
        var avertissement = new Finding(Rules.A06, null, "warn");
        var erreur = new Finding(Rules.A05, 1, "err");

        ValidationReport.ComputeVerdict(new[] { avertissement, erreur })
            .Should().Be(Verdict.NonConforme);
    }
}
