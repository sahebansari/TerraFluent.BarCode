using System.Xml.Linq;

namespace TerraFluent.BarCode.Tests;

public class BarcodeBuilderTests
{
    private static readonly byte[] PngSignature = { 137, 80, 78, 71, 13, 10, 26, 10 };

    [Fact]
    public void Png_has_signature_and_expected_size()
    {
        var png = Barcode.Code128("HELLO").ToPng();
        Assert.Equal(PngSignature, png.Take(8));
        Assert.Equal((90 + 20) * 2, ReadInt32BigEndian(png, 16)); // IHDR width
        Assert.Equal(80, ReadInt32BigEndian(png, 20));             // IHDR height
    }

    [Fact]
    public void Qr_image_size_includes_quiet_zone_on_all_sides()
    {
        var size = Barcode.QrCode("HELLO WORLD").ErrorCorrection(QrErrorCorrectionLevel.Q).ModuleSize(5).GetImageSize();
        Assert.Equal(((21 + 8) * 5, (21 + 8) * 5), size);
    }

    [Fact]
    public void Linear_size_follows_module_size_height_and_quiet_zone()
    {
        var size = Barcode.Ean13("590123412345").ModuleSize(3).Height(50).QuietZone(0).GetImageSize();
        Assert.Equal((95 * 3, 50), size);
    }

    [Fact]
    public void Jpeg_starts_with_soi_and_ends_with_eoi()
    {
        var jpg = Barcode.QrCode("test").Foreground("#123456").ToJpeg();
        Assert.Equal(new byte[] { 0xFF, 0xD8 }, jpg.Take(2));
        Assert.Equal(new byte[] { 0xFF, 0xD9 }, jpg.Skip(jpg.Length - 2));
    }

    [Fact]
    public void Svg_is_well_formed_and_uses_colours()
    {
        var svg = Barcode.Code39("ABC").Foreground("#336699").Background("#fff").ToSvg();
        var doc = XDocument.Parse(svg);
        Assert.Equal("svg", doc.Root!.Name.LocalName);
        Assert.Contains("fill=\"#336699\"", svg);
        Assert.Contains("fill=\"#FFFFFF\"", svg);
        Assert.Equal("0 0 " + Barcode.Code39("ABC").GetImageSize().Width + " 80", doc.Root.Attribute("viewBox")!.Value);
    }

    [Theory]
    [InlineData(BarcodeImageFormat.Png)]
    [InlineData(BarcodeImageFormat.Jpeg)]
    [InlineData(BarcodeImageFormat.Svg)]
    public void Base64_stream_and_bytes_agree(BarcodeImageFormat format)
    {
        var builder = Barcode.QrCode("https://example.com");
        var bytes = builder.ToBytes(format);
        Assert.Equal(bytes, Convert.FromBase64String(builder.ToBase64(format)));

        using var stream = builder.ToStream(format);
        Assert.Equal(0, stream.Position);
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        Assert.Equal(bytes, copy.ToArray());

        using var target = new MemoryStream();
        builder.WriteTo(target, format);
        Assert.Equal(bytes, target.ToArray());
    }

    [Fact]
    public void Data_uri_has_mime_type_prefix()
    {
        Assert.StartsWith("data:image/png;base64,iVBOR", Barcode.Code128("X").ToDataUri());
        Assert.StartsWith("data:image/jpeg;base64,", Barcode.Code128("X").ToDataUri(BarcodeImageFormat.Jpeg));
        Assert.StartsWith("data:image/svg+xml;base64,", Barcode.Code128("X").ToDataUri(BarcodeImageFormat.Svg));
    }

    [Theory]
    [InlineData("code.png", BarcodeImageFormat.Png)]
    [InlineData("code.JPG", BarcodeImageFormat.Jpeg)]
    [InlineData("code.jpeg", BarcodeImageFormat.Jpeg)]
    [InlineData("code.svg", BarcodeImageFormat.Svg)]
    public void Save_infers_format_from_extension(string fileName, BarcodeImageFormat expected)
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + "-" + fileName);
        try
        {
            var builder = Barcode.UpcA("03600029145");
            builder.Save(path);
            Assert.Equal(builder.ToBytes(expected), File.ReadAllBytes(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Unknown_extension_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => Barcode.Code128("X").Save("code.gif"));
    }

    [Fact]
    public void Symbology_specific_options_are_guarded()
    {
        Assert.Throws<InvalidOperationException>(() => Barcode.Code128("X").ErrorCorrection(QrErrorCorrectionLevel.H));
        Assert.Throws<InvalidOperationException>(() => Barcode.QrCode("X").Height(10));
        Assert.Throws<InvalidOperationException>(() => Barcode.Ean13("590123412345").WithChecksum());
    }

    [Fact]
    public void Invalid_arguments_are_rejected()
    {
        Assert.Throws<ArgumentException>(() => Barcode.Code128(""));
        Assert.Throws<ArgumentNullException>(() => Barcode.QrCode(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => Barcode.Code128("X").ModuleSize(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Barcode.Code128("X").QuietZone(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Barcode.Code128("X").JpegQuality(101));
        Assert.Throws<FormatException>(() => Barcode.Code128("X").Foreground("blue"));
    }

    [Fact]
    public void Encoding_errors_surface_when_output_is_requested()
    {
        var builder = Barcode.Ean13("12345");
        Assert.Throws<ArgumentException>(() => builder.ToPng());
    }

    [Fact]
    public void Changing_error_correction_re_encodes()
    {
        var builder = Barcode.QrCode(new string('A', 20)).ErrorCorrection(QrErrorCorrectionLevel.L);
        var small = builder.Encode().Width;
        var large = builder.ErrorCorrection(QrErrorCorrectionLevel.H).Encode().Width;
        Assert.True(large > small);
    }

    [Fact]
    public void Create_dispatches_by_type()
    {
        Assert.Equal(Barcode.Ean8("9638507").ToSvg(), Barcode.Create(BarcodeType.Ean8, "9638507").ToSvg());
    }

    [Fact]
    public void Colour_parsing_supports_short_and_long_hex()
    {
        Assert.Equal(new BarcodeColor(0x11, 0x22, 0x33), BarcodeColor.FromHex("#123"));
        Assert.Equal("#1A2B3C", BarcodeColor.FromHex("1a2b3c").ToHex());
    }

    private static int ReadInt32BigEndian(byte[] b, int offset) => b[offset] << 24 | b[offset + 1] << 16 | b[offset + 2] << 8 | b[offset + 3];
}
