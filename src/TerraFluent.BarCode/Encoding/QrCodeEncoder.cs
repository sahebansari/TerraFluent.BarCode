using System.Text;

namespace TerraFluent.BarCode.Encoding;

/// <summary>
/// QR code model 2 (ISO/IEC 18004), versions 1-40, all four error-correction
/// levels. The value is split into numeric, alphanumeric and byte (UTF-8)
/// segments, whichever mix needs the fewest bits. Values containing non-ASCII
/// characters are preceded by an ECI 26 (UTF-8) header so
/// readers do not fall back to ISO-8859-1 or Shift JIS. The mask with the
/// lowest penalty score is chosen automatically.
/// </summary>
internal static class QrCodeEncoder
{
    public const int QuietZone = 4;

    internal const string AlphanumericCharset = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:";

    internal enum Mode { Numeric = 1, Alphanumeric = 2, Byte = 4 }

    private const int EciMode = 7;
    private const int Utf8EciDesignator = 26;
    private const int EciHeaderBits = 4 + 8;

    // Indexed [level L/M/Q/H][version]; index 0 is unused.
    private static readonly int[][] EccCodewordsPerBlock =
    {
        new[] { -1, 7, 10, 15, 20, 26, 18, 20, 24, 30, 18, 20, 24, 26, 30, 22, 24, 28, 30, 28, 28, 28, 28, 30, 30, 26, 28, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 },
        new[] { -1, 10, 16, 26, 18, 24, 16, 18, 22, 22, 26, 30, 22, 22, 24, 24, 28, 28, 26, 26, 26, 26, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28 },
        new[] { -1, 13, 22, 18, 26, 18, 24, 18, 22, 20, 24, 28, 26, 24, 20, 30, 24, 28, 28, 26, 30, 28, 30, 30, 30, 30, 28, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 },
        new[] { -1, 17, 28, 22, 16, 22, 28, 26, 26, 24, 28, 24, 28, 22, 24, 24, 30, 28, 28, 26, 28, 30, 24, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 },
    };

    private static readonly int[][] ErrorCorrectionBlocks =
    {
        new[] { -1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 4, 4, 4, 4, 4, 6, 6, 6, 6, 7, 8, 8, 9, 9, 10, 12, 12, 12, 13, 14, 15, 16, 17, 18, 19, 19, 20, 21, 22, 24, 25 },
        new[] { -1, 1, 1, 1, 2, 2, 4, 4, 4, 5, 5, 5, 8, 9, 9, 10, 10, 11, 13, 14, 16, 17, 17, 18, 20, 21, 23, 25, 26, 28, 29, 31, 33, 35, 37, 38, 40, 43, 45, 47, 49 },
        new[] { -1, 1, 1, 2, 2, 4, 4, 6, 6, 8, 8, 8, 10, 12, 16, 12, 17, 16, 18, 21, 20, 23, 23, 25, 27, 29, 34, 34, 35, 38, 40, 43, 45, 48, 51, 53, 56, 59, 62, 65, 68 },
        new[] { -1, 1, 1, 2, 4, 4, 4, 5, 6, 8, 8, 11, 11, 16, 16, 18, 16, 19, 21, 25, 25, 25, 34, 30, 32, 35, 37, 40, 42, 45, 48, 51, 54, 57, 60, 63, 66, 70, 74, 77, 81 },
    };

    // 2-bit format-information code per level, ISO/IEC 18004 Table 12.
    private static readonly int[] FormatBits = { 1, 0, 3, 2 };

