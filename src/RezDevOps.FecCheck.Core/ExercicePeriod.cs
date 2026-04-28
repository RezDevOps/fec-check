// © 2026 Rudy Rezaire / RezDevOps. Licence MIT — voir LICENSE.

namespace RezDevOps.FecCheck.Core;

/// <summary>
/// Période d'exercice comptable contre laquelle les <c>EcritureDate</c> sont
/// confrontées par la règle <see cref="Rules.C05"/>. Les bornes sont incluses
/// (toute écriture dont l'<c>EcritureDate</c> appartient à <c>[Debut, Fin]</c>
/// est dans la période).
/// </summary>
/// <remarks>
/// Cette structure fait partie de l'API publique du Core : le CLI (et tout
/// futur consommateur, p. ex. un back-office) parse l'option <c>--exercice</c>
/// et instancie ce record pour le passer à <see cref="FecValidator.Validate(Stream, ExercicePeriod?)"/>.
/// </remarks>
/// <param name="Debut">Premier jour de l'exercice (inclus).</param>
/// <param name="Fin">Dernier jour de l'exercice (inclus).</param>
public sealed record ExercicePeriod(DateOnly Debut, DateOnly Fin)
{
    /// <summary>
    /// Construit une <see cref="ExercicePeriod"/> en validant que la borne
    /// de début n'est pas postérieure à la borne de fin. Lève une
    /// <see cref="ArgumentException"/> en cas d'incohérence.
    /// </summary>
    public static ExercicePeriod Create(DateOnly debut, DateOnly fin)
    {
        if (debut > fin)
        {
            throw new ArgumentException(
                $"La date de début d'exercice ({debut:yyyy-MM-dd}) doit être antérieure ou égale "
                + $"à la date de fin ({fin:yyyy-MM-dd}).",
                nameof(debut));
        }

        return new ExercicePeriod(debut, fin);
    }

    /// <summary>
    /// Vrai si <paramref name="date"/> est dans <c>[Debut, Fin]</c> (bornes incluses).
    /// </summary>
    public bool Contains(DateOnly date) => date >= Debut && date <= Fin;
}
