using System.Text;

namespace TerraFluent.Barcode.Encoding;

/// <summary>
/// GS1 DataBar Expanded (ISO/IEC 24724), single row: GS1 element strings
/// compacted into 12-bit data characters (encodation method 1 for a leading
/// GTIN, otherwise general-purpose numeric, alphanumeric and ISO 646
/// compaction), paired around finder patterns and protected by a modulo 211
/// check character.
/// </summary>
internal static class DataBarExpandedEncoder
{
    /// <summary>Symbol characters at most, the check character included.</summary>
    private const int MaxSymbolCharacters = 22;

    private const char Fnc1 = Code128Encoder.Fnc1;

    // ISO/IEC 24724 Table 12: 17-module characters, four bars and four spaces each.
    private static readonly (int Last, int OddModules, int EvenModules, int OddWidest, int EvenWidest, int EvenCount, int First)[] Groups =
    {
        (347, 12, 5, 7, 2, 4, 0),
        (1387, 10, 7, 5, 4, 20, 348),
        (2947, 8, 9, 4, 5, 52, 1388),
        (3987, 6, 11, 3, 6, 104, 2948),
        (4191, 4, 13, 1, 8, 204, 3988),
    };

    // ISO/IEC 24724 Table 13: finder patterns A to F.
    private static readonly int[][] Finders =
    {
        new[] { 1, 8, 4, 1, 1 },
        new[] { 3, 6, 4, 1, 1 },
        new[] { 3, 4, 6, 1, 1 },
        new[] { 3, 2, 8, 1, 1 },
        new[] { 2, 6, 5, 1, 1 },
        new[] { 2, 2, 9, 1, 1 },
    };

