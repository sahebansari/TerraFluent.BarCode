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

    public byte R { get; }
    public byte G { get; }
    public byte B { get; }

    public static BarcodeColor Black => new(0, 0, 0);
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

    public bool Equals(BarcodeColor other) => R == other.R && G == other.G && B == other.B;
    public override bool Equals(object? obj) => obj is BarcodeColor other && Equals(other);
    public override int GetHashCode() => (R << 16) | (G << 8) | B;
    public override string ToString() => ToHex();
    public static bool operator ==(BarcodeColor left, BarcodeColor right) => left.Equals(right);
    public static bool operator !=(BarcodeColor left, BarcodeColor right) => !left.Equals(right);
}
