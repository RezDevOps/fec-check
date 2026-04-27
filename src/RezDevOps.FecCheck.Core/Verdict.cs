// © 2026 Rudy Rezaire / RezDevOps. Licence MIT — voir LICENSE.

namespace RezDevOps.FecCheck.Core;

/// <summary>
/// Verdict global d'une analyse FEC, dérivé de l'ensemble des
/// <see cref="Finding"/> rapportés. Mappé sur les codes de retour processus
/// du cadrage §4.2 : 0 = Conforme, 1 = ConformeAvecAvertissements, 2 = NonConforme.
/// </summary>
/// <remarks>
/// Le code de retour 3 (« Erreur d'exécution ») n'apparaît pas ici : il est
/// produit par la couche CLI face à une exception I/O, pas par la validation.
/// </remarks>
public enum Verdict
{
    /// <summary>Aucune anomalie. Code de retour 0.</summary>
    Conforme = 0,

    /// <summary>Uniquement des avertissements. Code de retour 1.</summary>
    ConformeAvecAvertissements = 1,

    /// <summary>Au moins une erreur ou règle bloquante violée. Code de retour 2.</summary>
    NonConforme = 2,
}
