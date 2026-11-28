using System.Globalization;
using System.Text;

namespace TerraFluent.Barcode.Rendering;

/// <summary>
/// Renders a matrix as a compact SVG: a background rectangle plus one path
/// whose sub-paths cover horizontal runs of dark modules.
/// </summary>
internal static class SvgRenderer
{
    public static string Render(BarcodeMatrix matrix, RenderOptions options, bool transparent = false)
    {
        var (width, height) = options.ImageSize(matrix);
        var m = options.ModuleSize;
        var q = options.QuietZone;
        var rows = matrix.IsLinear ? 1 : matrix.Height;

        // Size the builder exactly for the path data, "M{x} {y}h{w}v{h}h-{w}z" per run of dark modules, so it never grows.
        long dataLength = 0;
        for (var y = 0; y < rows; y++)
        {
            var x = 0;
            while (x < matrix.Width)
            {
                if (!matrix.Get(x, y))
                {
                    x++;
                    continue;
                }

                var start = x;
                while (x < matrix.Width && matrix.Get(x, y)) x++;
                var runWidth = Digits((x - start) * m);
                dataLength += 7 + Digits((start + q) * m) + Digits(matrix.IsLinear ? 0 : (y + q) * m) + 2 * runWidth + Digits(matrix.IsLinear ? options.BarHeight : m);
            }
        }

        var sb = new StringBuilder((int)Math.Min(400 + dataLength, int.MaxValue / 2));
        sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" version=\"1.1\"")
            .Append(" width=\"");
        AppendNum(sb, width).Append("\" height=\"");
        AppendNum(sb, height).Append('"').Append(" viewBox=\"0 0 ");
        AppendNum(sb, width).Append(' ');
        AppendNum(sb, height).Append('"').Append(" shape-rendering=\"crispEdges\">");
        if (!transparent) sb.Append("<rect width=\"100%\" height=\"100%\" fill=\"").Append(options.Background.ToHex()).Append("\"/>");

        // The path element is written up front and taken out again if there turn out to be no dark modules.
        var pathStart = sb.Length;
        sb.Append("<path fill=\"").Append(options.Foreground.ToHex()).Append("\" d=\"");
        var dataStart = sb.Length;
        for (var y = 0; y < rows; y++)
        {
            var x = 0;
            while (x < matrix.Width)
            {
                if (!matrix.Get(x, y))
                {
                    x++;
                    continue;
                }

                var start = x;
                while (x < matrix.Width && matrix.Get(x, y)) x++;

                var left = (start + q) * m;
                var top = matrix.IsLinear ? 0 : (y + q) * m;
                var runWidth = (x - start) * m;
                var runHeight = matrix.IsLinear ? options.BarHeight : m;
                AppendNum(sb.Append('M'), left).Append(' ');
                AppendNum(sb, top).Append('h');
                AppendNum(sb, runWidth).Append('v');
                AppendNum(sb, runHeight).Append('h');
                AppendNum(sb, -runWidth).Append('z');
            }
        }

        if (sb.Length == dataStart) sb.Length = pathStart;
        else sb.Append("\"/>");

