// © 2026 Rudy Rezaire / RezDevOps. Licence MIT — voir LICENSE.

using System.Globalization;
using System.Text;

namespace RezDevOps.FecCheck.Core;

/// <summary>
/// Sérialise un <see cref="ValidationReport"/> en rapport Markdown lisible par
/// un dirigeant TPE/PME non-tech, conformément au cadrage §4.2.
/// </summary>
/// <remarks>
/// Le rapport est structuré en six sections : entête, verdict en bandeau,
/// caractéristiques du fichier, synthèse par famille, détail des anomalies
/// regroupées par famille, pied de page avec liens vers la documentation
/// canonique. Le format reste stable d'une version à l'autre — un test de
/// snapshot peut être adossé à cette sortie sans crainte de churn cosmétique
/// non motivé.
/// </remarks>
public static class MarkdownReportWriter
{
    /// <summary>Lien public vers la liste exhaustive des règles, cité en pied de rapport.</summary>
    public const string DocsRulesUrl = "https://github.com/RezDevOps/fec-check/blob/main/docs/regles.md";

    /// <summary>Lien public vers le schéma JSON, cité en pied de rapport.</summary>
    public const string DocsJsonSchemaUrl = "https://github.com/RezDevOps/fec-check/blob/main/docs/json-schema.md";

    /// <summary>
    /// Écrit le rapport Markdown correspondant à <paramref name="report"/>
    /// dans <paramref name="writer"/>. Le writer est laissé ouvert ; sa
    /// disposition reste à l'appelant.
    /// </summary>
    public static void Write(ValidationReport report, ReportEnvironment environment, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(writer);

        WriteHeader(writer, environment);
        WriteVerdictBanner(writer, report);
        WriteFileCharacteristics(writer, report, environment);
        WriteSynthesis(writer, report);
        WriteAnomalies(writer, report);
        WriteFooter(writer);
    }

    /// <summary>
    /// Surcharge qui retourne directement le texte Markdown. Utile aux tests
    /// et aux appelants programmatiques qui n'ont pas besoin d'un flux.
    /// </summary>
    public static string Serialize(ValidationReport report, ReportEnvironment environment)
    {
        var sb = new StringBuilder(capacity: 4096);
        using var writer = new StringWriter(sb) { NewLine = "\n" };
        Write(report, environment, writer);
        return sb.ToString();
    }

    // ------------------------------------------------------------------------
    // Sections
    // ------------------------------------------------------------------------

    private static void WriteHeader(TextWriter w, ReportEnvironment env)
    {
        w.WriteLine("# Rapport d'analyse FEC");
        w.WriteLine();
        w.Write("**Outil** : ");
        w.Write(env.ProductName);
        w.Write(' ');
        w.WriteLine(env.ProductVersion);
        w.Write("**Généré le** : ");
        w.WriteLine(env.GeneratedAt.ToUniversalTime()
            .ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture));
        if (!string.IsNullOrEmpty(env.FilePath))
        {
            w.Write("**Fichier analysé** : `");
            w.Write(EscapeInline(env.FilePath));
            w.WriteLine('`');
        }

