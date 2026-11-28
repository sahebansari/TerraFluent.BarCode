namespace TerraFluent.Barcode.Encoding;

/// <summary>
/// MaxiCode (ISO/IEC 16023): a fixed-size symbol of 884 hexagonal modules in
/// 33 rows of 30 around a bullseye finder, used on parcels. Mode 4 carries
/// 93 data codewords with standard error correction, mode 5 77 codewords with
/// enhanced error correction, mode 6 reader programming. Modes 2 and 3 start
/// with a structured carrier message (postal code, country, service class).
/// </summary>
internal static class MaxiCodeEncoder
{
    public const int Columns = 30;
    public const int Rows = 33;

    /// <summary>ISO/IEC 16023 asks for a quiet zone of one module on every side.</summary>
    public const int QuietZone = 1;

    private const int Pad = 33;
    private const int NumericShift = 31;
    private const int LatchA = 63, LatchB = 63, ShiftA = 59, ShiftB = 59, ShiftC = 60, ShiftD = 61, ShiftE = 62;

    // Code of each Latin-1 character in code sets A and B, or -1.
    private static readonly sbyte[] SetA = new sbyte[256];
    private static readonly sbyte[] SetB = new sbyte[256];

    // For each Latin-1 character, (set << 8) | code in the set that needs no shift if possible (A, then B), or -1.
    private static readonly int[] Lookup = new int[256];

    // Codeword bit (0-863, most significant bit of codeword 0 first) of each module, row by row;
    // Dark = orientation module that is always dark, Light = always light (orientation, bullseye area, unused).
    private const short Dark = -2, Light = -1;
    private static readonly short[] Placement = BuildPlacement();

    static MaxiCodeEncoder()
    {
        for (var i = 0; i < 256; i++)
        {
            SetA[i] = SetB[i] = -1;
            Lookup[i] = -1;
        }

        // Code sets as ISO/IEC 16023 Table 1 assigns them; codes not listed are shifts, latches, pad, ECI and NS.
        Assign(4, 0, Range(0x00, 0x1A));
        Assign(4, 30, "\u001B");
        Assign(4, 32, "\u001C\u001D\u001E\u001F\u009F\u00A0\u00A2\u00A3\u00A4\u00A5\u00A6\u00A7\u00A9\u00AD\u00AE\u00B6");
        Assign(4, 48, Range(0x95, 0x9E));
        Assign(4, 59, " ");
        Assign(3, 0, Range(0xE0, 0xFA));
        Assign(3, 28, "\u001C\u001D\u001E");
        Assign(3, 32, Range(0xFB, 0xFF) + "\u00A1\u00A8\u00AB\u00AF\u00B0\u00B4\u00B7\u00B8\u00BB\u00BF" + Range(0x8A, 0x94));
        Assign(3, 59, " ");
        Assign(2, 0, Range(0xC0, 0xDA));
        Assign(2, 28, "\u001C\u001D\u001E");
        Assign(2, 32, Range(0xDB, 0xDF) + "\u00AA\u00AC\u00B1\u00B2\u00B3\u00B5\u00B9\u00BA\u00BC\u00BD\u00BE" + Range(0x80, 0x89));
        Assign(2, 59, " ");
        Assign(1, 0, "`" + Range('a', 'z'));
        Assign(1, 28, "\u001C\u001D\u001E");
        Assign(1, 32, "{");
        Assign(1, 34, "}~\u007F;<=>?[\\]^_ ,./:@!|");
        Assign(0, 0, "\r" + Range('A', 'Z'));
        Assign(0, 28, "\u001C\u001D\u001E");
        Assign(0, 32, " ");
        Assign(0, 34, "\"#$%&'()*+,-./" + Range('0', '9') + ":");
    }

    private static string Range(int first, int last)
    {
        var chars = new char[last - first + 1];
        for (var i = 0; i < chars.Length; i++) chars[i] = (char)(first + i);
        return new string(chars);
    }

