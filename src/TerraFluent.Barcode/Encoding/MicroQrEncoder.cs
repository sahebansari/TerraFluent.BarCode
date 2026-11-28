using static TerraFluent.Barcode.Encoding.QrCodeEncoder;

namespace TerraFluent.Barcode.Encoding;

/// <summary>
/// Micro QR code (ISO/IEC 18004), versions M1 to M4: one finder pattern,
/// 11 to 17 modules square, for short data on small items. Numeric,
/// alphanumeric and byte (ISO-8859-1) segments are mixed for the fewest bits;
/// the smallest version that fits at the requested error-correction level is
/// used, and the best of the four masks is chosen by the standard's
/// edge-darkness rule.
/// </summary>
internal static class MicroQrEncoder
{
    public const int QuietZone = 2;

    /// <summary>Data characters of the largest symbol (M4-L, numeric).</summary>
    public const int MaxNumericLength = 35;

    private sealed class SymbolInfo
    {
        public SymbolInfo(int number, int version, QrErrorCorrectionLevel level, int dataBits, int eccCodewords)
        {
            Number = number;
            Version = version;
            Level = level;
            DataBits = dataBits;
            EccCodewords = eccCodewords;
        }

        /// <summary>The 3-bit symbol number written in the format information.</summary>
        public int Number { get; }

        public int Version { get; }

        public QrErrorCorrectionLevel Level { get; }

        /// <summary>Data capacity in bits; M1 and M3 end with a 4-bit codeword.</summary>
        public int DataBits { get; }

        public int EccCodewords { get; }
    }

    // ISO/IEC 18004:2015 Table 7 (capacities) and Table 13 (symbol numbers). M1 has error detection only; it is offered at level L.
    private static readonly SymbolInfo[] Symbols =
    {
        new(0, 1, QrErrorCorrectionLevel.L, 20, 2),
        new(1, 2, QrErrorCorrectionLevel.L, 40, 5),
        new(2, 2, QrErrorCorrectionLevel.M, 32, 6),
        new(3, 3, QrErrorCorrectionLevel.L, 84, 6),
        new(4, 3, QrErrorCorrectionLevel.M, 68, 8),
        new(5, 4, QrErrorCorrectionLevel.L, 128, 8),
        new(6, 4, QrErrorCorrectionLevel.M, 112, 10),
        new(7, 4, QrErrorCorrectionLevel.Q, 80, 14),
    };

    // Mask patterns 00-11 are QR masks 001, 100, 110 and 111.
    private static readonly int[] QrMasks = { 1, 4, 6, 7 };

    public static BarcodeMatrix Encode(string value, QrErrorCorrectionLevel level, int? fixedMask = null)
    {
        if (level == QrErrorCorrectionLevel.H)
        {
            throw new ArgumentException("Micro QR has error-correction levels L, M and Q only (Q needs version M4). Use a QR code for level H.", nameof(level));
        }

        Func<string, byte[]> latin1 = text => CompactQrSegmenter.Latin1(text, "Micro QR");
        foreach (var symbol in Symbols)
        {
            if (symbol.Level != level) continue;
            var v = symbol.Version;
            var segments = CompactQrSegmenter.Split(value, mode => HeaderBits(mode, v), latin1);
            if (segments is null) continue; // M1 is numeric only, M2 has no byte mode
            var bits = 0;
            var fits = true;
            foreach (var segment in segments)
            {
                bits += HeaderBits(segment.Mode, v) + segment.Bits.Count;
                fits &= segment.Count < 1 << CountBits(segment.Mode, v);
            }

            if (!fits || bits > symbol.DataBits) continue;
            return Build(symbol, segments, fixedMask);
        }

        throw new ArgumentException(
            $"The value is too long for a Micro QR code at error-correction level {level}: M4 holds at most {(level switch { QrErrorCorrectionLevel.L => "35 digits, 21 alphanumeric characters or 15 bytes", QrErrorCorrectionLevel.M => "30 digits, 18 alphanumeric characters or 13 bytes", _ => "21 digits, 13 alphanumeric characters or 9 bytes" })}. Use a QR code or rMQR for longer data.",
            nameof(value));
    }

    /// <summary>Mode indicator plus character count bits, or -1 when version <paramref name="v"/> lacks the mode.</summary>
    private static int HeaderBits(Mode mode, int v)
    {
        var count = CountBits(mode, v);
        return count < 0 ? -1 : v - 1 + count;
    }

    private static int CountBits(Mode mode, int v) => mode switch
    {
        Mode.Numeric => v + 2,
        Mode.Alphanumeric => v >= 2 ? v + 1 : -1,
        _ => v >= 3 ? v + 1 : -1,
    };

    private static int ModeIndicator(Mode mode) => mode switch
    {
        Mode.Numeric => 0,
        Mode.Alphanumeric => 1,
        _ => 2,
    };

