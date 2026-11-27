using System.Numerics;
using System.Text;

namespace TerraFluent.BarCode.Encoding;

/// <summary>
/// PDF417 (ISO/IEC 15438). Text, numeric and byte compaction are chosen per
/// run of the value; non-ASCII text is sent as UTF-8 behind an ECI 26 header.
/// Each row is drawn three modules high.
/// </summary>
internal static partial class Pdf417Encoder
{
    public const int QuietZone = 2;
    public const int RowHeight = 3;
    public const int MinColumns = 1;
    public const int MaxColumns = 30;
    public const int MinRows = 3;
    public const int MaxRows = 90;
    public const int MaxCodewords = 928;

    private const int TextLatch = 900;
    private const int ByteLatch = 901;
    private const int ByteLatchMultipleOf6 = 924;
    private const int NumericLatch = 902;
    private const int EciCodeword = 927;
    private const int PadCodeword = 900;

    private const int StartPattern = 0x1FEA8; // 11111111010101000
    private const int StopPattern = 0x3FA29;  // 111111101000101001

    // Text compaction sub-modes: each string lists the characters of values 0-25 (or 0-28), '\0' where the value is a latch or shift.
    private const string MixedChars = "0123456789&\r\t,:#-.$/+%*=^";
    private const string PunctChars = ";<>@[\\]_`~!\r\t,:\n-.$/\"|*()?{}'";

    private const int AlphaToLower = 27, AlphaToMixed = 28, PunctShift = 29;
    private const int LowerAlphaShift = 27, LowerToMixed = 28;
    private const int MixedToPunct = 25, MixedToLower = 27, MixedToAlpha = 28;
    private const int PunctToAlpha = 29;

    /// <summary>Recommended error-correction level for a number of data codewords (ISO/IEC 15438 Table 6, rounded up).</summary>
    public static int RecommendedLevel(int dataCodewords) => dataCodewords switch
    {
        <= 40 => 2,
        <= 160 => 3,
        <= 320 => 4,
        <= 863 => 5,
        _ => 6,
    };

    public static BarcodeMatrix Encode(string value, int? columns, int? level)
    {
        var data = HighLevelEncode(value);
        var eccLevel = level ?? RecommendedLevel(data.Count + 1);
        var eccCount = 1 << (eccLevel + 1);
        var needed = data.Count + 1 + eccCount; // + length descriptor
        if (needed > MaxCodewords)
        {
            throw new ArgumentException(
                $"The value is too long for PDF417 at error-correction level {eccLevel}: it needs {needed} codewords and a symbol holds {MaxCodewords}. Use a lower level or shorter data.",
                nameof(value));
        }

        var (cols, rows) = Dimensions(needed, columns, nameof(value));
        var dataCount = rows * cols - eccCount;
        var codewords = new List<int>(rows * cols) { dataCount };
        codewords.AddRange(data);
        while (codewords.Count < dataCount) codewords.Add(PadCodeword);
        codewords.AddRange(ErrorCorrection(codewords, eccLevel));
        return Render(codewords, rows, cols, eccLevel);
    }

    private static (int Columns, int Rows) Dimensions(int codewords, int? columns, string paramName)
    {
        if (columns is int c)
        {
            var r = Math.Max(MinRows, (codewords + c - 1) / c);
            if (r > MaxRows || r * c > MaxCodewords)
            {
                throw new ArgumentException(
                    $"{codewords} codewords do not fit in {c} columns (at most {MaxRows} rows and {MaxCodewords} codewords). Use more columns.", paramName);
            }

            return (c, r);
        }

        // Pick the column count whose symbol is closest to a 3:1 width-to-height ratio.
        (int, int) best = (0, 0);
        var bestScore = double.MaxValue;
        for (c = MinColumns; c <= MaxColumns; c++)
        {
            var r = Math.Max(MinRows, (codewords + c - 1) / c);
            if (r > MaxRows || r * c > MaxCodewords) continue;
            double width = 17 * (c + 4) + 1, height = r * RowHeight;
            var score = Math.Abs(Math.Log(width / height / 3.0)) + (r * c - codewords) * 0.002;
            if (score < bestScore)
            {
                bestScore = score;
                best = (c, r);
            }
        }

        if (best.Item1 == 0) throw new ArgumentException($"{codewords} codewords do not fit in a PDF417 symbol.", paramName);
        return best;
    }

