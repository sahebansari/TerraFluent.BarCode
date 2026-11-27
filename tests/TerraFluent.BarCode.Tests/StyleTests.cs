using System.Xml.Linq;
using TerraFluent.BarCode.Rendering;

namespace TerraFluent.BarCode.Tests;

public class StyleTests
{
    private const string Url = "https://github.com/sahebansari/TerraFluent.BarCode";

    public static TheoryData<QrModuleStyle, QrEyeStyle> AllQrStyles()
    {
        var data = new TheoryData<QrModuleStyle, QrEyeStyle>();
        foreach (var module in Enum.GetValues(typeof(QrModuleStyle)).Cast<QrModuleStyle>())
        {
            foreach (var eye in Enum.GetValues(typeof(QrEyeStyle)).Cast<QrEyeStyle>()) data.Add(module, eye);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(AllQrStyles))]
    public void Styled_qr_keeps_every_module_centre_readable(QrModuleStyle module, QrEyeStyle eye)
    {
        var builder = Barcode.QrCode(Url).ModuleSize(8).ModuleStyle(module).EyeStyle(eye)
            .Gradient("#1A3C8F", "#7B1FA2", GradientDirection.Radial);
        AssertModuleCentresMatch(builder, skipLogoArea: false);
    }

    [Fact]
    public void Logo_qr_keeps_modules_outside_the_logo_and_switches_to_level_h()
    {
        var logo = Barcode.QrCode("logo").Gradient("#E53935", "#FDD835").AllowLowContrast().ToPng();
        var plain = Barcode.QrCode(Url).Encode();
        var builder = Barcode.QrCode(Url).ModuleSize(8).Logo(logo, 0.25);

        Assert.True(builder.Encode().Width > plain.Width); // level H needs a larger version than M
        AssertModuleCentresMatch(builder, skipLogoArea: true);

        var scene = builder.CreateScene()!;
        var n = builder.Encode().Width;
        var reserved = (4 + 9) * 8; // quiet zone + finder, separator and format modules, in pixels
        Assert.NotNull(scene.Logo);
        Assert.True(scene.Logo!.X >= reserved && scene.Logo.Y >= reserved);
        Assert.True(scene.Logo.X + scene.Logo.Width <= (4 + n - 9) * 8);
    }

    [Fact]
    public void Explicit_error_correction_wins_over_the_logo_default()
    {
        var logo = Barcode.QrCode("logo").ModuleStyle(QrModuleStyle.Dots).ToPng();
        var withLogo = Barcode.QrCode(Url).ErrorCorrection(QrErrorCorrectionLevel.Q).Logo(logo).Encode();
        Assert.Equal(Barcode.QrCode(Url).ErrorCorrection(QrErrorCorrectionLevel.Q).Encode().ToString(), withLogo.ToString());
    }

    [Theory]
    [InlineData(BarcodeType.Code128, "TerraFluent-2026")]
    [InlineData(BarcodeType.Ean13, "590123412345")]
    [InlineData(BarcodeType.UpcA, "03600029145")]
    public void Rounded_gradient_bars_keep_every_bar(BarcodeType type, string value)
    {
        var builder = Barcode.Create(type, value).ModuleSize(3).RoundedBars().Gradient("#0D47A1", "#00838F", GradientDirection.Horizontal);
        var matrix = builder.Encode();
        var image = Rasterizer.Render(builder.CreateScene()!);
        var q = matrix.RecommendedQuietZone;
        for (var x = 0; x < matrix.Width; x++)
        {
            var px = (q + x) * 3 + 1;
            Assert.Equal(matrix[x, 0], IsDark(image[px, image.Height / 2]));
        }
    }

    [Fact]
    public void Plain_barcodes_still_use_one_bit_png()
    {
        Assert.Equal(1, Barcode.QrCode(Url).ToPng()[24]); // IHDR bit depth
        var styled = Barcode.QrCode(Url).ModuleStyle(QrModuleStyle.Dots).ToPng();
        Assert.Equal(8, styled[24]);
        Assert.Equal(2, styled[25]); // colour type RGB
    }

    [Fact]
    public void Styled_png_decodes_back_to_the_rasterised_pixels()
    {
        var builder = Barcode.QrCode("round trip").ModuleStyle(QrModuleStyle.Connected).EyeColor("#C62828").Gradient("#222", "#1565C0");
        var expected = Rasterizer.Render(builder.CreateScene()!);
        var decoded = PngDecoder.Decode(builder.ToPng(), "png");
        Assert.Equal((expected.Width, expected.Height), (decoded.Width, decoded.Height));
        for (var i = 0; i < expected.Width * expected.Height; i++)
        {
            Assert.Equal(expected.Pixels[i * 3], decoded.Pixels[i * 4]);
            Assert.Equal(expected.Pixels[i * 3 + 2], decoded.Pixels[i * 4 + 2]);
            Assert.Equal(255, decoded.Pixels[i * 4 + 3]);
        }
    }

