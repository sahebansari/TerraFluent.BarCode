namespace TerraFluent.Barcode.Encoding;

/// <summary>
/// GS1 DataBar Omnidirectional and GS1 DataBar Limited (ISO/IEC 24724): a
/// GTIN-14 (check digit not encoded) split into data characters whose bar and
/// space widths come from the (n, k) width enumeration, plus finder or check
/// patterns selected by a weighted checksum.
/// </summary>
internal static class DataBarEncoder
{
    /// <summary>DataBar needs no quiet zone beyond its guards; one module keeps edge detection simple.</summary>
    public const int QuietZone = 1;

    /// <summary>Largest 13-digit value (GTIN without check digit) DataBar Limited accepts: indicator digit 0 or 1.</summary>
    private const long LimitedMax = 1_999_999_999_999;

    // A data character group: value range, odd/even module counts, widest odd/even element,
    // number of odd and even width combinations used, and the group's first value.
    private readonly struct Group
    {
        public Group(int last, int oddModules, int evenModules, int oddWidest, int evenWidest, int oddCount, int evenCount, int first)
        {
            Last = last;
            OddModules = oddModules;
            EvenModules = evenModules;
            OddWidest = oddWidest;
            EvenWidest = evenWidest;
            OddCount = oddCount;
            EvenCount = evenCount;
            First = first;
        }

        public int Last { get; }
        public int OddModules { get; }
        public int EvenModules { get; }
        public int OddWidest { get; }
        public int EvenWidest { get; }
        public int OddCount { get; }
        public int EvenCount { get; }
        public int First { get; }
    }

    // ISO/IEC 24724 Table 3: outside characters (16 modules) of DataBar Omnidirectional.
    private static readonly Group[] Outside =
    {
        new(160, 12, 4, 8, 1, 161, 1, 0),
        new(960, 10, 6, 6, 3, 80, 10, 161),
        new(2014, 8, 8, 4, 5, 31, 34, 961),
        new(2714, 6, 10, 3, 6, 10, 70, 2015),
        new(2840, 4, 12, 1, 8, 1, 126, 2715),
    };

    // ISO/IEC 24724 Table 4: inside characters (15 modules).
    private static readonly Group[] Inside =
    {
        new(335, 5, 10, 2, 7, 4, 84, 0),
        new(1035, 7, 8, 4, 5, 20, 35, 336),
        new(1515, 9, 6, 6, 3, 48, 10, 1036),
        new(1596, 11, 4, 8, 1, 81, 1, 1516),
    };

    // ISO/IEC 24724 Table 6: DataBar Limited characters (26 modules, seven bars and seven spaces).
    private static readonly Group[] Limited =
    {
        new(183063, 17, 9, 6, 3, 6538, 28, 0),
        new(820063, 13, 13, 5, 4, 875, 728, 183064),
        new(1000775, 9, 17, 3, 6, 28, 6454, 820064),
        new(1491020, 15, 11, 5, 4, 2415, 203, 1000776),
        new(1979844, 11, 15, 4, 5, 203, 2408, 1491021),
        new(1996938, 19, 7, 8, 1, 17094, 1, 1979845),
        new(2013570, 7, 19, 1, 8, 1, 16632, 1996939),
    };

    // ISO/IEC 24724 Table 5: the nine DataBar Omnidirectional finder patterns.
    private static readonly int[][] Finders =
    {
        new[] { 3, 8, 2, 1, 1 },
        new[] { 3, 5, 5, 1, 1 },
        new[] { 3, 3, 7, 1, 1 },
        new[] { 3, 1, 9, 1, 1 },
        new[] { 2, 7, 4, 1, 1 },
        new[] { 2, 5, 6, 1, 1 },
        new[] { 2, 3, 8, 1, 1 },
        new[] { 1, 5, 7, 1, 1 },
        new[] { 1, 3, 9, 1, 1 },
    };

