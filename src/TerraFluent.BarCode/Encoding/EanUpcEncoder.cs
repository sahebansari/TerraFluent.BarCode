namespace TerraFluent.BarCode.Encoding;

/// <summary>EAN-13, EAN-8 and UPC-A (ISO/IEC 15420).</summary>
internal static class EanUpcEncoder
{
    private static readonly string[] LCodes =
    {
        "0001101", "0011001", "0010011", "0111101", "0100011",
        "0110001", "0101111", "0111011", "0110111", "0001011",
    };

    // Which left-half digits use odd (L) or even (G) parity, chosen by the first EAN-13 digit.
    private static readonly string[] Ean13Parity =
    {
        "LLLLLL", "LLGLGG", "LLGGLG", "LLGGGL", "LGLLGG",
        "LGGLLG", "LGGGLL", "LGLGLG", "LGLGGL", "LGGLGL",
    };

    private const string EdgeGuard = "101";
    private const string CentreGuard = "01010";

    public static BarcodeMatrix EncodeEan13(string value) => EncodeEan13Digits(Normalize(value, 13, "EAN-13"), quietZone: 11);

    public static BarcodeMatrix EncodeUpcA(string value) => EncodeEan13Digits("0" + Normalize(value, 12, "UPC-A"), quietZone: 9);

    public static BarcodeMatrix EncodeEan8(string value)
    {
        var digits = Normalize(value, 8, "EAN-8");
        var writer = new ModuleWriter();
        writer.AppendBits(EdgeGuard);
        for (var i = 0; i < 4; i++) writer.AppendBits(LCodes[digits[i] - '0']);
        writer.AppendBits(CentreGuard);
        for (var i = 4; i < 8; i++) writer.AppendBits(RCode(digits[i] - '0'));
        writer.AppendBits(EdgeGuard);
        return BarcodeMatrix.FromLinear(writer.Modules, 7);
    }

    /// <summary>
    /// Validates the digits and returns them with the check digit. A value one
    /// digit short of <paramref name="length"/> gets its check digit appended;
    /// a full-length value must carry a correct one.
    /// </summary>
    internal static string Normalize(string value, int length, string name)
    {
        foreach (var c in value)
        {
            if (c < '0' || c > '9') throw new ArgumentException($"{name} values must contain digits only.", nameof(value));
        }

        if (value.Length == length - 1) return value + CheckDigit(value);
        if (value.Length != length)
        {
            throw new ArgumentException(
                $"{name} needs {length - 1} digits (check digit added for you) or {length} digits including the check digit; got {value.Length}.",
                nameof(value));
        }

        var expected = CheckDigit(value.Substring(0, length - 1));
        if (value[length - 1] != expected)
        {
            throw new ArgumentException(
                $"Invalid {name} check digit: '{value[length - 1]}' should be '{expected}'.",
                nameof(value));
        }

        return value;
    }

    /// <summary>GS1 modulo 10 check digit: weights 3,1,3,... from the rightmost data digit.</summary>
    internal static char CheckDigit(string data)
    {
        var sum = 0;
        for (var i = 0; i < data.Length; i++)
        {
            var digit = data[data.Length - 1 - i] - '0';
            sum += i % 2 == 0 ? digit * 3 : digit;
        }

        return (char)('0' + (10 - sum % 10) % 10);
    }

    private static BarcodeMatrix EncodeEan13Digits(string digits, int quietZone)
    {
        var parity = Ean13Parity[digits[0] - '0'];
        var writer = new ModuleWriter();
        writer.AppendBits(EdgeGuard);
        for (var i = 1; i <= 6; i++)
        {
            var d = digits[i] - '0';
            writer.AppendBits(parity[i - 1] == 'L' ? LCodes[d] : GCode(d));
        }

        writer.AppendBits(CentreGuard);
        for (var i = 7; i <= 12; i++) writer.AppendBits(RCode(digits[i] - '0'));
        writer.AppendBits(EdgeGuard);
        return BarcodeMatrix.FromLinear(writer.Modules, quietZone);
    }

    private static string RCode(int digit)
    {
        var chars = LCodes[digit].ToCharArray();
        for (var i = 0; i < chars.Length; i++) chars[i] = chars[i] == '1' ? '0' : '1';
        return new string(chars);
    }

    private static string GCode(int digit)
    {
        var chars = RCode(digit).ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }
}
