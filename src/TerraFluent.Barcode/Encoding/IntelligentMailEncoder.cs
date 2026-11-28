using System.Numerics;

namespace TerraFluent.Barcode.Encoding;

/// <summary>
/// USPS Intelligent Mail barcode (USPS-B-3200): a 20-digit tracking code and
/// an optional 5-, 9- or 11-digit routing code packed into a 102-bit number,
/// protected by an 11-bit CRC, split into ten codewords, mapped to 13-bit
/// characters and spread over 65 four-state bars.
/// </summary>
/// <remarks>
/// The matrix is 129 modules wide (bars one module wide, one module apart) and
/// 6 high: a full bar fills all six rows, an ascender the top four, a
/// descender the bottom four and a tracker the middle two. At the default
/// 2-pixel module and 300 dpi that is close to the specification's 22 bars per
/// inch and 0.125 to 0.165 inch bar height.
/// </remarks>
internal static class IntelligentMailEncoder
{
    public const int TrackingLength = 20;
    public const int Height = 6;
    public const int QuietZone = 6;

    private const int Bars = 65;

    // Bar-to-character mapping, USPS-B-3200 Table 22: for each bar, the character and bit of its
    // descender, then of its ascender.
    private static readonly byte[] BarMap =
    {
        7, 2, 4, 3, 1, 10, 0, 0, 9, 12, 2, 8, 5, 5, 6, 11, 8, 9, 3, 1,
        0, 1, 5, 12, 2, 5, 1, 8, 4, 4, 9, 11, 6, 3, 8, 10, 3, 9, 7, 6,
        5, 11, 1, 4, 8, 5, 2, 12, 9, 10, 0, 2, 7, 1, 6, 7, 3, 6, 4, 9,
        0, 3, 8, 6, 6, 4, 2, 7, 1, 1, 9, 9, 7, 10, 5, 2, 4, 0, 3, 8,
        6, 2, 0, 4, 8, 11, 1, 0, 9, 8, 3, 12, 2, 6, 7, 7, 5, 1, 4, 10,
        1, 12, 6, 9, 7, 3, 8, 0, 5, 8, 9, 7, 4, 6, 2, 10, 3, 4, 0, 5,
        8, 4, 5, 7, 7, 11, 1, 9, 6, 0, 9, 6, 0, 6, 4, 8, 2, 1, 3, 2,
        5, 9, 8, 12, 4, 11, 6, 1, 9, 5, 7, 4, 3, 3, 1, 2, 0, 7, 2, 0,
        1, 3, 4, 1, 6, 10, 3, 5, 8, 7, 9, 4, 2, 11, 5, 6, 0, 8, 7, 12,
        4, 2, 8, 1, 5, 10, 3, 0, 9, 3, 0, 9, 6, 5, 2, 4, 7, 8, 1, 7,
        5, 0, 4, 5, 2, 3, 0, 10, 6, 12, 9, 2, 3, 11, 1, 6, 8, 8, 7, 9,
        5, 4, 0, 11, 1, 5, 2, 2, 9, 1, 4, 12, 8, 3, 6, 6, 7, 0, 3, 7,
        4, 7, 7, 5, 0, 12, 1, 11, 2, 9, 9, 0, 6, 8, 5, 3, 3, 10, 8, 2,
    };

    private static readonly int[] FiveOfThirteen = BuildTable(5, 1287);
    private static readonly int[] TwoOfThirteen = BuildTable(2, 78);

    /// <summary>The bar states as letters: F (full), A (ascender), D (descender), T (tracker).</summary>
    internal static string BarStates(string value)
    {
        var (descenders, ascenders) = Bits(value);
        var result = new char[Bars];
        for (var i = 0; i < Bars; i++) result[i] = descenders[i] ? (ascenders[i] ? 'F' : 'D') : (ascenders[i] ? 'A' : 'T');
        return new string(result);
    }

    public static BarcodeMatrix Encode(string value)
    {
        var (descenders, ascenders) = Bits(value);
        const int width = 2 * Bars - 1;
        var modules = new bool[width * Height];
        for (var bar = 0; bar < Bars; bar++)
        {
            var top = ascenders[bar] ? 0 : 2;
            var bottom = descenders[bar] ? Height : 4;
            for (var y = top; y < bottom; y++) modules[y * width + 2 * bar] = true;
        }

        return new BarcodeMatrix(width, Height, modules, isLinear: false, QuietZone);
    }

