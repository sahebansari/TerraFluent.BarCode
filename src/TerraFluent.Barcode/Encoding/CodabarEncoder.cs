namespace TerraFluent.Barcode.Encoding;

/// <summary>
/// Codabar (NW-7, ANSI/AIM BC3). Each character is four bars and three spaces,
/// two or three of them wide, separated by a narrow gap. The symbol is framed
/// by start and stop characters A, B, C or D.
/// </summary>
internal static class CodabarEncoder
{
    internal const string Alphabet = "0123456789-$:/.+ABCD";

    // 7-bit element patterns (bar, space, bar, ...), most significant bit first; 1 = wide.
    private static readonly int[] Patterns =
    {
        0x003, 0x006, 0x009, 0x060, 0x012, 0x042, 0x021, 0x024, 0x030, 0x048, // 0-9
        0x00C, 0x018, 0x045, 0x051, 0x054, 0x015,                             // - $ : / . +
        0x01A, 0x029, 0x00B, 0x00E,                                           // A B C D
    };

    private const int WideRatio = 3;
    private const int DataCharacters = 16;

    public const int QuietZone = 10;

    /// <summary>Longest accepted value, in characters, including start and stop characters.</summary>
    public const int MaxLength = 60;

    /// <summary>
    /// Encodes <paramref name="value"/>. When it begins and ends with one of
    /// A, B, C or D (or the alternative names T, N, * and E), those are the start
    /// and stop characters; otherwise A is used for both.
    /// </summary>
    public static BarcodeMatrix Encode(string value)
    {
        Compatibility.Guard.MaxLength(value, MaxLength, "Codabar", nameof(value));
        var text = Normalise(value);
        if (text.Length < 3)
        {
            throw new ArgumentException("Codabar needs at least one data character between the start and stop characters.", nameof(value));
        }

        var writer = new ModuleWriter();
        for (var i = 0; i < text.Length; i++)
        {
            var index = Alphabet.IndexOf(text[i]);
            var isGuard = i == 0 || i == text.Length - 1;
            if (index < 0 || (index >= DataCharacters) != isGuard)
            {
                var offset = value.Length == text.Length ? i : i - 1;
                var message = index >= DataCharacters
                    ? $"Codabar start/stop character '{value[offset]}' at index {offset} can only appear at the start or end."
                    : $"Character '{value[offset]}' at index {offset} cannot be encoded in Codabar. Allowed: 0-9 and - $ : / . +, framed by A, B, C or D.";
                throw new ArgumentException(message, nameof(value));
            }

            if (i > 0) writer.Append(false, 1);
            var pattern = Patterns[index];
            for (var bit = 6; bit >= 0; bit--)
            {
                var wide = ((pattern >> bit) & 1) == 1;
                writer.Append(bit % 2 == 0, wide ? WideRatio : 1);
            }
        }

        var matrix = BarcodeMatrix.FromLinear(writer.Modules, QuietZone);
        matrix.Layout!.CentredText = value;
        return matrix;
    }

    /// <summary>Upper-cases start/stop letters, maps T, N, * and E to A-D, and adds A...A when there are none.</summary>
    internal static string Normalise(string value)
    {
        var first = Guard(value[0]);
        var last = Guard(value[value.Length - 1]);
        if (value.Length >= 2 && first != '\0' && last != '\0')
        {
            return first + value.Substring(1, value.Length - 2) + last;
        }

        if (first != '\0' || last != '\0')
        {
            throw new ArgumentException(
                "Codabar values either start and end with a start/stop character (A, B, C or D) or have neither; this one has only one.",
                nameof(value));
        }

        return "A" + value + "A";
    }

    private static char Guard(char c) => c switch
    {
        'A' or 'a' or 'T' or 't' => 'A',
        'B' or 'b' or 'N' or 'n' => 'B',
        'C' or 'c' or '*' => 'C',
        'D' or 'd' or 'E' or 'e' => 'D',
        _ => '\0',
    };
}
