// © 2026 Rudy Rezaire / RezDevOps. Licence MIT — voir LICENSE.

using System.Globalization;

namespace RezDevOps.FecCheck.Core;

/// <summary>
/// Porte l'état nécessaire aux règles de la Famille C qui s'évaluent à
/// l'échelle du fichier (et non d'une seule ligne) :
/// <list type="bullet">
/// <item><description><see cref="Rules.C05"/> — chaque <c>EcritureDate</c> doit appartenir à la période d'exercice fournie via <c>--exercice</c> ; règle non évaluée si <see cref="ExercicePeriod"/> est <c>null</c> ;</description></item>
/// <item><description><see cref="Rules.C07"/> — au sein d'un journal, les écritures validées (<c>ValidDate</c> non vide) doivent avoir une <c>EcritureDate</c> croissante quand on les trie par <c>EcritureNum</c> ;</description></item>
/// <item><description><see cref="Rules.C08"/> — finding agrégé recensant le nombre d'écritures non validées (au moins une ligne sans <c>ValidDate</c>).</description></item>
/// </list>
/// </summary>
/// <remarks>
/// Pattern miroir à <see cref="AccountingContext"/> : instanciée une fois par
/// validation, alimentée ligne par ligne via <see cref="Observe"/>, émet ses
/// findings finaux via <see cref="EmitFinalFindings"/>. Empreinte mémoire en
/// <em>O(nombre d'écritures distinctes)</em>, pas O(lignes) — un FEC 100 Mo
/// reste largement sous la limite §6.3 du cadrage.
/// </remarks>
internal sealed class TemporalContext
{
    // Indices des champs (parallèles à FecHeader.ExpectedColumns).
    private const int IdxJournalCode = 0;
    private const int IdxEcritureNum = 2;
    private const int IdxEcritureDate = 3;
    private const int IdxValidDate = 15;

    /// <summary>Plafond d'échantillon affiché dans le finding agrégé C08, pour ne pas saturer le rapport.</summary>
    private const int C08SampleSize = 10;

    private readonly ExercicePeriod? _exercice;
    private readonly Dictionary<EcritureKey, EcritureTemporalAggregate> _ecritures = new();

    /// <summary>
    /// Construit le contexte temporel. Si <paramref name="exercice"/> est
    /// <c>null</c>, la règle <see cref="Rules.C05"/> n'est pas évaluée — le
    /// CLI affiche alors une ligne d'information à l'utilisateur (cf. §6.2).
    /// </summary>
    public TemporalContext(ExercicePeriod? exercice)
    {
        _exercice = exercice;
    }

    /// <summary>
    /// Observe une ligne de données : alimente l'agrégat par écriture
    /// (<c>JournalCode</c>, <c>EcritureNum</c>) avec la première
    /// <c>EcritureDate</c> parsable et le drapeau « au moins une ligne validée ».
    /// </summary>
    /// <param name="fields">Champs déjà découpés (longueur ≥ 16 attendue, tolère plus court).</param>
    /// <param name="lineNumber">Numéro de ligne 1-indexé.</param>
    /// <param name="sink">Accumulateur de findings (non utilisé en streaming, réservé à
    /// d'éventuelles futures règles temporelles par-ligne nécessitant l'état du contexte).</param>
    public void Observe(string[] fields, long lineNumber, List<Finding> sink)
    {
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(sink);

        var journalCode = IdxJournalCode < fields.Length ? fields[IdxJournalCode] : string.Empty;
        var ecritureNum = IdxEcritureNum < fields.Length ? fields[IdxEcritureNum] : string.Empty;

        // Comme AccountingContext, on n'agrège pas les lignes dont la clef
        // d'écriture est inutilisable (déjà signalée par A07) — sinon des
        // lignes orphelines pollueraient les statistiques C05/C07/C08.
        if (string.IsNullOrWhiteSpace(journalCode) || string.IsNullOrWhiteSpace(ecritureNum))
        {
            return;
        }

        var ecritureDateRaw = IdxEcritureDate < fields.Length ? fields[IdxEcritureDate] : string.Empty;
        var validDateRaw = IdxValidDate < fields.Length ? fields[IdxValidDate] : string.Empty;

        FecDateParser.TryParse(ecritureDateRaw, out var ecritureDate);
        var ligneEstValidee = !string.IsNullOrWhiteSpace(validDateRaw);

        var key = new EcritureKey(journalCode, ecritureNum);
        if (!_ecritures.TryGetValue(key, out var aggregate))
        {
            aggregate = new EcritureTemporalAggregate(
                PremiereLigne: lineNumber,
                EcritureDate: ecritureDate == default ? null : ecritureDate,
                AAuMoinsUneLigneValidee: ligneEstValidee);
            _ecritures[key] = aggregate;
            return;
        }

        // On garde la première EcritureDate parsable rencontrée pour C07 :
        // les lignes d'une même écriture sont supposées partager EcritureDate,
        // toute divergence relèverait d'une règle hors MVP.
        var nouvelleEcritureDate = aggregate.EcritureDate
            ?? (ecritureDate == default ? null : ecritureDate);

        _ecritures[key] = aggregate with
        {
            EcritureDate = nouvelleEcritureDate,
            AAuMoinsUneLigneValidee = aggregate.AAuMoinsUneLigneValidee || ligneEstValidee,
        };
    }

