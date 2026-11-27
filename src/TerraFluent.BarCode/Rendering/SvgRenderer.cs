using System.Globalization;
using System.Text;

namespace TerraFluent.BarCode.Rendering;

/// <summary>
/// Renders a matrix as a compact SVG: a background rectangle plus one path
/// whose sub-paths cover horizontal runs of dark modules.
/// </summary>
internal static class SvgRenderer
{
    public static string Render(BarcodeMatrix matrix, RenderOptions options)
    {
        if (options.IsStyled) return RenderStyled(SceneBuilder.Build(matrix, options));

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
        sb.Append("<rect width=\"100%\" height=\"100%\" fill=\"").Append(options.Background.ToHex()).Append("\"/>");
        if (path.Length > 0)
        {
            sb.Append("<path fill=\"").Append(options.Foreground.ToHex()).Append("\" d=\"").Append(path).Append("\"/>");
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    private static string Num(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Num(double value) =>
        Math.Round(value, 3).ToString("0.###", CultureInfo.InvariantCulture);

    /// <summary>Renders shapes, gradients, transparency and logos.</summary>
    internal static string RenderStyled(Scene scene)
    {
        var sb = new StringBuilder(4096);
        var crisp = scene.Foreground.Concat(scene.Finders).All(s => s.Kind == ShapeKind.Rect);
        sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" version=\"1.1\"")
            .Append(" width=\"").Append(Num(scene.Width)).Append("\" height=\"").Append(Num(scene.Height)).Append('"')
            .Append(" viewBox=\"0 0 ").Append(Num(scene.Width)).Append(' ').Append(Num(scene.Height)).Append('"');
        if (crisp) sb.Append(" shape-rendering=\"crispEdges\"");
        sb.Append('>');

        var foregroundFill = scene.ForegroundColor.ToHex();
        if (scene.GradientStart is BarcodeColor start && scene.GradientEnd is BarcodeColor end)
        {
            var (gx, gy, gw, gh) = scene.SymbolBounds;
            sb.Append("<defs>");
            if (scene.GradientDirection == GradientDirection.Radial)
            {
                sb.Append("<radialGradient id=\"tf-fg\" gradientUnits=\"userSpaceOnUse\" cx=\"").Append(Num(gx + gw / 2))
                    .Append("\" cy=\"").Append(Num(gy + gh / 2))
                    .Append("\" r=\"").Append(Num(Math.Sqrt(gw * gw + gh * gh) / 2)).Append("\">");
            }
            else
            {
                var (x2, y2) = scene.GradientDirection switch
                {
                    GradientDirection.Horizontal => (gx + gw, gy),
                    GradientDirection.Vertical => (gx, gy + gh),
                    _ => (gx + gw, gy + gh),
                };
                sb.Append("<linearGradient id=\"tf-fg\" gradientUnits=\"userSpaceOnUse\" x1=\"").Append(Num(gx))
                    .Append("\" y1=\"").Append(Num(gy)).Append("\" x2=\"").Append(Num(x2)).Append("\" y2=\"").Append(Num(y2)).Append("\">");
            }

            sb.Append("<stop offset=\"0\" stop-color=\"").Append(start.ToHex()).Append("\"/>")
                .Append("<stop offset=\"1\" stop-color=\"").Append(end.ToHex()).Append("\"/>")
                .Append(scene.GradientDirection == GradientDirection.Radial ? "</radialGradient>" : "</linearGradient>")
                .Append("</defs>");
            foregroundFill = "url(#tf-fg)";
        }

        if (!scene.Transparent)
        {
            sb.Append("<rect width=\"100%\" height=\"100%\" fill=\"").Append(scene.Background.ToHex()).Append("\"/>");
        }

        AppendPath(sb, scene.Foreground, foregroundFill);
        AppendPath(sb, scene.Finders, scene.FinderColor?.ToHex() ?? foregroundFill);

        if (scene.Logo is not null && scene.LogoBox is { } box)
        {
            sb.Append("<image x=\"").Append(Num(box.X)).Append("\" y=\"").Append(Num(box.Y))
                .Append("\" width=\"").Append(Num(box.Size)).Append("\" height=\"").Append(Num(box.Size))
                .Append("\" preserveAspectRatio=\"xMidYMid meet\" xlink:href=\"data:").Append(ImageMimeType(scene.Logo))
                .Append(";base64,").Append(Convert.ToBase64String(scene.Logo)).Append("\"/>");
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    private static void AppendPath(StringBuilder sb, List<Shape> shapes, string fill)
    {
        if (shapes.Count == 0) return;
        sb.Append("<path fill=\"").Append(fill).Append("\" fill-rule=\"evenodd\" d=\"");
        foreach (var shape in shapes)
        {
            AppendShape(sb, shape);
            if (shape.Hole is not null) AppendShape(sb, shape.Hole);
        }

        sb.Append("\"/>");
    }

    private static void AppendShape(StringBuilder d, Shape s)
    {
        switch (s.Kind)
        {
            case ShapeKind.Rect:
                d.Append('M').Append(Num(s.X)).Append(' ').Append(Num(s.Y))
                    .Append('h').Append(Num(s.W)).Append('v').Append(Num(s.H)).Append('h').Append(Num(-s.W)).Append('z');
                break;
            case ShapeKind.RoundRect:
            {
                var r = s.Radius;
                var arc = "a" + Num(r) + " " + Num(r) + " 0 0 1 ";
                d.Append('M').Append(Num(s.X + r)).Append(' ').Append(Num(s.Y))
                    .Append('h').Append(Num(s.W - 2 * r)).Append(arc).Append(Num(r)).Append(' ').Append(Num(r))
                    .Append('v').Append(Num(s.H - 2 * r)).Append(arc).Append(Num(-r)).Append(' ').Append(Num(r))
                    .Append('h').Append(Num(-(s.W - 2 * r))).Append(arc).Append(Num(-r)).Append(' ').Append(Num(-r))
                    .Append('v').Append(Num(-(s.H - 2 * r))).Append(arc).Append(Num(r)).Append(' ').Append(Num(-r))
                    .Append('z');
                break;
            }
            case ShapeKind.Circle:
            {
                var r = s.W / 2;
                d.Append('M').Append(Num(s.X)).Append(' ').Append(Num(s.Y + r))
                    .Append('a').Append(Num(r)).Append(' ').Append(Num(r)).Append(" 0 1 0 ").Append(Num(2 * r)).Append(" 0")
                    .Append('a').Append(Num(r)).Append(' ').Append(Num(r)).Append(" 0 1 0 ").Append(Num(-2 * r)).Append(" 0z");
                break;
            }
            default:
                d.Append('M').Append(Num(s.X + s.W / 2)).Append(' ').Append(Num(s.Y))
                    .Append('L').Append(Num(s.X + s.W)).Append(' ').Append(Num(s.Y + s.H / 2))
                    .Append('L').Append(Num(s.X + s.W / 2)).Append(' ').Append(Num(s.Y + s.H))
                    .Append('L').Append(Num(s.X)).Append(' ').Append(Num(s.Y + s.H / 2)).Append('z');
                break;
        }
    }

    /// <summary>Detects the logo's image type from its first bytes.</summary>
    internal static string ImageMimeType(byte[] data)
    {
        bool StartsWith(params byte[] magic) => data.Length >= magic.Length && magic.Select((b, i) => data[i] == b).All(x => x);
        if (StartsWith(0x89, 0x50, 0x4E, 0x47)) return "image/png";
        if (StartsWith(0xFF, 0xD8, 0xFF)) return "image/jpeg";
        if (StartsWith(0x47, 0x49, 0x46, 0x38)) return "image/gif";
        if (data.Length >= 12 && StartsWith(0x52, 0x49, 0x46, 0x46) && data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50) return "image/webp";
        var head = System.Text.Encoding.UTF8.GetString(data, 0, Math.Min(data.Length, 512)).TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
        if (head.StartsWith("<", StringComparison.Ordinal) && head.IndexOf("<svg", StringComparison.OrdinalIgnoreCase) >= 0) return "image/svg+xml";
        throw new NotSupportedException("Unrecognised logo image. Use PNG, JPEG, GIF, WebP or SVG.");
    }
}
