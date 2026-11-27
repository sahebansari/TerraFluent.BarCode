using System.Xml.Linq;
using TerraFluent.BarCode.Encoding;
using TerraFluent.BarCode.Rendering;

namespace TerraFluent.BarCode.Tests;

public class StylingTests
{
    private static byte[] Logo(int size = 32)
    {
        var canvas = new Canvas(size, size);
        for (var i = 0; i < canvas.Pixels.Length; i += 4)
        {
            canvas.Pixels[i] = 233;
            canvas.Pixels[i + 1] = 30;
            canvas.Pixels[i + 2] = 99;
            canvas.Pixels[i + 3] = 255;
        }

        return PngEncoder.EncodeRgba(canvas);
    }

    [Fact]
    public void Empty_style_renders_exactly_like_no_style()
    {
        Assert.Equal(Barcode.QrCode("plain").ToPng(), Barcode.QrCode("plain").Style(_ => { }).ToPng());
        Assert.Equal(Barcode.Code128("plain").ToSvg(), Barcode.Code128("plain").Style(_ => { }).ToSvg());
    }

    [Fact]
    public void Styled_png_is_rgba()
    {
        var png = Barcode.QrCode("dots").Style(s => s.ModuleShape(QrModuleShape.Circle)).ToPng();
        Assert.Equal(6, png[25]); // IHDR colour type
    }

    [Fact]
    public void Transparent_background_leaves_quiet_zone_clear()
    {
        var png = Barcode.QrCode("clear").Style(s => s.TransparentBackground()).ToPng();
        var image = PngDecoder.Decode(png);
        Assert.Equal(0, image.Pixels[3]); // top-left pixel alpha

        var svg = Barcode.QrCode("clear").Style(s => s.TransparentBackground()).ToSvg();
        Assert.DoesNotContain("<rect", svg);
    }

    [Fact]
    public void Transparent_jpeg_falls_back_to_background_colour()
    {
        var jpg = Barcode.QrCode("clear").Background("#FFFFFF").Style(s => s.TransparentBackground()).ToJpeg();
        Assert.Equal(new byte[] { 0xFF, 0xD8 }, jpg.Take(2));
    }

    [Fact]
    public void Gradient_changes_colour_across_the_symbol()
    {
        var builder = Barcode.Code128("GRADIENT").ModuleSize(2).QuietZone(0).Style(s => s.Gradient("#FF0000", "#0000FF", GradientDirection.Horizontal));
        var image = PngDecoder.Decode(builder.ToPng());
        bool IsBar(int x) => !(image.Pixels[x * 4] == 255 && image.Pixels[x * 4 + 1] == 255 && image.Pixels[x * 4 + 2] == 255);
        var left = image.Pixels.Skip(Enumerable.Range(0, image.Width).First(IsBar) * 4).Take(3).ToArray();
        var right = image.Pixels.Skip(Enumerable.Range(0, image.Width).Last(IsBar) * 4).Take(3).ToArray();
        Assert.True(left[0] > right[0], "red should fade left to right");
        Assert.True(left[2] < right[2], "blue should grow left to right");
    }

    [Theory]
    [InlineData(GradientDirection.Horizontal, "linearGradient")]
    [InlineData(GradientDirection.Diagonal, "linearGradient")]
    [InlineData(GradientDirection.Radial, "radialGradient")]
    public void Svg_gradient_is_declared_and_used(GradientDirection direction, string element)
    {
        var svg = Barcode.QrCode("svg").Style(s => s.Gradient("#1A3C8F", "#00897B", direction)).ToSvg();
        var doc = XDocument.Parse(svg);
        Assert.Single(doc.Descendants().Where(e => e.Name.LocalName == element));
        Assert.Contains("fill=\"url(#tf-fg)\"", svg);
    }

    [Fact]
    public void Finder_colour_gets_its_own_path()
    {
        var svg = Barcode.QrCode("eyes").Style(s => s.FinderColor("#E91E63").FinderShape(QrFinderShape.Circle)).ToSvg();
        Assert.Contains("fill=\"#E91E63\"", svg);
        Assert.Equal(2, XDocument.Parse(svg).Descendants().Count(e => e.Name.LocalName == "path"));
    }

    [Fact]
    public void Rounded_bars_use_arcs_in_svg()
    {
        var svg = Barcode.Ean13("590123412345").Style(s => s.BarShape(BarShape.Rounded)).ToSvg();
        Assert.Contains("a", XDocument.Parse(svg).Descendants().First(e => e.Name.LocalName == "path").Attribute("d")!.Value);
    }