    /// <summary>Gives <paramref name="chars"/> consecutive codes in <paramref name="set"/>. Sets are assigned E to A, so A, then B, wins in <see cref="Lookup"/>.</summary>
    private static void Assign(int set, int firstCode, string chars)
    {
        for (var i = 0; i < chars.Length; i++)
        {
            int c = chars[i], code = firstCode + i;
            if (set == 0) SetA[c] = (sbyte)code;
            if (set == 1 && SetB[c] < 0) SetB[c] = (sbyte)code;
            Lookup[c] = (set << 8) | code;
        }
    }

    /// <summary>
    /// The module sequence of ISO/IEC 16023: codewords 20-135 fill 2 x 3 blocks of
    /// modules in rows of blocks that alternate direction, around the centre; codewords
    /// 136-143 run down the right-hand column; codewords 0-19 (the primary message)
    /// surround the bullseye.
    /// </summary>
    private static short[] BuildPlacement()
    {
        var grid = new short[Columns * Rows];
        for (var i = 0; i < grid.Length; i++) grid[i] = Light;
        void Put(int row, int col, int bit) => grid[row * Columns + col] = (short)bit;

        // A standard block: bits 0-5 at (r, c+1), (r, c), (r+1, c+1), (r+1, c), (r+2, c+1), (r+2, c).
        void Block(int codeword, int row, int col)
        {
            for (var b = 0; b < 6; b++) Put(row + b / 2, col + 1 - b % 2, codeword * 6 + b);
        }

        var next = 20;
        for (var band = 0; band < 11; band++)
        {
            var row = band * 3;
            for (var step = 0; step < 14; step++)
            {
                var col = band % 2 == 0 ? step * 2 : 26 - step * 2;
                var centre = band is >= 3 and <= 7 && col >= (band is 3 or 7 ? 8 : 6) && col <= 20;
                if (!centre) Block(next++, row, col);
            }
        }

        for (var codeword = 136; codeword < 144; codeword++)
        {
            var row = 1 + (codeword - 136) * 4;
            int[] dRow = { 0, 1, 1, 2, 3, 3 }, dCol = { 28, 29, 28, 28, 29, 28 };
            for (var b = 0; b < 6; b++) Put(row + dRow[b], dCol[b], codeword * 6 + b);
        }

        // Primary message codewords 9-19 are standard blocks beside the bullseye.
        int[,] inner = { { 12, 8 }, { 18, 8 }, { 21, 18 }, { 21, 8 }, { 9, 8 }, { 12, 20 }, { 18, 20 }, { 18, 6 }, { 12, 6 }, { 9, 20 }, { 21, 20 } };
        for (var i = 0; i < inner.GetLength(0); i++) Block(9 + i, inner[i, 0], inner[i, 1]);

        // Codewords 0-8 wrap around the bullseye and orientation modules: (row, column) of bits 0-5 of each.
        int[] irregular =
        {
            15, 19, 17, 19, 9, 16, 10, 16, 11, 17, 11, 16,
            22, 13, 22, 12, 23, 13, 23, 12, 21, 17, 22, 16,
            9, 13, 9, 12, 10, 13, 10, 12, 12, 10, 20, 10,
            20, 18, 12, 19, 12, 18, 13, 19, 13, 18, 14, 19,
            23, 15, 23, 14, 18, 19, 19, 19, 19, 18, 20, 19,
            15, 8, 17, 8, 21, 10, 23, 11, 22, 15, 22, 14,
            9, 15, 9, 14, 10, 15, 10, 14, 10, 10, 11, 10,
            17, 21, 9, 19, 9, 18, 10, 19, 11, 19, 11, 18,
            15, 6, 16, 6, 17, 7, 17, 6, 15, 21, 15, 20,
        };
        for (var bit = 0; bit < 54; bit++) Put(irregular[bit * 2], irregular[bit * 2 + 1], bit);

        // Orientation: always-dark modules (the light ones keep the default).
        int[] dark = { 0, 28, 0, 29, 9, 10, 9, 11, 10, 11, 15, 7, 16, 8, 16, 20, 17, 20, 22, 10, 23, 10, 22, 17, 23, 17 };
        for (var i = 0; i < dark.Length; i += 2) Put(dark[i], dark[i + 1], Dark);
        return grid;
    }