    public static BarcodeMatrix Encode(string value, QrErrorCorrectionLevel level, int? fixedVersion = null, int? fixedMask = null)
    {
        var lvl = (int)level;
        var utf8Eci = value.Any(c => c >= 0x80);
        var eciBits = utf8Eci ? EciHeaderBits : 0;

        // The cheapest segmentation depends on the character-count field widths, which change at versions 10 and 27.
        var version = 0;
        List<Segment>? segments = null;
        var firstVersion = fixedVersion ?? 1;
        var lastVersion = fixedVersion ?? 40;
        List<Segment>? rangeSegments = null;
        var rangeOf = -1;
        for (var v = firstVersion; v <= lastVersion; v++)
        {
            var range = VersionRange(v);
            if (range != rangeOf)
            {
                rangeSegments = Segmenter.Split(value, v);
                rangeOf = range;
            }

            var bits = eciBits;
            var fits = true;
            foreach (var segment in rangeSegments!)
            {
                bits += 4 + CountBits(segment.Mode, v) + segment.Bits.Count;
                fits &= segment.Count < 1 << CountBits(segment.Mode, v);
            }

            if (fits && bits <= DataCodewords(v, lvl) * 8)
            {
                version = v;
                segments = rangeSegments;
                break;
            }
        }

        if (version == 0)
        {
            throw new ArgumentException(
                fixedVersion is null
                    ? $"The value is too long for a QR code at error-correction level {level} (needs more than version 40)."
                    : $"The value does not fit in a version {fixedVersion} QR code at error-correction level {level}.",
                nameof(value));
        }

        // Header, payload, terminator, byte padding and pad codewords.
        var capacityBits = DataCodewords(version, lvl) * 8;
        var buffer = new BitBuffer();
        if (utf8Eci)
        {
            buffer.Append(EciMode, 4);
            buffer.Append(Utf8EciDesignator, 8);
        }

        foreach (var segment in segments!)
        {
            buffer.Append((int)segment.Mode, 4);
            buffer.Append(segment.Count, CountBits(segment.Mode, version));
            buffer.AppendBits(segment.Bits);
        }

        buffer.Append(0, Math.Min(4, capacityBits - buffer.Count));
        buffer.Append(0, (8 - buffer.Count % 8) % 8);
        for (var pad = 0xEC; buffer.Count < capacityBits; pad ^= 0xEC ^ 0x11) buffer.Append(pad, 8);

        var codewords = AddEccAndInterleave(buffer.ToBytes(), version, lvl);
        var symbol = new Symbol(version);
        symbol.DrawFunctionPatterns();
        symbol.DrawCodewords(codewords);

        int mask;
        if (fixedMask is int m)
        {
            mask = m;
        }
        else
        {
            mask = 0;
            var best = int.MaxValue;
            for (var candidate = 0; candidate < 8; candidate++)
            {
                symbol.ApplyMask(candidate);
                symbol.DrawFormatBits(FormatBits[lvl], candidate);
                var penalty = symbol.Penalty();
                if (penalty < best)
                {
                    best = penalty;
                    mask = candidate;
                }

                symbol.ApplyMask(candidate); // XOR again to undo
            }
        }

        symbol.ApplyMask(mask);
        symbol.DrawFormatBits(FormatBits[lvl], mask);
        return new BarcodeMatrix(symbol.Size, symbol.Size, symbol.Modules, isLinear: false, QuietZone);
    }

    internal static (Mode Mode, List<bool> Payload, int Count, bool Utf8Eci) SelectMode(string value)
    {
        var bits = new BitBuffer();
        if (value.All(c => c >= '0' && c <= '9'))
        {
            for (var i = 0; i < value.Length; i += 3)
            {
                var n = Math.Min(3, value.Length - i);
                bits.Append(int.Parse(value.Substring(i, n), System.Globalization.CultureInfo.InvariantCulture), n * 3 + 1);
            }

            return (Mode.Numeric, bits.Bits, value.Length, false);
        }

        if (value.All(c => AlphanumericCharset.IndexOf(c) >= 0))
        {
            for (var i = 0; i + 1 < value.Length; i += 2)
            {
                bits.Append(AlphanumericCharset.IndexOf(value[i]) * 45 + AlphanumericCharset.IndexOf(value[i + 1]), 11);
            }

            if (value.Length % 2 == 1) bits.Append(AlphanumericCharset.IndexOf(value[value.Length - 1]), 6);
            return (Mode.Alphanumeric, bits.Bits, value.Length, false);
        }

        byte[] bytes;
        try
        {
            bytes = new UTF8Encoding(false, true).GetBytes(value);
        }
        catch (EncoderFallbackException ex)
        {
            throw new ArgumentException("The value contains an unpaired surrogate character and is not valid Unicode text.", nameof(value), ex);
        }

        var nonAscii = false;
        foreach (var b in bytes)
        {
            bits.Append(b, 8);
            nonAscii |= b >= 0x80;
        }

        return (Mode.Byte, bits.Bits, bytes.Length, nonAscii);
    }

