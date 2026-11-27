using System.Globalization;
using System.Text;

namespace TerraFluent.BarCode.Rendering;

/// <summary>A filled outline (one glyph): polygons for rasterising, path data for SVG.</summary>
internal sealed class PathShape
{
    public PathShape(List<(double X, double Y)[]> contours, string svgPath)
    {
        Contours = contours;
        SvgPath = svgPath;
        MinX = MinY = double.MaxValue;
        MaxX = MaxY = double.MinValue;
        foreach (var contour in contours)
        {
            foreach (var (x, y) in contour)
            {
                MinX = Math.Min(MinX, x);
                MinY = Math.Min(MinY, y);
                MaxX = Math.Max(MaxX, x);
                MaxY = Math.Max(MaxY, y);
            }
        }
    }

    /// <summary>Closed polygons in output pixels.</summary>
    public List<(double X, double Y)[]> Contours { get; }

    /// <summary>The same outline as SVG path data (with quadratic curves), in output pixels.</summary>
    public string SvgPath { get; }

    public double MinX { get; }
    public double MinY { get; }
    public double MaxX { get; }
    public double MaxY { get; }

    /// <summary>Non-zero winding rule, as TrueType outlines and SVG use.</summary>
    public bool Contains(double px, double py)
    {
        if (px < MinX || px > MaxX || py < MinY || py > MaxY) return false;
        var winding = 0;
        foreach (var contour in Contours)
        {
            for (int i = 0, j = contour.Length - 1; i < contour.Length; j = i++)
            {
                var (x0, y0) = contour[j];
                var (x1, y1) = contour[i];
                if (y0 <= py)
                {
                    if (y1 > py && Cross(x0, y0, x1, y1, px, py) > 0) winding++;
                }
                else if (y1 <= py && Cross(x0, y0, x1, y1, px, py) < 0)
                {
                    winding--;
                }
            }
        }

        return winding != 0;
    }

    private static double Cross(double x0, double y0, double x1, double y1, double px, double py) =>
        (x1 - x0) * (py - y0) - (px - x0) * (y1 - y0);
}

/// <summary>
/// The built-in monospaced font for human-readable text (printable ASCII),
/// so text renders identically in every output format without system fonts.
/// </summary>
internal static partial class TextFont
{
    private const int CurveSegments = 6;

    /// <summary>Width of one character, as a fraction of the font size.</summary>
    public const double AdvanceRatio = (double)Advance / UnitsPerEm;

    /// <summary>Height of digits and capitals, as a fraction of the font size.</summary>
    public const double CapHeightRatio = (double)CapHeight / UnitsPerEm;

    /// <summary>
    /// Lays out <paramref name="text"/> centred on <paramref name="centreX"/>, with
    /// its baseline at <paramref name="baseline"/>, at <paramref name="size"/> pixels per em.
    /// Characters outside printable ASCII are drawn as spaces.
    /// </summary>
    public static List<PathShape> Layout(string text, double centreX, double baseline, double size)
    {
        var scale = size / UnitsPerEm;
        var advance = Advance * scale;
        var x = centreX - advance * text.Length / 2;
        var shapes = new List<PathShape>(text.Length);
        foreach (var c in text)
        {
            if (c > ' ' && c <= '~' && Outlines[c - ' '].Length > 0) shapes.Add(Glyph(Outlines[c - ' '], x, baseline, scale));
            x += advance;
        }

        return shapes;
    }

    private static PathShape Glyph(string outline, double originX, double baseline, double scale)
    {
        var contours = new List<(double X, double Y)[]>();
        var current = new List<(double X, double Y)>();
        var svg = new StringBuilder(outline.Length + 32);
        (double X, double Y) Map(double fx, double fy) => (originX + fx * scale, baseline - fy * scale);

        var pos = 0;
        double ReadNumber()
        {
            while (outline[pos] == ' ') pos++;
            var start = pos;
            while (pos < outline.Length && (outline[pos] == '-' || char.IsDigit(outline[pos]))) pos++;
            return double.Parse(outline.Substring(start, pos - start), CultureInfo.InvariantCulture);
        }

        void Emit(char command, params (double X, double Y)[] points)
        {
            svg.Append(command);
            for (var i = 0; i < points.Length; i++)
            {
                if (i > 0) svg.Append(' ');
                svg.Append(Num(points[i].X)).Append(' ').Append(Num(points[i].Y));
            }
        }

        while (pos < outline.Length)
        {
            var command = outline[pos++];
            switch (command)
            {
                case 'M':
                {
                    var p = Map(ReadNumber(), ReadNumber());
                    current = new List<(double X, double Y)> { p };
                    Emit('M', p);
                    break;
                }

                case 'L':
                {
                    var p = Map(ReadNumber(), ReadNumber());
                    current.Add(p);
                    Emit('L', p);
                    break;
                }

                case 'Q':
                {
                    var c = Map(ReadNumber(), ReadNumber());
                    var p = Map(ReadNumber(), ReadNumber());
                    var start = current[current.Count - 1];
                    for (var i = 1; i <= CurveSegments; i++)
                    {
                        var t = (double)i / CurveSegments;
                        var u = 1 - t;
                        current.Add((u * u * start.X + 2 * u * t * c.X + t * t * p.X, u * u * start.Y + 2 * u * t * c.Y + t * t * p.Y));
                    }

                    Emit('Q', c, p);
                    break;
                }

                case 'Z':
                    if (current.Count > 2) contours.Add(current.ToArray());
                    current = new List<(double X, double Y)>();
                    svg.Append('Z');
                    break;
            }
        }

        return new PathShape(contours, svg.ToString());
    }

    private static string Num(double value) => Math.Round(value, 2).ToString("0.##", CultureInfo.InvariantCulture);
}