    // ISO/IEC 24724 Table 14: finder sequences by number of finders (2 to 11). Each entry is
    // 2 x finder + variant, variant 1 being the reversed pattern (A1 = 0, A2 = 1, B1 = 2, ...).
    private static readonly int[][] Sequences =
    {
        new[] { 0, 1 },
        new[] { 0, 3, 2 },
        new[] { 0, 5, 2, 7 },
        new[] { 0, 9, 2, 7, 4 },
        new[] { 0, 9, 2, 7, 6, 11 },
        new[] { 0, 9, 2, 7, 8, 11, 10 },
        new[] { 0, 1, 2, 3, 4, 5, 6, 7 },
        new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 },
        new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 },
        new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
    };

    private const string AlphanumericSpecials = "*,-./";
    private const string Iso646Specials = "!\"%&'()*+,-./:;<=>?_ ";

    private enum Mode { Numeric, Alphanumeric, Iso646 }

    /// <summary>
    /// Encodes the GS1 data as <see cref="Gs1.Result.Encoded"/> gives it: a leading
    /// FNC1, AIs and data, and FNC1 after each variable-length field.
    /// </summary>
    public static BarcodeMatrix Encode(string value)
    {
        var data = value[0] == Fnc1 ? value.Substring(1) : value;
        var bits = new StringBuilder("0"); // linkage flag: no 2D component
        string rest;
        var header = bits.Length;
        if (data.Length >= 16 && data.StartsWith("01", StringComparison.Ordinal))
        {
            // Method 1: the GTIN's indicator digit and next 12 digits; its check digit is implied.
            bits.Append('1').Append("XX");
            Append(bits, data[2] - '0', 4);
            for (var i = 0; i < 4; i++) Append(bits, int.Parse(data.Substring(3 + 3 * i, 3), System.Globalization.CultureInfo.InvariantCulture), 10);
            rest = data.Substring(16);
        }
        else
        {
            bits.Append("00").Append("XX");
            rest = data;
        }

        var vlsIndex = bits.ToString().IndexOf('X', header);
        var mode = GeneralPurpose(bits, rest);

        var dataCharacters = Math.Max(3, (bits.Length + 11) / 12);
        if (dataCharacters + 1 > MaxSymbolCharacters)
        {
            throw new ArgumentException(
                $"The value is too long for GS1 DataBar Expanded, which holds {MaxSymbolCharacters - 1} data characters of 12 bits (about 74 digits or 41 letters). Shorten the data or use GS1-128.",
                nameof(value));
        }

        // Pad with "00100" (a latch in the alphanumeric and ISO 646 modes), after an alphanumeric latch if still numeric.
        var padding = dataCharacters * 12 - bits.Length;
        var pad = new StringBuilder();
        if (mode == Mode.Numeric) pad.Append("0000");
        while (pad.Length < padding) pad.Append("00100");
        bits.Append(pad.ToString(0, padding));

        var symbolCharacters = dataCharacters + 1;
        bits[vlsIndex] = symbolCharacters % 2 == 1 ? '1' : '0';
        bits[vlsIndex + 1] = symbolCharacters > 14 ? '1' : '0';

        var values = new int[dataCharacters];
        for (var i = 0; i < dataCharacters; i++) values[i] = Convert.ToInt32(bits.ToString(12 * i, 12), 2);
        return Build(values);
    }

    private static Mode GeneralPurpose(StringBuilder bits, string data)
    {
        var mode = Mode.Numeric;
        var i = 0;
        while (i < data.Length)
        {
            var c = data[i];
            switch (mode)
            {
                case Mode.Numeric:
                    if (IsNumeric(c) && i + 1 < data.Length && IsNumeric(data[i + 1]) && !(c == Fnc1 && data[i + 1] == Fnc1))
                    {
                        Append(bits, 11 * NumericValue(c) + NumericValue(data[i + 1]) + 8, 7);
                        i += 2;
                    }
                    else
                    {
                        bits.Append("0000");
                        mode = Mode.Alphanumeric;
                    }

                    break;

                case Mode.Alphanumeric:
                    if (c >= '0' && c <= '9') Append(bits, c - '0' + 5, 5);
                    else if (c == Fnc1) { bits.Append("01111"); mode = Mode.Numeric; }
                    else if (c >= 'A' && c <= 'Z') Append(bits, c - 'A' + 32, 6);
                    else if (AlphanumericSpecials.IndexOf(c) >= 0) Append(bits, 58 + AlphanumericSpecials.IndexOf(c), 6);
                    else
                    {
                        bits.Append("00100");
                        mode = Mode.Iso646;
                        continue;
                    }

                    i++;
                    break;

                default:
                    if (c >= '0' && c <= '9') Append(bits, c - '0' + 5, 5);
                    else if (c == Fnc1) { bits.Append("01111"); mode = Mode.Numeric; }
                    else if (c >= 'A' && c <= 'Z') Append(bits, c - 'A' + 64, 7);
                    else if (c >= 'a' && c <= 'z') Append(bits, c - 'a' + 90, 7);
                    else Append(bits, 232 + Iso646Specials.IndexOf(c), 8);
                    i++;
                    break;
            }

            if (bits.Length > MaxSymbolCharacters * 12) break; // too long; reported by the caller
        }

        return mode;
    }

    private static bool IsNumeric(char c) => (c >= '0' && c <= '9') || c == Fnc1;

    private static int NumericValue(char c) => c == Fnc1 ? 10 : c - '0';

    private static void Append(StringBuilder bits, int value, int length)
    {
        for (var i = length - 1; i >= 0; i--) bits.Append(((value >> i) & 1) != 0 ? '1' : '0');
    }

    private static BarcodeMatrix Build(int[] values)
    {
        var symbolCharacters = values.Length + 1;
        var finders = (symbolCharacters + 1) / 2;
        var sequence = Sequences[finders - 2];
        var characters = new int[symbolCharacters][];
        for (var i = 0; i < values.Length; i++) characters[i + 1] = Character(values[i]);

        // Each data character's weights are 3^k mod 211 for one of 23 rows, chosen by
        // the finder it sits beside and on which side: row 2 x finder + side - 1.
        var checksum = 0;
        for (var i = 1; i < symbolCharacters; i++)
        {
            var row = 2 * sequence[i / 2] + i % 2 - 1;
            var weight = PowMod(3, 8 * row, 211);
            foreach (var width in characters[i])
            {
                checksum += width * weight;
                weight = weight * 3 % 211;
            }
        }

        characters[0] = Character(211 * (symbolCharacters - 4) + checksum % 211);

        var widths = new List<int> { 1, 1 };
        for (var pair = 0; pair < finders; pair++)
        {
            widths.AddRange(characters[2 * pair]);
            var finder = Finders[sequence[pair] / 2];
            widths.AddRange(sequence[pair] % 2 == 0 ? finder : DataBarWidths.Reversed(finder));
            if (2 * pair + 1 < symbolCharacters) widths.AddRange(DataBarWidths.Reversed(characters[2 * pair + 1]));
        }

        widths.Add(1);
        widths.Add(1);
        return BarcodeMatrix.FromLinear(DataBarWidths.ToModules(widths), DataBarEncoder.QuietZone);
    }

    private static int[] Character(int value)
    {
        foreach (var g in Groups)
        {
            if (value > g.Last) continue;
            var v = value - g.First;
            return DataBarWidths.Character(v / g.EvenCount, v % g.EvenCount, g.OddModules, g.EvenModules, 4, g.OddWidest, g.EvenWidest, oddNoNarrow: false);
        }

        throw new InvalidOperationException("Data character value out of range.");
    }

    private static int PowMod(int b, int e, int m)
    {
        var result = 1;
        for (var i = 0; i < e; i++) result = result * b % m;
        return result;
    }
}
