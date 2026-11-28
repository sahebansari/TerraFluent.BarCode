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
        var path = new StringBuilder();

        var rows = matrix.IsLinear ? 1 : matrix.Height;
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
                path.Append('M').Append(Num(left)).Append(' ').Append(Num(top))
                    .Append('h').Append(Num(runWidth))
                    .Append('v').Append(Num(runHeight))
                    .Append('h').Append(Num(-runWidth))
                    .Append('z');
            }
        }

        var sb = new StringBuilder(path.Length + 300);
        sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" version=\"1.1\"")
            .Append(" width=\"").Append(Num(width)).Append("\" height=\"").Append(Num(height)).Append('"')
            .Append(" viewBox=\"0 0 ").Append(Num(width)).Append(' ').Append(Num(height)).Append('"')
            .Append(" shape-rendering=\"crispEdges\">");
        if (!transparent) sb.Append("<rect width=\"100%\" height=\"100%\" fill=\"").Append(options.Background.ToHex()).Append("\"/>");
        if (path.Length > 0)
        {
            sb.Append("<path fill=\"").Append(options.Foreground.ToHex()).Append("\" d=\"").Append(path).Append("\"/>");
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    /// <summary>Renders a styled scene: gradients become SVG gradients, rounded shapes arcs, the logo an embedded PNG.</summary>
    public static string Render(Scene scene)
    {
        var sb = new StringBuilder(4096);
        sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" version=\"1.1\"");
        if (scene.Logo is not null) sb.Append(" xmlns:xlink=\"http://www.w3.org/1999/xlink\"");
        sb.Append(" width=\"").Append(Num(scene.Width)).Append("\" height=\"").Append(Num(scene.Height)).Append('"')
            .Append(" viewBox=\"0 0 ").Append(Num(scene.Width)).Append(' ').Append(Num(scene.Height)).Append('"');
        if (!scene.HasCurves) sb.Append(" shape-rendering=\"crispEdges\"");
        sb.Append('>');

        var gradients = new StringBuilder();
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
                gradients.Append("<radialGradient id=\"").Append(id).Append("\" gradientUnits=\"userSpaceOnUse\"")
                    .Append(" cx=\"").Append(Num(paint.X + paint.Width / 2)).Append("\" cy=\"").Append(Num(paint.Y + paint.Height / 2))
                    .Append("\" r=\"").Append(Num(paint.Radius)).Append("\">");
            }
            else
            {
                var x2 = paint.Direction == GradientDirection.Vertical ? paint.X : paint.X + paint.Width;
                var y2 = paint.Direction == GradientDirection.Horizontal ? paint.Y : paint.Y + paint.Height;
                gradients.Append("<linearGradient id=\"").Append(id).Append("\" gradientUnits=\"userSpaceOnUse\"")
                    .Append(" x1=\"").Append(Num(paint.X)).Append("\" y1=\"").Append(Num(paint.Y))
                    .Append("\" x2=\"").Append(Num(x2)).Append("\" y2=\"").Append(Num(y2)).Append("\">");
            }

            gradients.Append("<stop offset=\"0\" stop-color=\"").Append(paint.Start.ToHex()).Append("\"/>")
                .Append("<stop offset=\"1\" stop-color=\"").Append(paint.End.ToHex()).Append("\"/>")
                .Append(paint.Direction == GradientDirection.Radial ? "</radialGradient>" : "</linearGradient>");
        }

        if (gradients.Length > 0) sb.Append("<defs>").Append(gradients).Append("</defs>");
        if (!scene.Transparent) sb.Append("<rect width=\"100%\" height=\"100%\" fill=\"").Append(scene.Background.ToHex()).Append("\"/>");

        for (var i = 0; i < scene.Layers.Count; i++)
        {
            var layer = scene.Layers[i];
            if (layer.Shapes.Count == 0 && layer.Paths.Count == 0) continue;
            var d = new StringBuilder();
            foreach (var shape in layer.Shapes)
            {
                AppendShape(d, shape, clockwise: true);
                if (shape.Hole is { } hole) AppendShape(d, hole, clockwise: false);
            }

            foreach (var glyph in layer.Paths) d.Append(glyph.SvgPath);

            sb.Append("<path fill=\"").Append(fills[i]).Append("\" d=\"").Append(d).Append("\"/>");
        }

        if (scene.Logo is { } logo)
        {
            sb.Append("<image x=\"").Append(Num(logo.X)).Append("\" y=\"").Append(Num(logo.Y))
                .Append("\" width=\"").Append(Num(logo.Width)).Append("\" height=\"").Append(Num(logo.Height))
                .Append("\" preserveAspectRatio=\"none\" xlink:href=\"data:image/png;base64,")
                .Append(Convert.ToBase64String(logo.Png)).Append("\"/>");
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    /// <summary>
    /// Appends a rounded rectangle as a closed sub-path. Holes are wound the
    /// other way, so the default non-zero fill rule cuts them out while
    /// touching shapes still merge without seams.
    /// </summary>
    private static void AppendShape(StringBuilder d, Shape s, bool clockwise)
    {
        double x = s.X, y = s.Y, r = s.X + s.Width, b = s.Y + s.Height;
        void Move(double px, double py) => d.Append('M').Append(Num(px)).Append(' ').Append(Num(py));
        void Line(double px, double py) => d.Append('L').Append(Num(px)).Append(' ').Append(Num(py));
        void Arc(double radius, double px, double py)
        {
            if (radius <= 0) return;
            d.Append('A').Append(Num(radius)).Append(' ').Append(Num(radius)).Append(" 0 0 ").Append(clockwise ? '1' : '0')
                .Append(' ').Append(Num(px)).Append(' ').Append(Num(py));
        }

        Move(x + s.TopLeft, y);
        if (clockwise)
        {
            Line(r - s.TopRight, y);
            Arc(s.TopRight, r, y + s.TopRight);
            Line(r, b - s.BottomRight);
            Arc(s.BottomRight, r - s.BottomRight, b);
            Line(x + s.BottomLeft, b);
            Arc(s.BottomLeft, x, b - s.BottomLeft);
            Line(x, y + s.TopLeft);
            Arc(s.TopLeft, x + s.TopLeft, y);
        }
        else
        {
            Arc(s.TopLeft, x, y + s.TopLeft);
            Line(x, b - s.BottomLeft);
            Arc(s.BottomLeft, x + s.BottomLeft, b);
            Line(r - s.BottomRight, b);
            Arc(s.BottomRight, r, b - s.BottomRight);
            Line(r, y + s.TopRight);
            Arc(s.TopRight, r - s.TopRight, y);
        }

        d.Append('Z');
    }

    private static string Num(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Num(double value) => Math.Round(value, 3).ToString("0.###", CultureInfo.InvariantCulture);
}
