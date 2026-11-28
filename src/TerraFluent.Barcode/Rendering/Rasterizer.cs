namespace TerraFluent.Barcode.Rendering;

/// <summary>An 8-bit RGB pixel buffer, row by row, with an optional alpha channel.</summary>
internal sealed class RgbImage
{
    public RgbImage(int width, int height, bool withAlpha = false)
    {
        Width = width;
        Height = height;
        Pixels = new byte[checked(width * height * 3)];
        if (withAlpha) Alpha = new byte[width * height];
    }

    public int Width { get; }
    public int Height { get; }
    public byte[] Pixels { get; }

    /// <summary>Straight (non-premultiplied) alpha per pixel, or <c>null</c> for an opaque image.</summary>
    public byte[]? Alpha { get; }

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
/// Draws a <see cref="Scene"/> into <see cref="RgbImage"/> bands with 4x4
/// supersampling, so curves and gradients come out smooth. Rendering band by
/// band keeps memory proportional to the image width: encoders ask for the
/// next band of rows, compress it, and reuse the buffers.
/// </summary>
internal sealed class Rasterizer
{
    /// <summary>Rows per band; a multiple of 8 so JPEG blocks never straddle two bands.</summary>
    public const int BandHeight = 64;

    private const int Samples = 4;
    private const ushort Full = 0xFFFF;

    private readonly Scene _scene;
    private readonly ushort[] _coverage;
    private readonly List<(double X, int Direction)> _crossings = new();

    // Sample masks of anti-aliased shapes, keyed by size, corner radii and sub-pixel offset: the modules
    // of a styled QR code are the same few shapes repeated, so each is sampled once and then copied.
    private readonly Dictionary<ShapeKey, ushort[]> _shapeMasks = new();

    public Rasterizer(Scene scene, int bandHeight = BandHeight)
    {
        _scene = scene;
        BandRows = Math.Max(1, Math.Min(bandHeight, scene.Height));
        _coverage = new ushort[checked(scene.Width * BandRows)];
    }

    /// <summary>Rows in a full band (the last band may be shorter).</summary>
    public int BandRows { get; }

    /// <summary>Renders the whole scene into one image.</summary>
    public static RgbImage Render(Scene scene)
    {
        var image = new RgbImage(scene.Width, scene.Height, scene.Transparent);
        new Rasterizer(scene, scene.Height).RenderBand(0, image);
        return image;
    }

    /// <summary>A band buffer sized for this rasterizer, to pass to <see cref="RenderBand"/>.</summary>
    public RgbImage CreateBand() => new(_scene.Width, BandRows, _scene.Transparent);

    /// <summary>
    /// Renders rows <paramref name="y0"/> to <paramref name="y0"/> + <paramref name="rows"/> - 1
    /// into the first <paramref name="rows"/> rows of <paramref name="band"/>.
    /// </summary>
    public void RenderBand(int y0, RgbImage band, int rows = -1)
    {
        var scene = _scene;
        var width = scene.Width;
        if (rows < 0) rows = Math.Min(band.Height, scene.Height - y0);
        var y1 = y0 + rows;

        var px = band.Pixels;
        var bg = scene.Background;
        for (var i = 0; i < width * rows * 3; i += 3)
        {
            px[i] = bg.R;
            px[i + 1] = bg.G;
            px[i + 2] = bg.B;
        }

        if (band.Alpha is not null) Array.Clear(band.Alpha, 0, width * rows);

        // One bit per sample point; the union of a layer's shapes is OR-ed together, so shared edges leave no seams.
        var coverage = _coverage;
        foreach (var layer in scene.Layers)
        {
            Array.Clear(coverage, 0, width * rows);
            var any = false;
            foreach (var shape in layer.Shapes)
            {
                if (shape.Y >= y1 || shape.Y + shape.Height <= y0) continue;
                Cover(shape, coverage, width, y0, y1);
                any = true;
            }

            foreach (var path in layer.Paths)
            {
                if (path.MinY >= y1 || path.MaxY + 1 <= y0) continue;
                Cover(path, coverage, width, y0, y1);
                any = true;
            }

            if (any) Composite(band, coverage, layer.Paint, y0, rows);
        }

        if (scene.Logo is { } logo) DrawLogo(band, logo, y0, rows);
    }

    private void Cover(Shape shape, ushort[] coverage, int width, int bandY0, int bandY1)
    {
        var x0 = Math.Max(0, (int)Math.Floor(shape.X));
        var y0 = Math.Max(bandY0, (int)Math.Floor(shape.Y));
        var x1 = Math.Min(width, (int)Math.Ceiling(shape.X + shape.Width));
        var y1 = Math.Min(bandY1, (int)Math.Ceiling(shape.Y + shape.Height));

        if (shape.IsPixelAlignedRect)
        {
            for (var y = y0; y < y1; y++)
            {
                var row = (y - bandY0) * width;
                for (var x = x0; x < x1; x++) coverage[row + x] = Full;
            }

            return;
        }

        if (shape.Hole is null && Math.Floor(shape.X) >= 0 && Math.Ceiling(shape.X + shape.Width) <= width && _shapeMasks.Count < 256)
        {
            CoverCached(shape, coverage, width, bandY0, bandY1);
            return;
        }

        CoverSampled(shape, coverage, width, x0, x1, y0, y1, bandY0);
    }

