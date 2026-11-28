using System.Text;
using static TerraFluent.Barcode.Encoding.QrCodeEncoder;

namespace TerraFluent.Barcode.Encoding;

/// <summary>
/// Splits a value into numeric, alphanumeric and byte segments with the
/// fewest bits for the small QR family members (Micro QR and rMQR), whose
/// mode-indicator and character-count widths depend on the symbol size.
/// </summary>
internal static class CompactQrSegmenter
{
    private static readonly Mode[] Modes = { Mode.Numeric, Mode.Alphanumeric, Mode.Byte };

    /// <summary>A run of the value in one mode: its character (or byte) count and payload bits.</summary>
    internal readonly struct Run
    {
        public Run(Mode mode, int count, BitBuffer bits)
        {
            Mode = mode;
            Count = count;
            Bits = bits;
        }

        public Mode Mode { get; }

        public int Count { get; }

        public BitBuffer Bits { get; }
    }

    /// <summary>
    /// Segments <paramref name="value"/>. <paramref name="headerBits"/> gives the
    /// mode indicator plus character-count width for a mode, or -1 when the
    /// symbol does not support it. <paramref name="bytes"/> are the byte-mode
    /// bytes of each code point. Returns <c>null</c> when some character fits
    /// no available mode.
    /// </summary>
    public static List<Run>? Split(string value, Func<Mode, int> headerBits, Func<string, byte[]> bytes)
    {
        var starts = new List<int>(value.Length + 1);
        for (var i = 0; i < value.Length; i++)
        {
            starts.Add(i);
            if (char.IsHighSurrogate(value[i]) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1])) i++;
        }

        starts.Add(value.Length);
        var n = starts.Count - 1;
        var byteMode = headerBits(Mode.Byte) >= 0;
        var byteLengths = new int[n];
        if (byteMode)
        {
            for (var i = 0; i < n; i++) byteLengths[i] = bytes(value.Substring(starts[i], starts[i + 1] - starts[i])).Length;
        }

        // Costs in sixths of a bit: numeric 10 bits per 3 characters, alphanumeric 11 per 2, byte 8 per byte.
        const int Inf = int.MaxValue / 4;
        var cost = new int[3];
        var next = new int[3];
        var from = new int[n, 3];
        for (var i = 0; i < n; i++)
        {
            for (var m = 0; m < 3; m++)
            {
                next[m] = Inf;
                from[i, m] = -1;
                var header = headerBits(Modes[m]);
                if (header < 0 || !Allowed(Modes[m], value, starts[i], starts[i + 1] - starts[i])) continue;
                var charCost = Modes[m] switch
                {
                    Mode.Numeric => 20,
                    Mode.Alphanumeric => 33,
                    _ => 48 * byteLengths[i],
                };
                for (var pm = 0; pm < 3; pm++)
                {
                    var previous = i == 0 ? 0 : cost[pm];
                    if (previous >= Inf) continue;
                    var total = previous + (i > 0 && pm == m ? 0 : header * 6) + charCost;
                    if (total < next[m])
                    {
                        next[m] = total;
                        from[i, m] = pm;
                    }
                }
            }

            (cost, next) = (next, cost);
        }

        var result = new List<Run>();
        if (n == 0) return result;
        var best = 0;
        for (var m = 1; m < 3; m++)
        {
            if (cost[m] < cost[best]) best = m;
        }

        if (cost[best] >= Inf) return null;
        var modes = new int[n];
        for (var i = n - 1; i >= 0; i--)
        {
            modes[i] = best;
            best = from[i, best];
        }

        var start = 0;
        for (var i = 1; i <= n; i++)
        {
            if (i < n && modes[i] == modes[start]) continue;
            result.Add(Build(Modes[modes[start]], value.Substring(starts[start], starts[i] - starts[start]), bytes));
            start = i;
        }

        return result;
    }

    private static bool Allowed(Mode mode, string value, int start, int length) => mode switch
    {
        Mode.Numeric => length == 1 && value[start] >= '0' && value[start] <= '9',
        Mode.Alphanumeric => length == 1 && AlphanumericCharset.IndexOf(value[start]) >= 0,
        _ => true,
    };

    private static Run Build(Mode mode, string text, Func<string, byte[]> bytes)
    {
        var bits = new BitBuffer();
        switch (mode)
        {
            case Mode.Numeric:
                for (var i = 0; i < text.Length; i += 3)
                {
                    var n = Math.Min(3, text.Length - i);
                    var v = 0;
                    for (var j = i; j < i + n; j++) v = v * 10 + (text[j] - '0');
                    bits.Append(v, n * 3 + 1);
                }

                return new Run(mode, text.Length, bits);

            case Mode.Alphanumeric:
                for (var i = 0; i + 1 < text.Length; i += 2)
                {
                    bits.Append(AlphanumericCharset.IndexOf(text[i]) * 45 + AlphanumericCharset.IndexOf(text[i + 1]), 11);
                }

                if (text.Length % 2 == 1) bits.Append(AlphanumericCharset.IndexOf(text[text.Length - 1]), 6);
                return new Run(mode, text.Length, bits);

            default:
                var data = bytes(text);
                foreach (var b in data) bits.Append(b, 8);
                return new Run(mode, data.Length, bits);
        }
    }

    /// <summary>ISO-8859-1 bytes, the byte-mode default for readers; throws for other characters.</summary>
    public static byte[] Latin1(string value, string symbology)
    {
        var result = new byte[value.Length];
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] > 0xFF)
            {
                throw new ArgumentException(
                    $"{symbology} encodes bytes as ISO-8859-1 (Latin-1) and has no ECI for UTF-8; '{value[i]}' is outside it. Use a QR code or rMQR for other characters.",
                    nameof(value));
            }

            result[i] = (byte)value[i];
        }

        return result;
    }

    /// <summary>Strict UTF-8 bytes.</summary>
    public static byte[] Utf8(string value)
    {
        try
        {
            return new UTF8Encoding(false, true).GetBytes(value);
        }
        catch (EncoderFallbackException ex)
        {
            throw new ArgumentException("The value contains an unpaired surrogate character and is not valid Unicode text.", nameof(value), ex);
        }
    }

    /// <summary>
    /// Pads <paramref name="buffer"/> to <paramref name="capacityBits"/>: terminator,
    /// zero bits to a codeword boundary, then alternating 0xEC/0x11 pad codewords.
    /// A final half codeword (Micro QR M1 and M3) is filled with zeros.
    /// </summary>
    public static void Pad(BitBuffer buffer, int capacityBits, int terminatorBits)
    {
        buffer.Append(0, Math.Min(terminatorBits, capacityBits - buffer.Count));
        buffer.Append(0, Math.Min((8 - buffer.Count % 8) % 8, capacityBits - buffer.Count));
        for (var pad = 0xEC; buffer.Count < capacityBits; pad ^= 0xEC ^ 0x11)
        {
            if (capacityBits - buffer.Count >= 8) buffer.Append(pad, 8);
            else buffer.Append(0, capacityBits - buffer.Count);
        }
    }
}
