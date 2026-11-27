namespace TerraFluent.BarCode.Rendering;

/// <summary>Resolved layout and colour settings shared by all renderers.</summary>
internal sealed class RenderOptions
{
    public RenderOptions(int moduleSize, int barHeight, int quietZone, BarcodeColor foreground, BarcodeColor background, BarcodeStyle? style = null)
    {
        ModuleSize = moduleSize;
        BarHeight = barHeight;
        QuietZone = quietZone;
        Foreground = foreground;
        Background = background;
        Style = style;
    }

    public int ModuleSize { get; }
    public int BarHeight { get; }
    public int QuietZone { get; }
    public BarcodeColor Foreground { get; }
    public BarcodeColor Background { get; }

    /// <summary>Decorative style; <c>null</c> or plain means the classic fast paths are used.</summary>
    public BarcodeStyle? Style { get; }

    public bool IsStyled => Style is not null && !Style.IsPlain;

    /// <summary>Output size in pixels. Linear codes get the quiet zone left and right only.</summary>
    public (int Width, int Height) ImageSize(BarcodeMatrix matrix)
    {
        var width = checked((matrix.Width + 2 * QuietZone) * ModuleSize);
        var height = matrix.IsLinear ? BarHeight : checked((matrix.Height + 2 * QuietZone) * ModuleSize);
        return (width, height);
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
