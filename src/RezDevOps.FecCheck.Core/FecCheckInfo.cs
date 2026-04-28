// © 2026 Rudy Rezaire / RezDevOps. Licence MIT — voir LICENSE.

namespace RezDevOps.FecCheck.Core;

/// <summary>
/// Métadonnées statiques de la bibliothèque. Source de vérité pour le nom
/// du produit et la version, consommée par le CLI et les rapports.
/// </summary>
public static class FecCheckInfo
{
    /// <summary>Nom du produit, tel que présenté à l'utilisateur.</summary>
    public const string ProductName = "fec-check";

    /// <summary>
    /// Version courante du produit, source de vérité pour le CLI et le rapport.
    /// Mise à jour à chaque tag selon Semantic Versioning.
    /// </summary>
    /// <remarks>
    /// Historique :
    /// <list type="bullet">
    /// <item><description>0.4.0 — J4 : rapports JSON (schéma v1 figé) et Markdown finalisés, flags <c>--output-md</c> et <c>--output-json</c>.</description></item>
    /// <item><description>0.3.0 — J3 : Famille C (cohérence temporelle) opérationnelle, option <c>--exercice</c>.</description></item>
    /// <item><description>0.2.0 — J2 : Famille B (cohérence comptable) opérationnelle.</description></item>
    /// <item><description>0.1.0 — J1 : Famille A (conformité de format) opérationnelle.</description></item>
    /// <item><description>0.0.0 — J0 : cadrage initial du repo, aucune règle implémentée.</description></item>
    /// </list>
    /// </remarks>
    public const string Version = "0.4.0";

    /// <summary>
    /// Famille de règles de validation, telle que définie dans le cadrage §4.1.
    /// </summary>
    public enum RuleFamily
    {
        /// <summary>Conformité de format (encodage, séparateur, 18 colonnes, fin de ligne).</summary>
        Format,

        /// <summary>Cohérence comptable (équilibres débit/crédit, formats numériques).</summary>
        Accounting,

        /// <summary>Cohérence temporelle (dates, chronologie, validation).</summary>
        Temporal,
    }
}
