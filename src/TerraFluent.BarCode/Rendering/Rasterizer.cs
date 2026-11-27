namespace TerraFluent.BarCode.Rendering;

/// <summary>An 8-bit RGB pixel buffer, row by row.</summary>
internal sealed class RgbImage
{
    public RgbImage(int width, int height)
    {
        Width = width;
        Height = height;
        Pixels = new byte[checked(width * height * 3)];
    }

    public int Width { get; }
    public int Height { get; }
    public byte[] Pixels { get; }

    public BarcodeColor this[int x, int y]
    {
        get
        {
            var i = (y * Width + x) * 3;
            return new BarcodeColor(Pixels[i], Pixels[i + 1], Pixels[i + 2]);
        }
    }

    public bool IsGreyscale()
    {
        for (var i = 0; i < Pixels.Length; i += 3)
        {
            if (Pixels[i] != Pixels[i + 1] || Pixels[i + 1] != Pixels[i + 2]) return false;
        }

        return true;
    }
}

/// <summary>An 8-bit RGBA pixel buffer (straight alpha), row by row.</summary>
internal sealed class RgbaImage
{
    public RgbaImage(int width, int height, byte[] pixels)
    {
        Width = width;
        Height = height;
        Pixels = pixels;
    }

    public int Width { get; }
    public int Height { get; }
    public byte[] Pixels { get; }
}

/// <summary>
/// Draws a <see cref="Scene"/> into an <see cref="RgbImage"/> with 4x4
/// supersampling, so curves and gradients come out smooth.
/// </summary>
internal static class Rasterizer
{
    private const int Samples = 4;
    private const ushort Full = 0xFFFF;

    public static RgbImage Render(Scene scene)
    {
        var image = new RgbImage(scene.Width, scene.Height);
        var px = image.Pixels;
        var bg = scene.Background;
        for (var i = 0; i < px.Length; i += 3)
        {
            px[i] = bg.R;
            px[i + 1] = bg.G;
            px[i + 2] = bg.B;
        }

        // One bit per sample point; the union of a layer's shapes is OR-ed together, so shared edges leave no seams.
        var coverage = new ushort[scene.Width * scene.Height];
        foreach (var layer in scene.Layers)
        {
            Array.Clear(coverage, 0, coverage.Length);
            foreach (var shape in layer.Shapes) Cover(shape, coverage, scene.Width, scene.Height);
            Composite(image, coverage, layer.Paint);
        }

        if (scene.Logo is { } logo) DrawLogo(image, logo);
        return image;
    }

    private static void Cover(Shape shape, ushort[] coverage, int width, int height)
    {
        var x0 = Math.Max(0, (int)Math.Floor(shape.X));
        var y0 = Math.Max(0, (int)Math.Floor(shape.Y));
        var x1 = Math.Min(width, (int)Math.Ceiling(shape.X + shape.Width));
        var y1 = Math.Min(height, (int)Math.Ceiling(shape.Y + shape.Height));

        if (shape.IsPixelAlignedRect)
        {
            for (var y = y0; y < y1; y++)
            {
                for (var x = x0; x < x1; x++) coverage[y * width + x] = Full;
            }

            return;
        }

        for (var y = y0; y < y1; y++)
        {
            for (var x = x0; x < x1; x++)
            {
                var index = y * width + x;
                if (coverage[index] == Full) continue;
                var bits = 0;
                var bit = 0;
                for (var sy = 0; sy < Samples; sy++)
                {
                    var sampleY = y + (sy + 0.5) / Samples;
                    for (var sx = 0; sx < Samples; sx++, bit++)
                    {
                        if (shape.Contains(x + (sx + 0.5) / Samples, sampleY)) bits |= 1 << bit;
                    }
                }

                coverage[index] |= (ushort)bits;
            }
        }
    }

    private static void Composite(RgbImage image, ushort[] coverage, Paint paint)
    {
        var px = image.Pixels;
        for (var y = 0; y < image.Height; y++)
        {
            for (var x = 0; x < image.Width; x++)
            {
                var c = coverage[y * image.Width + x];
                if (c == 0) continue;
                var color = paint.At(x + 0.5, y + 0.5);
                var i = (y * image.Width + x) * 3;
                if (c == Full)
                {
                    px[i] = color.R;
                    px[i + 1] = color.G;
                    px[i + 2] = color.B;
                    continue;
                }

                var alpha = PopCount(c) / (double)(Samples * Samples);
                px[i] = Blend(px[i], color.R, alpha);
                px[i + 1] = Blend(px[i + 1], color.G, alpha);
                px[i + 2] = Blend(px[i + 2], color.B, alpha);
            }
        }
    }

