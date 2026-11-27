namespace TerraFluent.BarCode.Rendering;

/// <summary>Resolved layout and colour settings shared by all renderers.</summary>
internal sealed class RenderOptions
{
    public RenderOptions(int moduleSize, int barHeight, int quietZone, BarcodeColor foreground, BarcodeColor background, int fontSize = 0)
    {
        ModuleSize = moduleSize;
        BarHeight = barHeight;
        QuietZone = quietZone;
        Foreground = foreground;
        Background = background;
        FontSize = fontSize;
    }

    /// <summary>Size of human-readable text in pixels per em, or 0 for no text.</summary>
    public int FontSize { get; }

    /// <summary>Extra height below the bars for human-readable text.</summary>
    public int TextBand => FontSize == 0 ? 0 : (int)Math.Ceiling(FontSize * 1.05);

    /// <summary>Baseline of text under the bars.</summary>
    public double TextBaseline => BarHeight + FontSize * (0.15 + Rendering.TextFont.CapHeightRatio);

    /// <summary>Bottom of EAN/UPC guard bars, which extend beside the digits.</summary>
    public double GuardBottom => BarHeight + FontSize * 0.5;

    /// <summary>Baseline of add-on digits, which sit above the add-on bars.</summary>
    public double AddOnBaseline => FontSize * (0.1 + Rendering.TextFont.CapHeightRatio);

    /// <summary>Top of the add-on bars when add-on digits are shown above them.</summary>
    public double AddOnTop => FontSize * 0.95;

    public int ModuleSize { get; }
    public int BarHeight { get; }
    public int QuietZone { get; }
    public BarcodeColor Foreground { get; }
    public BarcodeColor Background { get; }

    /// <summary>
    /// Output size in pixels. Linear codes get the quiet zone left and right only.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The image would exceed <see cref="BarcodeBuilder.MaxImageDimension"/> on a side
    /// or <see cref="BarcodeBuilder.MaxImagePixels"/> in total.
    /// </exception>
    public (int Width, int Height) ImageSize(BarcodeMatrix matrix)
    {
        var width = ((long)matrix.Width + 2L * QuietZone) * ModuleSize;
        var height = matrix.IsLinear ? BarHeight + (long)TextBand : ((long)matrix.Height + 2L * QuietZone) * ModuleSize;
        if (width > BarcodeBuilder.MaxImageDimension || height > BarcodeBuilder.MaxImageDimension || width * height > BarcodeBuilder.MaxImagePixels)
        {
            throw new ArgumentException(
                $"The image would be {width} x {height} pixels, which exceeds the limit of {BarcodeBuilder.MaxImageDimension} pixels per side " +
                $"and {BarcodeBuilder.MaxImagePixels} pixels in total. Reduce ModuleSize, Height or QuietZone, or encode a shorter value.");
        }

        return ((int)width, (int)height);
    }

    /// <summary>Whether the output pixel (<paramref name="px"/>, <paramref name="py"/>) is dark.</summary>
    public bool IsDark(BarcodeMatrix matrix, int px, int py)
    {
        var mx = px / ModuleSize - QuietZone;
        if (mx < 0 || mx >= matrix.Width) return false;
        if (matrix.IsLinear) return matrix.Get(mx, 0);
        var my = py / ModuleSize - QuietZone;
        return my >= 0 && my < matrix.Height && matrix.Get(mx, my);
    }
}