        sb.Append("</svg>");
        return sb.ToString();
    }

    /// <summary>Renders a styled scene: gradients become SVG gradients, rounded shapes arcs, the logo an embedded PNG.</summary>
    public static string Render(Scene scene)
    {
        var sb = new StringBuilder(EstimateLength(scene));
        sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" version=\"1.1\"");
        if (scene.Logo is not null) sb.Append(" xmlns:xlink=\"http://www.w3.org/1999/xlink\"");
        sb.Append(" width=\"");
        AppendNum(sb, scene.Width).Append("\" height=\"");
        AppendNum(sb, scene.Height).Append('"').Append(" viewBox=\"0 0 ");
        AppendNum(sb, scene.Width).Append(' ');
        AppendNum(sb, scene.Height).Append('"');
        if (!scene.HasCurves) sb.Append(" shape-rendering=\"crispEdges\"");
        sb.Append('>');

        // Gradients go straight into a <defs> element, which is taken out again if no layer has one.
        var defsStart = sb.Length;
        sb.Append("<defs>");
        var gradientsStart = sb.Length;
        var fills = new string[scene.Layers.Count];
        for (var i = 0; i < scene.Layers.Count; i++)
        {
            var paint = scene.Layers[i].Paint;
            if (paint.Direction is null)
            {
                fills[i] = paint.Start.ToHex();
                continue;
            }

            var id = "g" + Num(i);
            fills[i] = "url(#" + id + ")";
            if (paint.Direction == GradientDirection.Radial)
            {
                sb.Append("<radialGradient id=\"").Append(id).Append("\" gradientUnits=\"userSpaceOnUse\"").Append(" cx=\"");
                AppendNum(sb, paint.X + paint.Width / 2).Append("\" cy=\"");
                AppendNum(sb, paint.Y + paint.Height / 2).Append("\" r=\"");
                AppendNum(sb, paint.Radius).Append("\">");
            }
            else
            {
                var x2 = paint.Direction == GradientDirection.Vertical ? paint.X : paint.X + paint.Width;
                var y2 = paint.Direction == GradientDirection.Horizontal ? paint.Y : paint.Y + paint.Height;
                sb.Append("<linearGradient id=\"").Append(id).Append("\" gradientUnits=\"userSpaceOnUse\"").Append(" x1=\"");
                AppendNum(sb, paint.X).Append("\" y1=\"");
                AppendNum(sb, paint.Y).Append("\" x2=\"");
                AppendNum(sb, x2).Append("\" y2=\"");
                AppendNum(sb, y2).Append("\">");
            }

            sb.Append("<stop offset=\"0\" stop-color=\"").Append(paint.Start.ToHex()).Append("\"/>")
                .Append("<stop offset=\"1\" stop-color=\"").Append(paint.End.ToHex()).Append("\"/>")
                .Append(paint.Direction == GradientDirection.Radial ? "</radialGradient>" : "</linearGradient>");
        }

        if (sb.Length == gradientsStart) sb.Length = defsStart;
        else sb.Append("</defs>");

        if (!scene.Transparent) sb.Append("<rect width=\"100%\" height=\"100%\" fill=\"").Append(scene.Background.ToHex()).Append("\"/>");

        for (var i = 0; i < scene.Layers.Count; i++)
        {
            var layer = scene.Layers[i];
            if (layer.Shapes.Count == 0 && layer.Paths.Count == 0) continue;
            sb.Append("<path fill=\"").Append(fills[i]).Append("\" d=\"");
            foreach (var shape in layer.Shapes)
            {
                AppendShape(sb, shape, clockwise: true);
                if (shape.Hole is { } hole) AppendShape(sb, hole, clockwise: false);
            }

            foreach (var glyph in layer.Paths) sb.Append(glyph.SvgPath);

            sb.Append("\"/>");
        }

        if (scene.Logo is { } logo)
        {
            sb.Append("<image x=\"");
            AppendNum(sb, logo.X).Append("\" y=\"");
            AppendNum(sb, logo.Y).Append("\" width=\"");
            AppendNum(sb, logo.Width).Append("\" height=\"");
            AppendNum(sb, logo.Height)
                .Append("\" preserveAspectRatio=\"none\" xlink:href=\"data:image/png;base64,")
                .Append(Convert.ToBase64String(logo.Png)).Append("\"/>");
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    /// <summary>A close estimate of the SVG length, so the builder rarely has to grow.</summary>
    private static int EstimateLength(Scene scene)
    {
        long length = 1024;
        foreach (var layer in scene.Layers)
        {
            foreach (var shape in layer.Shapes)
            {
                // A move, four lines and four arcs; holes double that.
                length += shape.HasCurves ? 200 : 80;
                if (shape.Hole is not null) length += 200;
            }

            foreach (var glyph in layer.Paths) length += glyph.SvgPath.Length;
        }

        if (scene.Logo is { } logo) length += (logo.Png.Length + 2) / 3 * 4;
        return (int)Math.Min(length, int.MaxValue / 2);
    }

    /// <summary>
    /// Appends a rounded rectangle as a closed sub-path. Holes are wound the
    /// other way, so the default non-zero fill rule cuts them out while
    /// touching shapes still merge without seams.
    /// </summary>
    private static void AppendShape(StringBuilder d, Shape s, bool clockwise)
    {
        double x = s.X, y = s.Y, r = s.X + s.Width, b = s.Y + s.Height;
        void Point(char command, double px, double py) => AppendNum(AppendNum(d.Append(command), px).Append(' '), py);
        void Arc(double radius, double px, double py)
        {
            if (radius <= 0) return;
            AppendNum(AppendNum(d.Append('A'), radius).Append(' '), radius).Append(" 0 0 ").Append(clockwise ? '1' : '0').Append(' ');
            AppendNum(AppendNum(d, px).Append(' '), py);
        }

        Point('M', x + s.TopLeft, y);
        if (clockwise)
        {
            Point('L', r - s.TopRight, y);
            Arc(s.TopRight, r, y + s.TopRight);
            Point('L', r, b - s.BottomRight);
            Arc(s.BottomRight, r - s.BottomRight, b);
            Point('L', x + s.BottomLeft, b);
            Arc(s.BottomLeft, x, b - s.BottomLeft);
            Point('L', x, y + s.TopLeft);
            Arc(s.TopLeft, x + s.TopLeft, y);
        }
        else
        {
            Arc(s.TopLeft, x, y + s.TopLeft);
            Point('L', x, b - s.BottomLeft);
            Arc(s.BottomLeft, x + s.BottomLeft, b);
            Point('L', r - s.BottomRight, b);
            Arc(s.BottomRight, r, b - s.BottomRight);
            Point('L', r, y + s.TopRight);
            Arc(s.TopRight, r - s.TopRight, y);
        }

        d.Append('Z');
    }

    private static string Num(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static int Digits(int value)
    {
        var digits = 1;
        while (value >= 10)
        {
            value /= 10;
            digits++;
        }

        return digits;
    }

    /// <summary>Appends an integer in invariant form ("-" sign, ASCII digits) without allocating a string.</summary>
    private static StringBuilder AppendNum(StringBuilder sb, int value)
    {
        if (value < 0)
        {
            if (value == int.MinValue) return sb.Append("-2147483648");
            sb.Append('-');
            value = -value;
        }

        var divisor = 1;
        while (value / divisor >= 10) divisor *= 10;
        for (; divisor > 0; divisor /= 10) sb.Append((char)('0' + value / divisor % 10));
        return sb;
    }

    /// <summary>
    /// Appends a value rounded to 3 decimals, formatted as "0.###" in the
    /// invariant culture. Whole numbers, the common case, take the integer path.
    /// </summary>
    private static StringBuilder AppendNum(StringBuilder sb, double value)
    {
        var rounded = Math.Round(value, 3);
        if (rounded == Math.Floor(rounded) && Math.Abs(rounded) < 1e9 && (rounded != 0 || !IsNegativeZero(rounded)))
        {
            return AppendNum(sb, (int)rounded);
        }

#if NET
        return sb.Append(CultureInfo.InvariantCulture, $"{rounded:0.###}");
#else
        return sb.Append(rounded.ToString("0.###", CultureInfo.InvariantCulture));
#endif
    }

    private static bool IsNegativeZero(double value) => BitConverter.DoubleToInt64Bits(value) == unchecked((long)0x8000000000000000UL);
}