    private static int VersionRange(int version) => version <= 9 ? 0 : version <= 26 ? 1 : 2;

    internal static int CountBits(Mode mode, int version)
    {
        var range = VersionRange(version);
        return mode switch
        {
            Mode.Numeric => new[] { 10, 12, 14 }[range],
            Mode.Alphanumeric => new[] { 9, 11, 13 }[range],
            _ => new[] { 8, 16, 16 }[range],
        };
    }

    /// <summary>Modules available for data and error correction (everything except function patterns).</summary>
    internal static int RawDataModules(int version)
    {
        var result = (16 * version + 128) * version + 64;
        if (version >= 2)
        {
            var numAlign = version / 7 + 2;
            result -= (25 * numAlign - 10) * numAlign - 55;
            if (version >= 7) result -= 36;
        }

        return result;
    }

    internal static int DataCodewords(int version, int level) =>
        RawDataModules(version) / 8 - EccCodewordsPerBlock[level][version] * ErrorCorrectionBlocks[level][version];

    internal static int[] AlignmentPositions(int version)
    {
        if (version == 1) return Array.Empty<int>();
        var numAlign = version / 7 + 2;
        var step = version == 32 ? 26 : (version * 4 + numAlign * 2 + 1) / (numAlign * 2 - 2) * 2;
        var result = new int[numAlign];
        result[0] = 6;
        for (int i = numAlign - 1, pos = version * 4 + 10; i >= 1; i--, pos -= step) result[i] = pos;
        return result;
    }

    private static byte[] AddEccAndInterleave(byte[] data, int version, int level)
    {
        var numBlocks = ErrorCorrectionBlocks[level][version];
        var eccLen = EccCodewordsPerBlock[level][version];
        var rawCodewords = RawDataModules(version) / 8;
        var numShortBlocks = numBlocks - rawCodewords % numBlocks;
        var shortBlockLen = rawCodewords / numBlocks;
        var divisor = ReedSolomon.Generator(eccLen);

        var blocks = new byte[numBlocks][];
        for (int i = 0, k = 0; i < numBlocks; i++)
        {
            var datLen = shortBlockLen - eccLen + (i < numShortBlocks ? 0 : 1);
            var dat = new byte[datLen];
            Array.Copy(data, k, dat, 0, datLen);
            k += datLen;
            var ecc = ReedSolomon.Remainder(dat, divisor);
            var block = new byte[shortBlockLen + 1];
            Array.Copy(dat, block, datLen);
            Array.Copy(ecc, 0, block, block.Length - eccLen, eccLen);
            blocks[i] = block;
        }

        var result = new List<byte>(rawCodewords);
        for (var i = 0; i < shortBlockLen + 1; i++)
        {
            for (var j = 0; j < numBlocks; j++)
            {
                // Short blocks have a padding slot at the end of their data part; skip it.
                if (i != shortBlockLen - eccLen || j >= numShortBlocks) result.Add(blocks[j][i]);
            }
        }

        return result.ToArray();
    }

    /// <summary>One run of the value in a single mode.</summary>
    internal sealed class Segment
    {
        public Segment(Mode mode, int count, List<bool> bits)
        {
            Mode = mode;
            Count = count;
            Bits = bits;
        }

        public Mode Mode { get; }

        /// <summary>Characters (numeric, alphanumeric) or bytes (byte mode), as written in the count field.</summary>
        public int Count { get; }

        public List<bool> Bits { get; }
    }

    /// <summary>
    /// Splits a value into numeric, alphanumeric and byte segments with the
    /// fewest bits in total, by dynamic programming over its code points.
    /// </summary>
    internal static class Segmenter
    {
        private static readonly Mode[] Modes = { Mode.Numeric, Mode.Alphanumeric, Mode.Byte };

