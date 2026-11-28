using System.Globalization;
using System.Text;

namespace TerraFluent.Barcode.Rendering;

/// <summary>
/// Draws a MaxiCode matrix as hexagons plus the bullseye, in units of the
/// hexagon width (the module size). Rows are <c>sqrt(3)/2</c> apart and odd
/// rows are offset by half a module, so neighbouring hexagons share edges.
/// </summary>
internal static class MaxiCodeGeometry
{
    private static readonly double Sqrt3 = Math.Sqrt(3);

    /// <summary>Circumradius of a hexagon one module wide.</summary>
    private static readonly double HexRadius = 1 / Sqrt3;

    private static readonly double RowPitch = Sqrt3 / 2;

    /// <summary>Height of the 33 rows of hexagons, in modules.</summary>
    public static readonly double SymbolHeight = 32 * RowPitch + 2 * HexRadius;

    // Bullseye centre: halfway along row 16, which the bullseye area is symmetric about.
    private const double CentreX = 14.5;
    private static readonly double CentreY = 16 * RowPitch + HexRadius;

    // Radii of the light centre and the three dark rings, in modules. The nearest hexagon centre
    // is 5 modules away, so the outer ring leaves a gap of about 0.1 module to the hexagons.
    private const double InnerRadius = 0.6;
    private const double RingWidth = 0.74;
    private const int CircleSegments = 96;

    public static void AddSymbol(BarcodeMatrix matrix, Layer layer, double originX, double originY, double m)
    {
        // Every hexagon is the same shape, so the rasteriser samples one per sub-pixel offset.
        var maskKey = new object();
        for (var row = 0; row < matrix.Height; row++)
        {
            for (var col = 0; col < matrix.Width; col++)
            {
                if (!matrix.Get(col, row)) continue;
                var cx = originX + (col + 0.5 + 0.5 * (row & 1)) * m;
                var cy = originY + (row * RowPitch + HexRadius) * m;
                layer.Paths.Add(Hexagon(cx, cy, m, maskKey));
            }
        }

        var bx = originX + CentreX * m;
        var by = originY + CentreY * m;
        for (var ring = 0; ring < 3; ring++)
        {
            var inner = (InnerRadius + 2 * ring * RingWidth) * m;
            layer.Paths.Add(Ring(bx, by, inner + RingWidth * m, inner));
        }
    }

    private static PathShape Hexagon(double cx, double cy, double m, object maskKey)
    {
        double r = HexRadius * m, half = m / 2;
        var points = new[]
        {
            (cx, cy - r), (cx + half, cy - r / 2), (cx + half, cy + r / 2),
            (cx, cy + r), (cx - half, cy + r / 2), (cx - half, cy - r / 2),
        };

        // SVG path data is built from the polygon only when SVG is written.
        return new PathShape(new List<(double X, double Y)[]> { points }, svgPath: null, maskKey);
    }

    /// <summary>An annulus: the outer circle clockwise and the inner one anticlockwise, so non-zero filling leaves the hole.</summary>
    private static PathShape Ring(double cx, double cy, double outer, double inner)
    {
        var contours = new List<(double X, double Y)[]> { Circle(cx, cy, outer, clockwise: true), Circle(cx, cy, inner, clockwise: false) };
        var d = new StringBuilder(160);
        AppendCircle(d, cx, cy, outer, sweep: '1');
        AppendCircle(d, cx, cy, inner, sweep: '0');
        return new PathShape(contours, d.ToString());
    }

    private static (double X, double Y)[] Circle(double cx, double cy, double r, bool clockwise)
    {
        var points = new (double X, double Y)[CircleSegments];
        for (var i = 0; i < CircleSegments; i++)
        {
            var angle = 2 * Math.PI * i / CircleSegments * (clockwise ? 1 : -1);
            points[i] = (cx + r * Math.Cos(angle), cy + r * Math.Sin(angle));
        }

        return points;
    }

    private static void AppendCircle(StringBuilder d, double cx, double cy, double r, char sweep)
    {
        var radius = Num(r);
        d.Append('M').Append(Num(cx + r)).Append(' ').Append(Num(cy))
            .Append('A').Append(radius).Append(' ').Append(radius).Append(" 0 1 ").Append(sweep).Append(' ').Append(Num(cx - r)).Append(' ').Append(Num(cy))
            .Append('A').Append(radius).Append(' ').Append(radius).Append(" 0 1 ").Append(sweep).Append(' ').Append(Num(cx + r)).Append(' ').Append(Num(cy))
            .Append('Z');
    }

    private static string Num(double value) => Math.Round(value, 3).ToString("0.###", CultureInfo.InvariantCulture);
}