    /// <summary>Encodes <paramref name="value"/> in mode 4, 5 or 6.</summary>
    public static BarcodeMatrix Encode(string value, int mode)
    {
        var capacity = mode == 5 ? 77 : 93;
        var data = Message(value, capacity, nameof(value));
        var codewords = new int[144];
        codewords[0] = mode;
        Array.Copy(data, 0, codewords, 1, 9);
        Array.Copy(data, 9, codewords, 20, capacity - 9);
        return Build(codewords, enhanced: mode == 5);
    }

    /// <summary>
    /// Mode 2 (numeric postal code of up to 9 digits) or mode 3 (alphanumeric
    /// postal code of up to 6 characters) with a structured carrier message.
    /// </summary>
    public static BarcodeMatrix EncodeStructured(string postalCode, int countryCode, int serviceClass, string message)
    {
        if (postalCode is null) throw new ArgumentNullException(nameof(postalCode));
        if (message is null) throw new ArgumentNullException(nameof(message));
        Compatibility.Guard.InRange(countryCode, 0, 999, nameof(countryCode));
        Compatibility.Guard.InRange(serviceClass, 0, 999, nameof(serviceClass));
        if (postalCode.Length == 0) throw new ArgumentException("The postal code must not be empty.", nameof(postalCode));

        var codewords = new int[144];
        var numeric = postalCode.Length <= 9 && postalCode.All(c => c is >= '0' and <= '9');
        if (numeric)
        {
            var postcode = int.Parse(postalCode, System.Globalization.CultureInfo.InvariantCulture);
            var length = postalCode.Length;
            codewords[0] = ((postcode & 0x03) << 4) | 2;
            codewords[1] = (postcode & 0xFC) >> 2;
            codewords[2] = (postcode & 0x3F00) >> 8;
            codewords[3] = (postcode & 0xFC000) >> 14;
            codewords[4] = (postcode & 0x3F00000) >> 20;
            codewords[5] = ((postcode & 0x3C000000) >> 26) | ((length & 0x3) << 4);
            codewords[6] = (length & 0x3C) >> 2;
        }
        else
        {
            if (postalCode.Length > 6)
            {
                throw new ArgumentException(
                    $"A MaxiCode postal code is up to 9 digits or up to 6 letters, digits and spaces; got {postalCode.Length} characters.",
                    nameof(postalCode));
            }

            var pc = new int[6];
            for (var i = 0; i < 6; i++)
            {
                var c = i < postalCode.Length ? postalCode[i] : ' ';
                var code = c < 256 ? Lookup[c] : -1;
                if (code < 0 || code >> 8 != 0)
                {
                    throw new ArgumentException(
                        $"Character '{c}' at index {i} cannot be used in an alphanumeric MaxiCode postal code. Allowed: A-Z, 0-9, space and \" # $ % & ' ( ) * + , - . / :.",
                        nameof(postalCode));
                }

                pc[i] = code & 0x3F;
            }

            codewords[0] = ((pc[5] & 0x03) << 4) | 3;
            codewords[1] = ((pc[4] & 0x03) << 4) | ((pc[5] & 0x3C) >> 2);
            codewords[2] = ((pc[3] & 0x03) << 4) | ((pc[4] & 0x3C) >> 2);
            codewords[3] = ((pc[2] & 0x03) << 4) | ((pc[3] & 0x3C) >> 2);
            codewords[4] = ((pc[1] & 0x03) << 4) | ((pc[2] & 0x3C) >> 2);
            codewords[5] = ((pc[0] & 0x03) << 4) | ((pc[1] & 0x3C) >> 2);
            codewords[6] = (pc[0] & 0x3C) >> 2;
        }

        codewords[6] |= (countryCode & 0x3) << 4;
        codewords[7] = (countryCode & 0xFC) >> 2;
        codewords[8] = ((countryCode & 0x300) >> 8) | ((serviceClass & 0xF) << 2);
        codewords[9] = (serviceClass & 0x3F0) >> 4;

        var data = Message(message, 84, nameof(message));
        Array.Copy(data, 0, codewords, 20, 84);
        return Build(codewords, enhanced: false);
    }

