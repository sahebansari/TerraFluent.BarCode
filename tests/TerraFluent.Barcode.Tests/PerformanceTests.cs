using TerraFluent.Barcode.Encoding;
using TerraFluent.Barcode.Rendering;

namespace TerraFluent.Barcode.Tests;

/// <summary>Checks that the faster code paths give the same results as the straightforward ones.</summary>
public class PerformanceTests
{
    private const string Url = "https://github.com/sahebansari/TerraFluent.Barcode";

    [Theory]
    [InlineData(21, 1)]
    [InlineData(25, 2)]
    [InlineData(57, 3)]
    [InlineData(64, 4)]
    [InlineData(65, 5)]
    [InlineData(128, 6)]
    [InlineData(129, 7)]
    [InlineData(177, 8)]
    public void Bit_parallel_mask_penalty_matches_the_module_by_module_rules(int size, int seed)
    {
        var random = new Random(seed);
        var modules = new bool[size * size];
        var isFunction = new bool[size * size];
        for (var i = 0; i < modules.Length; i++)
        {
            // Long runs and finder-like patterns turn up more often with a bias towards one colour.
            modules[i] = random.Next(5) < 2;
            isFunction[i] = random.Next(8) == 0;
        }

        // Plant finder-like patterns in both directions so rule N3 is exercised.
        bool[] pattern = { true, false, true, true, true, false, true, false, false, false, false };
        for (var i = 0; i < pattern.Length && i < size; i++)
        {
            modules[3 * size + i] = pattern[i];
            modules[i * size + 5] = pattern[pattern.Length - 1 - i];
            isFunction[3 * size + i] = isFunction[i * size + 5] = true;
        }

        var scorer = new QrMaskPenalty(size, modules, isFunction);
        for (var mask = 0; mask < 8; mask++)
        {
            var masked = new bool[modules.Length];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var i = y * size + x;
                    masked[i] = modules[i] ^ (!isFunction[i] && QrCodeEncoder.MaskInverts(mask, x, y));
                }
            }