    // ISO/IEC 24724 Table 8: the 89 DataBar Limited check character patterns (14 elements, 18 modules), by checksum.
    private static readonly byte[] LimitedCheckPatterns =
    {
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 3, 3, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 3, 2, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 3, 3, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 2, 1, 1, 3, 2, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 2, 1, 2, 3, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 3, 1, 1, 3, 1, 1, 1,
        1, 1, 1, 1, 1, 2, 1, 1, 1, 1, 3, 2, 1, 1,
        1, 1, 1, 1, 1, 2, 1, 1, 1, 2, 3, 1, 1, 1,
        1, 1, 1, 1, 1, 2, 1, 2, 1, 1, 3, 1, 1, 1,
        1, 1, 1, 1, 1, 3, 1, 1, 1, 1, 3, 1, 1, 1,
        1, 1, 1, 2, 1, 1, 1, 1, 1, 1, 3, 2, 1, 1,
        1, 1, 1, 2, 1, 1, 1, 1, 1, 2, 3, 1, 1, 1,
        1, 1, 1, 2, 1, 1, 1, 2, 1, 1, 3, 1, 1, 1,
        1, 1, 1, 2, 1, 2, 1, 1, 1, 1, 3, 1, 1, 1,
        1, 1, 1, 3, 1, 1, 1, 1, 1, 1, 3, 1, 1, 1,
        1, 2, 1, 1, 1, 1, 1, 1, 1, 1, 3, 2, 1, 1,
        1, 2, 1, 1, 1, 1, 1, 1, 1, 2, 3, 1, 1, 1,
        1, 2, 1, 1, 1, 1, 1, 2, 1, 1, 3, 1, 1, 1,
        1, 2, 1, 1, 1, 2, 1, 1, 1, 1, 3, 1, 1, 1,
        1, 2, 1, 2, 1, 1, 1, 1, 1, 1, 3, 1, 1, 1,
        1, 3, 1, 1, 1, 1, 1, 1, 1, 1, 3, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 2, 3, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 2, 3, 2, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 2, 2, 1, 2, 2, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 3, 2, 1, 2, 1, 1, 1,
        1, 1, 1, 1, 1, 2, 1, 1, 2, 1, 2, 2, 1, 1,
        1, 1, 1, 1, 1, 2, 1, 1, 2, 2, 2, 1, 1, 1,
        1, 1, 1, 1, 1, 2, 1, 2, 2, 1, 2, 1, 1, 1,
        1, 1, 1, 1, 1, 3, 1, 1, 2, 1, 2, 1, 1, 1,
        1, 1, 1, 2, 1, 1, 1, 1, 2, 1, 2, 2, 1, 1,
        1, 1, 1, 2, 1, 1, 1, 1, 2, 2, 2, 1, 1, 1,
        1, 1, 1, 2, 1, 1, 1, 2, 2, 1, 2, 1, 1, 1,
        1, 1, 1, 2, 1, 2, 1, 1, 2, 1, 2, 1, 1, 1,
        1, 1, 1, 3, 1, 1, 1, 1, 2, 1, 2, 1, 1, 1,
        1, 2, 1, 1, 1, 1, 1, 1, 2, 1, 2, 2, 1, 1,
        1, 2, 1, 1, 1, 1, 1, 1, 2, 2, 2, 1, 1, 1,
        1, 2, 1, 1, 1, 1, 1, 2, 2, 1, 2, 1, 1, 1,
        1, 2, 1, 1, 1, 2, 1, 1, 2, 1, 2, 1, 1, 1,
        1, 2, 1, 2, 1, 1, 1, 1, 2, 1, 2, 1, 1, 1,
        1, 3, 1, 1, 1, 1, 1, 1, 2, 1, 2, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 3, 1, 1, 3, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 3, 2, 1, 2, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 2, 3, 1, 1, 2, 1, 1,
        1, 1, 1, 2, 1, 1, 1, 1, 3, 1, 1, 2, 1, 1,
        1, 2, 1, 1, 1, 1, 1, 1, 3, 1, 1, 2, 1, 1,
        1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 2, 3, 1, 1,
        1, 1, 1, 1, 1, 1, 2, 1, 1, 2, 2, 2, 1, 1,
        1, 1, 1, 1, 1, 1, 2, 1, 1, 3, 2, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 2, 2, 1, 1, 2, 2, 1, 1,
        1, 1, 1, 2, 1, 1, 2, 1, 1, 1, 2, 2, 1, 1,
        1, 1, 1, 2, 1, 1, 2, 1, 1, 2, 2, 1, 1, 1,
        1, 1, 1, 2, 1, 1, 2, 2, 1, 1, 2, 1, 1, 1,
        1, 1, 1, 2, 1, 2, 2, 1, 1, 1, 2, 1, 1, 1,
        1, 1, 1, 3, 1, 1, 2, 1, 1, 1, 2, 1, 1, 1,
        1, 2, 1, 1, 1, 1, 2, 1, 1, 1, 2, 2, 1, 1,
        1, 2, 1, 1, 1, 1, 2, 1, 1, 2, 2, 1, 1, 1,
        1, 2, 1, 2, 1, 1, 2, 1, 1, 1, 2, 1, 1, 1,
        1, 1, 1, 1, 2, 1, 1, 1, 1, 1, 2, 3, 1, 1,
        1, 1, 1, 1, 2, 1, 1, 1, 1, 2, 2, 2, 1, 1,
        1, 1, 1, 1, 2, 1, 1, 1, 1, 3, 2, 1, 1, 1,
        1, 1, 1, 1, 2, 1, 1, 2, 1, 1, 2, 2, 1, 1,
        1, 1, 1, 1, 2, 1, 1, 2, 1, 2, 2, 1, 1, 1,
        1, 1, 1, 1, 2, 2, 1, 1, 1, 1, 2, 2, 1, 1,
        1, 2, 1, 1, 2, 1, 1, 1, 1, 1, 2, 2, 1, 1,
        1, 2, 1, 1, 2, 1, 1, 1, 1, 2, 2, 1, 1, 1,
        1, 2, 1, 1, 2, 1, 1, 2, 1, 1, 2, 1, 1, 1,
        1, 2, 1, 1, 2, 2, 1, 1, 1, 1, 2, 1, 1, 1,
        1, 2, 1, 2, 2, 1, 1, 1, 1, 1, 2, 1, 1, 1,
        1, 3, 1, 1, 2, 1, 1, 1, 1, 1, 2, 1, 1, 1,
        1, 1, 2, 1, 1, 1, 1, 1, 1, 1, 2, 3, 1, 1,
        1, 1, 2, 1, 1, 1, 1, 1, 1, 2, 2, 2, 1, 1,
        1, 1, 2, 1, 1, 1, 1, 1, 1, 3, 2, 1, 1, 1,
        1, 1, 2, 1, 1, 1, 1, 2, 1, 1, 2, 2, 1, 1,
        1, 1, 2, 1, 1, 1, 1, 2, 1, 2, 2, 1, 1, 1,
        1, 1, 2, 1, 1, 1, 1, 3, 1, 1, 2, 1, 1, 1,
        1, 1, 2, 1, 1, 2, 1, 1, 1, 1, 2, 2, 1, 1,
        1, 1, 2, 1, 1, 2, 1, 1, 1, 2, 2, 1, 1, 1,
        1, 1, 2, 2, 1, 1, 1, 1, 1, 1, 2, 2, 1, 1,
        2, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 1, 1,
        2, 1, 1, 1, 1, 1, 1, 1, 1, 3, 2, 1, 1, 1,
        2, 1, 1, 1, 1, 1, 1, 2, 1, 1, 2, 2, 1, 1,
        2, 1, 1, 1, 1, 1, 1, 2, 1, 2, 2, 1, 1, 1,
        2, 1, 1, 1, 1, 1, 1, 3, 1, 1, 2, 1, 1, 1,
        2, 1, 1, 1, 1, 2, 1, 1, 1, 2, 2, 1, 1, 1,
        2, 1, 1, 1, 1, 2, 1, 2, 1, 1, 2, 1, 1, 1,
        2, 1, 1, 2, 1, 1, 1, 1, 1, 2, 2, 1, 1, 1,
        2, 1, 1, 1, 1, 1, 1, 1, 2, 2, 1, 2, 1, 1,
    };

