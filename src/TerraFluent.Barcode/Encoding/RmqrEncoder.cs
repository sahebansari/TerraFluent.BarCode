using static TerraFluent.Barcode.Encoding.QrCodeEncoder;

namespace TerraFluent.Barcode.Encoding;

/// <summary>
/// Rectangular Micro QR code (rMQR, ISO/IEC 23941): 32 sizes from 7 x 43 to
/// 17 x 139 modules, at error-correction level M or H, for long narrow print
/// areas. Numeric, alphanumeric and byte segments are mixed for the fewest
/// bits; byte data is UTF-8, preceded by an ECI 26 header when it is not plain
/// ASCII. rMQR has a single mask pattern.
/// </summary>
internal static class RmqrEncoder
{
    public const int QuietZone = 2;

    /// <summary>The symbol heights rMQR defines, in modules.</summary>
    public static readonly int[] Heights = { 7, 9, 11, 13, 15, 17 };

    private const int EciMode = 7;
    private const int Utf8EciDesignator = 26;

    private sealed class VersionInfo
    {
        public VersionInfo(int index, int height, int width, int[] countBits, int total, int dataM, int blocksM, int dataH, int blocksH)
        {
            Index = index;
            Height = height;
            Width = width;
            CountBitsByMode = countBits;
            TotalCodewords = total;
            DataM = dataM;
            BlocksM = blocksM;
            DataH = dataH;
            BlocksH = blocksH;
        }

        public int Index { get; }
        public int Height { get; }
        public int Width { get; }

        /// <summary>Character count widths for numeric, alphanumeric and byte mode.</summary>
        public int[] CountBitsByMode { get; }

        public int TotalCodewords { get; }
        public int DataM { get; }
        public int BlocksM { get; }
        public int DataH { get; }
        public int BlocksH { get; }
    }

    // ISO/IEC 23941 Tables 3, 6 and 8: size, character count widths (numeric, alphanumeric, byte),
    // total codewords, and data codewords and error-correction blocks for levels M and H.
    private static readonly VersionInfo[] Versions =
    {
        new(0, 7, 43, new[] { 4, 3, 3 }, 13, 6, 1, 3, 1),
        new(1, 7, 59, new[] { 5, 5, 4 }, 21, 12, 1, 7, 1),
        new(2, 7, 77, new[] { 6, 5, 5 }, 32, 20, 1, 10, 1),
        new(3, 7, 99, new[] { 7, 6, 5 }, 44, 28, 1, 14, 1),
        new(4, 7, 139, new[] { 7, 6, 6 }, 68, 44, 1, 24, 2),
        new(5, 9, 43, new[] { 5, 5, 4 }, 21, 12, 1, 7, 1),
        new(6, 9, 59, new[] { 6, 5, 5 }, 33, 21, 1, 11, 1),
        new(7, 9, 77, new[] { 7, 6, 5 }, 49, 31, 1, 17, 2),
        new(8, 9, 99, new[] { 7, 6, 6 }, 66, 42, 1, 22, 2),
        new(9, 9, 139, new[] { 8, 7, 6 }, 99, 63, 2, 33, 3),
        new(10, 11, 27, new[] { 4, 4, 3 }, 15, 7, 1, 5, 1),
        new(11, 11, 43, new[] { 6, 5, 5 }, 31, 19, 1, 11, 1),
        new(12, 11, 59, new[] { 7, 6, 5 }, 47, 31, 1, 15, 2),
        new(13, 11, 77, new[] { 7, 6, 6 }, 67, 43, 1, 23, 2),
        new(14, 11, 99, new[] { 8, 7, 6 }, 89, 57, 2, 29, 2),
        new(15, 11, 139, new[] { 8, 7, 7 }, 132, 84, 2, 42, 3),
        new(16, 13, 27, new[] { 5, 5, 4 }, 21, 12, 1, 7, 1),
        new(17, 13, 43, new[] { 6, 6, 5 }, 41, 27, 1, 13, 1),
        new(18, 13, 59, new[] { 7, 6, 6 }, 60, 38, 1, 20, 2),
        new(19, 13, 77, new[] { 7, 7, 6 }, 85, 53, 2, 29, 2),
        new(20, 13, 99, new[] { 8, 7, 7 }, 113, 73, 2, 35, 3),
        new(21, 13, 139, new[] { 8, 8, 7 }, 166, 106, 3, 54, 4),
        new(22, 15, 43, new[] { 7, 6, 6 }, 51, 33, 1, 15, 2),
        new(23, 15, 59, new[] { 7, 7, 6 }, 74, 48, 1, 26, 2),
        new(24, 15, 77, new[] { 8, 7, 7 }, 103, 67, 2, 31, 3),
        new(25, 15, 99, new[] { 8, 7, 7 }, 136, 88, 2, 48, 4),
        new(26, 15, 139, new[] { 9, 8, 7 }, 199, 127, 3, 69, 5),
        new(27, 17, 43, new[] { 7, 6, 6 }, 61, 39, 1, 21, 2),
        new(28, 17, 59, new[] { 8, 7, 6 }, 88, 56, 2, 28, 2),
        new(29, 17, 77, new[] { 8, 7, 7 }, 122, 78, 2, 38, 3),
        new(30, 17, 99, new[] { 8, 7, 7 }, 160, 100, 3, 56, 4),
        new(31, 17, 139, new[] { 9, 8, 8 }, 232, 152, 4, 76, 6),
    };

