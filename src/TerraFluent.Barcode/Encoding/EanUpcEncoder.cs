namespace TerraFluent.Barcode.Encoding;

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

    // EAN-5 add-on parity, indexed by its check value; EAN-2 parity is indexed by the value modulo 4.
    private static readonly string[] Ean5Parity =
    {
        "GGLLL", "GLGLL", "GLLGL", "GLLLG", "LGGLL", "LLGGL", "LLLGG", "LGLGL", "LGLLG", "LLGLG",
    };

    private static readonly string[] Ean2Parity = { "LL", "LG", "GL", "GG" };

    /// <summary>Gap between the main symbol and an add-on, in modules (the standard allows 7-12).</summary>
    internal const int AddOnGap = 9;

    public static BarcodeMatrix EncodeEan13(string value, string? addOn = null)
    {
        var digits = Normalize(value, 13, "EAN-13");
        var layout = new LinearLayout();
        layout.ExtendedBars.Add((0, 3));
        layout.ExtendedBars.Add((45, 50));
        layout.ExtendedBars.Add((92, 95));
        layout.Items.Add(new TextItem(digits.Substring(0, 1), -5));
        for (var i = 0; i < 6; i++)
        {
            layout.Items.Add(new TextItem(digits.Substring(1 + i, 1), 3 + 7 * i + 3.5));
            layout.Items.Add(new TextItem(digits.Substring(7 + i, 1), 50 + 7 * i + 3.5));
        }

        return EncodeEan13Digits(digits, quietZone: 11, layout, addOn);
    }

    public static BarcodeMatrix EncodeUpcA(string value, string? addOn = null)
    {
        var digits = Normalize(value, 12, "UPC-A");
        var layout = new LinearLayout();
        layout.ExtendedBars.Add((0, 10));  // start guard and the number-system digit
        layout.ExtendedBars.Add((45, 50));
        layout.ExtendedBars.Add((85, 95)); // check digit and end guard
        layout.Items.Add(new TextItem(digits.Substring(0, 1), -4.5, small: true));
        for (var i = 1; i < 6; i++) layout.Items.Add(new TextItem(digits.Substring(i, 1), 3 + 7 * i + 3.5));
        for (var i = 0; i < 5; i++) layout.Items.Add(new TextItem(digits.Substring(6 + i, 1), 50 + 7 * i + 3.5));
        layout.Items.Add(new TextItem(digits.Substring(11, 1), 95 + 4.5, small: true));
        return EncodeEan13Digits("0" + digits, quietZone: 9, layout, addOn);
    }

    public static BarcodeMatrix EncodeEan8(string value)
    {
        var digits = Normalize(value, 8, "EAN-8");
        var writer = new ModuleWriter();
        writer.AppendBits(EdgeGuard);
        for (var i = 0; i < 4; i++) writer.AppendBits(LCodes[digits[i] - '0']);
        writer.AppendBits(CentreGuard);
        for (var i = 4; i < 8; i++) writer.AppendBits(RCode(digits[i] - '0'));
        writer.AppendBits(EdgeGuard);

        var layout = new LinearLayout();
        layout.ExtendedBars.Add((0, 3));
        layout.ExtendedBars.Add((31, 36));
        layout.ExtendedBars.Add((64, 67));
        for (var i = 0; i < 4; i++)
        {
            layout.Items.Add(new TextItem(digits.Substring(i, 1), 3 + 7 * i + 3.5));
            layout.Items.Add(new TextItem(digits.Substring(4 + i, 1), 36 + 7 * i + 3.5));
        }

        return BarcodeMatrix.FromLinear(writer.Modules, 7, layout);
    }

    /// <summary>Validates a 2- or 5-digit EAN/UPC add-on.</summary>
    internal static string ValidateAddOn(string? addOn, string paramName)
    {
        if (addOn is null) throw new ArgumentNullException(paramName);
        if ((addOn.Length != 2 && addOn.Length != 5) || addOn.Any(c => c < '0' || c > '9'))
        {
            throw new ArgumentException("An add-on must be exactly 2 or 5 digits (EAN-2 or EAN-5).", paramName);
        }

        return addOn;
    }

    private static void AppendAddOn(ModuleWriter writer, LinearLayout layout, string addOn)
    {
        writer.Append(false, AddOnGap);
        layout.AddOnStart = writer.Count;
        string parity;
        if (addOn.Length == 2)
        {
            parity = Ean2Parity[int.Parse(addOn, System.Globalization.CultureInfo.InvariantCulture) % 4];
        }
        else
        {
            int Digit(int i) => addOn[i] - '0';
            parity = Ean5Parity[(3 * (Digit(0) + Digit(2) + Digit(4)) + 9 * (Digit(1) + Digit(3))) % 10];
        }

        writer.AppendBits("1011");
        for (var i = 0; i < addOn.Length; i++)
        {
            if (i > 0) writer.AppendBits("01");
            var d = addOn[i] - '0';
            layout.Items.Add(new TextItem(addOn.Substring(i, 1), writer.Count + 3.5, aboveAddOn: true));
            writer.AppendBits(parity[i] == 'L' ? LCodes[d] : GCode(d));
        }
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

    private static BarcodeMatrix EncodeEan13Digits(string digits, int quietZone, LinearLayout layout, string? addOn)
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
        if (addOn is not null) AppendAddOn(writer, layout, addOn);
        return BarcodeMatrix.FromLinear(writer.Modules, quietZone, layout);
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