    public static BarcodeMatrix EncodeOmnidirectional(string value)
    {
        var gtin = ParseGtin(value, "GS1 DataBar");
        var number = long.Parse(gtin.Substring(0, 13), System.Globalization.CultureInfo.InvariantCulture);
        var left = number / 4537077;
        var right = number % 4537077;
        var characters = new[]
        {
            Character((int)(left / 1597), Outside, 4, outside: true),
            Character((int)(left % 1597), Inside, 4, outside: false),
            Character((int)(right / 1597), Outside, 4, outside: true),
            Character((int)(right % 1597), Inside, 4, outside: false),
        };

        // Weights are 3^i mod 79 over the 32 elements in character order.
        var checksum = 0;
        var weight = 1;
        foreach (var character in characters)
        {
            foreach (var width in character)
            {
                checksum += width * weight;
                weight = weight * 3 % 79;
            }
        }

        checksum %= 79;
        if (checksum >= 8) checksum++;
        if (checksum >= 72) checksum++;

        // Guard, character 1, left finder, character 2 (reversed) | character 4, right finder (reversed), character 3 (reversed), guard.
        var widths = new List<int>(46) { 1, 1 };
        widths.AddRange(characters[0]);
        widths.AddRange(Finders[checksum / 9]);
        widths.AddRange(DataBarWidths.Reversed(characters[1]));
        widths.AddRange(characters[3]);
        widths.AddRange(DataBarWidths.Reversed(Finders[checksum % 9]));
        widths.AddRange(DataBarWidths.Reversed(characters[2]));
        widths.Add(1);
        widths.Add(1);
        return Linear(widths, gtin);
    }