        public static List<Segment> Split(string value, int version)
        {
            // Code points, so a surrogate pair is never split between segments.
            var points = new List<string>(value.Length);
            for (var i = 0; i < value.Length; i++)
            {
                if (char.IsHighSurrogate(value[i]) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
                {
                    points.Add(value.Substring(i, 2));
                    i++;
                }
                else
                {
                    points.Add(value.Substring(i, 1));
                }
            }

            var utf8 = new UTF8Encoding(false, true);
            var byteLengths = new int[points.Count];
            for (var i = 0; i < points.Count; i++)
            {
                try
                {
                    byteLengths[i] = utf8.GetByteCount(points[i]);
                }
                catch (EncoderFallbackException ex)
                {
                    throw new ArgumentException("The value contains an unpaired surrogate character and is not valid Unicode text.", nameof(value), ex);
                }
            }

            // Costs in sixths of a bit: numeric 10 bits per 3 characters, alphanumeric 11 per 2, byte 8 per byte.
            const int Inf = int.MaxValue / 4;
            var n = points.Count;
            var cost = new int[3];
            var from = new int[n, 3];
            for (var i = 0; i < n; i++)
            {
                var next = new int[3];
                for (var m = 0; m < 3; m++)
                {
                    next[m] = Inf;
                    from[i, m] = -1;
                    if (!Allowed(Modes[m], points[i])) continue;
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
                        var header = i > 0 && pm == m ? 0 : (4 + CountBits(Modes[m], version)) * 6;
                        var total = previous + header + charCost;
                        if (total < next[m])
                        {
                            next[m] = total;
                            from[i, m] = pm;
                        }
                    }
                }

                cost = next;
            }

            var result = new List<Segment>();
            if (n == 0) return result;

            var modes = new int[n];
            var best = 0;
            for (var m = 1; m < 3; m++)
            {
                if (cost[m] < cost[best]) best = m;
            }

            for (var i = n - 1; i >= 0; i--)
            {
                modes[i] = best;
                best = from[i, best];
            }

            var start = 0;
            for (var i = 1; i <= n; i++)
            {
                if (i < n && modes[i] == modes[start]) continue;
                result.Add(Build(Modes[modes[start]], string.Concat(points.Skip(start).Take(i - start))));
                start = i;
            }

            return result;
        }

        private static bool Allowed(Mode mode, string point) => mode switch
        {
            Mode.Numeric => point.Length == 1 && point[0] >= '0' && point[0] <= '9',
            Mode.Alphanumeric => point.Length == 1 && AlphanumericCharset.IndexOf(point[0]) >= 0,
            _ => true,
        };

        private static Segment Build(Mode mode, string text)
        {
            var bits = new BitBuffer();
            switch (mode)
            {
                case Mode.Numeric:
                    for (var i = 0; i < text.Length; i += 3)
                    {
                        var n = Math.Min(3, text.Length - i);
                        bits.Append(int.Parse(text.Substring(i, n), System.Globalization.CultureInfo.InvariantCulture), n * 3 + 1);
                    }

                    return new Segment(mode, text.Length, bits.Bits);

                case Mode.Alphanumeric:
                    for (var i = 0; i + 1 < text.Length; i += 2)
                    {
                        bits.Append(AlphanumericCharset.IndexOf(text[i]) * 45 + AlphanumericCharset.IndexOf(text[i + 1]), 11);
                    }

                    if (text.Length % 2 == 1) bits.Append(AlphanumericCharset.IndexOf(text[text.Length - 1]), 6);
                    return new Segment(mode, text.Length, bits.Bits);

                default:
                    var bytes = new UTF8Encoding(false, true).GetBytes(text);
                    foreach (var b in bytes) bits.Append(b, 8);
                    return new Segment(mode, bytes.Length, bits.Bits);
            }
        }
    }

    private sealed class BitBuffer
    {
        public List<bool> Bits { get; } = new();

        public int Count => Bits.Count;

        public void Append(int value, int length)
        {
            for (var i = length - 1; i >= 0; i--) Bits.Add(((value >> i) & 1) != 0);
        }

        public void AppendBits(IEnumerable<bool> bits) => Bits.AddRange(bits);

        public byte[] ToBytes()
        {
            var bytes = new byte[Bits.Count / 8];
            for (var i = 0; i < Bits.Count; i++)
            {
                if (Bits[i]) bytes[i >> 3] |= (byte)(0x80 >> (i & 7));
            }

            return bytes;
        }
    }

