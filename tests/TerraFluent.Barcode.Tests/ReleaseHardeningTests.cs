using TerraFluent.Barcode.Encoding;

namespace TerraFluent.Barcode.Tests;

/// <summary>Input validation, limits and interoperability fixes made for the 1.0 release.</summary>
public class ReleaseHardeningTests
{
    private static readonly byte[] Logo = Barcodes.QrCode("logo").ToPng();

    [Theory]
    [InlineData("Grüße", true)]
    [InlineData("日本", true)]
    [InlineData("https://example.com/lower-case", false)]
    public void Non_ascii_byte_mode_values_get_a_utf8_eci_header(string value, bool expected)
    {
        var (mode, _, _, eci) = QrCodeEncoder.SelectMode(value);
        Assert.Equal(QrCodeEncoder.Mode.Byte, mode);
        Assert.Equal(expected, eci);
    }

    [Fact]
    public void Eci_header_counts_towards_capacity()
    {
        // Version 1-L holds 19 data codewords: 4 mode + 8 count + 17 bytes * 8 = 148 bits fits; with the 12-bit ECI header, 160 bits does not.
        var ascii = new string('a', 17);
        var utf8 = "é" + new string('a', 15); // 17 UTF-8 bytes
        Assert.Equal(21, QrCodeEncoder.Encode(ascii, QrErrorCorrectionLevel.L).Width);
        Assert.Equal(25, QrCodeEncoder.Encode(utf8, QrErrorCorrectionLevel.L).Width);
    }

    [Fact]
    public void Unpaired_surrogate_throws_argument_exception()
    {
        var ex = Assert.Throws<ArgumentException>(() => Barcodes.QrCode("a\ud800b").Encode());
        Assert.Equal("value", ex.ParamName);
    }

    [Fact]
    public void Oversized_images_are_rejected_before_allocating()
    {
        Assert.Throws<ArgumentException>(() => Barcodes.QrCode("hi").ModuleSize(3000).ToPng());
        Assert.Throws<ArgumentException>(() => Barcodes.QrCode("hi").ModuleSize(3000).GetImageSize());
        Assert.Throws<ArgumentException>(() => Barcodes.Code128(new string('A', 256)).ModuleSize(20).ToJpeg());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(BarcodeBuilder.MaxImageDimension + 1)]
    [InlineData(int.MaxValue)]
    public void Size_settings_are_range_checked(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Barcodes.QrCode("x").ModuleSize(value));
        Assert.Throws<ArgumentOutOfRangeException>(() => Barcodes.Code128("x").Height(value));
    }

    [Fact]
    public void Quiet_zone_is_range_checked()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Barcodes.QrCode("x").QuietZone(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Barcodes.QrCode("x").QuietZone(int.MaxValue));
    }

    [Fact]
    public void Largest_allowed_image_still_renders()
    {
        var size = Barcodes.Code128("ABC").ModuleSize(1).Height(BarcodeBuilder.MaxImageDimension).GetImageSize();
        Assert.Equal(BarcodeBuilder.MaxImageDimension, size.Height);
    }

    [Fact]
    public void Linear_values_have_a_length_limit()
    {
        Barcodes.Code128(new string('A', Barcodes.MaxCode128Length)).Encode();
        Barcodes.Code39(new string('A', Barcodes.MaxCode39Length)).Encode();
        Assert.Throws<ArgumentException>(() => Barcodes.Code128(new string('A', Barcodes.MaxCode128Length + 1)).Encode());
        Assert.Throws<ArgumentException>(() => Barcodes.Code39(new string('A', Barcodes.MaxCode39Length + 1)).Encode());
    }

    [Theory]
    [InlineData(QrErrorCorrectionLevel.L, 0.1)]
    [InlineData(QrErrorCorrectionLevel.M, 0.1)]
    [InlineData(QrErrorCorrectionLevel.Q, 0.3)]
    public void Logo_with_too_little_error_correction_is_rejected(QrErrorCorrectionLevel level, double scale)
    {
        var builder = Barcodes.QrCode("https://example.com").ErrorCorrection(level).Logo(Logo, scale);
        Assert.Throws<InvalidOperationException>(() => builder.ToPng());
        Assert.Throws<InvalidOperationException>(() => builder.ToSvg());
    }

    [Theory]
    [InlineData(QrErrorCorrectionLevel.Q, 0.25)]
    [InlineData(QrErrorCorrectionLevel.H, 0.3)]
    public void Logo_with_enough_error_correction_renders(QrErrorCorrectionLevel level, double scale)
    {
        Assert.NotEmpty(Barcodes.QrCode("https://example.com").Logo(Logo, scale).ErrorCorrection(level).ToPng());
    }

    [Fact]
    public void Low_contrast_is_rejected_unless_allowed()
    {
        Assert.Throws<InvalidOperationException>(() => Barcodes.QrCode("x").Foreground("#FFF").ToPng());
        Assert.Throws<InvalidOperationException>(() => Barcodes.Code128("x").Gradient("#000", "#EEE").ToSvg());
        Assert.Throws<InvalidOperationException>(() => Barcodes.QrCode("x").EyeColor("#FFEB3B").ToJpeg());
        Assert.NotEmpty(Barcodes.QrCode("x").Foreground("#FFEB3B").AllowLowContrast().ToPng());
    }

    [Fact]
    public void Inverted_colours_with_strong_contrast_are_allowed()
    {
        Assert.NotEmpty(Barcodes.QrCode("x").Foreground("#FFF").Background("#000").ToPng());
    }

    [Theory]
    [InlineData("#000000", "#FFFFFF", 21.0)]
    [InlineData("#777777", "#777777", 1.0)]
    public void Contrast_ratio_follows_wcag(string a, string b, double expected)
    {
        Assert.Equal(expected, BarcodeColor.ContrastRatio(BarcodeColor.FromHex(a), BarcodeColor.FromHex(b)), 2);
    }

    [Fact]
    public void Builder_output_is_safe_to_call_concurrently()
    {
        var builder = Barcodes.QrCode("shared").ModuleStyle(QrModuleStyle.Dots);
        var expected = builder.ToPng();
        var results = new byte[32][];
        Parallel.For(0, results.Length, i => results[i] = builder.ToPng());
        Assert.All(results, r => Assert.Equal(expected, r));
    }

    [Fact]
    public void Png_logo_with_huge_chunk_length_is_rejected()
    {
        var png = (byte[])Logo.Clone();
        // Overwrite the IHDR chunk length with 0x7FFFFFF0 so pos + length would overflow a 32-bit int.
        png[8] = 0x7F; png[9] = 0xFF; png[10] = 0xFF; png[11] = 0xF0;
        Assert.Throws<ArgumentException>(() => Barcodes.QrCode("x").Logo(png));
    }
}
