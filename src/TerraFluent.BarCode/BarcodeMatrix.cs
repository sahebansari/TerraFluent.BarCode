namespace TerraFluent.BarCode;

/// <summary>
/// The encoded symbol as a grid of dark/light modules, before any scaling,
/// colouring or quiet zone. A linear (1D) barcode is a single row; a QR code
/// is a square. Useful for custom renderers.
/// </summary>
public sealed class BarcodeMatrix
{
    private readonly bool[] _modules;

    internal BarcodeMatrix(int width, int height, bool[] modules, bool isLinear, int recommendedQuietZone)
    {
        if (modules.Length != width * height) throw new ArgumentException("Module count does not match size.", nameof(modules));
        Width = width;
        Height = height;
        _modules = modules;
        IsLinear = isLinear;
        RecommendedQuietZone = recommendedQuietZone;
    }

    /// <summary>Width in modules.</summary>
    public int Width { get; }

    /// <summary>Height in modules (1 for linear barcodes).</summary>
    public int Height { get; }

    /// <summary><c>true</c> for 1D barcodes, whose single row is stretched to the bar height when rendered.</summary>
    public bool IsLinear { get; }

    /// <summary>The quiet zone, in modules, that the symbology's specification asks for on each side.</summary>
    public int RecommendedQuietZone { get; }

    /// <summary>Whether the module at (<paramref name="x"/>, <paramref name="y"/>) is dark.</summary>
    public bool this[int x, int y]
    {
        get
        {
            if ((uint)x >= (uint)Width) throw new ArgumentOutOfRangeException(nameof(x));
            if ((uint)y >= (uint)Height) throw new ArgumentOutOfRangeException(nameof(y));
            return _modules[y * Width + x];
        }
    }

    internal bool Get(int x, int y) => _modules[y * Width + x];

    /// <summary>Text placement for linear barcodes; <c>null</c> for 2D symbols.</summary>
    internal Encoding.LinearLayout? Layout { get; set; }

    internal static BarcodeMatrix FromLinear(IReadOnlyList<bool> bars, int recommendedQuietZone, Encoding.LinearLayout? layout = null)
    {
        var modules = new bool[bars.Count];
        for (var i = 0; i < modules.Length; i++) modules[i] = bars[i];
        return new BarcodeMatrix(modules.Length, 1, modules, isLinear: true, recommendedQuietZone) { Layout = layout ?? new Encoding.LinearLayout() };
    }

    /// <summary>Renders the matrix as text, one line per row, for debugging (<c>#</c> = dark).</summary>
    public override string ToString()
    {
        var sb = new System.Text.StringBuilder((Width + 1) * Height);
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++) sb.Append(Get(x, y) ? '#' : '.');
            if (y < Height - 1) sb.Append('\n');
        }

        return sb.ToString();
    }
}
