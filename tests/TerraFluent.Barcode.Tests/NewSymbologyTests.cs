using TerraFluent.Barcode.Encoding;
using ZXing;

namespace TerraFluent.Barcode.Tests;

/// <summary>Interleaved 2 of 5, ITF-14, Codabar, Aztec and MaxiCode, each read back with ZXing.Net.</summary>
public class NewSymbologyTests
{
    // ---- Interleaved 2 of 5 and ITF-14 ----

    [Theory]
    [InlineData("00")]
    [InlineData("1234567890")]
    [InlineData("30712345000010")]
    [InlineData("0123456789012345678901234567890123456789")]
    public void Itf_scans(string value)
    {
        var png = Barcodes.Itf(value).ToPng();
        Assert.Equal(value, DecoderHelper.DecodeText(png, BarcodeFormat.ITF, o => o.AllowedLengths = new[] { value.Length }));
    }

    [Fact]
    public void Itf_checksum_appends_a_gs1_check_digit()
    {
        var png = Barcodes.Itf("1234567").WithChecksum().ToPng();
        Assert.Equal("12345670", DecoderHelper.DecodeText(png, BarcodeFormat.ITF, o => o.AllowedLengths = new[] { 8 }));
        Assert.Equal('0', EanUpcEncoder.CheckDigit("1234567"));
    }

    [Fact]
    public void Itf_symbol_has_expected_width()
    {
        // Start (4) + 5 modules per digit pair element group: each pair is 2 x (3 narrow + 2 wide x 3) = 18, stop (3 + 1 + 1).
        var matrix = Barcodes.Itf("1234").Encode();
        Assert.Equal(4 + 2 * 18 + 5, matrix.Width);
        Assert.True(matrix.IsLinear);
    }

    [Theory]
    [InlineData("123", false)]       // odd count
    [InlineData("1234", true)]       // even count plus check digit is odd
    [InlineData("12a4", false)]      // not a digit
    public void Invalid_itf_values_throw(string value, bool checksum)
    {
        var builder = Barcodes.Itf(value);
        if (checksum) builder.WithChecksum();
        Assert.Throws<ArgumentException>(() => builder.Encode());
    }

    [Fact]
    public void Itf_length_is_limited() =>
        Assert.Throws<ArgumentException>(() => Barcodes.Itf(new string('1', Barcodes.MaxItfLength + 2)).Encode());

    [Theory]
    [InlineData("1540014128876", "15400141288763")]
    [InlineData("15400141288763", "15400141288763")]
    public void Itf14_adds_or_validates_the_check_digit(string value, string expected)
    {
        var png = Barcodes.Itf14(value).ToPng();
        Assert.Equal(expected, DecoderHelper.DecodeText(png, BarcodeFormat.ITF, o => o.AllowedLengths = new[] { 14 }));
    }

    [Theory]
    [InlineData("15400141288762")]   // wrong check digit
    [InlineData("123456789012")]     // too short
    [InlineData("154001412887A")]    // not a digit
    public void Invalid_itf14_values_throw(string value) =>
        Assert.Throws<ArgumentException>(() => Barcodes.Itf14(value).Encode());

    [Fact]
    public void Checksum_option_is_limited_to_code39_and_itf()
    {
        Assert.Throws<InvalidOperationException>(() => Barcodes.Itf14("1540014128876").WithChecksum());
        Assert.Throws<InvalidOperationException>(() => Barcodes.Codabar("A1B").WithChecksum());
    }

    // ---- Codabar ----

    [Theory]
    [InlineData("A40156B", "A40156B")]
    [InlineData("c12-34$5:6/7.8+9d", "C12-34$5:6/7.8+9D")]
    [InlineData("T123N", "A123B")]
    [InlineData("31117013206375", "A31117013206375A")]
    public void Codabar_scans(string value, string expected)
    {
        var png = Barcodes.Codabar(value).ToPng();
        Assert.Equal(expected, DecoderHelper.DecodeText(png, BarcodeFormat.CODABAR, o => o.ReturnCodabarStartEnd = true));
    }