    /// <summary>Converts the value into data codewords (without the length descriptor).</summary>
    internal static List<int> HighLevelEncode(string value)
    {
        var result = new List<int>();
        byte[] bytes;
        string text;
        if (value.All(c => c < 128))
        {
            text = value;
            bytes = value.Select(c => (byte)c).ToArray();
        }
        else
        {
            try
            {
                bytes = new UTF8Encoding(false, true).GetBytes(value);
            }
            catch (EncoderFallbackException ex)
            {
                throw new ArgumentException("The value contains an unpaired surrogate character and is not valid Unicode text.", nameof(value), ex);
            }

            result.Add(EciCodeword);
            result.Add(26);
            text = new string(bytes.Select(b => (char)b).ToArray()); // one char per byte, for run detection
        }

        var pos = 0;
        var mode = TextLatch; // text compaction is the default mode at the start of a symbol
        while (pos < bytes.Length)
        {
            var digits = DigitRun(text, pos);
            if (digits >= 13)
            {
                result.Add(NumericLatch);
                mode = NumericLatch;
                EncodeNumeric(text.Substring(pos, digits), result);
                pos += digits;
                continue;
            }

            var textRun = TextRun(text, pos);
            if (textRun >= 5 || (textRun > 0 && pos + textRun == bytes.Length && mode != ByteLatch))
            {
                if (mode != TextLatch || result.Count > 0)
                {
                    result.Add(TextLatch);
                }

                mode = TextLatch;
                EncodeText(text.Substring(pos, textRun), result);
                pos += textRun;
                continue;
            }

            // Bytes up to the next run worth switching for.
            var end = pos + Math.Max(1, textRun);
            while (end < bytes.Length && DigitRun(text, end) < 13 && TextRun(text, end) < 5) end++;
            EncodeBytes(bytes, pos, end - pos, result);
            mode = ByteLatch;
            pos = end;
        }

        return result;
    }

    private static int DigitRun(string s, int from)
    {
        var i = from;
        while (i < s.Length && s[i] >= '0' && s[i] <= '9') i++;
        return i - from;
    }

    private static bool IsText(char c) => c is '\t' or '\n' or '\r' || (c >= ' ' && c <= '~');

    private static int TextRun(string s, int from)
    {
        var i = from;
        while (i < s.Length && IsText(s[i]))
        {
            if (DigitRun(s, i) >= 13) break;
            i++;
        }

        return i - from;
    }

    private enum SubMode { Alpha, Lower, Mixed, Punct }

    private static void EncodeText(string text, List<int> result)
    {
        var values = new List<int>(text.Length * 2);
        var sub = SubMode.Alpha;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            switch (sub)
            {
                case SubMode.Alpha:
                    if (IsUpper(c) || c == ' ') values.Add(c == ' ' ? 26 : c - 'A');
                    else if (IsLower(c)) { values.Add(AlphaToLower); sub = SubMode.Lower; i--; }
                    else if (MixedChars.IndexOf(c) >= 0) { values.Add(AlphaToMixed); sub = SubMode.Mixed; i--; }
                    else { values.Add(PunctShift); values.Add(PunctChars.IndexOf(c)); }
                    break;

                case SubMode.Lower:
                    if (IsLower(c) || c == ' ') values.Add(c == ' ' ? 26 : c - 'a');
                    else if (IsUpper(c)) { values.Add(LowerAlphaShift); values.Add(c - 'A'); }
                    else if (MixedChars.IndexOf(c) >= 0) { values.Add(LowerToMixed); sub = SubMode.Mixed; i--; }
                    else { values.Add(PunctShift); values.Add(PunctChars.IndexOf(c)); }
                    break;

                case SubMode.Mixed:
                    var mixed = MixedChars.IndexOf(c);
                    if (mixed >= 0) values.Add(mixed);
                    else if (c == ' ') values.Add(26);
                    else if (IsUpper(c)) { values.Add(MixedToAlpha); sub = SubMode.Alpha; i--; }
                    else if (IsLower(c)) { values.Add(MixedToLower); sub = SubMode.Lower; i--; }
                    else if (i + 1 < text.Length && PunctChars.IndexOf(text[i + 1]) >= 0 && MixedChars.IndexOf(text[i + 1]) < 0)
                    {
                        values.Add(MixedToPunct);
                        sub = SubMode.Punct;
                        i--;
                    }
                    else { values.Add(PunctShift); values.Add(PunctChars.IndexOf(c)); }
                    break;

                default: // Punct
                    var punct = PunctChars.IndexOf(c);
                    if (punct >= 0) values.Add(punct);
                    else { values.Add(PunctToAlpha); sub = SubMode.Alpha; i--; }
                    break;
            }
        }