    /// <summary>
    /// Émet les findings agrégés à émettre après lecture complète :
    /// <list type="number">
    /// <item><description>C05 — une entrée par écriture hors période d'exercice (si <see cref="ExercicePeriod"/> fourni) ;</description></item>
    /// <item><description>C07 — une entrée par paire d'écritures validées en violation de chronologie au sein du même journal ;</description></item>
    /// <item><description>C08 — une entrée agrégée recensant les écritures non validées (avec échantillon).</description></item>
    /// </list>
    /// L'ordre d'émission est stable et reproductible : C05/C08 par ordre
    /// d'apparition (première ligne croissante), C07 par journal puis par
    /// <c>EcritureNum</c> croissant.
    /// </summary>
    public void EmitFinalFindings(List<Finding> sink)
    {
        ArgumentNullException.ThrowIfNull(sink);

        // C05 — écritures hors période d'exercice (uniquement si l'option a été fournie).
        if (_exercice is not null)
        {
            foreach (var (key, aggregate) in _ecritures.OrderBy(kv => kv.Value.PremiereLigne))
            {
                if (aggregate.EcritureDate is not { } date)
                {
                    continue; // EcritureDate non parsable : C01 l'a déjà signalée.
                }

                if (_exercice.Contains(date))
                {
                    continue;
                }

                sink.Add(new Finding(
                    Rule: Rules.C05,
                    LineNumber: aggregate.PremiereLigne,
                    Message:
                        $"Écriture « {key.JournalCode} / {key.EcritureNum} » : EcritureDate "
                        + $"{date:yyyy-MM-dd} hors de la période d'exercice déclarée "
                        + $"[{_exercice.Debut:yyyy-MM-dd} ; {_exercice.Fin:yyyy-MM-dd}].",
                    Contexte: null));
            }
        }

        // C07 — chronologie des écritures validées au sein de chaque journal.
        // On regroupe par JournalCode, on filtre les écritures validées avec
        // EcritureDate parsable, on les trie par EcritureNum lexicographique
        // (la doctrine BOFiP postule que la numérotation séquentielle reflète
        // l'ordre chronologique), puis on signale toute paire (n, n+1) en
        // recul de date.
        var parJournal = _ecritures
            .Where(kv => kv.Value.AAuMoinsUneLigneValidee && kv.Value.EcritureDate is not null)
            .GroupBy(kv => kv.Key.JournalCode, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal);

        foreach (var groupe in parJournal)
        {
            var ordonnees = groupe
                .OrderBy(kv => kv.Key.EcritureNum, StringComparer.Ordinal)
                .ToList();

            for (var i = 1; i < ordonnees.Count; i++)
            {
                var precedente = ordonnees[i - 1];
                var courante = ordonnees[i];

                // Les EcritureDate sont garanties non null par le filtre ci-dessus.
                var datePrec = precedente.Value.EcritureDate!.Value;
                var dateCour = courante.Value.EcritureDate!.Value;

                if (dateCour < datePrec)
                {
                    sink.Add(new Finding(
                        Rule: Rules.C07,
                        LineNumber: courante.Value.PremiereLigne,
                        Message:
                            $"Journal « {groupe.Key} » : l'écriture validée n° {courante.Key.EcritureNum} "
                            + $"({dateCour:yyyy-MM-dd}) est antérieure à l'écriture validée n° "
                            + $"{precedente.Key.EcritureNum} ({datePrec:yyyy-MM-dd}). La numérotation "
                            + "séquentielle d'un journal doit refléter l'ordre chronologique des écritures.",
                        Contexte: null));
                }
            }
        }

        // C08 — finding agrégé : nombre d'écritures non validées + échantillon.
        var nonValidees = _ecritures
            .Where(kv => !kv.Value.AAuMoinsUneLigneValidee)
            .OrderBy(kv => kv.Value.PremiereLigne)
            .ToList();

        if (nonValidees.Count > 0)
        {
            var echantillon = nonValidees
                .Take(C08SampleSize)
                .Select(kv => $"{kv.Key.JournalCode}/{kv.Key.EcritureNum}");

            var suffixe = nonValidees.Count > C08SampleSize
                ? $" (échantillon des {C08SampleSize} premières ; "
                  + $"{nonValidees.Count - C08SampleSize} autres non listées)"
                : string.Empty;

            sink.Add(new Finding(
                Rule: Rules.C08,
                LineNumber: nonValidees[0].Value.PremiereLigne,
                Message:
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} écriture(s) sans ValidDate (non validée(s)) : {1}{2}.",
                        nonValidees.Count,
                        string.Join(", ", echantillon),
                        suffixe),
                Contexte: null));
        }
    }

    private readonly record struct EcritureKey(string JournalCode, string EcritureNum);

    private sealed record EcritureTemporalAggregate(
        long PremiereLigne,
        DateOnly? EcritureDate,
        bool AAuMoinsUneLigneValidee);
}