    [Fact]
    public void Logo_defaults_error_correction_to_H()
    {
        var plain = Barcode.QrCode("https://terrafluent.dev").Encode().Width;
        var withLogo = Barcode.QrCode("https://terrafluent.dev").Style(s => s.Logo(Logo())).Encode().Width;
        var explicitH = Barcode.QrCode("https://terrafluent.dev").ErrorCorrection(QrErrorCorrectionLevel.H).Encode().Width;
        Assert.True(withLogo >= explicitH);
        Assert.True(withLogo > plain);
    }

    [Fact]
    public void Logo_with_low_error_correction_is_rejected()
    {
        var builder = Barcode.QrCode("x").ErrorCorrection(QrErrorCorrectionLevel.M).Style(s => s.Logo(Logo()));
        var ex = Assert.Throws<InvalidOperationException>(() => builder.ToPng());
        Assert.Contains("Q or H", ex.Message);
    }

    [Fact]
    public void Large_logo_on_short_value_raises_the_version_until_recoverable()
    {
        var minimal = QrCodeEncoder.Encode("HI", QrErrorCorrectionLevel.H).Width;
        var withLogo = Barcode.QrCode("HI").Style(s => s.Logo(Logo(), 0.3)).Encode().Width;
        Assert.True(withLogo > minimal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.31)]
    [InlineData(double.NaN)]
    public void Logo_size_is_validated(double ratio)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Barcode.QrCode("x").Style(s => s.Logo(Logo(), ratio)));
    }

    [Fact]
    public void Svg_embeds_the_logo_as_a_data_uri()
    {
        var svg = Barcode.QrCode("logo").Style(s => s.Logo(Logo())).ToSvg();
        Assert.Contains("xlink:href=\"data:image/png;base64,", svg);
        XDocument.Parse(svg);
    }

    [Fact]
    public void Jpeg_logo_works_in_svg_but_not_in_raster_output()
    {
        var jpegLogo = Barcode.Code128("x").ToJpeg();
        Assert.Contains("data:image/jpeg;base64,", Barcode.QrCode("j").Style(s => s.Logo(jpegLogo)).ToSvg());
        Assert.Throws<NotSupportedException>(() => Barcode.QrCode("j").Style(s => s.Logo(jpegLogo)).ToPng());
    }

    [Fact]
    public void Logo_pixels_appear_in_the_centre()
    {
        var image = PngDecoder.Decode(Barcode.QrCode("centre").ModuleSize(4).Style(s => s.Logo(Logo())).ToPng());
        var c = (image.Height / 2 * image.Width + image.Width / 2) * 4;
        Assert.Equal(new byte[] { 233, 30, 99, 255 }, image.Pixels.Skip(c).Take(4));
    }

    [Fact]
    public void Options_are_guarded_by_symbology()
    {
        Assert.Throws<InvalidOperationException>(() => Barcode.Code128("x").Style(s => s.ModuleShape(QrModuleShape.Circle)));
        Assert.Throws<InvalidOperationException>(() => Barcode.Code128("x").Style(s => s.Logo(Logo())));
        Assert.Throws<InvalidOperationException>(() => Barcode.QrCode("x").Style(s => s.BarShape(BarShape.Rounded)));
    }

    [Fact]
    public void Png_decoder_round_trips_rgba()
    {
        var canvas = new Canvas(3, 2);
        for (var i = 0; i < canvas.Pixels.Length; i++) canvas.Pixels[i] = (byte)(i * 10);
        var decoded = PngDecoder.Decode(PngEncoder.EncodeRgba(canvas));
        Assert.Equal(canvas.Pixels, decoded.Pixels);
    }

    [Fact]
    public void Png_decoder_reads_one_bit_palette_images()
    {
        // Our own unstyled output is a 1-bit indexed PNG.
        var image = PngDecoder.Decode(Barcode.QrCode("x").Foreground("#102030").ToPng());
        Assert.Equal(new byte[] { 255, 255, 255, 255 }, image.Pixels.Take(4));
        var q = 4 * 4; // first module after a 4-module quiet zone at module size 4
        var finder = (q * image.Width + q) * 4;
        Assert.Equal(new byte[] { 0x10, 0x20, 0x30, 255 }, image.Pixels.Skip(finder).Take(4));
    }
}