        if (values.Count % 2 == 1) values.Add(PunctShift); // pad with a harmless shift
        for (var i = 0; i < values.Count; i += 2) result.Add(values[i] * 30 + values[i + 1]);
    }

    private static bool IsUpper(char c) => c >= 'A' && c <= 'Z';

    private static bool IsLower(char c) => c >= 'a' && c <= 'z';

    private static void EncodeNumeric(string digits, List<int> result)
    {
        for (var i = 0; i < digits.Length; i += 44)
        {
            var group = BigInteger.Parse("1" + digits.Substring(i, Math.Min(44, digits.Length - i)), System.Globalization.CultureInfo.InvariantCulture);
            var words = new List<int>();
            while (group > 0)
            {
                words.Add((int)(group % 900));
                group /= 900;
            }

            words.Reverse();
            result.AddRange(words);
        }
    }

    private static void EncodeBytes(byte[] bytes, int start, int count, List<int> result)
    {
        result.Add(count % 6 == 0 ? ByteLatchMultipleOf6 : ByteLatch);
        var i = start;
        var end = start + count;
        for (; i + 6 <= end; i += 6)
        {
            long t = 0;
            for (var j = 0; j < 6; j++) t = (t << 8) | bytes[i + j];
            var words = new int[5];
            for (var j = 4; j >= 0; j--)
            {
                words[j] = (int)(t % 900);
                t /= 900;
            }

            result.AddRange(words);
        }

        for (; i < end; i++) result.Add(bytes[i]);
    }

    /// <summary>Reed-Solomon over GF(929) with generator roots 3^1..3^k.</summary>
    internal static int[] ErrorCorrection(IReadOnlyList<int> data, int level)
    {
        var factors = GeneratorFactors(level);
        var count = factors.Length;
        var ecc = new int[count];
        foreach (var word in data)
        {
            var t = (word + ecc[count - 1]) % 929;
            for (var x = count - 1; x >= 0; x--)
            {
                var previous = x > 0 ? ecc[x - 1] : 0;
                ecc[x] = (previous + 929 - t * factors[x] % 929) % 929;
            }
        }

        var result = new int[count];
        for (var i = 0; i < count; i++)
        {
            var v = ecc[count - 1 - i];
            result[i] = v > 0 ? 929 - v : 0;
        }

        return result;
    }

    /// <summary>Coefficients a0..a(k-1) of the generator polynomial prod (x - 3^i), i = 1..k, with k = 2^(level+1).</summary>
    internal static int[] GeneratorFactors(int level)
    {
        var k = 1 << (level + 1);
        var g = new long[k + 1];
        g[0] = 1; // g[i] is the coefficient of x^i
        long root = 1;
        for (var i = 1; i <= k; i++)
        {
            root = root * 3 % 929;
            // Multiply by (x - root).
            for (var j = i; j >= 0; j--)
            {
                var shifted = j > 0 ? g[j - 1] : 0;
                g[j] = ((shifted - root * g[j]) % 929 + 929) % 929;
            }
        }

        return g.Take(k).Select(v => (int)v).ToArray();
    }

    private static BarcodeMatrix Render(List<int> codewords, int rows, int cols, int level)
    {
        var width = 17 * (cols + 4) + 1;
        var height = rows * RowHeight;
        var modules = new bool[width * height];
        for (var r = 0; r < rows; r++)
        {
            var cluster = r % 3;
            var x = 0;
            var y = r * RowHeight;

            void Write(int pattern, int length)
            {
                for (var b = length - 1; b >= 0; b--, x++)
                {
                    var dark = ((pattern >> b) & 1) == 1;
                    for (var dy = 0; dy < RowHeight; dy++) modules[(y + dy) * width + x] = dark;
                }
            }

            var rowGroup = r / 3 * 30;
            int left, right;
            switch (cluster)
            {
                case 0:
                    left = rowGroup + (rows - 1) / 3;
                    right = rowGroup + cols - 1;
                    break;
                case 1:
                    left = rowGroup + level * 3 + (rows - 1) % 3;
                    right = rowGroup + (rows - 1) / 3;
                    break;
                default:
                    left = rowGroup + cols - 1;
                    right = rowGroup + level * 3 + (rows - 1) % 3;
                    break;
            }

            Write(StartPattern, 17);
            Write(Patterns[cluster][left], 17);
            for (var c = 0; c < cols; c++) Write(Patterns[cluster][codewords[r * cols + c]], 17);
            Write(Patterns[cluster][right], 17);
            Write(StopPattern, 18);
        }

        return new BarcodeMatrix(width, height, modules, isLinear: false, QuietZone);
    }
}
