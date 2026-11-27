namespace TerraFluent.BarCode.Rendering;

/// <summary>
/// An RGBA pixel buffer that a <see cref="Scene"/> is drawn into, with 4x4
/// supersampled anti-aliasing for curved and diagonal edges.
/// </summary>
internal sealed class Canvas
{
    private const int Samples = 4;

    public Canvas(int width, int height)
    {
        Width = width;
        Height = height;
        Pixels = new byte[checked(width * height * 4)];
    }

    public int Width { get; }
    public int Height { get; }

    /// <summary>Straight (non-premultiplied) RGBA, row by row.</summary>
    public byte[] Pixels { get; }

    public static Canvas Render(Scene scene, bool forceOpaque = false)
    {
        var canvas = new Canvas(scene.Width, scene.Height);
        if (!scene.Transparent || forceOpaque) canvas.Fill(scene.Background);

        foreach (var shape in scene.Foreground) canvas.Draw(shape, scene.ForegroundAt);
        foreach (var shape in scene.Finders)
        {
            if (scene.FinderColor is BarcodeColor fc) canvas.Draw(shape, (_, _) => fc);
            else canvas.Draw(shape, scene.ForegroundAt);
        }

        if (scene.Logo is not null && scene.LogoBox is { } box) canvas.DrawLogo(scene.Logo, box);
        return canvas;
    }

    private void Fill(BarcodeColor color)
    {
        for (var i = 0; i < Pixels.Length; i += 4)
        {
            Pixels[i] = color.R;
            Pixels[i + 1] = color.G;
            Pixels[i + 2] = color.B;
            Pixels[i + 3] = 255;
        }
    }

    private void Draw(Shape shape, Func<double, double, BarcodeColor> paint)
    {
        var ix0 = Math.Max(0, (int)Math.Floor(shape.X));
        var iy0 = Math.Max(0, (int)Math.Floor(shape.Y));
        var ix1 = Math.Min(Width, (int)Math.Ceiling(shape.X + shape.W));
        var iy1 = Math.Min(Height, (int)Math.Ceiling(shape.Y + shape.H));
        var aligned = shape.IsPixelAligned;

        for (var py = iy0; py < iy1; py++)
        {
            for (var px = ix0; px < ix1; px++)
            {
                double coverage;
                if (aligned)
                {
                    coverage = 1;
                }
                else
                {
                    var hits = 0;
                    for (var sy = 0; sy < Samples; sy++)
                    {
                        for (var sx = 0; sx < Samples; sx++)
                        {
                            if (shape.Contains(px + (sx + 0.5) / Samples, py + (sy + 0.5) / Samples)) hits++;
                        }
                    }

                    if (hits == 0) continue;
                    coverage = hits / (double)(Samples * Samples);
                }

                var c = paint(px + 0.5, py + 0.5);
                Blend(px, py, c.R, c.G, c.B, coverage);
            }
        }
    }

    private void DrawLogo(byte[] logoBytes, (double X, double Y, double Size) box)
    {
        var logo = PngDecoder.Decode(logoBytes);

        // Fit inside the box, keeping the aspect ratio.
        var scale = Math.Min(box.Size / logo.Width, box.Size / logo.Height);
        var w = logo.Width * scale;
        var h = logo.Height * scale;
        var left = box.X + (box.Size - w) / 2;
        var top = box.Y + (box.Size - h) / 2;

        var ix0 = Math.Max(0, (int)Math.Floor(left));
        var iy0 = Math.Max(0, (int)Math.Floor(top));
        var ix1 = Math.Min(Width, (int)Math.Ceiling(left + w));
        var iy1 = Math.Min(Height, (int)Math.Ceiling(top + h));
        for (var py = iy0; py < iy1; py++)
        {
            for (var px = ix0; px < ix1; px++)
            {
                // Average the source pixels under this output pixel (box filter).
                var sx0 = (px - left) / scale;
                var sy0 = (py - top) / scale;
                var sx1 = (px + 1 - left) / scale;
                var sy1 = (py + 1 - top) / scale;
                double r = 0, g = 0, b = 0, a = 0, n = 0;
                for (var sy = Math.Max(0, (int)Math.Floor(sy0)); sy < Math.Min(logo.Height, (int)Math.Ceiling(sy1)); sy++)
                {
                    for (var sx = Math.Max(0, (int)Math.Floor(sx0)); sx < Math.Min(logo.Width, (int)Math.Ceiling(sx1)); sx++)
                    {
                        var i = (sy * logo.Width + sx) * 4;
                        var alpha = logo.Pixels[i + 3] / 255.0;
                        r += logo.Pixels[i] * alpha;
                        g += logo.Pixels[i + 1] * alpha;
                        b += logo.Pixels[i + 2] * alpha;
                        a += alpha;
                        n++;
                    }
                }

                if (n == 0 || a == 0) continue;
                var coverage = a / n;

                // Partial coverage at the logo's own edges.
                var edgeX = Math.Min(px + 1, left + w) - Math.Max(px, left);
                var edgeY = Math.Min(py + 1, top + h) - Math.Max(py, top);
                coverage *= Math.Max(0, Math.Min(1, edgeX)) * Math.Max(0, Math.Min(1, edgeY));
                Blend(px, py, (byte)Math.Round(r / a), (byte)Math.Round(g / a), (byte)Math.Round(b / a), coverage);
            }
        }
    }

    private void Blend(int px, int py, byte r, byte g, byte b, double alpha)
    {
        var i = (py * Width + px) * 4;
        var dstA = Pixels[i + 3] / 255.0;
        var outA = alpha + dstA * (1 - alpha);
        if (outA <= 0) return;
        Pixels[i] = (byte)Math.Round((r * alpha + Pixels[i] * dstA * (1 - alpha)) / outA);
        Pixels[i + 1] = (byte)Math.Round((g * alpha + Pixels[i + 1] * dstA * (1 - alpha)) / outA);
        Pixels[i + 2] = (byte)Math.Round((b * alpha + Pixels[i + 2] * dstA * (1 - alpha)) / outA);
        Pixels[i + 3] = (byte)Math.Round(outA * 255);
    }
}
