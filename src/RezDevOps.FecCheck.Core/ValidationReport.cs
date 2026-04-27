// © 2026 Rudy Rezaire / RezDevOps. Licence MIT — voir LICENSE.

namespace RezDevOps.FecCheck.Core;

/// <summary>
/// Encodage de caractères détecté par <c>FecValidator</c> au moment de la
/// lecture du fichier. Sert au rapport pour expliciter à l'utilisateur ce
/// que l'outil a réellement vu, et à départager les anomalies de la règle A01.
/// </summary>
public enum DetectedEncoding
{
    /// <summary>UTF-8 sans BOM, ou ASCII pur (sous-ensemble strict d'UTF-8).</summary>
    Utf8,

    /// <summary>UTF-8 avec marqueur BOM en tête de fichier (toléré, cf. règle A01).</summary>
    Utf8WithBom,

    /// <summary>ISO-8859-15 (Latin-9), tel qu'autorisé par l'A. 47 A-1 LPF.</summary>
    Iso8859_15,

    /// <summary>Aucun des encodages autorisés n'a permis un décodage propre.</summary>
    Inconnu,
}

/// <summary>
/// Convention de fin de ligne détectée dans le fichier. Un FEC valide doit
/// utiliser CRLF ou LF de manière homogène (règle A06).
/// </summary>
public enum DetectedLineEnding
{
    /// <summary>Fin de ligne <c>\r\n</c> (Windows), homogène sur tout le fichier.</summary>
    Crlf,

    /// <summary>Fin de ligne <c>\n</c> (Unix), homogène sur tout le fichier.</summary>
    Lf,

    /// <summary>Mélange de CRLF et de LF dans le même fichier (déclenche A06).</summary>
    Mixte,

    /// <summary>Aucune fin de ligne détectée (fichier vide ou tout sur une seule ligne).</summary>
    Aucune,
}

/// <summary>
/// Rapport d'analyse complet d'un FEC. Regroupe le verdict global, l'ensemble
/// des anomalies détectées et les caractéristiques effectives du fichier
/// (encodage, séparateur, fin de ligne) pour exposition à l'utilisateur.
/// </summary>
/// <remarks>
/// Cette structure est l'API publique stable du Core, figée à v0.1.0. Les
/// jalons J2 et J3 ajoutent des règles dans <see cref="Findings"/> sans
/// modifier la forme du rapport, garantissant la non-régression du contrat
/// pour les futurs consommateurs (back-office, web, scripts CI).
/// </remarks>
/// <param name="Verdict">Verdict global, dérivé de la sévérité maximale présente dans <see cref="Findings"/>.</param>
/// <param name="Findings">Anomalies détectées, dans l'ordre de leur découverte (en général : règles globales puis lignes croissantes).</param>
/// <param name="EncodageDetecte">Encodage effectif du fichier tel que reconnu par l'analyseur.</param>
/// <param name="SeparateurDetecte">Séparateur de champ effectif (<c>'\t'</c> ou <c>'|'</c>), ou <c>null</c> si non déterminable.</param>
/// <param name="FinDeLigneDetectee">Convention de fin de ligne détectée dans le fichier.</param>
/// <param name="LignesLues">Nombre total de lignes lues (en-tête comprise). 0 si le fichier est vide.</param>
public sealed record ValidationReport(
    Verdict Verdict,
    IReadOnlyList<Finding> Findings,
    DetectedEncoding EncodageDetecte,
    char? SeparateurDetecte,
    DetectedLineEnding FinDeLigneDetectee,
    long LignesLues)
{
    /// <summary>
    /// Construit un verdict à partir d'une liste de <see cref="Finding"/> selon
    /// la règle simple : la sévérité maximale présente fixe le verdict.
    /// Aucune anomalie → <see cref="Verdict.Conforme"/>.
    /// </summary>
    public static Verdict ComputeVerdict(IEnumerable<Finding> findings)
    {
        ArgumentNullException.ThrowIfNull(findings);

        var maxSeverity = (Severity?)null;
        foreach (var f in findings)
        {
            if (maxSeverity is null || f.Rule.Severity > maxSeverity)
            {
                maxSeverity = f.Rule.Severity;
            }
        }

        return maxSeverity switch
        {
            null => Verdict.Conforme,
            Severity.Avertissement => Verdict.ConformeAvecAvertissements,
            Severity.Erreur or Severity.Bloquante => Verdict.NonConforme,
            _ => Verdict.NonConforme,
        };
    }
}
