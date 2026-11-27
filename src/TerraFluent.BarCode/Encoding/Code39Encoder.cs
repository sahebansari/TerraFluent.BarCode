namespace TerraFluent.BarCode.Encoding;

/// <summary>
/// Code 39 (ISO/IEC 16388). Each character is five bars and four spaces, three
/// of them wide; characters are separated by a narrow gap and the symbol is
/// framed by <c>*</c> start/stop characters.
/// </summary>
internal static class Code39Encoder
{
    internal const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%";

    // 9-bit element patterns (bar, space, bar, ...), most significant bit first; 1 = wide.
    private static readonly int[] Patterns =
    {
        0x034, 0x121, 0x061, 0x160, 0x031, 0x130, 0x070, 0x025, 0x124, 0x064, // 0-9
        0x109, 0x049, 0x148, 0x019, 0x118, 0x058, 0x00D, 0x10C, 0x04C, 0x01C, // A-J
        0x103, 0x043, 0x142, 0x013, 0x112, 0x052, 0x007, 0x106, 0x046, 0x016, // K-T
        0x181, 0x0C1, 0x1C0, 0x091, 0x190, 0x0D0,                             // U-Z
        0x085, 0x184, 0x0C4, 0x0A8, 0x0A2, 0x08A, 0x02A,                      // - . space $ / + %
    };

    private const int StartStop = 0x094; // '*'
    private const int WideRatio = 3;

    public const int QuietZone = 10;

    /// <summary>Longest accepted value, in characters.</summary>
    public const int MaxLength = 128;

    // Full ASCII (extended) Code 39: each ASCII character as one or two Code 39 characters.
    private static readonly string[] FullAsciiTable = BuildFullAsciiTable();

    public static BarcodeMatrix Encode(string value, bool includeChecksum, bool fullAscii = false)
    {
        Compatibility.Guard.MaxLength(value, MaxLength, "Code 39", nameof(value));
        if (fullAscii) value = ToFullAscii(value);
        var indices = new List<int>(value.Length + 1);
        for (var i = 0; i < value.Length; i++)
        {
            var index = Alphabet.IndexOf(value[i]);
            if (index < 0)
            {
                var hint = char.IsLower(value[i]) ? " Code 39 has no lower-case letters; call FullAscii() to encode them, or use Code 128." : string.Empty;
                throw new ArgumentException(
                    $"Character '{value[i]}' at index {i} cannot be encoded in Code 39. Allowed: 0-9, A-Z, space and - . $ / + %.{hint}",
                    nameof(value));
            }

            indices.Add(index);
        }

        if (includeChecksum) indices.Add(Checksum(indices));

        var writer = new ModuleWriter();
        AppendCharacter(writer, StartStop);
        foreach (var index in indices)
        {
            writer.Append(false, 1);
            AppendCharacter(writer, Patterns[index]);
        }

        writer.Append(false, 1);
        AppendCharacter(writer, StartStop);
        return BarcodeMatrix.FromLinear(writer.Modules, QuietZone);
    }

    /// <summary>Maps ASCII text to the Code 39 character pairs of the full ASCII extension.</summary>
    internal static string ToFullAscii(string value)
    {
        var sb = new System.Text.StringBuilder(value.Length * 2);
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (c > 127)
            {
                throw new ArgumentException(
                    $"Code 39 full ASCII supports ASCII characters (0-127) only; '{c}' at index {i} is not ASCII.", nameof(value));
            }

            sb.Append(FullAsciiTable[c]);
        }

        return sb.ToString();
    }

    private static string[] BuildFullAsciiTable()
    {
        var t = new string[128];
        t[0] = "%U";
        for (var c = 1; c <= 26; c++) t[c] = "$" + (char)('A' + c - 1);
        for (var c = 27; c <= 31; c++) t[c] = "%" + (char)('A' + c - 27);
        t[' '] = " ";
        const string punct = "!\"#$%&'()*+,"; // 33-44 -> /A-/L
        for (var i = 0; i < punct.Length; i++) t[punct[i]] = "/" + (char)('A' + i);
        t['-'] = "-";
        t['.'] = ".";
        t['/'] = "/O";
        for (var c = '0'; c <= '9'; c++) t[c] = c.ToString();
        t[':'] = "/Z";
        const string lowPunct = ";<=>?"; // 59-63 -> %F-%J
        for (var i = 0; i < lowPunct.Length; i++) t[lowPunct[i]] = "%" + (char)('F' + i);
        t['@'] = "%V";
        for (var c = 'A'; c <= 'Z'; c++) t[c] = c.ToString();
        const string brackets = "[\\]^_"; // 91-95 -> %K-%O
        for (var i = 0; i < brackets.Length; i++) t[brackets[i]] = "%" + (char)('K' + i);
        t['`'] = "%W";
        for (var c = 'a'; c <= 'z'; c++) t[c] = "+" + (char)(c - 32);
        const string braces = "{|}~"; // 123-126 -> %P-%S
        for (var i = 0; i < braces.Length; i++) t[braces[i]] = "%" + (char)('P' + i);
        t[127] = "%T";
        return t;
    }

    /// <summary>The modulo 43 check character for <paramref name="value"/>.</summary>
    internal static char ChecksumCharacter(string value)
    {
        var indices = value.Select(c => Alphabet.IndexOf(c)).ToList();
        return Alphabet[Checksum(indices)];
    }

    private static int Checksum(IEnumerable<int> indices) => indices.Sum() % 43;

    private static void AppendCharacter(ModuleWriter writer, int pattern)
    {
        for (var bit = 8; bit >= 0; bit--)
        {
            var wide = ((pattern >> bit) & 1) == 1;
            writer.Append(bit % 2 == 0, wide ? WideRatio : 1);
        }
    }
}
