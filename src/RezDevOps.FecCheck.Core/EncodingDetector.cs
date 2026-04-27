// © 2026 Rudy Rezaire / RezDevOps. Licence MIT — voir LICENSE.

using System.Text;

namespace RezDevOps.FecCheck.Core;

/// <summary>
/// Détecte l'encodage d'un FEC parmi l'ensemble autorisé par
/// l'A. 47 A-1 LPF : ASCII (sous-ensemble UTF-8), UTF-8 avec ou sans BOM,
/// et ISO-8859-15. Tout autre encodage déclenche une violation de la
/// règle <see cref="Rules.A01"/>.
/// </summary>
/// <remarks>
/// La détection est volontairement simple et reproductible :
/// <list type="number">
/// <item><description>Lecture des premiers octets pour repérer un BOM connu.</description></item>
/// <item><description>Heuristique UTF-16/32 sans BOM (densité de NUL bytes) — déclenche A01.</description></item>
/// <item><description>Tentative de décodage UTF-8 strict sur l'échantillon ; succès ⇒ UTF-8.</description></item>
/// <item><description>Sinon, repli sur ISO-8859-15 (single-byte, ne peut pas échouer).</description></item>
/// </list>
/// Cette stratégie privilégie la robustesse aux faux positifs : un fichier
/// ASCII sera classé UTF-8, ce qui est compatible avec la norme FEC.
/// </remarks>
internal static class EncodingDetector
{
    /// <summary>
    /// Taille de l'échantillon utilisé pour la détection. 4 ko couvrent largement
    /// la première ligne d'un FEC (en-tête de ~250 caractères) plus une marge
    /// pour les heuristiques.
    /// </summary>
    public const int SampleSize = 4096;

    /// <summary>
    /// Garantit que l'encodage ISO-8859-15 est disponible via <see cref="Encoding.GetEncoding(int)"/>.
    /// Idempotent et sans coût après le premier appel.
    /// </summary>
    public static void EnsureCodePagesRegistered()
    {
        // L'enregistrement est silencieux si déjà fait. Utile à appeler depuis
        // tout point d'entrée (FecValidator, tests) sans coordination.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    /// Détecte l'encodage du flux fourni et le retourne avec l'objet
    /// <see cref="Encoding"/> à utiliser pour la lecture du reste du fichier.
    /// Le flux <paramref name="input"/> est repositionné <em>après l'éventuel
    /// BOM consommé</em> à la fin de l'appel, prêt à être passé tel quel à
    /// <see cref="FecLineReader"/>. Doit être <see cref="Stream.CanSeek"/>.
    /// </summary>
    /// <param name="input">Flux ouvert en lecture, seekable. Non disposé par cette méthode.</param>
    /// <returns>
    /// Un tuple <c>(detected, encoding, bomLength)</c>. Si <c>detected</c> vaut
    /// <see cref="DetectedEncoding.Inconnu"/>, <c>encoding</c> est <c>null</c>
    /// et A01 doit être rapportée comme violation bloquante. <c>bomLength</c>
    /// est le nombre d'octets de BOM consommés (0 si pas de BOM, 3 pour UTF-8 BOM).
    /// </returns>
    /// <exception cref="ArgumentException">Si <paramref name="input"/> n'est pas seekable.</exception>
    public static (DetectedEncoding Detected, Encoding? Encoding, int BomLength) Detect(Stream input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!input.CanSeek)
        {
            throw new ArgumentException(
                "Le flux doit être seekable pour détecter l'encodage.",
                nameof(input));
        }

        EnsureCodePagesRegistered();

        var origin = input.Position;
        Span<byte> buffer = stackalloc byte[SampleSize];
        var read = ReadFully(input, buffer);

        var sample = buffer[..read];

        // 1. BOM UTF-8 (toléré par A01) — on consomme les 3 octets pour que
        //    les lecteurs en aval ne retrouvent pas le BOM en début de ligne.
        if (sample.Length >= 3 && sample[0] == 0xEF && sample[1] == 0xBB && sample[2] == 0xBF)
        {
            input.Position = origin + 3;
            return (DetectedEncoding.Utf8WithBom, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), 3);
        }

        // 2. BOMs interdits (UTF-16, UTF-32) — déclenche A01.
        input.Position = origin;
        if (HasForbiddenBom(sample))
        {
            return (DetectedEncoding.Inconnu, null, 0);
        }

        // 3. Heuristique UTF-16/32 sans BOM : densité anormale de NUL bytes.
        //    Un FEC en ISO-8859-15 ou UTF-8 ne contient jamais d'octet 0x00.
        if (HasSuspiciousNulDensity(sample))
        {
            return (DetectedEncoding.Inconnu, null, 0);
        }

        // 4. UTF-8 strict.
        var utf8Strict = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        try
        {
            _ = utf8Strict.GetCharCount(sample);
            return (DetectedEncoding.Utf8, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), 0);
        }
        catch (DecoderFallbackException)
        {
            // Repli ISO-8859-15.
        }

        // 5. ISO-8859-15 — single-byte, accepte n'importe quelle séquence.
        var iso = Encoding.GetEncoding(28605);
        return (DetectedEncoding.Iso8859_15, iso, 0);
    }

    private static bool HasForbiddenBom(ReadOnlySpan<byte> sample)
    {
        // UTF-32 LE : FF FE 00 00 (à tester avant UTF-16 LE qui est un préfixe).
        if (sample.Length >= 4 && sample[0] == 0xFF && sample[1] == 0xFE && sample[2] == 0x00 && sample[3] == 0x00)
        {
            return true;
        }

        // UTF-32 BE : 00 00 FE FF.
        if (sample.Length >= 4 && sample[0] == 0x00 && sample[1] == 0x00 && sample[2] == 0xFE && sample[3] == 0xFF)
        {
            return true;
        }

        // UTF-16 LE : FF FE.
        if (sample.Length >= 2 && sample[0] == 0xFF && sample[1] == 0xFE)
        {
            return true;
        }

        // UTF-16 BE : FE FF.
        if (sample.Length >= 2 && sample[0] == 0xFE && sample[1] == 0xFF)
        {
            return true;
        }

        return false;
    }

    private static bool HasSuspiciousNulDensity(ReadOnlySpan<byte> sample)
    {
        if (sample.Length < 16)
        {
            return false; // Échantillon trop court pour conclure.
        }

        var nulCount = 0;
        foreach (var b in sample)
        {
            if (b == 0x00)
            {
                nulCount++;
            }
        }

        // Plus de 10 % de NUL est anormal pour un FEC text/single-byte ou UTF-8.
        // UTF-16 LE en texte ASCII produit ~50 % de NUL, donc seuil très conservateur.
        return nulCount * 10 > sample.Length;
    }

    private static int ReadFully(Stream stream, Span<byte> buffer)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var read = stream.Read(buffer[total..]);
            if (read == 0)
            {
                break;
            }

            total += read;
        }

        return total;
    }
}
