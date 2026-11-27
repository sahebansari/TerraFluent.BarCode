namespace TerraFluent.BarCode.Rendering;

/// <summary>
/// A vector description of a styled barcode, in output pixels: a background,
/// layers of shapes that share one paint, and an optional logo on top. Both
/// the SVG renderer and the rasteriser draw from it, so every output format
/// shows the same picture.
/// </summary>
internal sealed class Scene
{
    public Scene(int width, int height, BarcodeColor background)
    {
        Width = width;
        Height = height;
        Background = background;
    }

    public int Width { get; }
    public int Height { get; }
    public BarcodeColor Background { get; }
    public List<Layer> Layers { get; } = new();
    public LogoPlacement? Logo { get; set; }

    /// <summary>Whether any shape has rounded corners (SVG then needs anti-aliasing).</summary>
    public bool HasCurves => Layers.Any(l => l.Shapes.Any(s => s.HasCurves));
}

/// <summary>Shapes filled with one paint. Their union is filled, so touching shapes leave no seams.</summary>
internal sealed class Layer
{
    public Layer(Paint paint) => Paint = paint;

    public Paint Paint { get; }
    public List<Shape> Shapes { get; } = new();
}

/// <summary>A logo drawn over the centre of the symbol.</summary>
internal sealed class LogoPlacement
{
    public LogoPlacement(RgbaImage image, byte[] png, int x, int y, int width, int height)
    {
        Image = image;
        Png = png;
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public RgbaImage Image { get; }

    /// <summary>The original PNG file, embedded as-is in SVG output.</summary>
    public byte[] Png { get; }

    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int Height { get; }
}

/// <summary>
/// A rectangle with an independent radius per corner, optionally with a
/// rounded-rectangle hole (used for the frame of a QR eye).
/// </summary>
internal sealed class Shape
{
    public Shape(double x, double y, double width, double height, double topLeft, double topRight, double bottomRight, double bottomLeft, Shape? hole = null)
    {
        var max = Math.Min(width, height) / 2;
        X = x;
        Y = y;
        Width = width;
        Height = height;
        TopLeft = Math.Min(topLeft, max);
        TopRight = Math.Min(topRight, max);
        BottomRight = Math.Min(bottomRight, max);
        BottomLeft = Math.Min(bottomLeft, max);
        Hole = hole;
    }

    public double X { get; }
    public double Y { get; }
    public double Width { get; }
    public double Height { get; }
    public double TopLeft { get; }
    public double TopRight { get; }
    public double BottomRight { get; }
    public double BottomLeft { get; }
    public Shape? Hole { get; }

    public bool HasCurves => TopLeft > 0 || TopRight > 0 || BottomRight > 0 || BottomLeft > 0 || (Hole?.HasCurves ?? false);

    /// <summary>A plain rectangle on whole pixels, which the rasteriser can fill without sampling.</summary>
    public bool IsPixelAlignedRect =>
        !HasCurves && Hole is null && IsWhole(X) && IsWhole(Y) && IsWhole(Width) && IsWhole(Height);

    public static Shape Rounded(double x, double y, double width, double height, double radius, Shape? hole = null) =>
        new(x, y, width, height, radius, radius, radius, radius, hole);

    public bool Contains(double px, double py)
    {
        if (px < X || py < Y || px >= X + Width || py >= Y + Height) return false;
        if (!InsideCorners(px, py)) return false;
        return Hole is null || !Hole.Contains(px, py);
    }

    private bool InsideCorners(double px, double py)
    {
        double right = X + Width, bottom = Y + Height;
        if (px < X + TopLeft && py < Y + TopLeft) return Within(px, py, X + TopLeft, Y + TopLeft, TopLeft);
        if (px > right - TopRight && py < Y + TopRight) return Within(px, py, right - TopRight, Y + TopRight, TopRight);
        if (px > right - BottomRight && py > bottom - BottomRight) return Within(px, py, right - BottomRight, bottom - BottomRight, BottomRight);
        if (px < X + BottomLeft && py > bottom - BottomLeft) return Within(px, py, X + BottomLeft, bottom - BottomLeft, BottomLeft);
        return true;
    }

    private static bool Within(double px, double py, double cx, double cy, double r)
    {
        double dx = px - cx, dy = py - cy;
        return dx * dx + dy * dy <= r * r;
    }

    private static bool IsWhole(double value) => Math.Abs(value - Math.Round(value)) < 1e-9;
}

/// <summary>A solid colour or a two-stop gradient spread over a box.</summary>
internal sealed class Paint
{
    private Paint(BarcodeColor start, BarcodeColor end, GradientDirection? direction, double x, double y, double width, double height)
    {
        Start = start;
        End = end;
        Direction = direction;
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public BarcodeColor Start { get; }
    public BarcodeColor End { get; }

    /// <summary><c>null</c> for a solid colour (<see cref="Start"/>).</summary>
    public GradientDirection? Direction { get; }

    public double X { get; }
    public double Y { get; }
    public double Width { get; }
    public double Height { get; }

    /// <summary>Radius of a radial gradient: half the box diagonal, so the corners get the end colour.</summary>
    public double Radius => Math.Sqrt(Width * Width + Height * Height) / 2;

    public static Paint Solid(BarcodeColor color) => new(color, color, null, 0, 0, 0, 0);

    public static Paint Gradient(BarcodeColor start, BarcodeColor end, GradientDirection direction, double x, double y, double width, double height) =>
        new(start, end, direction, x, y, width, height);

    public BarcodeColor At(double px, double py)
    {
        if (Direction is null) return Start;
        double t;
        switch (Direction.Value)
        {
            case GradientDirection.Horizontal:
                t = Width > 0 ? (px - X) / Width : 0;
                break;
            case GradientDirection.Vertical:
                t = Height > 0 ? (py - Y) / Height : 0;
                break;
            case GradientDirection.Diagonal:
                var lengthSquared = Width * Width + Height * Height;
                t = lengthSquared > 0 ? ((px - X) * Width + (py - Y) * Height) / lengthSquared : 0;
                break;
            default:
                double dx = px - (X + Width / 2), dy = py - (Y + Height / 2);
                t = Radius > 0 ? Math.Sqrt(dx * dx + dy * dy) / Radius : 0;
                break;
        }

        t = t < 0 ? 0 : t > 1 ? 1 : t;
        return new BarcodeColor(Lerp(Start.R, End.R, t), Lerp(Start.G, End.G, t), Lerp(Start.B, End.B, t));
    }

    private static byte Lerp(byte a, byte b, double t) => (byte)Math.Round(a + (b - a) * t);
}
