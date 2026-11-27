using System.Globalization;

namespace TerraFluent.BarCode;

/// <summary>An opaque RGB colour used for bars/modules and the background.</summary>
public readonly struct BarcodeColor : IEquatable<BarcodeColor>
{
    /// <summary>Creates a colour from red, green and blue components (0-255).</summary>
    public BarcodeColor(byte r, byte g, byte b)
    {
        R = r;
        G = g;
        B = b;
    }

    /// <summary>Red component (0-255).</summary>
    public byte R { get; }

    /// <summary>Green component (0-255).</summary>
    public byte G { get; }

    /// <summary>Blue component (0-255).</summary>
    public byte B { get; }

    /// <summary>Black (<c>#000000</c>).</summary>
    public static BarcodeColor Black => new(0, 0, 0);

    /// <summary>White (<c>#FFFFFF</c>).</summary>
    public static BarcodeColor White => new(255, 255, 255);

    /// <summary>Parses <c>#RGB</c> or <c>#RRGGBB</c> (the leading <c>#</c> is optional).</summary>
    /// <exception cref="FormatException">The text is not a hex colour.</exception>
    public static BarcodeColor FromHex(string hex)
    {
        if (hex is null) throw new ArgumentNullException(nameof(hex));
        var s = hex.Trim();
        if (s.StartsWith("#", StringComparison.Ordinal)) s = s.Substring(1);
        if (s.Length == 3) s = new string(new[] { s[0], s[0], s[1], s[1], s[2], s[2] });
        if (s.Length != 6 || !int.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
        {
            throw new FormatException($"'{hex}' is not a valid #RGB or #RRGGBB colour.");
        }

        return new BarcodeColor((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
    }

    /// <summary>Formats the colour as <c>#RRGGBB</c>.</summary>
    public string ToHex() => "#" + R.ToString("X2", CultureInfo.InvariantCulture) + G.ToString("X2", CultureInfo.InvariantCulture) + B.ToString("X2", CultureInfo.InvariantCulture);

    /// <summary>
    /// Relative luminance from 0 (black) to 1 (white), as defined by WCAG 2.x
    /// for sRGB colours.
    /// </summary>
    public double RelativeLuminance => 0.2126 * Linear(R) + 0.7152 * Linear(G) + 0.0722 * Linear(B);

    /// <summary>
    /// WCAG contrast ratio between two colours, from 1 (identical) to 21 (black on white).
    /// Scanners need strong contrast between bars and background.
    /// </summary>
    public static double ContrastRatio(BarcodeColor a, BarcodeColor b)
    {
        double la = a.RelativeLuminance, lb = b.RelativeLuminance;
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    /// <inheritdoc />
    public bool Equals(BarcodeColor other) => R == other.R && G == other.G && B == other.B;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is BarcodeColor other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => (R << 16) | (G << 8) | B;

    /// <summary>Formats the colour as <c>#RRGGBB</c>.</summary>
    public override string ToString() => ToHex();

    /// <summary>Whether two colours are equal.</summary>
    public static bool operator ==(BarcodeColor left, BarcodeColor right) => left.Equals(right);

    /// <summary>Whether two colours differ.</summary>
    public static bool operator !=(BarcodeColor left, BarcodeColor right) => !left.Equals(right);

    private static double Linear(byte component)
    {
        var c = component / 255.0;
        return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }
}
