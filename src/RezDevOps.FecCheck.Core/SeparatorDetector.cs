// © 2026 Rudy Rezaire / RezDevOps. Licence MIT — voir LICENSE.

namespace RezDevOps.FecCheck.Core;

/// <summary>
/// Détecte le séparateur de champs d'un FEC à partir de sa première ligne
/// (en-tête). L'A. 47 A-1 LPF autorise tabulation <c>\t</c> ou pipe <c>|</c>,
/// le séparateur retenu doit ensuite être homogène sur tout le fichier
/// (cf. règle <see cref="Rules.A02"/>, validée ailleurs).
/// </summary>
internal static class SeparatorDetector
{
    /// <summary>Caractère tabulation, séparateur le plus courant dans les FEC.</summary>
    public const char Tabulation = '\t';

    /// <summary>Caractère pipe, alternative autorisée par la norme FEC.</summary>
    public const char Pipe = '|';

    /// <summary>
    /// Tente de détecter le séparateur utilisé dans la ligne d'en-tête. Retourne
    /// <c>null</c> si le séparateur ne peut pas être déterminé (ligne sans
    /// séparateur reconnu, ou les deux séparateurs présents — ambiguïté qui
    /// se résoudra via les règles A03/A04).
    /// </summary>
    /// <param name="headerLine">Première ligne du fichier, sans la fin de ligne.</param>
    public static char? Detect(string headerLine)
    {
        ArgumentNullException.ThrowIfNull(headerLine);

        var tabCount = 0;
        var pipeCount = 0;
        foreach (var c in headerLine)
        {
            if (c == Tabulation)
            {
                tabCount++;
            }
            else if (c == Pipe)
            {
                pipeCount++;
            }
        }

        // Cas standard : une seule sorte de séparateur présente.
        if (tabCount > 0 && pipeCount == 0)
        {
            return Tabulation;
        }

        if (pipeCount > 0 && tabCount == 0)
        {
            return Pipe;
        }

        // Ambiguïté (les deux ou aucun) : non décidable ici. Les règles A02/A03
        // remonteront l'anomalie sur la base d'autres signaux.
        return null;
    }
}
