namespace TerraFluent.Barcode.Rendering;

/// <summary>Decorative settings. A barcode without any of them takes the plain, smallest-output path.</summary>
internal sealed class StyleOptions
{
    public QrModuleStyle ModuleStyle { get; init; }
    public QrEyeStyle EyeStyle { get; init; }
    public BarcodeColor? EyeColor { get; init; }
    public (BarcodeColor Start, BarcodeColor End, GradientDirection Direction)? Gradient { get; init; }
    public bool RoundedBars { get; init; }
    public RgbaImage? Logo { get; init; }
    public byte[]? LogoPng { get; init; }
    public double LogoScale { get; init; }

    /// <summary>The symbol has QR finder patterns (eyes); other 2D symbols are drawn as plain modules.</summary>
    public bool IsQr { get; init; }

    public bool Transparent { get; init; }
}

/// <summary>Turns a module matrix plus layout and style settings into a <see cref="Scene"/>.</summary>
internal static class SceneBuilder
{
    // Finder (7) + separator (1) + format information (1) on each side must stay visible.
    private const int ReservedEdgeModules = 9;

    public static Scene Build(BarcodeMatrix matrix, RenderOptions options, StyleOptions style)
    {
        var (width, height) = options.ImageSize(matrix);
        double m = options.ModuleSize;
        var originX = options.QuietZone * m;
        var originY = matrix.IsLinear ? 0 : options.QuietZone * m;
        var symbolWidth = matrix.Width * m;
        var symbolHeight = matrix.IsLinear ? options.BarHeight : matrix.Height * m;

        var paint = style.Gradient is { } g
            ? Paint.Gradient(g.Start, g.End, g.Direction, originX, originY, symbolWidth, symbolHeight)
            : Paint.Solid(options.Foreground);

        var scene = new Scene(width, height, options.Background, style.Transparent);
        var main = new Layer(paint);
        scene.Layers.Add(main);

        if (matrix.IsLinear)
        {
            AddBars(matrix, main, originX, m, options, style.RoundedBars);
            if (options.FontSize > 0) AddText(matrix, main, originX, m, options);
        }
        else if (style.IsQr)
        {
            AddQr(matrix, scene, main, style, originX, originY, m);
        }
        else
        {
            AddModules(matrix, main, originX, originY, m);
        }

        return scene;
    }