    /// <summary>
    /// Text to codewords: Sets A and B by latch or shift, Sets C, D and E by
    /// shift, runs of 9 digits by numeric shift, then padding.
    /// </summary>
    internal static int[] Message(string value, int capacity, string paramName)
    {
        var output = new List<int>(capacity);
        var set = 0;
        var i = 0;
        while (i < value.Length)
        {
            if (DigitRun(value, i) >= 9)
            {
                var n = int.Parse(value.Substring(i, 9), System.Globalization.CultureInfo.InvariantCulture);
                output.Add(NumericShift);
                output.Add((n >> 24) & 0x3F);
                output.Add((n >> 18) & 0x3F);
                output.Add((n >> 12) & 0x3F);
                output.Add((n >> 6) & 0x3F);
                output.Add(n & 0x3F);
                i += 9;
                continue;
            }

            var c = value[i];
            var entry = c < 256 ? Lookup[c] : -1;
            if (entry < 0)
            {
                throw new ArgumentException(
                    $"Character '{c}' at index {i} cannot be encoded in MaxiCode, which supports ISO-8859-1 (Latin-1) text only.",
                    paramName);
            }

            var target = entry >> 8;
            var code = entry & 0x3F;
            if (CodeIn(set, c) is var here && here >= 0)
            {
                output.Add(here);
            }
            else if (target <= 1)
            {
                // Latch for a run of three or more, or a run that reaches the end; shift otherwise.
                var run = 0;
                while (i + run < value.Length && CodeIn(set, value[i + run]) < 0 && CodeIn(target, value[i + run]) >= 0) run++;
                if (run >= 3 || i + run == value.Length)
                {
                    output.Add(target == 0 ? LatchA : LatchB);
                    set = target;
                }
                else
                {
                    output.Add(target == 0 ? ShiftA : ShiftB);
                }

                output.Add(code);
            }
            else
            {
                output.Add(target switch { 2 => ShiftC, 3 => ShiftD, _ => ShiftE });
                output.Add(code);
            }

            i++;
        }

        if (output.Count > capacity)
        {
            throw new ArgumentException(
                $"The value needs {output.Count} MaxiCode codewords but this mode holds {capacity}. Shorten it" +
                (capacity == 77 ? " or use mode 4." : "."),
                paramName);
        }

        while (output.Count < capacity) output.Add(Pad);
        return output.ToArray();
    }

    private static int CodeIn(int set, char c) => c > 0xFF ? -1 : set == 0 ? SetA[c] : SetB[c];

    private static int DigitRun(string value, int start)
    {
        var i = start;
        while (i < value.Length && value[i] is >= '0' and <= '9') i++;
        return i - start;
    }

    private static BarcodeMatrix Build(int[] codewords, bool enhanced)
    {
        var field = GaloisField.MaxiCode;
        field.Encode(codewords, 0, 10, 10);
        var (data, ecc) = enhanced ? (34, 28) : (42, 20);
        field.Encode(codewords, 20, data, ecc, stride: 2);
        field.Encode(codewords, 21, data, ecc, stride: 2);

        var modules = new bool[Columns * Rows];
        for (var i = 0; i < modules.Length; i++)
        {
            int bit = Placement[i];
            modules[i] = bit == Dark || (bit >= 0 && ((codewords[bit / 6] >> (5 - bit % 6)) & 1) == 1);
        }

        return new BarcodeMatrix(Columns, Rows, modules, isLinear: false, QuietZone) { IsHexagonal = true };
    }
}