            Assert.Equal(ReferencePenalty(masked, size), scorer.Score(mask));
        }
    }

    [Fact]
    public void Format_line_refresh_picks_up_changed_format_modules()
    {
        const int size = 25;
        var modules = new bool[size * size];
        var isFunction = new bool[size * size];
        for (var i = 0; i < size; i++) isFunction[8 * size + i] = isFunction[i * size + 8] = true;
        var scorer = new QrMaskPenalty(size, modules, isFunction);

        for (var i = 0; i < size; i += 3) modules[8 * size + i] = modules[i * size + 8] = true;
        scorer.RefreshFormatLines(modules);

        var masked = new bool[modules.Length];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++) masked[y * size + x] = modules[y * size + x] ^ (!isFunction[y * size + x] && QrCodeEncoder.MaskInverts(2, x, y));
        }

        Assert.Equal(ReferencePenalty(masked, size), scorer.Score(2));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(64)]
    public void Banded_rendering_matches_rendering_the_whole_image(int bandHeight)
    {
        var builder = Barcodes.QrCode(Url).ModuleSize(5).ModuleStyle(QrModuleStyle.Dots).EyeStyle(QrEyeStyle.Rounded)
            .Gradient("#0D47A1", "#00838F", GradientDirection.Radial).Logo(Barcodes.QrCode("logo").ModuleSize(2).ToPng());
        var scene = builder.CreateScene()!;
        var whole = Rasterizer.Render(scene);

        var rasterizer = new Rasterizer(scene, bandHeight);
        var band = rasterizer.CreateBand();
        for (var y0 = 0; y0 < scene.Height; y0 += rasterizer.BandRows)
        {
            rasterizer.RenderBand(y0, band);
            var rows = Math.Min(rasterizer.BandRows, scene.Height - y0);
            for (var i = 0; i < rows * scene.Width * 3; i++) Assert.Equal(whole.Pixels[y0 * scene.Width * 3 + i], band.Pixels[i]);
        }
    }

    [Fact]
    public void Banded_text_rendering_matches_rendering_the_whole_image()
    {
        var scene = Barcodes.UpcA("03600029145").AddOn("12").HumanReadableText().TransparentBackground().CreateScene()!;
        var whole = Rasterizer.Render(scene);
        var rasterizer = new Rasterizer(scene, 8);
        var band = rasterizer.CreateBand();
        for (var y0 = 0; y0 < scene.Height; y0 += 8)
        {
            rasterizer.RenderBand(y0, band);
            var rows = Math.Min(8, scene.Height - y0);
            for (var i = 0; i < rows * scene.Width; i++) Assert.Equal(whole.Alpha![y0 * scene.Width + i], band.Alpha![i]);
        }
    }

    [Fact]
    public void Slicing_by_8_crc_matches_the_bytewise_crc_for_every_length_and_offset()
    {
        Assert.Equal(0xCBF43926u, PngEncoder.Crc32(0xFFFFFFFFu, System.Text.Encoding.ASCII.GetBytes("123456789"), 0, 9) ^ 0xFFFFFFFFu);

        var data = new byte[64];
        new Random(3).NextBytes(data);
        for (var offset = 0; offset < 8; offset++)
        {
            for (var count = 0; offset + count <= data.Length; count++)
            {
                var expected = 0xFFFFFFFFu;
                for (var i = offset; i < offset + count; i++)
                {
                    expected ^= data[i];
                    for (var k = 0; k < 8; k++) expected = (expected & 1) != 0 ? 0xEDB88320u ^ (expected >> 1) : expected >> 1;
                }

                Assert.Equal(expected, PngEncoder.Crc32(0xFFFFFFFFu, data, offset, count));
            }
        }
    }

    [Fact]
    public void Large_styled_png_spans_several_idat_chunks_and_decodes_to_the_rasterised_pixels()
    {
        var builder = Barcodes.QrCode(new string('x', 300)).ModuleSize(20).ModuleStyle(QrModuleStyle.Rounded).Gradient("#222222", "#1565C0");
        var png = builder.ToPng();
        Assert.True(CountChunks(png, "IDAT") > 1);

        var expected = Rasterizer.Render(builder.CreateScene()!);
        var decoded = PngDecoder.Decode(png, "png");
        for (var i = 0; i < expected.Width * expected.Height; i += 7)
        {
            Assert.Equal(expected.Pixels[i * 3], decoded.Pixels[i * 4]);
            Assert.Equal(expected.Pixels[i * 3 + 1], decoded.Pixels[i * 4 + 1]);
            Assert.Equal(expected.Pixels[i * 3 + 2], decoded.Pixels[i * 4 + 2]);
        }
    }

    public static TheoryData<string> Builders() => new() { "code128", "code128-text", "qr", "qr-styled", "pdf417" };

    [Theory]
    [MemberData(nameof(Builders))]
    public void WriteTo_writes_the_same_bytes_as_ToBytes(string kind)
    {
        foreach (var format in new[] { BarcodeImageFormat.Png, BarcodeImageFormat.Jpeg, BarcodeImageFormat.Svg })
        {
            using var stream = new MemoryStream();
            Make(kind).WriteTo(stream, format);
            Assert.Equal(Make(kind).ToBytes(format), stream.ToArray());

            using var copy = Make(kind).ToStream(format);
            Assert.False(copy.CanWrite);
            Assert.Equal(0, copy.Position);
            Assert.Equal(stream.ToArray(), ReadAll(copy));
            Assert.Equal(Convert.ToBase64String(stream.ToArray()), Make(kind).ToBase64(format));
        }
    }

    [Fact]
    public void Image_size_limit_rejects_larger_images_before_rendering()
    {
        var builder = Barcodes.QrCode(Url).ModuleSize(10);
        var (width, height) = builder.GetImageSize();
        builder.ImageSizeLimit((long)width * height);
        Assert.NotEmpty(builder.ToPng());

        builder.ImageSizeLimit((long)width * height - 1);
        Assert.Throws<ArgumentException>(() => builder.ToPng());
        Assert.Throws<ArgumentException>(() => builder.ToSvg());
        Assert.Throws<ArgumentException>(() => builder.ModuleStyle(QrModuleStyle.Dots).ToJpeg());

        using var stream = new MemoryStream();
        Assert.Throws<ArgumentException>(() => builder.WriteTo(stream));
        Assert.Equal(0, stream.Length);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    [InlineData(BarcodeBuilder.MaxImagePixels + 1)]
    public void Image_size_limit_must_be_in_range(long limit) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Barcodes.QrCode(Url).ImageSizeLimit(limit));

    [Fact]
    public void Generator_applies_the_image_size_limit_option()
    {
        var generator = new BarcodeGenerator(new BarcodeOptions { MaxImagePixels = 10_000 });
        Assert.Throws<ArgumentException>(() => generator.Generate(BarcodeType.QrCode, Url));
        Assert.NotEmpty(generator.Generate(BarcodeType.QrCode, Url, options: new BarcodeOptions()));
    }

    [Fact]
    public async Task Cancelled_writes_stop_before_writing_anything()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        using var stream = new MemoryStream();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Barcodes.QrCode(Url).WriteToAsync(stream, cancellationToken: cancelled.Token));
        Assert.Equal(0, stream.Length);

        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".png");
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Barcodes.QrCode(Url).SaveAsync(path, cancelled.Token));
        Assert.False(File.Exists(path));
    }

    private static BarcodeBuilder Make(string kind) => kind switch
    {
        "code128" => Barcodes.Code128("ABC-123"),
        "code128-text" => Barcodes.Code128("ABC-123").HumanReadableText(),
        "qr" => Barcodes.QrCode(Url),
        "qr-styled" => Barcodes.QrCode(Url).ModuleStyle(QrModuleStyle.Connected).EyeColor("#C62828"),
        _ => Barcodes.Pdf417(Url),
    };

    private static byte[] ReadAll(Stream stream)
    {
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }

    private static int CountChunks(byte[] png, string type)
    {
        var count = 0;
        for (var offset = 8; offset + 8 <= png.Length;)
        {
            var length = (png[offset] << 24) | (png[offset + 1] << 16) | (png[offset + 2] << 8) | png[offset + 3];
            if (System.Text.Encoding.ASCII.GetString(png, offset + 4, 4) == type) count++;
            offset += 12 + length;
        }

        return count;
    }

    /// <summary>The ISO/IEC 18004 section 7.8.3 rules (N1-N4), written out module by module.</summary>
    private static int ReferencePenalty(bool[] modules, int size)
    {
        bool Get(int x, int y) => modules[y * size + x];
        bool[] a = { true, false, true, true, true, false, true, false, false, false, false };
        bool[] b = { false, false, false, false, true, false, true, true, true, false, true };
        var penalty = 0;
        for (var pass = 0; pass < 2; pass++)
        {
            for (var line = 0; line < size; line++)
            {
                bool At(int i) => pass == 0 ? Get(i, line) : Get(line, i);
                var run = 0;
                for (var i = 0; i < size; i++)
                {
                    run = i > 0 && At(i) == At(i - 1) ? run + 1 : 1;
                    if (run == 5) penalty += 3;
                    else if (run > 5) penalty++;

                    if (i < 10) continue;
                    bool matchA = true, matchB = true;
                    for (var k = 0; k < 11; k++)
                    {
                        matchA &= At(i - 10 + k) == a[k];
                        matchB &= At(i - 10 + k) == b[k];
                    }

                    if (matchA || matchB) penalty += 40;
                }
            }
        }

        for (var y = 0; y < size - 1; y++)
        {
            for (var x = 0; x < size - 1; x++)
            {
                var c = Get(x, y);
                if (c == Get(x + 1, y) && c == Get(x, y + 1) && c == Get(x + 1, y + 1)) penalty += 3;
            }
        }

        var dark = modules.Count(m => m);
        var total = size * size;
        var k4 = (Math.Abs(dark * 20 - total * 10) + total - 1) / total - 1;
        return penalty + k4 * 10;
    }
}