    private static void AddModules(BarcodeMatrix matrix, Layer layer, double originX, double originY, double m)
    {
        // Horizontal runs keep the shape count low; their union is filled, so it looks the same as single modules.
        for (var y = 0; y < matrix.Height; y++)
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
                layer.Shapes.Add(Shape.Rounded(originX + start * m, originY + y * m, (x - start) * m, m, 0));
            }
        }
    }

    private static void AddText(BarcodeMatrix matrix, Layer layer, double originX, double m, RenderOptions options)
    {
        var layout = matrix.Layout;
        if (layout is null) return;
        if (layout.CentredText is { Length: > 0 } text)
        {
            // Shrink long text so it never runs past the bars.
            var size = Math.Min(options.FontSize, matrix.Width * m / (text.Length * TextFont.AdvanceRatio));
            layer.Paths.AddRange(TextFont.Layout(text, originX + matrix.Width * m / 2, options.TextBaseline, size));
        }

        foreach (var item in layout.Items)
        {
            var size = item.Small ? options.FontSize * 0.75 : options.FontSize;
            var baseline = item.AboveAddOn ? options.AddOnBaseline : options.TextBaseline;
            layer.Paths.AddRange(TextFont.Layout(item.Text, originX + item.CentreModule * m, baseline, size));
        }
    }

    private static void AddBars(BarcodeMatrix matrix, Layer layer, double originX, double m, RenderOptions options, bool rounded)
    {
        var layout = matrix.Layout;
        var text = options.FontSize > 0;
        var x = 0;
        while (x < matrix.Width)
        {
            if (!matrix.Get(x, 0))
            {
                x++;
                continue;
            }

            var start = x;
            while (x < matrix.Width && matrix.Get(x, 0)) x++;
            var barWidth = (x - start) * m;
            double top = 0, bottom = options.BarHeight;
            if (text && layout is not null)
            {
                var isAddOn = layout.AddOnStart >= 0 && start >= layout.AddOnStart;
                if (isAddOn) top = options.AddOnTop;
                if (isAddOn || layout.IsExtended(start)) bottom = options.GuardBottom;
            }

            layer.Shapes.Add(Shape.Rounded(originX + start * m, top, barWidth, bottom - top, rounded ? barWidth / 2 : 0));
        }
    }

    private static void AddQr(BarcodeMatrix matrix, Scene scene, Layer main, StyleOptions style, double originX, double originY, double m)
    {
        var n = matrix.Width;
        var cleared = new bool[n * n];

        if (style.Logo is { } logo && style.LogoPng is { } png)
        {
            // Keep the logo between the reserved bands so finder, timing and format modules stay visible.
            var side = Math.Min(style.LogoScale * n, n - 2 * ReservedEdgeModules - 1.0);
            if (side >= 1)
            {
                double logoW = side, logoH = side;
                if (logo.Width >= logo.Height) logoH = side * logo.Height / logo.Width;
                else logoW = side * logo.Width / logo.Height;

                var left = (n - logoW) / 2;
                var top = (n - logoH) / 2;
                const double padding = 0.5;
                for (var y = 0; y < n; y++)
                {
                    if (y + 1 <= top - padding || y >= top + logoH + padding) continue;
                    for (var x = 0; x < n; x++)
                    {
                        if (x + 1 > left - padding && x < left + logoW + padding) cleared[y * n + x] = true;
                    }
                }

                var px = (int)Math.Round(originX + left * m);
                var py = (int)Math.Round(originY + top * m);
                var pw = Math.Max(1, (int)Math.Round(logoW * m));
                var ph = Math.Max(1, (int)Math.Round(logoH * m));
                scene.Logo = new LogoPlacement(logo, png, px, py, pw, ph);
            }
        }

        bool InFinder(int x, int y) => (x < 7 && y < 7) || (x >= n - 7 && y < 7) || (x < 7 && y >= n - 7);
        bool Dark(int x, int y) => x >= 0 && y >= 0 && x < n && y < n && matrix.Get(x, y) && !cleared[y * n + x] && !InFinder(x, y);

        for (var y = 0; y < n; y++)
        {
            for (var x = 0; x < n; x++)
            {
                if (!Dark(x, y)) continue;
                var left = originX + x * m;
                var top = originY + y * m;
                main.Shapes.Add(style.ModuleStyle switch
                {
                    QrModuleStyle.Rounded => Shape.Rounded(left, top, m, m, 0.3 * m),
                    QrModuleStyle.Dots => Shape.Rounded(left + 0.05 * m, top + 0.05 * m, 0.9 * m, 0.9 * m, 0.45 * m),
                    QrModuleStyle.Connected => new Shape(
                        left, top, m, m,
                        !Dark(x - 1, y) && !Dark(x, y - 1) ? m / 2 : 0,
                        !Dark(x + 1, y) && !Dark(x, y - 1) ? m / 2 : 0,
                        !Dark(x + 1, y) && !Dark(x, y + 1) ? m / 2 : 0,
                        !Dark(x - 1, y) && !Dark(x, y + 1) ? m / 2 : 0),
                    _ => Shape.Rounded(left, top, m, m, 0),
                });
            }
        }

        var eyes = main;
        if (style.EyeColor is { } eyeColor)
        {
            eyes = new Layer(Paint.Solid(eyeColor));
            scene.Layers.Add(eyes);
        }

        var (frameRadius, holeRadius, centreRadius) = style.EyeStyle switch
        {
            QrEyeStyle.Rounded => (2.0 * m, 1.0 * m, 1.0 * m),
            QrEyeStyle.Circle => (3.5 * m, 2.5 * m, 1.5 * m),
            _ => (0.0, 0.0, 0.0),
        };

        foreach (var (fx, fy) in new[] { (0, 0), (n - 7, 0), (0, n - 7) })
        {
            var left = originX + fx * m;
            var top = originY + fy * m;
            var hole = Shape.Rounded(left + m, top + m, 5 * m, 5 * m, holeRadius);
            eyes.Shapes.Add(Shape.Rounded(left, top, 7 * m, 7 * m, frameRadius, hole));
            eyes.Shapes.Add(Shape.Rounded(left + 2 * m, top + 2 * m, 3 * m, 3 * m, centreRadius));
        }
    }
}
