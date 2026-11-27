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

    public static int Positive(int value, string paramName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, value, "Value must be greater than zero.");
        }

        return value;
    }

    public static int NonNegative(int value, string paramName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, value, "Value must be zero or greater.");
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