    /// <summary>Alignment pattern centre columns by symbol width.</summary>
    private static int[] AlignmentColumns(int width) => width switch
    {
        43 => new[] { 21 },
        59 => new[] { 19, 39 },
        77 => new[] { 25, 51 },
        99 => new[] { 23, 49, 75 },
        139 => new[] { 27, 55, 83, 111 },
        _ => Array.Empty<int>(),
    };

    /// <summary>
    /// Encodes <paramref name="value"/> at level M or H. With
    /// <paramref name="height"/> set, only symbols of that height are tried;
    /// otherwise the symbol with the smallest area that fits is used.
    /// </summary>
    public static BarcodeMatrix Encode(string value, QrErrorCorrectionLevel level, int? height = null)
    {
        if (level is not (QrErrorCorrectionLevel.M or QrErrorCorrectionLevel.H))
        {
            throw new ArgumentException($"rMQR has error-correction levels M and H only; {level} was requested.", nameof(level));
        }

        if (height is int h && Array.IndexOf(Heights, h) < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), h, "rMQR symbols are 7, 9, 11, 13, 15 or 17 modules high.");
        }

        var utf8Eci = value.Any(c => c >= 0x80);
        var eciBits = utf8Eci ? 3 + 8 : 0;
        var high = level == QrErrorCorrectionLevel.H;

        VersionInfo? chosen = null;
        List<CompactQrSegmenter.Run>? chosenSegments = null;
        foreach (var info in Versions.OrderBy(i => i.Height * i.Width).ThenBy(i => i.Height))
        {
            if (height is int fixedHeight && info.Height != fixedHeight) continue;
            var segments = CompactQrSegmenter.Split(value, mode => 3 + info.CountBitsByMode[ModeIndex(mode)], CompactQrSegmenter.Utf8)!;
            var bits = eciBits;
            var fits = true;
            foreach (var segment in segments)
            {
                bits += 3 + info.CountBitsByMode[ModeIndex(segment.Mode)] + segment.Bits.Count;
                fits &= segment.Count < 1 << info.CountBitsByMode[ModeIndex(segment.Mode)];
            }

            if (fits && bits <= (high ? info.DataH : info.DataM) * 8)
            {
                chosen = info;
                chosenSegments = segments;
                break;
            }
        }

        if (chosen is null)
        {
            throw new ArgumentException(
                height is null
                    ? $"The value is too long for an rMQR code at error-correction level {level} (the largest, 17 x 139, holds 361 digits or 150 bytes at level M). Use a QR code for longer data."
                    : $"The value does not fit in an rMQR code {height} modules high at error-correction level {level}. Allow a taller symbol or shorten the value.",
                nameof(value));
        }

        return Build(chosen, high, chosenSegments!, utf8Eci);
    }

    private static int ModeIndex(Mode mode) => mode switch
    {
        Mode.Numeric => 0,
        Mode.Alphanumeric => 1,
        _ => 2,
    };

    private static BarcodeMatrix Build(VersionInfo info, bool high, List<CompactQrSegmenter.Run> segments, bool utf8Eci)
    {
        var dataCodewords = high ? info.DataH : info.DataM;
        var buffer = new BitBuffer();
        if (utf8Eci)
        {
            buffer.Append(EciMode, 3);
            buffer.Append(Utf8EciDesignator, 8);
        }

        foreach (var segment in segments)
        {
            buffer.Append(segment.Mode switch { Mode.Numeric => 1, Mode.Alphanumeric => 2, _ => 3 }, 3);
            buffer.Append(segment.Count, info.CountBitsByMode[ModeIndex(segment.Mode)]);
            buffer.AppendBits(segment.Bits);
        }

        CompactQrSegmenter.Pad(buffer, dataCodewords * 8, 3);
        var codewords = Interleave(buffer.ToBytes(), info.TotalCodewords, high ? info.BlocksH : info.BlocksM);

        int w = info.Width, h = info.Height;
        var modules = new bool[w * h];
        var function = new bool[w * h];
        DrawFunctionPatterns(w, h, modules, function);
        MicroQrEncoder.PlaceBits(w, h, modules, function, codewords, codewords.Length * 8, w - 2);

        // The single mask pattern: QR mask 100.
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                var i = y * w + x;
                if (!function[i] && (y / 2 + x / 3) % 2 == 0) modules[i] = !modules[i];
            }
        }

        DrawFormat(info, high, w, h, modules);
        return new BarcodeMatrix(w, h, modules, isLinear: false, QuietZone);
    }

    /// <summary>Splits the data into blocks (short blocks first), adds error correction and interleaves.</summary>
    private static byte[] Interleave(byte[] data, int total, int blocks)
    {
        var eccLen = (total - data.Length) / blocks;
        var shortLen = data.Length / blocks;
        var longBlocks = data.Length % blocks;
        var shortBlocks = blocks - longBlocks;
        var dataBlocks = new byte[blocks][];
        var eccBlocks = new byte[blocks][];
        for (int b = 0, k = 0; b < blocks; b++)
        {
            var len = shortLen + (b < shortBlocks ? 0 : 1);
            dataBlocks[b] = new byte[len];
            Array.Copy(data, k, dataBlocks[b], 0, len);
            k += len;
            eccBlocks[b] = CompactQrSegmenter.Ecc(dataBlocks[b], eccLen);
        }

        var result = new List<byte>(total);
        for (var i = 0; i <= shortLen; i++)
        {
            foreach (var block in dataBlocks)
            {
                if (i < block.Length) result.Add(block[i]);
            }
        }

        for (var i = 0; i < eccLen; i++)
        {
            foreach (var block in eccBlocks) result.Add(block[i]);
        }

        return result.ToArray();
    }

    private static void DrawFunctionPatterns(int w, int h, bool[] modules, bool[] function)
    {
        void Set(int x, int y, bool dark)
        {
            modules[y * w + x] = dark;
            function[y * w + x] = true;
        }

        // Finder pattern (7x7, top left) with its separator.
        for (var y = 0; y <= Math.Min(7, h - 1); y++)
        {
            for (var x = 0; x <= 7; x++)
            {
                var d = Math.Max(Math.Abs(x - 3), Math.Abs(y - 3));
                Set(x, y, d != 2 && d < 4);
            }
        }

        // Finder sub-pattern (5x5, bottom right).
        for (var y = h - 5; y < h; y++)
        {
            for (var x = w - 5; x < w; x++)
            {
                var d = Math.Max(Math.Abs(x - (w - 3)), Math.Abs(y - (h - 3)));
                Set(x, y, d != 1);
            }
        }

        // Alignment patterns (3x3, light centre) on the top and bottom edges.
        var columns = AlignmentColumns(w);
        foreach (var cx in columns)
        {
            for (var dy = 0; dy < 3; dy++)
            {
                for (var dx = -1; dx <= 1; dx++)
                {
                    var dark = !(dx == 0 && dy == 1);
                    Set(cx + dx, dy, dark);
                    Set(cx + dx, h - 1 - dy, dark);
                }
            }
        }

        // Corner finder patterns: top right and bottom left.
        Set(w - 1, 0, true);
        Set(w - 2, 0, true);
        Set(w - 1, 1, true);
        Set(w - 2, 1, false);
        Set(0, h - 1, true);
        Set(1, h - 1, true);
        Set(2, h - 1, true);
        if (h >= 11)
        {
            Set(0, h - 2, true);
            Set(1, h - 2, false);
        }

        // Timing patterns: top and bottom rows, the outer columns and the alignment columns.
        for (var x = 0; x < w; x++)
        {
            foreach (var y in new[] { 0, h - 1 })
            {
                if (!function[y * w + x]) Set(x, y, x % 2 == 0);
            }
        }

        foreach (var x in columns.Concat(new[] { 0, w - 1 }))
        {
            for (var y = 0; y < h; y++)
            {
                if (!function[y * w + x]) Set(x, y, y % 2 == 0);
            }
        }

        // Format information areas (drawn last).
        for (var n = 0; n < 18; n++)
        {
            Set(8 + n / 5, 1 + n % 5, false);
            if (n < 15) Set(w - 8 + n / 5, h - 6 + n % 5, false);
            else Set(w - 5 + n - 15, h - 6, false);
        }
    }

    private static void DrawFormat(VersionInfo info, bool high, int w, int h, bool[] modules)
    {
        var data = (high ? 1 : 0) << 5 | info.Index;
        var rem = data;
        for (var i = 0; i < 12; i++) rem = (rem << 1) ^ ((rem >> 11) * 0x1F25);
        var bits = data << 12 | rem;
        var left = bits ^ 0x1FAB2;
        var right = bits ^ 0x20A7B;
        for (var n = 0; n < 18; n++)
        {
            modules[(1 + n % 5) * w + 8 + n / 5] = ((left >> n) & 1) != 0;
            var rightBit = ((right >> n) & 1) != 0;
            if (n < 15) modules[(h - 6 + n % 5) * w + w - 8 + n / 5] = rightBit;
            else modules[(h - 6) * w + w - 5 + n - 15] = rightBit;
        }
    }

    /// <summary>Modules left for data and error correction in a symbol (for tests).</summary>
    internal static int DataModules(int index)
    {
        var info = Versions[index];
        var modules = new bool[info.Width * info.Height];
        var function = new bool[info.Width * info.Height];
        DrawFunctionPatterns(info.Width, info.Height, modules, function);
        return function.Count(f => !f);
    }

    internal static int TotalCodewords(int index) => Versions[index].TotalCodewords;
}
