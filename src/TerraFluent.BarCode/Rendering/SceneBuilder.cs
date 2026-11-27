namespace TerraFluent.BarCode.Rendering;

/// <summary>Turns a module matrix plus render settings and style into a <see cref="Scene"/>.</summary>
internal static class SceneBuilder
{
    public static Scene Build(BarcodeMatrix matrix, RenderOptions options)
    {
        var style = options.Style;
        var (width, height) = options.ImageSize(matrix);
        double m = options.ModuleSize;
        var q = options.QuietZone;
        var x0 = q * m;
        var y0 = matrix.IsLinear ? 0 : q * m;
        var symbolW = matrix.Width * m;
        var symbolH = matrix.IsLinear ? options.BarHeight : matrix.Height * m;

        var scene = new Scene
        {
            Width = width,
            Height = height,
            SymbolBounds = (x0, y0, symbolW, symbolH),
            Background = options.Background,
            ForegroundColor = options.Foreground,
            Transparent = style?.Transparent ?? false,
            FinderColor = style?.FinderColorValue,
            GradientStart = style?.GradientStart,
            GradientEnd = style?.GradientEnd,
            GradientDirection = style?.GradientDirectionValue ?? GradientDirection.Horizontal,
            Logo = style?.LogoBytes,
        };

        if (matrix.IsLinear)
        {
            var rounded = style?.BarShapeValue == BarShape.Rounded;
            for (var x = 0; x < matrix.Width;)
            {
                if (!matrix.Get(x, 0))
                {
                    x++;
                    continue;
                }

                var start = x;
                while (x < matrix.Width && matrix.Get(x, 0)) x++;
                var bw = (x - start) * m;
                scene.Foreground.Add(rounded
                    ? new Shape(ShapeKind.RoundRect, x0 + start * m, y0, bw, symbolH, Math.Min(bw, symbolH) / 2)
                    : new Shape(ShapeKind.Rect, x0 + start * m, y0, bw, symbolH));
            }

            return scene;
        }

        var size = matrix.Width;
        var moduleShape = style?.ModuleShapeValue ?? QrModuleShape.Square;
        var finderShape = style?.FinderShapeValue ?? QrFinderShape.Square;

        // Logo box, centred, and the modules it clears (the encoder has already made sure they are recoverable).
        var cleared = new bool[size * size];
        if (style?.LogoBytes is not null)
        {
            var (left, top, logo) = LogoLayout.Box(size, style.LogoSizeRatio);
            scene.LogoBox = (x0 + left * m, y0 + top * m, logo * m);
            if (style.LogoClearBehind) cleared = LogoLayout.HiddenModules(size, style.LogoSizeRatio);
        }

        bool InFinder(int mx, int my) =>
            (mx < 7 && my < 7) || (mx >= size - 7 && my < 7) || (mx < 7 && my >= size - 7);

        foreach (var (fx, fy) in new[] { (0, 0), (size - 7, 0), (0, size - 7) })
        {
            AddFinder(scene, finderShape, x0 + fx * m, y0 + fy * m, m);
        }

        for (var my = 0; my < size; my++)
        {
            for (var mx = 0; mx < size; mx++)
            {
                if (!matrix.Get(mx, my) || InFinder(mx, my) || cleared[my * size + mx]) continue;
                double left = x0 + mx * m, top = y0 + my * m;

                if (moduleShape == QrModuleShape.Square)
                {
                    // Merge horizontal runs into one rectangle to keep SVG output small.
                    var start = mx;
                    while (mx + 1 < size && matrix.Get(mx + 1, my) && !InFinder(mx + 1, my) && !cleared[my * size + mx + 1]) mx++;
                    scene.Foreground.Add(new Shape(ShapeKind.Rect, x0 + start * m, top, (mx - start + 1) * m, m));
                    continue;
                }

                if (moduleShape == QrModuleShape.Connected)
                {
                    bool Dark(int x, int y) => x >= 0 && y >= 0 && x < size && y < size &&
                                               matrix.Get(x, y) && !InFinder(x, y) && !cleared[y * size + x];
                    bool up = Dark(mx, my - 1), down = Dark(mx, my + 1), l = Dark(mx - 1, my), r = Dark(mx + 1, my);
                    var corners = Corners.None;
                    if (!up && !l) corners |= Corners.TopLeft;
                    if (!up && !r) corners |= Corners.TopRight;
                    if (!down && !r) corners |= Corners.BottomRight;
                    if (!down && !l) corners |= Corners.BottomLeft;
                    scene.Foreground.Add(corners == Corners.None
                        ? new Shape(ShapeKind.Rect, left, top, m, m)
                        : new Shape(ShapeKind.CornerRect, left, top, m, m, m / 2, rounded: corners));
                    continue;
                }

                scene.Foreground.Add(moduleShape switch
                {
                    QrModuleShape.RoundedSquare => new Shape(ShapeKind.RoundRect, left, top, m, m, m * 0.3),
                    QrModuleShape.Circle => new Shape(ShapeKind.Circle, left + m * 0.05, top + m * 0.05, m * 0.9, m * 0.9),
                    _ => new Shape(ShapeKind.Diamond, left, top, m, m),
                });
            }
        }

        return scene;
    }

    private static void AddFinder(Scene scene, QrFinderShape shape, double x, double y, double m)
    {
        switch (shape)
        {
            case QrFinderShape.Rounded:
                scene.Finders.Add(new Shape(ShapeKind.RoundRect, x, y, 7 * m, 7 * m, 2 * m,
                    new Shape(ShapeKind.RoundRect, x + m, y + m, 5 * m, 5 * m, 1.3 * m)));
                scene.Finders.Add(new Shape(ShapeKind.RoundRect, x + 2 * m, y + 2 * m, 3 * m, 3 * m, 0.9 * m));
                break;
            case QrFinderShape.Circle:
                scene.Finders.Add(new Shape(ShapeKind.Circle, x, y, 7 * m, 7 * m, 0,
                    new Shape(ShapeKind.Circle, x + m, y + m, 5 * m, 5 * m)));
                scene.Finders.Add(new Shape(ShapeKind.Circle, x + 2 * m, y + 2 * m, 3 * m, 3 * m));
                break;
            default:
                scene.Finders.Add(new Shape(ShapeKind.Rect, x, y, 7 * m, 7 * m, 0,
                    new Shape(ShapeKind.Rect, x + m, y + m, 5 * m, 5 * m)));
                scene.Finders.Add(new Shape(ShapeKind.Rect, x + 2 * m, y + 2 * m, 3 * m, 3 * m));
                break;
        }
    }
}
