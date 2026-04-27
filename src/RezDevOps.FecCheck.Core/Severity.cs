// © 2026 Rudy Rezaire / RezDevOps. Licence MIT — voir LICENSE.

namespace RezDevOps.FecCheck.Core;

/// <summary>
/// Sévérité d'une règle de validation. Détermine l'effet d'une violation sur
/// le verdict global et l'éventuel arrêt anticipé de l'analyse.
/// </summary>
/// <remarks>
/// Niveaux conformes au cadrage <c>11_FEC_CHECK_CADRAGE.md</c> §4 et au
/// document <c>docs/regles.md</c> du repo. L'ordre numérique est volontaire :
/// plus la valeur est élevée, plus la violation est grave.
/// </remarks>
public enum Severity
{
    /// <summary>
    /// Anomalie signalée mais le fichier reste considéré conforme. Code de retour 1.
    /// </summary>
    Avertissement = 0,

    /// <summary>
    /// Anomalie qui rend le fichier non conforme. Code de retour 2. L'analyse
    /// se poursuit pour rapporter le maximum d'anomalies en un seul passage.
    /// </summary>
    Erreur = 1,

    /// <summary>
    /// Anomalie qui empêche la suite de l'analyse (ex : fichier illisible,
    /// en-tête absent). Code de retour 2. L'analyse s'arrête immédiatement.
    /// </summary>
    Bloquante = 2,
}
