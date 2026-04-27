// © 2026 Rudy Rezaire / RezDevOps. Licence MIT — voir LICENSE.

namespace RezDevOps.FecCheck.Core;

/// <summary>
/// Métadonnées statiques de la bibliothèque. Sert de point d'entrée trivial
/// pour vérifier que l'assemblage est référencé correctement (J0).
/// La logique de validation viendra aux jalons J1, J2, J3.
/// </summary>
public static class FecCheckInfo
{
    /// <summary>Nom du produit, tel que présenté à l'utilisateur.</summary>
    public const string ProductName = "fec-check";

    /// <summary>
    /// Version courante du produit, source de vérité pour le CLI et le rapport.
    /// Mise à jour à chaque tag selon Semantic Versioning.
    /// </summary>
    public const string Version = "0.0.0";

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
