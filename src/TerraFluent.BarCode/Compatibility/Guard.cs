namespace TerraFluent.BarCode.Compatibility;

/// <summary>Shared argument validation helpers for public API boundaries.</summary>
internal static class Guard
{
    public static string NotNullOrEmpty(string? value, string paramName)
    {
        if (value is null) throw new ArgumentNullException(paramName);
        if (value.Length == 0) throw new ArgumentException("Value must not be empty.", paramName);
        return value;
    }

    public static string MaxLength(string value, int max, string symbology, string paramName)
    {
        if (value.Length > max)
        {
            throw new ArgumentException(
                $"{symbology} values are limited to {max} characters (got {value.Length}); longer symbols are too wide for scanners to read reliably. Use a QR code for long data.",
                paramName);
        }

        return value;
    }

    public static int InRange(int value, int min, int max, string paramName)
    {
        if (value < min || value > max)
        {
            throw new ArgumentOutOfRangeException(paramName, value, $"Value must be between {min} and {max}.");
        }

        return value;
    }
}