    private static void DrawLogo(RgbImage image, LogoPlacement logo)
    {
        var src = logo.Image;
        var px = image.Pixels;
        double scaleX = (double)src.Width / logo.Width, scaleY = (double)src.Height / logo.Height;
        for (var ty = 0; ty < logo.Height; ty++)
        {
            var y = logo.Y + ty;
            if ((uint)y >= (uint)image.Height) continue;
            for (var tx = 0; tx < logo.Width; tx++)
            {
                var x = logo.X + tx;
                if ((uint)x >= (uint)image.Width) continue;

                var (r, g, b, a) = Sample(src, tx * scaleX, ty * scaleY, (tx + 1) * scaleX, (ty + 1) * scaleY);
                if (a <= 0) continue;
                var i = (y * image.Width + x) * 3;
                // Sample returns premultiplied colour.
                px[i] = ClampByte(px[i] * (1 - a) + r);
                px[i + 1] = ClampByte(px[i + 1] * (1 - a) + g);
                px[i + 2] = ClampByte(px[i + 2] * (1 - a) + b);
            }
        }
    }

    /// <summary>
    /// Averages the source over the footprint [x0, x1) x [y0, y1) of one target
    /// pixel (box filter when shrinking, bilinear when enlarging). Returns
    /// premultiplied RGB (0-255) and alpha (0-1).
    /// </summary>
    private static (double R, double G, double B, double A) Sample(RgbaImage src, double x0, double y0, double x1, double y1)
    {
        if (x1 - x0 <= 1 && y1 - y0 <= 1) return Bilinear(src, (x0 + x1) / 2 - 0.5, (y0 + y1) / 2 - 0.5);

        var ix0 = (int)Math.Floor(x0);
        var iy0 = (int)Math.Floor(y0);
        var ix1 = Math.Min(src.Width, Math.Max(ix0 + 1, (int)Math.Ceiling(x1)));
        var iy1 = Math.Min(src.Height, Math.Max(iy0 + 1, (int)Math.Ceiling(y1)));
        double r = 0, g = 0, b = 0, a = 0;
        var count = 0;
        for (var y = iy0; y < iy1; y++)
        {
            for (var x = ix0; x < ix1; x++)
            {
                var i = (y * src.Width + x) * 4;
                var alpha = src.Pixels[i + 3] / 255.0;
                r += src.Pixels[i] * alpha;
                g += src.Pixels[i + 1] * alpha;
                b += src.Pixels[i + 2] * alpha;
                a += alpha;
                count++;
            }
        }

        return count == 0 ? (0, 0, 0, 0) : (r / count, g / count, b / count, a / count);
    }

    private static (double R, double G, double B, double A) Bilinear(RgbaImage src, double x, double y)
    {
        x = Math.Max(0, Math.Min(src.Width - 1, x));
        y = Math.Max(0, Math.Min(src.Height - 1, y));
        var xa = (int)x;
        var ya = (int)y;
        var xb = Math.Min(src.Width - 1, xa + 1);
        var yb = Math.Min(src.Height - 1, ya + 1);
        double fx = x - xa, fy = y - ya;
        double r = 0, g = 0, b = 0, a = 0;
        void Add(int sx, int sy, double w)
        {
            var i = (sy * src.Width + sx) * 4;
            var alpha = src.Pixels[i + 3] / 255.0 * w;
            r += src.Pixels[i] * alpha;
            g += src.Pixels[i + 1] * alpha;
            b += src.Pixels[i + 2] * alpha;
            a += alpha;
        }

        Add(xa, ya, (1 - fx) * (1 - fy));
        Add(xb, ya, fx * (1 - fy));
        Add(xa, yb, (1 - fx) * fy);
        Add(xb, yb, fx * fy);
        return (r, g, b, a);
    }

    private static byte Blend(byte under, byte over, double alpha) => ClampByte(under + (over - under) * alpha);

    private static byte ClampByte(double value) => value <= 0 ? (byte)0 : value >= 255 ? (byte)255 : (byte)Math.Round(value);

    private static int PopCount(int value)
    {
        var count = 0;
        while (value != 0)
        {
            value &= value - 1;
            count++;
        }

        return count;
    }
}
