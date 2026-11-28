namespace TerraFluent.Barcode.Encoding;

/// <summary>
/// Interleaved 2 of 5 (ISO/IEC 16390) and ITF-14 (GS1). Digits are encoded in
/// pairs: the first digit of a pair in the five bars, the second in the five
/// spaces between them, two of each five wide. The symbol starts with four
/// narrow elements and ends with a wide bar, a narrow space and a narrow bar.
/// </summary>
internal static class ItfEncoder
{
    // Element widths for each digit, 1 = wide: n n w w n for 0, and so on.
    private static readonly byte[][] Patterns =
    {
        new byte[] { 0, 0, 1, 1, 0 }, // 0
        new byte[] { 1, 0, 0, 0, 1 }, // 1
        new byte[] { 0, 1, 0, 0, 1 }, // 2
        new byte[] { 1, 1, 0, 0, 0 }, // 3
        new byte[] { 0, 0, 1, 0, 1 }, // 4
        new byte[] { 1, 0, 1, 0, 0 }, // 5
        new byte[] { 0, 1, 1, 0, 0 }, // 6
        new byte[] { 0, 0, 0, 1, 1 }, // 7
        new byte[] { 1, 0, 0, 1, 0 }, // 8
        new byte[] { 0, 1, 0, 1, 0 }, // 9
    };

    private const int WideRatio = 3;

    public const int QuietZone = 10;

    /// <summary>Longest accepted value, in digits (including any check digit).</summary>
    public const int MaxLength = 80;

    /// <summary>
    /// Interleaved 2 of 5 for an even number of digits, or an odd number when
    /// <paramref name="includeChecksum"/> adds the modulo 10 check digit.
    /// </summary>
    public static BarcodeMatrix Encode(string value, bool includeChecksum)
    {
        Compatibility.Guard.MaxLength(value, includeChecksum ? MaxLength - 1 : MaxLength, "Interleaved 2 of 5", nameof(value));
        RequireDigits(value, "Interleaved 2 of 5");
        var digits = includeChecksum ? value + EanUpcEncoder.CheckDigit(value) : value;
        if (digits.Length % 2 != 0)
        {
            var hint = includeChecksum
                ? "With the check digit added the count must be even, so pass an odd number of digits (add a leading zero)."
                : "Pass an even number of digits: add a leading zero, or call WithChecksum() to append a check digit.";
            throw new ArgumentException($"Interleaved 2 of 5 encodes digits in pairs; got {digits.Length} digits. {hint}", nameof(value));
        }

        return EncodeDigits(digits);
    }

    /// <summary>ITF-14 from 13 digits (check digit added) or 14 digits (check digit validated).</summary>
    public static BarcodeMatrix EncodeItf14(string value)
    {
        RequireDigits(value, "ITF-14");
        string digits;
        if (value.Length == 13)
        {
            digits = value + EanUpcEncoder.CheckDigit(value);
        }
        else if (value.Length == 14)
        {
            var expected = EanUpcEncoder.CheckDigit(value, 13);
            if (value[13] != expected)
            {
                throw new ArgumentException($"Invalid ITF-14 check digit: '{value[13]}' should be '{expected}'.", nameof(value));
            }

            digits = value;
        }
        else
        {
            throw new ArgumentException(
                $"ITF-14 needs 13 digits (check digit added for you) or 14 digits including the check digit; got {value.Length}.",
                nameof(value));
        }

        return EncodeDigits(digits);
    }

    private static void RequireDigits(string value, string name)
    {
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] is < '0' or > '9')
            {
                throw new ArgumentException($"{name} encodes digits only; '{value[i]}' at index {i} is not a digit.", nameof(value));
            }
        }
    }

    private static BarcodeMatrix EncodeDigits(string digits)
    {
        var writer = new ModuleWriter();
        // Start: narrow bar, narrow space, narrow bar, narrow space.
        writer.Append(true, 1);
        writer.Append(false, 1);
        writer.Append(true, 1);
        writer.Append(false, 1);
        for (var i = 0; i < digits.Length; i += 2)
        {
            var bars = Patterns[digits[i] - '0'];
            var spaces = Patterns[digits[i + 1] - '0'];
            for (var e = 0; e < 5; e++)
            {
                writer.Append(true, bars[e] == 1 ? WideRatio : 1);
                writer.Append(false, spaces[e] == 1 ? WideRatio : 1);
            }
        }

        // Stop: wide bar, narrow space, narrow bar.
        writer.Append(true, WideRatio);
        writer.Append(false, 1);
        writer.Append(true, 1);
        var matrix = BarcodeMatrix.FromLinear(writer.Modules, QuietZone);
        matrix.Layout!.CentredText = digits;
        return matrix;
    }
}