    private void CoverCached(Shape shape, ushort[] coverage, int width, int bandY0, int bandY1)
    {
        var left = (int)Math.Floor(shape.X);
        var top = (int)Math.Floor(shape.Y);
        var maskWidth = (int)Math.Ceiling(shape.X + shape.Width) - left;
        var maskHeight = (int)Math.Ceiling(shape.Y + shape.Height) - top;
        var key = new ShapeKey(shape, left, top);
        if (!_shapeMasks.TryGetValue(key, out var mask))
        {
            // Sample the first shape of this kind at its real position, then reuse the per-pixel masks.
            mask = new ushort[maskWidth * maskHeight];
            CoverSampled(shape, mask, maskWidth, 0, maskWidth, 0, maskHeight, 0, left, top);
            _shapeMasks.Add(key, mask);
        }

        var y0 = Math.Max(bandY0, top);
        var y1 = Math.Min(bandY1, top + maskHeight);
        for (var y = y0; y < y1; y++)
        {
            var source = (y - top) * maskWidth;
            var target = (y - bandY0) * width + left;
            for (var x = 0; x < maskWidth; x++) coverage[target + x] |= mask[source + x];
        }
    }

    /// <summary>
    /// Samples <paramref name="shape"/> over pixels [x0, x1) x [y0, y1) of the image and ORs
    /// the masks into <paramref name="coverage"/>, whose row r holds image row r + <paramref name="rowOffset"/>
    /// shifted left by <paramref name="originX"/> and up by <paramref name="originY"/>.
    /// </summary>
    private static void CoverSampled(Shape shape, ushort[] coverage, int width, int x0, int x1, int y0, int y1, int rowOffset, int originX = 0, int originY = 0)
    {
        // Pixels inside the rectangle, clear of the corner squares and the hole, have every sample inside.
        double innerLeft = shape.X + Math.Max(shape.TopLeft, shape.BottomLeft);
        double innerRight = shape.X + shape.Width - Math.Max(shape.TopRight, shape.BottomRight);
        double innerTop = shape.Y + Math.Max(shape.TopLeft, shape.TopRight);
        double innerBottom = shape.Y + shape.Height - Math.Max(shape.BottomLeft, shape.BottomRight);
        var hole = shape.Hole;

        for (var cy = y0; cy < y1; cy++)
        {
            var y = cy + originY;
            var row = (cy - rowOffset) * width;
            var rowInside = y >= shape.Y && y + 1 <= shape.Y + shape.Height;
            for (var cx = x0; cx < x1; cx++)
            {
                var x = cx + originX;
                var index = row + cx;
                if (coverage[index] == Full) continue;

                if (rowInside && x >= shape.X && x + 1 <= shape.X + shape.Width
                    && ((x >= innerLeft && x + 1 <= innerRight) || (y >= innerTop && y + 1 <= innerBottom))
                    && (hole is null || x + 1 <= hole.X || x >= hole.X + hole.Width || y + 1 <= hole.Y || y >= hole.Y + hole.Height))
                {
                    coverage[index] = Full;
                    continue;
                }

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

    /// <summary>
    /// Scanline fill with the non-zero winding rule: for each row of sample
    /// points, the edges crossing it are found once and sorted, then the
    /// samples are walked left to right. A sample at x is inside when the
    /// crossings to its right do not cancel out, as in <see cref="PathShape.Contains"/>.
    /// </summary>
    private void Cover(PathShape path, ushort[] coverage, int width, int bandY0, int bandY1)
    {
        var x0 = Math.Max(0, (int)Math.Floor(path.MinX));
        var y0 = Math.Max(bandY0, (int)Math.Floor(path.MinY));
        var x1 = Math.Min(width, (int)Math.Ceiling(path.MaxX) + 1);
        var y1 = Math.Min(bandY1, (int)Math.Ceiling(path.MaxY) + 1);
        var crossings = _crossings;

        for (var y = y0; y < y1; y++)
        {
            var row = (y - bandY0) * width;
            for (var sy = 0; sy < Samples; sy++)
            {
                var py = y + (sy + 0.5) / Samples;
                if (py < path.MinY || py > path.MaxY) continue;

                crossings.Clear();
                var total = 0;
                foreach (var contour in path.Contours)
                {
                    for (int i = 0, j = contour.Length - 1; i < contour.Length; j = i++)
                    {
                        var (ex0, ey0) = contour[j];
                        var (ex1, ey1) = contour[i];
                        int direction;
                        if (ey0 <= py && ey1 > py) direction = 1;
                        else if (ey0 > py && ey1 <= py) direction = -1;
                        else continue;

                        crossings.Add((ex0 + (py - ey0) * (ex1 - ex0) / (ey1 - ey0), direction));
                        total += direction;
                    }
                }

                if (crossings.Count == 0) continue;
                crossings.Sort((a, b) => a.X.CompareTo(b.X));

                // winding(px) = sum of directions of crossings with X > px.
                var next = 0;
                var winding = total;
                for (var x = x0; x < x1; x++)
                {
                    var index = row + x;
                    if (coverage[index] == Full) continue;
                    var bits = 0;
                    for (var sx = 0; sx < Samples; sx++)
                    {
                        var sampleX = x + (sx + 0.5) / Samples;
                        if (sampleX < path.MinX || sampleX > path.MaxX) continue;
                        while (next < crossings.Count && crossings[next].X <= sampleX) winding -= crossings[next++].Direction;
                        if (winding != 0) bits |= 1 << (sy * Samples + sx);
                    }

                    coverage[index] |= (ushort)bits;
                }
            }
        }
    }

    /// <summary>What makes two shapes rasterise to the same masks: size, corner radii and sub-pixel position.</summary>
    private readonly struct ShapeKey : IEquatable<ShapeKey>
    {
        private readonly double _fracX, _fracY, _width, _height, _topLeft, _topRight, _bottomRight, _bottomLeft;

        public ShapeKey(Shape shape, int left, int top)
        {
            _fracX = shape.X - left;
            _fracY = shape.Y - top;
            _width = shape.Width;
            _height = shape.Height;
            _topLeft = shape.TopLeft;
            _topRight = shape.TopRight;
            _bottomRight = shape.BottomRight;
            _bottomLeft = shape.BottomLeft;
        }

        public bool Equals(ShapeKey other) =>
            _fracX == other._fracX && _fracY == other._fracY && _width == other._width && _height == other._height
            && _topLeft == other._topLeft && _topRight == other._topRight && _bottomRight == other._bottomRight && _bottomLeft == other._bottomLeft;

        public override bool Equals(object? obj) => obj is ShapeKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = _fracX.GetHashCode();
                hash = hash * 31 + _fracY.GetHashCode();
                hash = hash * 31 + _width.GetHashCode();
                hash = hash * 31 + _height.GetHashCode();
                hash = hash * 31 + _topLeft.GetHashCode();
                hash = hash * 31 + _topRight.GetHashCode();
                hash = hash * 31 + _bottomRight.GetHashCode();
                return hash * 31 + _bottomLeft.GetHashCode();
            }
        }
    }

    private static void Composite(RgbImage image, ushort[] coverage, Paint paint, int y0, int rows)
    {
        var px = image.Pixels;
        var width = image.Width;
        var solid = paint.Direction is null;
        var flat = paint.Start;
        for (var y = 0; y < rows; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var c = coverage[y * width + x];
                if (c == 0) continue;
                var color = solid ? flat : paint.At(x + 0.5, y0 + y + 0.5);
                var i = (y * width + x) * 3;
                if (image.Alpha is { } alphaChannel)
                {
                    Over(px, alphaChannel, i, color.R, color.G, color.B, PopCount(c) / (double)(Samples * Samples));
                    continue;
                }

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

    private static void DrawLogo(RgbImage image, LogoPlacement logo, int y0, int rows)
    {
        var src = logo.Image;
        var px = image.Pixels;
        double scaleX = (double)src.Width / logo.Width, scaleY = (double)src.Height / logo.Height;
        for (var ty = 0; ty < logo.Height; ty++)
        {
            var y = logo.Y + ty - y0;
            if ((uint)y >= (uint)rows) continue;
            for (var tx = 0; tx < logo.Width; tx++)
            {
                var x = logo.X + tx;
                if ((uint)x >= (uint)image.Width) continue;

                var (r, g, b, a) = Sample(src, tx * scaleX, ty * scaleY, (tx + 1) * scaleX, (ty + 1) * scaleY);
                if (a <= 0) continue;
                var i = (y * image.Width + x) * 3;
                if (image.Alpha is { } alphaChannel)
                {
                    Over(px, alphaChannel, i, r / a, g / a, b / a, a);
                    continue;
                }

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

    /// <summary>Porter-Duff "over" onto a pixel with straight alpha.</summary>
    private static void Over(byte[] px, byte[] alpha, int i, double r, double g, double b, double a)
    {
        var dstA = alpha[i / 3] / 255.0;
        var outA = a + dstA * (1 - a);
        if (outA <= 0) return;
        var keep = dstA * (1 - a);
        px[i] = ClampByte((r * a + px[i] * keep) / outA);
        px[i + 1] = ClampByte((g * a + px[i + 1] * keep) / outA);
        px[i + 2] = ClampByte((b * a + px[i + 2] * keep) / outA);
        alpha[i / 3] = ClampByte(outA * 255);
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