    [Fact]
    public void Png_decoder_reads_one_bit_palette_images()
    {
        var builder = Barcode.Code39("ABC").Foreground("#336699");
        var decoded = PngDecoder.Decode(builder.ToPng(), "png");
        var matrix = builder.Encode();
        var x = matrix.RecommendedQuietZone * 2; // the start character's first (dark) bar
        Assert.Equal(new byte[] { 0x33, 0x66, 0x99, 255 }, decoded.Pixels.Skip(x * 4).Take(4));
        Assert.Equal(new byte[] { 255, 255, 255, 255 }, decoded.Pixels.Take(4));
    }

    [Fact]
    public void Styled_svg_is_well_formed_with_gradient_and_logo()
    {
        var logo = Barcode.QrCode("logo").ToPng();
        var svg = Barcode.QrCode(Url).ModuleStyle(QrModuleStyle.Rounded).EyeStyle(QrEyeStyle.Circle)
            .Gradient("#1A3C8F", "#00897B").EyeColor("#E65100").Logo(logo).ToSvg();
        var doc = XDocument.Parse(svg);
        XNamespace ns = "http://www.w3.org/2000/svg";
        Assert.Single(doc.Descendants(ns + "linearGradient"));
        Assert.Equal(2, doc.Descendants(ns + "path").Count()); // modules + separately coloured eyes
        Assert.Contains("fill=\"#E65100\"", svg);
        Assert.Single(doc.Descendants(ns + "image"));
        Assert.DoesNotContain("crispEdges", svg);
    }

    [Fact]
    public void Gradient_only_linear_svg_stays_crisp()
    {
        var svg = Barcode.Code128("X").Gradient("#000", "#333", GradientDirection.Vertical).ToSvg();
        Assert.Contains("crispEdges", svg);
        Assert.Contains("<linearGradient", svg);
    }

    [Theory]
    [InlineData(BarcodeImageFormat.Png)]
    [InlineData(BarcodeImageFormat.Jpeg)]
    [InlineData(BarcodeImageFormat.Svg)]
    public void Styled_output_works_in_every_format(BarcodeImageFormat format)
    {
        var bytes = Barcode.QrCode("fmt").ModuleStyle(QrModuleStyle.Dots).Gradient("#000", "#1565C0").ToBytes(format);
        Assert.NotEmpty(bytes);
        Assert.StartsWith($"data:{format.ToMimeType()};base64,", Barcode.Ean8("9638507").RoundedBars().ToDataUri(format));
    }

    [Fact]
    public void Style_options_are_guarded()
    {
        Assert.Throws<InvalidOperationException>(() => Barcode.Code128("X").ModuleStyle(QrModuleStyle.Dots));
        Assert.Throws<InvalidOperationException>(() => Barcode.Ean13("590123412345").EyeStyle(QrEyeStyle.Circle));
        Assert.Throws<InvalidOperationException>(() => Barcode.Code39("X").EyeColor("#F00"));
        Assert.Throws<InvalidOperationException>(() => Barcode.QrCode("X").RoundedBars());
        Assert.Throws<InvalidOperationException>(() => Barcode.Code128("X").Logo(Barcode.QrCode("x").ToPng()));
        Assert.Throws<ArgumentException>(() => Barcode.QrCode("X").Logo(new byte[] { 1, 2, 3 }));
        Assert.Throws<ArgumentNullException>(() => Barcode.QrCode("X").Logo(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => Barcode.QrCode("X").Logo(Barcode.QrCode("x").ToPng(), 0.5));
        Assert.Throws<ArgumentOutOfRangeException>(() => Barcode.QrCode("X").ModuleStyle((QrModuleStyle)99));
        Assert.Throws<FormatException>(() => Barcode.QrCode("X").Gradient("red", "#000"));
    }

    /// <summary>Checks the pixel at every module's centre against the matrix, as a scanner samples it.</summary>
    private static void AssertModuleCentresMatch(BarcodeBuilder builder, bool skipLogoArea)
    {
        var matrix = builder.Encode();
        var scene = builder.CreateScene()!;
        var image = Rasterizer.Render(scene);
        const int m = 8, q = 4;
        var mismatches = 0;
        for (var y = 0; y < matrix.Height; y++)
        {
            for (var x = 0; x < matrix.Width; x++)
            {
                int px = (q + x) * m + m / 2, py = (q + y) * m + m / 2;
                if (skipLogoArea && scene.Logo is { } logo
                    && px >= logo.X - m && px < logo.X + logo.Width + m && py >= logo.Y - m && py < logo.Y + logo.Height + m)
                {
                    continue;
                }

                // Rounded eyes deliberately change the corners of the 7x7 finder; scanners find it by the
                // 1:1:3:1:1 run ratio through its centre, so only the centre row and column must match there.
                var n = matrix.Width;
                var fx = x < 7 ? x : x >= n - 7 ? x - (n - 7) : -1;
                var fy = y < 7 ? y : y >= n - 7 ? y - (n - 7) : -1;
                var inFinder = fx >= 0 && fy >= 0 && !(x >= n - 7 && y >= n - 7);
                if (inFinder && fx != 3 && fy != 3) continue;

                if (IsDark(image[px, py]) != matrix[x, y]) mismatches++;
            }
        }

        Assert.Equal(0, mismatches);
    }

    private static bool IsDark(BarcodeColor c) => 0.299 * c.R + 0.587 * c.G + 0.114 * c.B < 128;
}