    private static class ReedSolomon
    {
        public static byte[] Generator(int degree)
        {
            var result = new byte[degree];
            result[degree - 1] = 1;
            var root = 1;
            for (var i = 0; i < degree; i++)
            {
                for (var j = 0; j < result.Length; j++)
                {
                    result[j] = Multiply(result[j], root);
                    if (j + 1 < result.Length) result[j] ^= result[j + 1];
                }

                root = Multiply(root, 0x02);
            }

            return result;
        }

        public static byte[] Remainder(byte[] data, byte[] divisor)
        {
            var result = new byte[divisor.Length];
            foreach (var b in data)
            {
                var factor = b ^ result[0];
                Array.Copy(result, 1, result, 0, result.Length - 1);
                result[result.Length - 1] = 0;
                for (var i = 0; i < result.Length; i++) result[i] ^= Multiply(divisor[i], factor);
            }

            return result;
        }

        private static byte Multiply(int x, int y)
        {
            var z = 0;
            for (var i = 7; i >= 0; i--)
            {
                z = (z << 1) ^ ((z >> 7) * 0x11D);
                z ^= ((y >> i) & 1) * x;
            }

            return (byte)z;
        }
    }

    private sealed class Symbol
    {
        private readonly bool[] _isFunction;

        public Symbol(int version)
        {
            Version = version;
            Size = version * 4 + 17;
            Modules = new bool[Size * Size];
            _isFunction = new bool[Size * Size];
        }

        public int Version { get; }
        public int Size { get; }
        public bool[] Modules { get; }

        private bool Get(int x, int y) => Modules[y * Size + x];

        private void SetFunction(int x, int y, bool dark)
        {
            Modules[y * Size + x] = dark;
            _isFunction[y * Size + x] = true;
        }

        public void DrawFunctionPatterns()
        {
            for (var i = 0; i < Size; i++)
            {
                SetFunction(6, i, i % 2 == 0);
                SetFunction(i, 6, i % 2 == 0);
            }

            DrawFinder(3, 3);
            DrawFinder(Size - 4, 3);
            DrawFinder(3, Size - 4);

            var positions = AlignmentPositions(Version);
            var n = positions.Length;
            for (var i = 0; i < n; i++)
            {
                for (var j = 0; j < n; j++)
                {
                    // Skip the three corners occupied by finder patterns.
                    if ((i == 0 && j == 0) || (i == 0 && j == n - 1) || (i == n - 1 && j == 0)) continue;
                    DrawAlignment(positions[i], positions[j]);
                }
            }

            DrawFormatBits(0, 0); // reserve the area; real bits are drawn after masking
            DrawVersion();
        }

        private void DrawFinder(int cx, int cy)
        {
            for (var dy = -4; dy <= 4; dy++)
            {
                for (var dx = -4; dx <= 4; dx++)
                {
                    var dist = Math.Max(Math.Abs(dx), Math.Abs(dy));
                    int x = cx + dx, y = cy + dy;
                    if (x >= 0 && x < Size && y >= 0 && y < Size) SetFunction(x, y, dist != 2 && dist != 4);
                }
            }
        }

        private void DrawAlignment(int cx, int cy)
        {
            for (var dy = -2; dy <= 2; dy++)
            {
                for (var dx = -2; dx <= 2; dx++) SetFunction(cx + dx, cy + dy, Math.Max(Math.Abs(dx), Math.Abs(dy)) != 1);
            }
        }

        public void DrawFormatBits(int levelBits, int mask)
        {
            var data = levelBits << 3 | mask;
            var rem = data;
            for (var i = 0; i < 10; i++) rem = (rem << 1) ^ ((rem >> 9) * 0x537);
            var bits = (data << 10 | rem) ^ 0x5412;

            for (var i = 0; i <= 5; i++) SetFunction(8, i, Bit(bits, i));
            SetFunction(8, 7, Bit(bits, 6));
            SetFunction(8, 8, Bit(bits, 7));
            SetFunction(7, 8, Bit(bits, 8));
            for (var i = 9; i < 15; i++) SetFunction(14 - i, 8, Bit(bits, i));

            for (var i = 0; i < 8; i++) SetFunction(Size - 1 - i, 8, Bit(bits, i));
            for (var i = 8; i < 15; i++) SetFunction(8, Size - 15 + i, Bit(bits, i));
            SetFunction(8, Size - 8, true); // the always-dark module
        }

