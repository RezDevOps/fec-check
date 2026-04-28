// © 2026 Rudy Rezaire / RezDevOps. Licence MIT — voir LICENSE.

using System.Text;

namespace RezDevOps.FecCheck.Core;

/// <summary>
/// Helpers d'écriture des rapports vers un fichier disque avec un encodage
/// homogène (UTF-8 sans BOM, fin de ligne LF). Utilisé par le CLI pour les
/// flags <c>--output-md</c> et <c>--output-json</c>.
/// </summary>
/// <remarks>
/// L'encodage UTF-8 sans BOM est imposé pour deux raisons :
/// <list type="bullet">
/// <item><description>Cohérence avec la pratique majoritaire des outils CI / pipelines.</description></item>
/// <item><description>Évite qu'un consommateur naïf qui parse octet à octet (jq, grep, etc.) trébuche sur le BOM.</description></item>
/// </list>
/// La fin de ligne est forcée en LF pour rester déterministe quel que soit
/// l'OS d'exécution — un FEC peut être analysé sur Windows et le rapport
/// commité dans un repo Linux sans devoir gérer une normalisation côté git.
/// </remarks>
public static class ReportFileWriter
{
    /// <summary>Encodage standard des fichiers de rapport : UTF-8 sans BOM.</summary>
    public static readonly Encoding ReportEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    /// Écrit le rapport JSON correspondant à <paramref name="report"/> dans le
    /// fichier <paramref name="path"/>. Crée les répertoires parents si
    /// nécessaire. Le fichier est écrasé s'il existe déjà.
    /// </summary>
    public static void WriteJson(string path, ValidationReport report, ReportEnvironment environment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(environment);

        EnsureParentDirectory(path);

        using var writer = new StreamWriter(path, append: false, ReportEncoding)
        {
            NewLine = "\n",
        };
        JsonReportWriter.Write(report, environment, writer);
        writer.WriteLine();
    }

    /// <summary>
    /// Écrit le rapport Markdown correspondant à <paramref name="report"/>
    /// dans le fichier <paramref name="path"/>. Crée les répertoires parents
    /// si nécessaire. Le fichier est écrasé s'il existe déjà.
    /// </summary>
    public static void WriteMarkdown(string path, ValidationReport report, ReportEnvironment environment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(environment);

        EnsureParentDirectory(path);

        using var writer = new StreamWriter(path, append: false, ReportEncoding)
        {
            NewLine = "\n",
        };
        MarkdownReportWriter.Write(report, environment, writer);
    }

    private static void EnsureParentDirectory(string path)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }
}
