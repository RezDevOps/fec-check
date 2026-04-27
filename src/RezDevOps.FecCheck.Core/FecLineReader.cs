// © 2026 Rudy Rezaire / RezDevOps. Licence MIT — voir LICENSE.

using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace RezDevOps.FecCheck.Core;

/// <summary>
/// Lit un FEC ligne par ligne en streaming, en gardant trace des conventions
/// de fin de ligne effectivement rencontrées (CRLF / LF / CR isolé) pour
/// alimenter la règle <see cref="Rules.A06"/>.
/// </summary>
/// <remarks>
/// Implémentation byte-level délibérée : <see cref="StreamReader.ReadLine"/>
/// masque le caractère de fin de ligne, ce qui empêche de distinguer CRLF de LF.
/// On lit donc les octets bruts par chunks, on identifie la fin de ligne
/// par octets <c>0x0D</c> et <c>0x0A</c>, puis on décode la ligne complète
/// via l'<see cref="Encoding"/> fourni.
///
/// Cette approche est sûre pour UTF-8 et ISO-8859-15 car ni <c>0x0A</c> ni
/// <c>0x0D</c> n'apparaissent jamais comme octet de continuation de séquence
/// multi-octet (UTF-8 réserve les octets de continuation à <c>0x80–0xBF</c>,
/// ISO-8859-15 est mono-octet par construction).
/// </remarks>
internal sealed class FecLineReader : IDisposable
{
    private const int DefaultBufferSize = 8192;

    private readonly Stream _stream;
    private readonly Encoding _encoding;
    private readonly bool _leaveOpen;
    private readonly byte[] _buffer;
    private int _bufferPos;
    private int _bufferLen;
    private bool _eof;

    /// <summary>
    /// Numéro de la dernière ligne retournée par <see cref="TryReadLine"/>, 1-indexé.
    /// 0 tant qu'aucune ligne n'a été lue. La ligne 1 est l'en-tête.
    /// </summary>
    public long LineNumber { get; private set; }

    /// <summary>Nombre de fins de ligne CRLF rencontrées.</summary>
    public int CrlfCount { get; private set; }

    /// <summary>Nombre de fins de ligne LF seul rencontrées.</summary>
    public int LfOnlyCount { get; private set; }

    /// <summary>
    /// Nombre de fins de ligne CR seul (Mac classique). Très rare dans la pratique
    /// FEC ; comptabilisé séparément pour signaler une convention exotique.
    /// </summary>
    public int CrOnlyCount { get; private set; }

    /// <summary>
    /// <c>true</c> si la dernière ligne du fichier ne se termine pas par CR/LF
    /// (ex : fichier sans saut de ligne final). Information neutre vis-à-vis de A06.
    /// </summary>
    public bool LastLineHasNoEol { get; private set; }

    /// <summary>
    /// Construit un lecteur ligne-par-ligne pour le flux fourni.
    /// </summary>
    /// <param name="stream">Flux ouvert en lecture, positionné après l'éventuel BOM consommé par le détecteur.</param>
    /// <param name="encoding">Encodage à utiliser pour décoder les octets de chaque ligne.</param>
    /// <param name="leaveOpen">Si <c>true</c>, ne dispose pas <paramref name="stream"/> à la fermeture.</param>
    /// <param name="bufferSize">Taille du buffer interne. 8 ko par défaut.</param>
    public FecLineReader(
        Stream stream,
        Encoding encoding,
        bool leaveOpen = false,
        int bufferSize = DefaultBufferSize)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(encoding);
        if (bufferSize < 64)
        {
            throw new ArgumentOutOfRangeException(nameof(bufferSize), bufferSize, "Buffer trop petit.");
        }

        _stream = stream;
        _encoding = encoding;
        _leaveOpen = leaveOpen;
        _buffer = new byte[bufferSize];
    }

    /// <summary>
    /// Tente de lire la ligne suivante. Retourne <c>false</c> en fin de fichier.
    /// La ligne retournée n'inclut pas la séquence de fin de ligne.
    /// </summary>
    public bool TryReadLine([NotNullWhen(true)] out string? line)
    {
        // Buffer dynamique pour les octets de la ligne en cours. Un FEC moyen
        // a des lignes de 200 à 2000 caractères ; on commence à 256 pour limiter
        // les réallocations sans gaspiller pour des FEC compacts.
        var lineBytes = new List<byte>(256);
        var foundEol = false;

        while (!foundEol)
        {
            if (_bufferPos == _bufferLen && !RefillBuffer())
            {
                break; // EOF
            }

            var b = _buffer[_bufferPos++];
            if (b == 0x0A) // LF
            {
                LfOnlyCount++;
                foundEol = true;
            }
            else if (b == 0x0D) // CR
            {
                // Lecture anticipée : si CR suivi de LF, c'est CRLF (un seul EOL).
                if (_bufferPos == _bufferLen)
                {
                    RefillBuffer();
                }

                if (_bufferPos < _bufferLen && _buffer[_bufferPos] == 0x0A)
                {
                    _bufferPos++;
                    CrlfCount++;
                }
                else
                {
                    CrOnlyCount++;
                }

                foundEol = true;
            }
            else
            {
                lineBytes.Add(b);
            }
        }

        // EOF sans contenu accumulé : pas de ligne à retourner.
        if (!foundEol && lineBytes.Count == 0)
        {
            line = null;
            return false;
        }

        if (!foundEol)
        {
            LastLineHasNoEol = true;
        }

        LineNumber++;
        line = _encoding.GetString(lineBytes.ToArray());
        return true;
    }

    /// <summary>
    /// Calcule la convention de fin de ligne globale du fichier à partir des
    /// compteurs accumulés. À appeler une fois la lecture terminée.
    /// </summary>
    public DetectedLineEnding GetDetectedLineEnding()
    {
        var distinctConventions =
            (CrlfCount > 0 ? 1 : 0)
            + (LfOnlyCount > 0 ? 1 : 0)
            + (CrOnlyCount > 0 ? 1 : 0);

        return distinctConventions switch
        {
            0 => DetectedLineEnding.Aucune,
            > 1 => DetectedLineEnding.Mixte,
            _ when CrlfCount > 0 => DetectedLineEnding.Crlf,
            _ when LfOnlyCount > 0 => DetectedLineEnding.Lf,

            // CR seul : convention exotique (Mac classique). Considérée comme
            // « mixte » par rapport à la norme FEC qui prévoit CRLF ou LF.
            _ => DetectedLineEnding.Mixte,
        };
    }

    private bool RefillBuffer()
    {
        if (_eof)
        {
            return false;
        }

        _bufferLen = _stream.Read(_buffer, 0, _buffer.Length);
        _bufferPos = 0;
        if (_bufferLen == 0)
        {
            _eof = true;
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_leaveOpen)
        {
            _stream.Dispose();
        }
    }
}
