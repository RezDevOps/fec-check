// © 2026 Rudy Rezaire / RezDevOps. Licence MIT — voir LICENSE.

using System.Globalization;

namespace RezDevOps.FecCheck.Core;

/// <summary>
/// Parseur strict des dates FEC au format <c>AAAAMMJJ</c>. Source de vérité
/// unique pour l'interprétation des champs <c>EcritureDate</c>, <c>PieceDate</c>,
/// <c>ValidDate</c> et <c>DateLet</c> (cf. règles <see cref="Rules.C01"/> à
/// <see cref="Rules.C04"/>). Utilisé par <see cref="DataLineValidator"/> pour
/// la qualification par-ligne et par <see cref="TemporalContext"/> pour les
/// règles inter-lignes (<see cref="Rules.C05"/>, <see cref="Rules.C07"/>).
/// </summary>
/// <remarks>
/// Le format attendu par l'A. 47 A-1 LPF est <c>AAAAMMJJ</c> exactement
/// (8 chiffres, sans séparateur). On s'appuie sur
/// <see cref="DateOnly.TryParseExact(string, string, IFormatProvider?, DateTimeStyles, out DateOnly)"/>
/// pour rejeter les dates impossibles (ex : <c>20240230</c>, <c>20240431</c>).
/// La culture est <see cref="CultureInfo.InvariantCulture"/> pour ne pas
/// dépendre du <c>LANG</c> de l'utilisateur ; le style est
/// <see cref="DateTimeStyles.None"/> pour rester strict (pas d'espaces tolérés).
/// </remarks>
internal static class FecDateParser
{
    /// <summary>
    /// Format de date prescrit par l'arrêté du 29 juillet 2013 pour le FEC.
    /// </summary>
    public const string ExpectedFormat = "yyyyMMdd";

    /// <summary>
    /// Tente de parser un champ date FEC en <see cref="DateOnly"/>. Retourne
    /// <c>true</c> uniquement si la valeur est exactement 8 chiffres formant
    /// une date du calendrier grégorien valide. Un champ vide ou null retourne
    /// <c>false</c> sans <c>date</c> initialisée — l'appelant doit traiter le
    /// cas « champ vide » séparément (les règles C02, C03, C04 tolèrent un
    /// champ vide ; C01 exige une date présente).
    /// </summary>
    /// <param name="raw">Valeur brute du champ, telle que lue dans le FEC (peut être null).</param>
    /// <param name="date">Date parsée si la méthode retourne <c>true</c>.</param>
    public static bool TryParse(string? raw, out DateOnly date)
    {
        if (string.IsNullOrEmpty(raw))
        {
            date = default;
            return false;
        }

        return DateOnly.TryParseExact(
            raw,
            ExpectedFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);
    }

    /// <summary>
    /// Vrai si le champ est non vide et non parsable. Utilisé par les règles
    /// C01-C04 qui ne déclenchent que sur un champ rempli mais mal formé
    /// (un champ optionnel vide n'est pas une anomalie).
    /// </summary>
    public static bool IsFilledButInvalid(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        return !DateOnly.TryParseExact(
            raw,
            ExpectedFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _);
    }
}