        private void DrawVersion()
        {
            if (Version < 7) return;
            var rem = Version;
            for (var i = 0; i < 12; i++) rem = (rem << 1) ^ ((rem >> 11) * 0x1F25);
            var bits = Version << 12 | rem;
            for (var i = 0; i < 18; i++)
            {
                var bit = Bit(bits, i);
                int a = Size - 11 + i % 3, b = i / 3;
                SetFunction(a, b, bit);
                SetFunction(b, a, bit);
            }
        }

        public void DrawCodewords(byte[] data)
        {
            var i = 0;
            for (var right = Size - 1; right >= 1; right -= 2)
            {
                if (right == 6) right = 5; // skip the vertical timing pattern
                for (var vert = 0; vert < Size; vert++)
                {
                    for (var j = 0; j < 2; j++)
                    {
                        var x = right - j;
                        var upward = ((right + 1) & 2) == 0;
                        var y = upward ? Size - 1 - vert : vert;
                        if (!_isFunction[y * Size + x] && i < data.Length * 8)
                        {
                            Modules[y * Size + x] = Bit(data[i >> 3], 7 - (i & 7));
                            i++;
                        }
                    }
                }
            }
        }

        public void ApplyMask(int mask)
        {
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    var invert = mask switch
                    {
                        0 => (x + y) % 2 == 0,
                        1 => y % 2 == 0,
                        2 => x % 3 == 0,
                        3 => (x + y) % 3 == 0,
                        4 => (x / 3 + y / 2) % 2 == 0,
                        5 => x * y % 2 + x * y % 3 == 0,
                        6 => (x * y % 2 + x * y % 3) % 2 == 0,
                        _ => ((x + y) % 2 + x * y % 3) % 2 == 0,
                    };
                    if (invert && !_isFunction[y * Size + x]) Modules[y * Size + x] ^= true;
                }
            }
        }

        /// <summary>ISO/IEC 18004 section 7.8.3 mask penalty (rules N1-N4).</summary>
        public int Penalty()
        {
            var penalty = 0;
            for (var pass = 0; pass < 2; pass++)
            {
                for (var a = 0; a < Size; a++)
                {
                    var runColor = false;
                    var run = 0;
                    for (var b = 0; b < Size; b++)
                    {
                        var dark = pass == 0 ? Get(b, a) : Get(a, b);
                        if (b == 0 || dark != runColor)
                        {
                            runColor = dark;
                            run = 1;
                        }
                        else
                        {
                            run++;
                            if (run == 5) penalty += 3;
                            else if (run > 5) penalty++;
                        }

                        // N3: 1:1:3:1:1 finder-like pattern with four light modules on one side.
                        if (b >= 10 && Matches(pass, a, b - 10)) penalty += 40;
                    }
                }
            }

            for (var y = 0; y < Size - 1; y++)
            {
                for (var x = 0; x < Size - 1; x++)
                {
                    var c = Get(x, y);
                    if (c == Get(x + 1, y) && c == Get(x, y + 1) && c == Get(x + 1, y + 1)) penalty += 3;
                }
            }

            var darkCount = Modules.Count(m => m);
            var total = Size * Size;
            var k = (Math.Abs(darkCount * 20 - total * 10) + total - 1) / total - 1;
            penalty += k * 10;
            return penalty;
        }

        private static readonly bool[] PatternA = { true, false, true, true, true, false, true, false, false, false, false };
        private static readonly bool[] PatternB = { false, false, false, false, true, false, true, true, true, false, true };

        private bool Matches(int pass, int line, int start)
        {
            bool matchA = true, matchB = true;
            for (var i = 0; i < 11 && (matchA || matchB); i++)
            {
                var dark = pass == 0 ? Get(start + i, line) : Get(line, start + i);
                if (dark != PatternA[i]) matchA = false;
                if (dark != PatternB[i]) matchB = false;
            }

            return matchA || matchB;
        }

        private static bool Bit(int value, int index) => ((value >> index) & 1) != 0;
    }
}
