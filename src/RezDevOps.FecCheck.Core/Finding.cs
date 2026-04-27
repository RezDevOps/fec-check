// © 2026 Rudy Rezaire / RezDevOps. Licence MIT — voir LICENSE.

namespace RezDevOps.FecCheck.Core;

/// <summary>
/// Anomalie détectée dans un FEC. Couple une règle violée à un emplacement
/// précis et à un message en langage humain. Un rapport <see cref="ValidationReport"/>
/// est composé de zéro ou plusieurs <see cref="Finding"/>.
/// </summary>
/// <param name="Rule">La règle violée. Sa <see cref="Core.Rule.Source"/> est citée dans le rapport.</param>
/// <param name="LineNumber">
/// Numéro de ligne (1-indexé) où l'anomalie a été détectée, ou <c>null</c> si la
/// règle s'applique au fichier dans son ensemble (ex : encodage, séparateur).
/// La ligne 1 est la ligne d'en-tête.
/// </param>
/// <param name="Message">
/// Description précise de l'anomalie, en français, destinée à un dirigeant TPE/PME
/// non-tech. Doit indiquer ce qui ne va pas et, si pertinent, ce qui était attendu.
/// </param>
/// <param name="Contexte">
/// Extrait du fichier (ligne brute, séquence d'octets…) à présenter à l'utilisateur
/// pour qu'il identifie l'anomalie sur son fichier. <c>null</c> si non pertinent.
/// </param>
public sealed record Finding(
    Rule Rule,
    long? LineNumber,
    string Message,
    string? Contexte = null);
