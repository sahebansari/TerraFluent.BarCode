namespace TerraFluent.BarCode.Rendering;

internal enum ShapeKind { Rect, RoundRect, Circle, Diamond, CornerRect }

/// <summary>Which corners of a <see cref="ShapeKind.CornerRect"/> are rounded.</summary>
[Flags]
internal enum Corners { None = 0, TopLeft = 1, TopRight = 2, BottomRight = 4, BottomLeft = 8 }

/// <summary>One filled shape in pixel coordinates, optionally with a hole cut out of it.</summary>
internal sealed class Shape
{
    public Shape(ShapeKind kind, double x, double y, double w, double h, double radius = 0, Shape? hole = null, Corners rounded = Corners.None)
    {
        Rounded = rounded;
        Kind = kind;
        X = x;
        Y = y;
        W = w;
        H = h;
        Radius = radius;
        Hole = hole;
    }

    public ShapeKind Kind { get; }
    public double X { get; }
    public double Y { get; }
    public double W { get; }
    public double H { get; }
    public double Radius { get; }
    public Shape? Hole { get; }
    public Corners Rounded { get; }

    public bool Contains(double px, double py)
    {
        if (!ContainsOuter(px, py)) return false;
        return Hole is null || !Hole.ContainsOuter(px, py);
    }

    private bool ContainsOuter(double px, double py)
    {
        if (px < X || py < Y || px >= X + W || py >= Y + H) return false;
        switch (Kind)
        {
            case ShapeKind.Rect:
                return true;
            case ShapeKind.RoundRect:
            {
                var dx = Math.Max(Math.Max(X + Radius - px, px - (X + W - Radius)), 0);
                var dy = Math.Max(Math.Max(Y + Radius - py, py - (Y + H - Radius)), 0);
                return dx * dx + dy * dy <= Radius * Radius;
            }
            case ShapeKind.CornerRect:
            {
                var left = px < X + Radius;
                var right = px > X + W - Radius;
                var top = py < Y + Radius;
                var bottom = py > Y + H - Radius;
                Corners corner = top && left ? Corners.TopLeft : top && right ? Corners.TopRight
                    : bottom && right ? Corners.BottomRight : bottom && left ? Corners.BottomLeft : Corners.None;
                if (corner == Corners.None || (Rounded & corner) == 0) return true;
                var cx = left ? X + Radius : X + W - Radius;
                var cy = top ? Y + Radius : Y + H - Radius;
                return (px - cx) * (px - cx) + (py - cy) * (py - cy) <= Radius * Radius;
            }
            case ShapeKind.Circle:
            {
                double cx = X + W / 2, cy = Y + H / 2, r = W / 2;
                return (px - cx) * (px - cx) + (py - cy) * (py - cy) <= r * r;
            }
            default:
                return Math.Abs(px - (X + W / 2)) / (W / 2) + Math.Abs(py - (Y + H / 2)) / (H / 2) <= 1;
        }
    }

    /// <summary>True when the shape covers whole pixels exactly (no anti-aliasing needed).</summary>
    public bool IsPixelAligned =>
        Kind == ShapeKind.Rect && Hole is null &&
        X == Math.Floor(X) && Y == Math.Floor(Y) && W == Math.Floor(W) && H == Math.Floor(H);
}

/// <summary>
/// A styled barcode as vector shapes: the common input of the styled SVG
/// writer and the raster canvas used for PNG and JPEG.
/// </summary>
internal sealed class Scene
{
    public int Width { get; set; }
    public int Height { get; set; }

    /// <summary>Bars or data modules, filled with the foreground colour or gradient.</summary>
    public List<Shape> Foreground { get; } = new();

    /// <summary>QR finder patterns, filled with the finder colour if one is set.</summary>
    public List<Shape> Finders { get; } = new();

    /// <summary>Symbol area (without quiet zone) that gradients span.</summary>
    public (double X, double Y, double W, double H) SymbolBounds { get; set; }

    /// <summary>Square box in which the logo is fitted, if any.</summary>
    public (double X, double Y, double Size)? LogoBox { get; set; }

    public BarcodeColor Background { get; set; }
    public bool Transparent { get; set; }
    public BarcodeColor ForegroundColor { get; set; }
    public BarcodeColor? FinderColor { get; set; }
    public BarcodeColor? GradientStart { get; set; }
    public BarcodeColor? GradientEnd { get; set; }
    public GradientDirection GradientDirection { get; set; }
    public byte[]? Logo { get; set; }

    /// <summary>Gradient position (0-1) of a point, matching SVG userSpaceOnUse gradients.</summary>
    public double GradientT(double px, double py)
    {
        var (gx, gy, gw, gh) = SymbolBounds;
        double t;
        switch (GradientDirection)
        {
            case GradientDirection.Horizontal:
                t = (px - gx) / gw;
                break;
            case GradientDirection.Vertical:
                t = (py - gy) / gh;
                break;
            case GradientDirection.Diagonal:
                t = ((px - gx) * gw + (py - gy) * gh) / (gw * gw + gh * gh);
                break;
            default:
                var r = Math.Sqrt(gw * gw + gh * gh) / 2;
                var dx = px - (gx + gw / 2);
                var dy = py - (gy + gh / 2);
                t = Math.Sqrt(dx * dx + dy * dy) / r;
                break;
        }

        return t < 0 ? 0 : t > 1 ? 1 : t;
    }

    public BarcodeColor ForegroundAt(double px, double py)
    {
        if (GradientStart is not BarcodeColor a || GradientEnd is not BarcodeColor b) return ForegroundColor;
        var t = GradientT(px, py);
        return new BarcodeColor(Lerp(a.R, b.R, t), Lerp(a.G, b.G, t), Lerp(a.B, b.B, t));
    }

    private static byte Lerp(byte a, byte b, double t) => (byte)Math.Round(a + (b - a) * t);
}
