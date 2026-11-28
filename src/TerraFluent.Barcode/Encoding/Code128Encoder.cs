namespace TerraFluent.Barcode.Encoding;

/// <summary>
/// Code 128 (ISO/IEC 15417). Accepts ASCII 0-127 and picks code sets A, B and C
/// automatically: set C packs runs of digits two per symbol, set A covers
/// control characters and set B covers lower-case letters.
/// </summary>
internal static class Code128Encoder
{
    // Bar/space widths for symbol values 0-106 (106 = stop, including the termination bar).
    internal static readonly string[] Patterns =
    {
        "212222", "222122", "222221", "121223", "121322", "131222", "122213", "122312", "132212", "221213",
        "221312", "231212", "112232", "122132", "122231", "113222", "123122", "123221", "223211", "221132",
        "221231", "213212", "223112", "312131", "311222", "321122", "321221", "312212", "322112", "322211",
        "212123", "212321", "232121", "111323", "131123", "131321", "112313", "132113", "132311", "211313",
        "231113", "231311", "112133", "112331", "132131", "113123", "113321", "133121", "313121", "211331",
        "231131", "213113", "213311", "213131", "311123", "311321", "331121", "312113", "312311", "332111",
        "314111", "221411", "431111", "111224", "111422", "121124", "121421", "141122", "141221", "112214",
        "112412", "122114", "122411", "142112", "142211", "241211", "221114", "413111", "241112", "134111",
        "111242", "121142", "121241", "114212", "124112", "124211", "411212", "421112", "421211", "212141",
        "214121", "412121", "111143", "111341", "131141", "114113", "114311", "411113", "411311", "113141",
        "114131", "311141", "411131", "211412", "211214", "211232", "2331112",
    };

    /// <summary>Stands for the FNC1 function character in values passed by the GS1-128 encoder.</summary>
    internal const char Fnc1 = '\uF001';

    private const int Fnc1Value = 102;
    private const int CodeC = 99;
    private const int CodeB = 100;
    private const int CodeA = 101;
    private const int StartA = 103;
    private const int StartB = 104;
    private const int StartC = 105;
    private const int Stop = 106;

    public const int QuietZone = 10;

    /// <summary>Longest accepted value, in characters.</summary>
    public const int MaxLength = 256;

    private enum CodeSet { A, B, C }

    public static BarcodeMatrix Encode(string value, bool allowFnc1 = false)
    {
        var values = EncodeValues(value, allowFnc1);
        var writer = new ModuleWriter();
        foreach (var v in values) writer.AppendWidths(Patterns[v]);
        return BarcodeMatrix.FromLinear(writer.Modules, QuietZone);
    }

    /// <summary>Returns the symbol values: start, data, checksum and stop.</summary>
    internal static List<int> EncodeValues(string value, bool allowFnc1 = false)
    {
        Compatibility.Guard.MaxLength(value, MaxLength, "Code 128", nameof(value));
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] > 127 && !(allowFnc1 && value[i] == Fnc1))
            {
                throw new ArgumentException(
                    $"Code 128 supports ASCII characters (0-127) only; '{value[i]}' at index {i} is not ASCII.",
                    nameof(value));
            }
        }

        var codes = new List<int>(value.Length + 4);
        var set = ChooseStartSet(value);
        codes.Add(set switch { CodeSet.A => StartA, CodeSet.B => StartB, _ => StartC });

        var pos = 0;
        while (pos < value.Length)
        {
            if (value[pos] == Fnc1)
            {
                codes.Add(Fnc1Value); // FNC1 has the same value in every code set
                pos++;
                continue;
            }

            if (set == CodeSet.C)
            {
                if (DigitRun(value, pos) >= 2)
                {
                    codes.Add((value[pos] - '0') * 10 + (value[pos + 1] - '0'));
                    pos += 2;
                    continue;
                }

                set = NeedsSetA(value, pos) ? CodeSet.A : CodeSet.B;
                codes.Add(set == CodeSet.A ? CodeA : CodeB);
                continue;
            }

            // In A or B: switch to C for a long enough digit run (an even
            // number of digits; an odd leading digit stays in the current set).
            var run = DigitRun(value, pos);
            var atEnd = pos + run == value.Length;
            if (run >= (atEnd ? 4 : 6))
            {
                if (run % 2 == 1)
                {
                    codes.Add(ValueIn(set, value[pos]));
                    pos++;
                }

                set = CodeSet.C;
                codes.Add(CodeC);
                continue;
            }

            var ch = value[pos];
            if (!InSet(set, ch))
            {
                set = set == CodeSet.A ? CodeSet.B : CodeSet.A;
                codes.Add(set == CodeSet.A ? CodeA : CodeB);
            }

            codes.Add(ValueIn(set, ch));
            pos++;
        }

        var checksum = codes[0];
        for (var i = 1; i < codes.Count; i++) checksum += codes[i] * i;
        codes.Add(checksum % 103);
        codes.Add(Stop);
        return codes;
    }

    private static CodeSet ChooseStartSet(string value)
    {
        var from = 0;
        while (from < value.Length && value[from] == Fnc1) from++;
        var run = DigitRun(value, from);
        if (run >= 4 || (run == 2 && value.Length - from == 2)) return CodeSet.C;
        return NeedsSetA(value, from) ? CodeSet.A : CodeSet.B;
    }

    /// <summary>Whether a control character appears before the next lower-case character.</summary>
    private static bool NeedsSetA(string value, int from)
    {
        for (var i = from; i < value.Length; i++)
        {
            if (value[i] == Fnc1) continue;
            if (value[i] < 32) return true;
            if (value[i] >= 96) return false;
        }

        return false;
    }

    private static int DigitRun(string value, int from)
    {
        var i = from;
        while (i < value.Length && value[i] >= '0' && value[i] <= '9') i++;
        return i - from;
    }

    private static bool InSet(CodeSet set, char ch) => set == CodeSet.A ? ch < 96 : ch >= 32;

    private static int ValueIn(CodeSet set, char ch) => set == CodeSet.A
        ? (ch < 32 ? ch + 64 : ch - 32)
        : ch - 32;
}