    public static BarcodeMatrix EncodeLimited(string value)
    {
        var gtin = ParseGtin(value, "GS1 DataBar Limited");
        var number = long.Parse(gtin.Substring(0, 13), System.Globalization.CultureInfo.InvariantCulture);
        if (number > LimitedMax)
        {
            throw new ArgumentException("GS1 DataBar Limited encodes a GTIN-14 whose first digit (the indicator) is 0 or 1. Use GS1 DataBar for other GTINs.", nameof(value));
        }

        var left = Character((int)(number / 2013571), Limited, 7, outside: true);
        var right = Character((int)(number % 2013571), Limited, 7, outside: true);

        // Weights are 3^i mod 89 over the 28 elements of both characters.
        var checksum = 0;
        var weight = 1;
        foreach (var width in left.Concat(right))
        {
            checksum += width * weight;
            weight = weight * 3 % 89;
        }

        checksum %= 89;
        var widths = new List<int>(47) { 1, 1 };
        widths.AddRange(left);
        for (var i = 0; i < 14; i++) widths.Add(LimitedCheckPatterns[checksum * 14 + i]);
        widths.AddRange(right);
        widths.Add(1);
        widths.Add(1);
        widths.Add(5); // the 5-module space after the right guard (ISO/IEC 24724:2011)
        return Linear(widths, gtin);
    }

    private static BarcodeMatrix Linear(List<int> widths, string gtin)
    {
        var matrix = BarcodeMatrix.FromLinear(DataBarWidths.ToModules(widths), QuietZone);
        matrix.Layout!.CentredText = "(01)" + gtin;
        return matrix;
    }

    /// <summary>The widths of one data character, odd and even elements interleaved.</summary>
    private static int[] Character(int value, Group[] groups, int elementsEach, bool outside)
    {
        foreach (var g in groups)
        {
            if (value > g.Last) continue;
            var v = value - g.First;
            // Outside characters (and Limited) take the odd value from the quotient; inside characters from the remainder.
            int odd, even;
            if (outside)
            {
                odd = v / g.EvenCount;
                even = v % g.EvenCount;
            }
            else
            {
                even = v / g.OddCount;
                odd = v % g.OddCount;
            }

            return DataBarWidths.Character(odd, even, g.OddModules, g.EvenModules, elementsEach, g.OddWidest, g.EvenWidest, oddNoNarrow: outside);
        }

        throw new InvalidOperationException("Data character value out of range.");
    }

    /// <summary>Accepts 13 digits (check digit added), 14 digits (validated), either optionally after "(01)" or "01" with 14 digits.</summary>
    internal static string ParseGtin(string value, string symbology)
    {
        var digits = value;
        if (digits.StartsWith("(01)", StringComparison.Ordinal)) digits = digits.Substring(4);
        else if (digits.Length == 16 && digits.StartsWith("01", StringComparison.Ordinal)) digits = digits.Substring(2);
        if (digits.Length is not (13 or 14) || digits.Any(c => c < '0' || c > '9'))
        {
            throw new ArgumentException($"{symbology} encodes a GTIN: 13 digits (the check digit is added) or 14 digits, optionally written after (01).", nameof(value));
        }

        var check = EanUpcEncoder.CheckDigit(digits.Substring(0, 13));
        if (digits.Length == 13) return digits + check;
        if (digits[13] != check)
        {
            throw new ArgumentException($"The GTIN check digit should be {check}, not {digits[13]}.", nameof(value));
        }

        return digits;
    }
}