    [Theory]
    [InlineData("A12X4B")]           // not in the alphabet
    [InlineData("A12B4B")]           // start/stop character inside
    [InlineData("A1234")]            // start without stop
    [InlineData("AB")]               // no data
    public void Invalid_codabar_values_throw(string value) =>
        Assert.Throws<ArgumentException>(() => Barcodes.Codabar(value).Encode());

    // ---- Aztec ----

    public static TheoryData<string> AztecValues => new()
    {
        "A",
        "Hello, World!",
        "https://terrafluent.dev/barcode/?q=aztec&lang=en",
        "0123456789012345678901234567890123456789",
        "Mixed CASE text. With: punctuation, \"quotes\" and\r\nline breaks; @home ~tilde|pipe",
        "Bytes \u0000\u0001ÿ and Latin-1: café über",
        "UTF-8: 你好 привет \U0001F600",
        new string('X', 300) + string.Concat(Enumerable.Range(0, 200).Select(i => (char)('a' + i % 26))),
    };

    [Theory]
    [MemberData(nameof(AztecValues))]
    public void Aztec_scans(string value)
    {
        var png = Barcodes.Aztec(value).ToPng();
        Assert.Equal(value, DecoderHelper.DecodeText(png, BarcodeFormat.AZTEC));
    }

    [Fact]
    public void Aztec_long_binary_data_scans()
    {
        var bytes = Enumerable.Range(0, 1200).Select(i => (char)((i * 37) % 256)).ToArray();
        var value = new string(bytes);
        var builder = Barcodes.Aztec(value).ModuleSize(3);
        var matrix = builder.Encode();
        Assert.True(matrix.Width > 27, "expected a full-range symbol");
        Assert.Equal(value, DecoderHelper.DecodeText(builder.ToPng(), BarcodeFormat.AZTEC));
    }

    [Fact]
    public void Aztec_picks_the_smallest_symbol()
    {
        Assert.Equal(15, Barcodes.Aztec("A").Encode().Width);                 // compact, 1 layer
        Assert.Equal(19, Barcodes.Aztec("Hello, World!").Encode().Width);     // compact, 2 layers
        var full = Barcodes.Aztec(new string('7', 400)).Encode();
        Assert.Equal(full.Width, full.Height);
        Assert.True(full.Width >= 31);
    }

    [Fact]
    public void Aztec_error_correction_grows_the_symbol()
    {
        var value = "TerraFluent.Barcode Aztec error correction";
        var low = Barcodes.Aztec(value).AztecErrorCorrection(5).Encode();
        var high = Barcodes.Aztec(value).AztecErrorCorrection(80).Encode();
        Assert.True(high.Width > low.Width);
        Assert.Equal(value, DecoderHelper.DecodeText(Barcodes.Aztec(value).AztecErrorCorrection(80).ToPng(), BarcodeFormat.AZTEC));
        Assert.Throws<ArgumentOutOfRangeException>(() => Barcodes.Aztec(value).AztecErrorCorrection(2));
        Assert.Throws<InvalidOperationException>(() => Barcodes.QrCode(value).AztecErrorCorrection(50));
    }

    [Fact]
    public void Aztec_rejects_data_that_does_not_fit() =>
        Assert.Throws<ArgumentException>(() => Barcodes.Aztec(new string('é', 4000)).Encode());

    [Fact]
    public void Aztec_bit_stuffing_avoids_all_zero_and_all_one_words()
    {
        var bits = Enumerable.Repeat(false, 12).Concat(Enumerable.Repeat(true, 6)).ToList();
        var words = AztecEncoder.Stuff(bits, 6);
        Assert.Equal(new[] { 0b000001, 0b000001, 0b001111, 0b111110 }, words);
        Assert.All(words, w => Assert.True(w != 0 && w != 0b111111));
    }

    // ---- MaxiCode ----

    [Theory]
    [InlineData("MaxiCode mode 4 test")]
    [InlineData("lower case and UPPER CASE, digits 0123456789 and 1234567890123456789")]
    [InlineData("Latin-1: Àéÿ£ and control \u001d\u001e")]
    public void MaxiCode_mode4_scans(string value)
    {
        var builder = Barcodes.MaxiCode(value);
        var result = DecoderHelper.Decode(builder.ToPng(), BarcodeFormat.MAXICODE);
        Assert.NotNull(result);
        Assert.Equal(value, result!.Text);
    }