    private static (bool[] Descenders, bool[] Ascenders) Bits(string value)
    {
        if (value.Length is not (20 or 25 or 29 or 31) || value.Any(c => c < '0' || c > '9'))
        {
            throw new ArgumentException(
                "An Intelligent Mail barcode needs a 20-digit tracking code followed by an optional 5-, 9- or 11-digit routing (ZIP) code: 20, 25, 29 or 31 digits in all.",
                nameof(value));
        }

        if (value[1] > '4')
        {
            throw new ArgumentException("The second digit of the barcode identifier (the first two digits of the tracking code) must be 0 to 4.", nameof(value));
        }

        // Routing code, offset so that each length has its own range.
        var routing = value.Substring(TrackingLength);
        var number = routing.Length switch
        {
            0 => BigInteger.Zero,
            5 => BigInteger.Parse(routing, System.Globalization.CultureInfo.InvariantCulture) + 1,
            9 => BigInteger.Parse(routing, System.Globalization.CultureInfo.InvariantCulture) + 100000 + 1,
            _ => BigInteger.Parse(routing, System.Globalization.CultureInfo.InvariantCulture) + 1000000000 + 100000 + 1,
        };

        number = number * 10 + (value[0] - '0');
        number = number * 5 + (value[1] - '0');
        for (var i = 2; i < TrackingLength; i++) number = number * 10 + (value[i] - '0');

        var bytes = new byte[13];
        var big = number.ToByteArray(); // little-endian, possibly with a sign byte
        for (var i = 0; i < 13 && i < big.Length; i++) bytes[12 - i] = big[i];
        var fcs = Crc11(bytes);

        var codewords = new int[10];
        codewords[9] = (int)(number % 636);
        number /= 636;
        for (var i = 8; i >= 1; i--)
        {
            codewords[i] = (int)(number % 1365);
            number /= 1365;
        }

        codewords[0] = (int)number;
        codewords[9] *= 2;
        if ((fcs & 0x400) != 0) codewords[0] += 659;

        var characters = new int[10];
        for (var i = 0; i < 10; i++)
        {
            var c = codewords[i] < 1287 ? FiveOfThirteen[codewords[i]] : TwoOfThirteen[codewords[i] - 1287];
            characters[i] = (fcs >> i & 1) != 0 ? ~c & 0x1FFF : c;
        }

        var descenders = new bool[Bars];
        var ascenders = new bool[Bars];
        for (var bar = 0; bar < Bars; bar++)
        {
            descenders[bar] = (characters[BarMap[bar * 4]] >> BarMap[bar * 4 + 1] & 1) != 0;
            ascenders[bar] = (characters[BarMap[bar * 4 + 2]] >> BarMap[bar * 4 + 3] & 1) != 0;
        }

        return (descenders, ascenders);
    }

    /// <summary>The 11-bit frame check sequence over the 102 data bits (the two high bits of the first byte are unused).</summary>
    private static int Crc11(byte[] bytes)
    {
        const int polynomial = 0x0F35;
        var fcs = 0x07FF;
        var data = bytes[0] << 5;
        for (var bit = 2; bit < 8; bit++)
        {
            fcs = ((fcs ^ data) & 0x400) != 0 ? (fcs << 1) ^ polynomial : fcs << 1;
            fcs &= 0x7FF;
            data <<= 1;
        }

        for (var i = 1; i < 13; i++)
        {
            data = bytes[i] << 3;
            for (var bit = 0; bit < 8; bit++)
            {
                fcs = ((fcs ^ data) & 0x400) != 0 ? (fcs << 1) ^ polynomial : fcs << 1;
                fcs &= 0x7FF;
                data <<= 1;
            }
        }

        return fcs;
    }

    /// <summary>
    /// The 13-bit characters with <paramref name="ones"/> bits set: pairs of a
    /// pattern and its bit reversal fill the table from the front, symmetric
    /// patterns from the back.
    /// </summary>
    private static int[] BuildTable(int ones, int length)
    {
        var table = new int[length];
        int lower = 0, upper = length - 1;
        for (var count = 0; count < 8192; count++)
        {
            if (PopCount(count) != ones) continue;
            var reverse = Reverse13(count);
            if (reverse < count) continue;
            if (reverse == count)
            {
                table[upper--] = count;
            }
            else
            {
                table[lower++] = count;
                table[lower++] = reverse;
            }
        }

        return table;
    }

    private static int PopCount(int value)
    {
        var count = 0;
        for (; value != 0; value &= value - 1) count++;
        return count;
    }

    private static int Reverse13(int value)
    {
        var result = 0;
        for (var i = 0; i < 13; i++) result |= (value >> i & 1) << (12 - i);
        return result;
    }

    /// <summary>Each (character, bit) pair is used by exactly one bar half (for tests).</summary>
    internal static bool BarMapIsPermutation()
    {
        var seen = new bool[130];
        for (var i = 0; i < BarMap.Length; i += 2)
        {
            int c = BarMap[i], b = BarMap[i + 1];
            if (c > 9 || b > 12 || seen[c * 13 + b]) return false;
            seen[c * 13 + b] = true;
        }

        return true;
    }
}
