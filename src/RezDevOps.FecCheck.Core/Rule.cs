// © 2026 Rudy Rezaire / RezDevOps. Licence MIT — voir LICENSE.

namespace RezDevOps.FecCheck.Core;

/// <summary>
/// Définition d'une règle de validation FEC. L'identifiant est stable et
/// citable dans les rapports ; la source réglementaire est obligatoire pour
/// que l'utilisateur puisse remonter au texte de référence.
/// </summary>
/// <param name="Id">Identifiant stable au format <c>&lt;Famille&gt;&lt;NN&gt;</c> (ex : <c>A01</c>).</param>
/// <param name="Famille">Famille à laquelle la règle appartient (cf. cadrage §4.1).</param>
/// <param name="Severity">Effet d'une violation sur le verdict.</param>
/// <param name="Libelle">Libellé court de la règle, en français, tel qu'affiché dans le rapport.</param>
/// <param name="Source">
/// Référence réglementaire ou doctrinale exigible (ex : <c>A. 47 A-1 LPF</c>,
/// <c>BOI-CF-IOR-60-40-20</c>, <c>PCG</c>). Citée telle quelle dans le rapport.
/// </param>
public sealed record Rule(
    string Id,
    FecCheckInfo.RuleFamily Famille,
    Severity Severity,
    string Libelle,
    string Source);