    [Fact]
    public void MaxiCode_mode5_scans()
    {
        const string value = "Enhanced error correction";
        var result = DecoderHelper.Decode(Barcodes.MaxiCode(value).MaxiCodeMode(5).ToPng(), BarcodeFormat.MAXICODE);
        Assert.Equal(value, result!.Text);
        Assert.Equal("5", result.ResultMetadata[ResultMetadataType.ERROR_CORRECTION_LEVEL]);
    }

    [Fact]
    public void MaxiCode_structured_numeric_postcode_is_mode2()
    {
        var png = Barcodes.MaxiCode("152382802", 840, 1, "[)>\u001e01\u001d961Z00004951\u001dUPSN\u001d06X610\u001d159\u001d1234567\u001d1/1\u001d\u001dY\u001d634 ALPHA DR\u001dPITTSBURGH\u001dPA\u001e\u0004").ToPng();
        var result = DecoderHelper.Decode(png, BarcodeFormat.MAXICODE);
        Assert.NotNull(result);
        Assert.Equal("2", result!.ResultMetadata[ResultMetadataType.ERROR_CORRECTION_LEVEL]);
        Assert.StartsWith("[)>\u001e01\u001d96152382802\u001d840\u001d001\u001d1Z00004951", result.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void MaxiCode_structured_alphanumeric_postcode_is_mode3()
    {
        var result = DecoderHelper.Decode(Barcodes.MaxiCode("B1050", 56, 999, "Parcel 42").ToPng(), BarcodeFormat.MAXICODE);
        Assert.NotNull(result);
        Assert.Equal("3", result!.ResultMetadata[ResultMetadataType.ERROR_CORRECTION_LEVEL]);
        Assert.Equal("B1050 \u001d056\u001d999\u001dParcel 42", result.Text);
    }

    [Fact]
    public void MaxiCode_is_hexagonal_and_fixed_size()
    {
        var matrix = Barcodes.MaxiCode("A").Encode();
        Assert.True(matrix.IsHexagonal);
        Assert.False(matrix.IsLinear);
        Assert.Equal(30, matrix.Width);
        Assert.Equal(33, matrix.Height);
        Assert.True(matrix[28, 0] && matrix[29, 0]); // top-right orientation modules
        var full = Barcodes.MaxiCode(new string('Z', 90)).Encode();
        Assert.Equal((30, 33), (full.Width, full.Height));
    }

    [Fact]
    public void MaxiCode_validates_input()
    {
        Assert.Throws<ArgumentException>(() => Barcodes.MaxiCode(new string('A', 94)).Encode());
        Assert.Throws<ArgumentException>(() => Barcodes.MaxiCode(new string('A', 78)).MaxiCodeMode(5).Encode());
        Assert.Throws<ArgumentException>(() => Barcodes.MaxiCode("你").Encode());
        Assert.Throws<ArgumentOutOfRangeException>(() => Barcodes.MaxiCode("A").MaxiCodeMode(3));
        Assert.Throws<ArgumentOutOfRangeException>(() => Barcodes.MaxiCode("12345", 1000, 1, "x"));
        Assert.Throws<ArgumentException>(() => Barcodes.MaxiCode("ABCDEFG", 1, 1, "x").Encode());
        Assert.Throws<ArgumentException>(() => Barcodes.MaxiCode("ab", 1, 1, "x").Encode());
        Assert.Throws<InvalidOperationException>(() => Barcodes.MaxiCode("12345", 1, 1, "x").MaxiCodeMode(4));
        Assert.Throws<InvalidOperationException>(() => Barcodes.Aztec("x").MaxiCodeMode(4));
        Assert.Equal(93, MaxiCodeEncoder.Message(new string('1', 138), 93, "value").Length);
        Assert.Throws<ArgumentException>(() => MaxiCodeEncoder.Message(new string('1', 139) + "AAAA", 93, "value"));
    }

    [Fact]
    public void MaxiCode_image_has_hexagon_proportions()
    {
        var (width, height) = Barcodes.MaxiCode("A").ModuleSize(10).GetImageSize();
        Assert.Equal((30 + 2) * 10, width);
        Assert.Equal((int)Math.Ceiling((32 * Math.Sqrt(3) / 2 + 2 / Math.Sqrt(3) + 2) * 10), height);
        var svg = Barcodes.MaxiCode("A").ToSvg();
        Assert.Matches("A[0-9.]+ [0-9.]+ 0 1 1 ", svg); // bullseye rings are arcs
        Assert.DoesNotContain("crispEdges", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void MaxiCode_with_gradient_and_transparency_scans()
    {
        var png = Barcodes.MaxiCode("Styled").Gradient("#1a237e", "#000000").TransparentBackground().ModuleSize(8).ToPng();
        Assert.Equal("Styled", DecoderHelper.DecodeText(png, BarcodeFormat.MAXICODE));
    }

    // ---- All outputs and options ----

    public static TheoryData<BarcodeType, string> NewTypes => new()
    {
        { BarcodeType.Itf, "12345678" },
        { BarcodeType.Itf14, "1540014128876" },
        { BarcodeType.Codabar, "A40156B" },
        { BarcodeType.Aztec, "Aztec" },
        { BarcodeType.MaxiCode, "MaxiCode" },
    };

    [Theory]
    [MemberData(nameof(NewTypes))]
    public void Every_output_format_works(BarcodeType type, string value)
    {
        var builder = Barcodes.Create(type, value);
        Assert.Equal(new byte[] { 137, 80, 78, 71 }, builder.ToPng().Take(4));
        Assert.Equal(new byte[] { 0xFF, 0xD8 }, builder.ToJpeg().Take(2));
        Assert.StartsWith("<svg", builder.ToSvg(), StringComparison.Ordinal);
        Assert.StartsWith("data:image/png;base64,", builder.ToDataUri(), StringComparison.Ordinal);
        using var stream = builder.ToStream(BarcodeImageFormat.Jpeg);
        Assert.True(stream.Length > 0);
        Assert.Equal(builder.ToPng(), Convert.FromBase64String(builder.ToBase64()));
    }

    [Theory]
    [InlineData(BarcodeType.Itf, "12345678")]
    [InlineData(BarcodeType.Itf14, "1540014128876")]
    [InlineData(BarcodeType.Codabar, "A40156B")]
    public void Linear_types_print_human_readable_text(BarcodeType type, string value)
    {
        var plain = Barcodes.Create(type, value).GetImageSize();
        var withText = Barcodes.Create(type, value).HumanReadableText();
        Assert.True(withText.GetImageSize().Height > plain.Height);
        Assert.Contains("<path", withText.ToSvg(), StringComparison.Ordinal);
        Assert.NotNull(DecoderHelper.Decode(withText.ToPng(), type == BarcodeType.Codabar ? BarcodeFormat.CODABAR : BarcodeFormat.ITF,
            o => o.AllowedLengths = new[] { 8, 14 }));
    }

    [Fact]
    public void Two_d_types_reject_linear_options()
    {
        Assert.Throws<InvalidOperationException>(() => Barcodes.Aztec("x").Height(10));
        Assert.Throws<InvalidOperationException>(() => Barcodes.MaxiCode("x").HumanReadableText());
    }

    [Fact]
    public void Generator_applies_new_options()
    {
        var generator = new BarcodeGenerator(new BarcodeOptions
        {
            ItfChecksum = true,
            AztecErrorCorrection = 80,
            MaxiCodeMode = 5,
            Height = 50,
            HumanReadableText = true,
        });

        Assert.Equal("12345670", DecoderHelper.DecodeText(generator.Generate(BarcodeType.Itf, "1234567"), BarcodeFormat.ITF, o => o.AllowedLengths = new[] { 8 }));
        Assert.Equal(Barcodes.Aztec("abc").AztecErrorCorrection(80).Encode().Width, generator.Create(BarcodeType.Aztec, "abc").Encode().Width);
        var maxi = DecoderHelper.Decode(generator.Generate(BarcodeType.MaxiCode, "abc"), BarcodeFormat.MAXICODE);
        Assert.Equal("5", maxi!.ResultMetadata[ResultMetadataType.ERROR_CORRECTION_LEVEL]);
    }
}