    private static BarcodeMatrix Build(SymbolInfo info, List<CompactQrSegmenter.Run> segments, int? fixedMask)
    {
        var v = info.Version;
        var buffer = new BitBuffer();
        foreach (var segment in segments)
        {
            buffer.Append(ModeIndicator(segment.Mode), v - 1);
            buffer.Append(segment.Count, CountBits(segment.Mode, v));
            buffer.AppendBits(segment.Bits);
        }

        CompactQrSegmenter.Pad(buffer, info.DataBits, 2 * v + 1);

        // A 4-bit final codeword takes part in Reed-Solomon as its high nibble.
        var data = buffer.ToPaddedBytes();
        var ecc = CompactQrSegmenter.Ecc(data, info.EccCodewords);
        var stream = new BitBuffer();
        stream.AppendBits(buffer);
        foreach (var b in ecc) stream.Append(b, 8);

        var size = 2 * v + 9;
        var modules = new bool[size * size];
        var function = new bool[size * size];
        DrawFunctionPatterns(size, modules, function);
        PlaceBits(size, modules, function, stream.ToPaddedBytes(), stream.Count);

        var mask = fixedMask ?? BestMask(size, modules, function);
        ApplyMask(size, modules, function, mask);
        DrawFormat(size, modules, info.Number, mask);
        return new BarcodeMatrix(size, size, modules, isLinear: false, QuietZone);
    }

    private static void DrawFunctionPatterns(int size, bool[] modules, bool[] function)
    {
        void Set(int x, int y, bool dark)
        {
            modules[y * size + x] = dark;
            function[y * size + x] = true;
        }

        // Finder (7x7) and its separator.
        for (var y = 0; y <= 7; y++)
        {
            for (var x = 0; x <= 7; x++)
            {
                var d = Math.Max(Math.Abs(x - 3), Math.Abs(y - 3));
                Set(x, y, d != 2 && d < 4);
            }
        }

        // Timing patterns along the top row and the left column.
        for (var i = 8; i < size; i++)
        {
            Set(i, 0, i % 2 == 0);
            Set(0, i, i % 2 == 0);
        }

        // Format information area (drawn after masking).
        for (var i = 1; i <= 8; i++)
        {
            Set(8, i, false);
            Set(i, 8, false);
        }
    }

    /// <summary>Two-module-wide columns from the bottom right, alternating up and down.</summary>
    internal static void PlaceBits(int width, int height, bool[] modules, bool[] function, byte[] data, int bitCount, int firstColumn)
    {
        var i = 0;
        var upward = true;
        for (var right = firstColumn; right >= 1; right -= 2)
        {
            for (var step = 0; step < height; step++)
            {
                var y = upward ? height - 1 - step : step;
                for (var j = 0; j < 2; j++)
                {
                    var x = right - j;
                    var index = y * width + x;
                    if (function[index] || i >= bitCount) continue;
                    modules[index] = ((data[i >> 3] >> (7 - (i & 7))) & 1) != 0;
                    i++;
                }
            }

            upward = !upward;
        }
    }

    private static void PlaceBits(int size, bool[] modules, bool[] function, byte[] data, int bitCount) =>
        PlaceBits(size, size, modules, function, data, bitCount, size - 1);

    private static void ApplyMask(int size, bool[] modules, bool[] function, int mask)
    {
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var i = y * size + x;
                if (!function[i] && MaskInverts(QrMasks[mask], x, y)) modules[i] = !modules[i];
            }
        }
    }

    /// <summary>
    /// ISO/IEC 18004 section 7.8.3.2: count the dark modules on the right and
    /// bottom edges (timing excluded); the mask with the highest
    /// <c>16 x min + max</c> wins.
    /// </summary>
    private static int BestMask(int size, bool[] modules, bool[] function)
    {
        var best = 0;
        var bestScore = -1;
        var trial = new bool[modules.Length];
        for (var mask = 0; mask < 4; mask++)
        {
            Array.Copy(modules, trial, modules.Length);
            ApplyMask(size, trial, function, mask);
            int right = 0, bottom = 0;
            for (var i = 1; i < size; i++)
            {
                if (trial[i * size + size - 1]) right++;
                if (trial[(size - 1) * size + i]) bottom++;
            }

            var score = right <= bottom ? right * 16 + bottom : bottom * 16 + right;
            if (score > bestScore)
            {
                bestScore = score;
                best = mask;
            }
        }

        return best;
    }

    private static void DrawFormat(int size, bool[] modules, int number, int mask)
    {
        var data = number << 2 | mask;
        var rem = data;
        for (var i = 0; i < 10; i++) rem = (rem << 1) ^ ((rem >> 9) * 0x537);
        var bits = (data << 10 | rem) ^ 0x4445;
        for (var i = 0; i < 8; i++) modules[(i + 1) * size + 8] = ((bits >> i) & 1) != 0;
        for (var i = 8; i < 15; i++) modules[8 * size + 15 - i] = ((bits >> i) & 1) != 0;
    }
}