        w.WriteLine();
    }

    private static void WriteVerdictBanner(TextWriter w, ValidationReport report)
    {
        var verdictLabel = HumanVerdict(report.Verdict);
        w.Write("## Verdict — ");
        w.WriteLine(verdictLabel);
        w.WriteLine();

        if (report.Findings.Count == 0)
        {
            w.WriteLine("> Aucune anomalie détectée. Le fichier respecte les vingt-et-une règles couvertes par `fec-check`.");
            w.WriteLine();
            return;
        }

        var counts = CountBySeverity(report.Findings);
        var fragments = new List<string>(3);
        if (counts.bloquante > 0)
        {
            fragments.Add($"{counts.bloquante} bloquante{(counts.bloquante > 1 ? "s" : string.Empty)}");
        }
        if (counts.erreur > 0)
        {
            fragments.Add($"{counts.erreur} erreur{(counts.erreur > 1 ? "s" : string.Empty)}");
        }
        if (counts.avertissement > 0)
        {
            fragments.Add(
                $"{counts.avertissement} avertissement{(counts.avertissement > 1 ? "s" : string.Empty)}");
        }

        var pluriel = report.Findings.Count > 1;
        w.Write("> **");
        w.Write(report.Findings.Count);
        w.Write(" anomalie");
        if (pluriel)
        {
            w.Write('s');
        }

        w.Write("** détectée");
        if (pluriel)
        {
            w.Write('s');
        }

        w.Write(" : ");
        w.Write(string.Join(", ", fragments));
        w.WriteLine('.');
        w.WriteLine();
    }

    private static void WriteFileCharacteristics(TextWriter w, ValidationReport report, ReportEnvironment env)
    {
        w.WriteLine("## Caractéristiques du fichier");
        w.WriteLine();
        w.WriteLine("| Propriété      | Valeur |");
        w.WriteLine("|----------------|--------|");
        w.Write("| Encodage       | ");
        w.Write(JsonReportWriter.SerializeEncodage(report.EncodageDetecte));
        w.WriteLine(" |");
        w.Write("| Séparateur     | ");
        w.Write(JsonReportWriter.SerializeSeparateur(report.SeparateurDetecte) ?? "non détecté");
        w.WriteLine(" |");
        w.Write("| Fin de ligne   | ");
        w.Write(JsonReportWriter.SerializeFinDeLigne(report.FinDeLigneDetectee));
        w.WriteLine(" |");
        w.Write("| Lignes lues    | ");
        w.Write(report.LignesLues.ToString(CultureInfo.InvariantCulture));
        w.WriteLine(" |");
        if (env.Exercice is not null)
        {
            w.Write("| Exercice       | du ");
            w.Write(env.Exercice.Debut.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            w.Write(" au ");
            w.Write(env.Exercice.Fin.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            w.WriteLine(" |");
        }
        else
        {
            w.WriteLine("| Exercice       | non précisé (règle C05 non évaluée) |");
        }

        w.WriteLine();
    }

    private static void WriteSynthesis(TextWriter w, ValidationReport report)
    {
        if (report.Findings.Count == 0)
        {
            return;
        }

        w.WriteLine("## Synthèse par famille de règles");
        w.WriteLine();
        w.WriteLine("| Famille | Anomalies |");
        w.WriteLine("|---------|-----------|");

        var perFamily = CountByFamily(report.Findings);
        if (perFamily.format > 0)
        {
            w.Write("| Famille A — Conformité de format | ");
            w.Write(perFamily.format);
            w.WriteLine(" |");
        }
        if (perFamily.comptable > 0)
        {
            w.Write("| Famille B — Cohérence comptable | ");
            w.Write(perFamily.comptable);
            w.WriteLine(" |");
        }
        if (perFamily.temporel > 0)
        {
            w.Write("| Famille C — Cohérence temporelle | ");
            w.Write(perFamily.temporel);
            w.WriteLine(" |");
        }

        w.WriteLine();
    }

    private static void WriteAnomalies(TextWriter w, ValidationReport report)
    {
        if (report.Findings.Count == 0)
        {
            return;
        }

        w.WriteLine("## Anomalies détectées");
        w.WriteLine();

        // On regroupe par famille pour préserver la lisibilité, en gardant
        // l'ordre relatif d'apparition au sein d'une famille.
        WriteFamilySection(w, "Famille A — Conformité de format",
            report.Findings.Where(f => f.Rule.Famille == FecCheckInfo.RuleFamily.Format));
        WriteFamilySection(w, "Famille B — Cohérence comptable",
            report.Findings.Where(f => f.Rule.Famille == FecCheckInfo.RuleFamily.Accounting));
        WriteFamilySection(w, "Famille C — Cohérence temporelle",
            report.Findings.Where(f => f.Rule.Famille == FecCheckInfo.RuleFamily.Temporal));
    }

    private static void WriteFamilySection(TextWriter w, string title, IEnumerable<Finding> findings)
    {
        var list = findings.ToList();
        if (list.Count == 0)
        {
            return;
        }

        w.Write("### ");
        w.WriteLine(title);
        w.WriteLine();

        foreach (var f in list)
        {
            WriteFinding(w, f);
        }
    }

    private static void WriteFinding(TextWriter w, Finding f)
    {
        w.Write("#### ");
        w.Write(f.Rule.Id);
        w.Write(" — ");
        w.Write(EscapeInline(f.Rule.Libelle));
        w.Write(" (");
        w.Write(HumanSeverity(f.Rule.Severity));
        w.WriteLine(')');
        w.WriteLine();

        w.Write("- **Source** : ");
        w.WriteLine(EscapeInline(f.Rule.Source));

        w.Write("- **Emplacement** : ");
        if (f.LineNumber is { } n)
        {
            w.Write("ligne ");
            w.WriteLine(n.ToString(CultureInfo.InvariantCulture));
        }
        else
        {
            w.WriteLine("fichier (anomalie globale)");
        }

        w.Write("- **Message** : ");
        w.WriteLine(EscapeInline(f.Message));

        if (!string.IsNullOrEmpty(f.Contexte))
        {
            w.WriteLine();
            w.WriteLine("```text");
            // L'extrait peut contenir des fins de ligne ou des séquences
            // particulières — on l'écrit tel quel à l'intérieur d'un bloc
            // code-fence pour préserver fidèlement ce que le validateur a vu.
            w.WriteLine(f.Contexte);
            w.WriteLine("```");
        }

        w.WriteLine();
    }

    private static void WriteFooter(TextWriter w)
    {
        w.WriteLine("---");
        w.WriteLine();
        w.WriteLine("## Pour aller plus loin");
        w.WriteLine();
        w.Write("- Liste exhaustive des règles : <");
        w.Write(DocsRulesUrl);
        w.WriteLine(">");
        w.Write("- Schéma JSON (contrat figé v1) : <");
        w.Write(DocsJsonSchemaUrl);
        w.WriteLine(">");
        w.WriteLine("- Texte officiel : Article A. 47 A-1 du Livre des procédures fiscales, BOI-CF-IOR-60-40-20.");
        w.WriteLine();
        w.WriteLine("> *Rapport généré par `fec-check`, utilitaire libre publié par RezDevOps sous licence MIT. Aucune donnée n'est transmise sur le réseau ; l'analyse est 100 % locale.*");
    }

    // ------------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------------

    private static (int avertissement, int erreur, int bloquante) CountBySeverity(
        IReadOnlyList<Finding> findings)
    {
        var avert = 0;
        var erreur = 0;
        var bloquante = 0;
        foreach (var f in findings)
        {
            switch (f.Rule.Severity)
            {
                case Severity.Avertissement: avert++; break;
                case Severity.Erreur: erreur++; break;
                case Severity.Bloquante: bloquante++; break;
            }
        }

        return (avert, erreur, bloquante);
    }

    private static (int format, int comptable, int temporel) CountByFamily(
        IReadOnlyList<Finding> findings)
    {
        var format = 0;
        var comptable = 0;
        var temporel = 0;
        foreach (var f in findings)
        {
            switch (f.Rule.Famille)
            {
                case FecCheckInfo.RuleFamily.Format: format++; break;
                case FecCheckInfo.RuleFamily.Accounting: comptable++; break;
                case FecCheckInfo.RuleFamily.Temporal: temporel++; break;
            }
        }

        return (format, comptable, temporel);
    }

    private static string HumanVerdict(Verdict v) => v switch
    {
        Verdict.Conforme => "CONFORME",
        Verdict.ConformeAvecAvertissements => "CONFORME AVEC AVERTISSEMENTS",
        Verdict.NonConforme => "NON CONFORME",
        _ => v.ToString(),
    };

    private static string HumanSeverity(Severity s) => s switch
    {
        Severity.Avertissement => "Avertissement",
        Severity.Erreur => "Erreur",
        Severity.Bloquante => "Bloquante",
        _ => s.ToString(),
    };

    /// <summary>
    /// Échappe les caractères Markdown structurants susceptibles de casser le
    /// rendu (pipe en cellule de tableau, backtick en run inline). On reste
    /// volontairement minimaliste : les libellés et messages sont contrôlés
    /// par le code, ils ne contiennent pas de HTML hostile.
    /// </summary>
    private static string EscapeInline(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var needsEscape = false;
        foreach (var c in text)
        {
            if (c == '|' || c == '\r' || c == '\n')
            {
                needsEscape = true;
                break;
            }
        }

        if (!needsEscape)
        {
            return text;
        }

        var sb = new StringBuilder(text.Length + 4);
        foreach (var c in text)
        {
            switch (c)
            {
                case '|':
                    sb.Append("\\|");
                    break;
                case '\r':
                    // ignoré : la fin de ligne est gérée par le writer.
                    break;
                case '\n':
                    sb.Append(' ');
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }

        return sb.ToString();
    }
}
