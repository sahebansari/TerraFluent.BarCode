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
}
