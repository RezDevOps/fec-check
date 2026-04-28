// © 2026 Rudy Rezaire / RezDevOps. Licence MIT — voir LICENSE.

namespace RezDevOps.FecCheck.Core;

/// <summary>
/// Métadonnées d'environnement portées par la couche CLI au moment d'écrire un
/// rapport. Le <see cref="ValidationReport"/> seul ne sait pas, par construction,
/// d'où vient le flux qu'il a analysé (cf. cadrage §6.2 — Core sans I/O).
/// Cette structure transporte ces informations contextuelles vers les writers.
/// </summary>
/// <remarks>
/// Type figé dès <c>v0.4.0</c> : les writers JSON et Markdown s'appuient
/// dessus, et son contenu est exposé dans les fichiers de rapport. Toute
/// évolution doit être additive (nouveau champ optionnel) pour préserver le
/// contrat de schéma JSON v1 (cf. <c>docs/json-schema.md</c>).
/// </remarks>
/// <param name="ProductName">Nom du produit (typiquement <see cref="FecCheckInfo.ProductName"/>).</param>
/// <param name="ProductVersion">Version du produit (typiquement <see cref="FecCheckInfo.Version"/>).</param>
/// <param name="FilePath">
/// Chemin du FEC tel que fourni à la CLI, ou <c>null</c> si l'analyse a été
/// faite sur un flux non identifié (consommation programmatique).
/// </param>
/// <param name="GeneratedAt">
/// Horodatage de génération du rapport. Injecté pour rester déterministe sous
/// test ; en exécution réelle, la CLI passe <see cref="DateTimeOffset.UtcNow"/>.
/// </param>
/// <param name="Exercice">
/// Période d'exercice telle qu'effectivement utilisée pour évaluer la règle
/// <see cref="Rules.C05"/>, ou <c>null</c> si la règle n'a pas été évaluée.
/// </param>
public sealed record ReportEnvironment(
    string ProductName,
    string ProductVersion,
    string? FilePath,
    DateTimeOffset GeneratedAt,
    ExercicePeriod? Exercice);
