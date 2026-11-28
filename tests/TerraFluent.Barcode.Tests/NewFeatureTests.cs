using System.Linq;
using TerraFluent.Barcode.Encoding;

namespace TerraFluent.Barcode.Tests;

/// <summary>Human-readable text, add-ons, GS1-128, Code 39 full ASCII, Data Matrix, PDF417, mixed-mode QR, transparency, SaveAsync and IBarcodeGenerator.</summary>
public class NewFeatureTests
{
    private static readonly byte[] PngSignature = { 137, 80, 78, 71, 13, 10, 26, 10 };

    // ---- Code 39 full ASCII ----

    [Theory]
    [InlineData("a", "+A")]
    [InlineData("\0", "%U")]
    [InlineData("\u0001", "$A")]
    [InlineData("!", "/A")]
    [InlineData("/", "/O")]
    [InlineData(":", "/Z")]
    [InlineData("@", "%V")]
    [InlineData("`", "%W")]
    [InlineData("~", "%S")]
    [InlineData("\u007f", "%T")]
    [InlineData("AB-1. ", "AB-1. ")]
    public void Full_ascii_maps_to_code39_pairs(string input, string expected) =>
        Assert.Equal(expected, Code39Encoder.ToFullAscii(input));

    [Fact]
    public void Full_ascii_accepts_lower_case_and_rejects_non_ascii()
    {
        Assert.Throws<ArgumentException>(() => Barcode.Code39("abc").Encode());
        var full = Barcode.Code39("abc").FullAscii().Encode();
        var plain = Barcode.Code39("ABC").Encode();
        Assert.True(full.Width > plain.Width);
        Assert.Throws<ArgumentException>(() => Barcode.Code39("é").FullAscii().Encode());
        Assert.Throws<InvalidOperationException>(() => Barcode.Code128("a").FullAscii());
    }

    // ---- EAN add-ons ----

    [Theory]
    [InlineData("12", 95 + 9 + 20)]
    [InlineData("52495", 95 + 9 + 47)]
    public void Ean13_add_on_extends_the_symbol(string addOn, int width) =>
        Assert.Equal(width, Barcode.Ean13("978123456789").AddOn(addOn).Encode().Width);

    [Fact]
    public void Add_on_is_validated()
    {
        Assert.Throws<ArgumentException>(() => Barcode.Ean13("978123456789").AddOn("123"));
        Assert.Throws<ArgumentException>(() => Barcode.UpcA("03600029145").AddOn("1a"));
        Assert.Throws<InvalidOperationException>(() => Barcode.Ean8("9638507").AddOn("12"));
        Assert.Equal(95 + 9 + 20, Barcode.UpcA("03600029145").AddOn("12").Encode().Width);
    }

    // ---- GS1-128 ----

    [Fact]
    public void Gs1_inserts_fnc1_only_after_variable_length_fields()
    {
        var f = Code128Encoder.Fnc1;
        var r = Gs1.Parse("(01)09501101530003(10)ABC(21)XYZ", "value");
        Assert.Equal($"{f}0109501101530003" + $"10ABC{f}" + "21XYZ", r.Encoded);
        Assert.Equal("(01)09501101530003(10)ABC(21)XYZ", r.HumanReadable);

        var fixedOnly = Gs1.Parse("(01)09501101530003(17)250101(10)ABC123", "value");
        Assert.Equal($"{f}010950110153000317250101" + "10ABC123", fixedOnly.Encoded);
    }

    [Theory]
    [InlineData("0109501101530003")]               // no parentheses
    [InlineData("(01)09501101530004")]             // wrong check digit
    [InlineData("(01)123")]                        // wrong length
    [InlineData("(1)123")]                         // AI too short
    [InlineData("(10)")]                           // no data
    [InlineData("(10)ABC~")]                       // outside the GS1 character set
    [InlineData("(01")]                            // unclosed
    [InlineData("(9999)X")]                        // no such AI
    [InlineData("(011)1234567890128")]             // no such AI
    [InlineData("(17)991399")]                     // month 99
    [InlineData("(17)250230")]                     // 30 February
    [InlineData("(10)ABCDEFGHIJKLMNOPQRSTU")]      // 21 characters; AI 10 allows 20
    [InlineData("(3103)12345")]                    // weight needs 6 digits
    [InlineData("(3106)123456")]                   // no 6-decimal weight AI
    [InlineData("(7003)2501012460")]               // hour 24
    [InlineData("(402)95011015300000017")]         // GSIN check digit should be 0
    [InlineData("(8003)1234567890123")]            // GRAI too short
    [InlineData("(253)1234567890ABC")]             // GDTI document type must be digits
    public void Invalid_gs1_element_strings_throw(string value)
    {
        var ex = Assert.Throws<ArgumentException>(() => Barcode.Gs1Code128(value).Encode());
        Assert.Equal("value", ex.ParamName);
    }

    [Theory]
    [InlineData("(17)250100")]                     // day 00 means the end of the month
    [InlineData("(17)240229")]
    [InlineData("(10)ABCDEFGHIJKLMNOPQRST")]       // exactly 20 characters
    [InlineData("(3103)001250(15)261231")]
    [InlineData("(3925)12345(8008)2501011230")]
    [InlineData("(253)9501101530003")]             // GDTI without serial
    [InlineData("(253)9501101530003ABC-1")]
    [InlineData("(7003)2501012359")]
    [InlineData("(402)95011015300000010")]
    [InlineData("(90)internal(99)anything")]
    public void Valid_gs1_element_strings_encode(string value) => Assert.True(Barcode.Gs1Code128(value).Encode().Width > 0);

    [Fact]
    public void Gs1_pairs_allow_parentheses_in_data()
    {
        var f = Code128Encoder.Fnc1;
        var builder = Barcode.Gs1Code128(("01", "09501101530003"), ("10", "LOT(A)"), ("21", "S/1"));
        Assert.Equal("(01)09501101530003(10)LOT(A)(21)S/1", builder.Value);
        var matrix = builder.Encode();
        Assert.Equal(Code128Encoder.Encode($"{f}0109501101530003" + $"10LOT(A){f}" + "21S/1", allowFnc1: true).ToString(), matrix.ToString());
    }

    [Fact]
    public void Gs1_pairs_are_validated_like_element_strings()
    {
        var ex = Assert.Throws<ArgumentException>(() => Barcode.Gs1Code128(("17", "991399")).Encode());
        Assert.Equal("elements", ex.ParamName);
        Assert.Contains("YYMMDD", ex.Message, StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() => Barcode.Gs1Code128(Array.Empty<(string, string)>()));
        Assert.Throws<ArgumentNullException>(() => Barcode.Gs1Code128((ValueTuple<string, string>[])null!));
    }

    [Fact]
    public void Gs1_128_starts_with_fnc1_after_the_start_character()
    {
        var values = Code128Encoder.EncodeValues(Gs1.Parse("(00)095011015300003011", "value").Encoded, allowFnc1: true);
        Assert.Equal(102, values[1]);
    }

    // ---- Human-readable text ----

    [Theory]
    [InlineData(BarcodeType.Ean13, "590123412345")]
    [InlineData(BarcodeType.UpcA, "03600029145")]
    [InlineData(BarcodeType.Ean8, "9638507")]
    [InlineData(BarcodeType.Code128, "ABC-123")]
    [InlineData(BarcodeType.Code39, "ABC-123")]
    [InlineData(BarcodeType.Gs1Code128, "(01)09501101530003")]
    public void Human_readable_text_adds_a_text_band(BarcodeType type, string value)
    {
        var plain = Barcode.Create(type, value).GetImageSize();
        var withText = Barcode.Create(type, value).HumanReadableText();
        var size = withText.GetImageSize();
        Assert.Equal(plain.Width, size.Width);
        Assert.True(size.Height > plain.Height);

        var png = withText.ToPng();
        Assert.Equal(PngSignature, png.Take(8).ToArray());
        Assert.Contains("<path", withText.ToSvg());
    }

    [Fact]
    public void Human_readable_text_is_for_linear_codes_only()
    {
        Assert.Throws<InvalidOperationException>(() => Barcode.QrCode("x").HumanReadableText());
        Assert.Throws<InvalidOperationException>(() => Barcode.DataMatrix("x").TextSize(10));
        Assert.Throws<ArgumentOutOfRangeException>(() => Barcode.Code128("x").TextSize(3));
    }

    [Fact]
    public void Larger_text_size_makes_a_taller_image()
    {
        var small = Barcode.Code128("ABC").HumanReadableText().TextSize(10).GetImageSize();
        var large = Barcode.Code128("ABC").HumanReadableText().TextSize(30).GetImageSize();
        Assert.True(large.Height > small.Height);
    }

    // ---- Transparency ----

    [Fact]
    public void Transparent_plain_png_has_a_trns_chunk()
    {
        var png = Barcode.Code128("ABC").TransparentBackground().ToPng();
        Assert.True(ContainsAscii(png, "tRNS"));
        Assert.False(ContainsAscii(Barcode.Code128("ABC").ToPng(), "tRNS"));
    }

    [Fact]
    public void Transparent_styled_png_is_rgba()
    {
        var png = Barcode.Ean13("590123412345").HumanReadableText().TransparentBackground().ToPng();
        Assert.Equal(6, png[25]); // IHDR colour type: truecolour with alpha
        Assert.Equal(2, Barcode.Ean13("590123412345").HumanReadableText().ToPng()[25]);
    }

    [Fact]
    public void Transparent_svg_has_no_background_rect_and_jpeg_throws()
    {
        var opaque = Barcode.QrCode("x").ToSvg();
        var clear = Barcode.QrCode("x").TransparentBackground().ToSvg();
        Assert.Contains("#FFFFFF", opaque, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("#FFFFFF", clear, StringComparison.OrdinalIgnoreCase);
        Assert.Throws<InvalidOperationException>(() => Barcode.QrCode("x").TransparentBackground().ToJpeg());
    }

    // ---- Data Matrix ----

    [Theory]
    [InlineData("A", DataMatrixShape.Square, 10, 10)]
    [InlineData("123456", DataMatrixShape.Square, 10, 10)]
    [InlineData("1234567", DataMatrixShape.Square, 12, 12)]
    [InlineData("12345678", DataMatrixShape.Rectangle, 18, 8)]
    [InlineData("Hello, World", DataMatrixShape.Square, 16, 16)] // 12 codewords
    [InlineData("Hello, World!", DataMatrixShape.Square, 18, 18)] // 13 codewords
    public void Data_matrix_picks_the_smallest_symbol(string value, DataMatrixShape shape, int width, int height)
    {
        var m = Barcode.DataMatrix(value).DataMatrixShape(shape).Encode();
        Assert.Equal(width, m.Width);
        Assert.Equal(height, m.Height);
        Assert.False(m.IsLinear);
    }

    [Fact]
    public void Data_matrix_has_its_finder_and_timing_patterns()
    {
        var m = Barcode.DataMatrix("TerraFluent").Encode();
        for (var i = 0; i < m.Height; i++) Assert.True(m[0, i]);
        for (var i = 0; i < m.Width; i++) Assert.True(m[i, m.Height - 1]);
        for (var i = 0; i < m.Width; i++) Assert.Equal(i % 2 == 0, m[i, 0]);
        for (var i = 0; i < m.Height; i++) Assert.Equal(i % 2 == 1, m[m.Width - 1, i]);
    }

    [Fact]
    public void Data_matrix_ascii_packs_digit_pairs_and_rejects_oversized_input()
    {
        Assert.Equal(new[] { 130 + 12, 130 + 34, 'A' + 1 }, DataMatrixEncoder.EncodeAscii("1234A"));
        Assert.Throws<ArgumentException>(() => Barcode.DataMatrix(new string('x', 2000)).Encode());
        Assert.Throws<InvalidOperationException>(() => Barcode.QrCode("x").DataMatrixShape(DataMatrixShape.Any));
    }

    // ---- PDF417 ----

    [Fact]
    public void Pdf417_generator_matches_the_reference_tables()
    {
        Assert.Equal(new[] { 27, 917 }, Pdf417Encoder.GeneratorFactors(0));
        Assert.Equal(new[] { 522, 568, 723, 809 }, Pdf417Encoder.GeneratorFactors(1));
        Assert.Equal(new[] { 237, 308, 436, 284, 646, 653, 428, 379 }, Pdf417Encoder.GeneratorFactors(2));
        // Sums, first and last factors of the ISO/IEC 15438 Annex F tables for levels 3-8.
        var expected = new (int Sum, int First, int Last)[]
        {
            (5738, 274, 65), (14990, 361, 410), (29820, 539, 543), (59929, 521, 539), (118237, 524, 10), (235491, 352, 263),
        };
        for (var level = 3; level <= 8; level++)
        {
            var g = Pdf417Encoder.GeneratorFactors(level);
            var e = expected[level - 3];
            Assert.Equal(1 << (level + 1), g.Length);
            Assert.Equal(e.Sum, g.Sum());
            Assert.Equal(e.First, g[0]);
            Assert.Equal(e.Last, g[g.Length - 1]);
        }
    }

    [Fact]
    public void Pdf417_layout_follows_columns_and_row_height()
    {
        var m = Barcode.Pdf417("TerraFluent PDF417").Pdf417Columns(3).Encode();
        Assert.Equal(17 * (3 + 4) + 1, m.Width);
        Assert.Equal(0, m.Height % 3);
        Assert.Throws<ArgumentOutOfRangeException>(() => Barcode.Pdf417("x").Pdf417Columns(31));
        Assert.Throws<ArgumentOutOfRangeException>(() => Barcode.Pdf417("x").Pdf417ErrorCorrection(9));
    }

    [Fact]
    public void Pdf417_uses_numeric_compaction_for_long_digit_runs()
    {
        var digits = Pdf417Encoder.HighLevelEncode(new string('7', 44));
        var text = Pdf417Encoder.HighLevelEncode(new string('A', 44));
        Assert.Contains(902, digits);
        Assert.True(digits.Count < text.Count);
    }

    [Fact]
    public void Higher_pdf417_error_correction_makes_a_larger_symbol()
    {
        var low = Barcode.Pdf417("ABC").Pdf417Columns(4).Pdf417ErrorCorrection(0).Encode();
        var high = Barcode.Pdf417("ABC").Pdf417Columns(4).Pdf417ErrorCorrection(5).Encode();
        Assert.True(high.Height > low.Height);
    }

    // ---- Mixed-mode QR ----

    [Fact]
    public void Mixed_mode_qr_is_smaller_than_byte_mode()
    {
        // 13 bytes + 40 digits: 264 bits mixed (version 3-M) versus 436 bits in byte mode (version 4-M).
        var m = QrCodeEncoder.Encode("https://x.io/" + new string('1', 40), QrErrorCorrectionLevel.M);
        Assert.Equal(29, m.Width);
    }

    [Fact]
    public void Segmenter_splits_text_into_cheapest_modes()
    {
        var segments = QrCodeEncoder.Segmenter.Split("abc" + new string('9', 30), 1);
        Assert.Equal(2, segments.Count);
    }

    // ---- SaveAsync ----

    [Fact]
    public async Task SaveAsync_writes_the_format_from_the_extension()
    {
        var dir = Path.Combine(Path.GetTempPath(), "tfb-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var png = Path.Combine(dir, "a.png");
            var svg = Path.Combine(dir, "a.svg");
            await Barcode.QrCode("save").SaveAsync(png);
            await Barcode.QrCode("save").SaveAsync(svg);
            Assert.Equal(PngSignature, File.ReadAllBytes(png).Take(8).ToArray());
            Assert.StartsWith("<svg", File.ReadAllText(svg).TrimStart().Replace("<?xml", "<svg").Substring(0, 4));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task SaveAsync_honours_cancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var path = Path.Combine(Path.GetTempPath(), "tfb-" + Guid.NewGuid().ToString("N") + ".png");
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Barcode.QrCode("x").SaveAsync(path, cts.Token));
        if (File.Exists(path)) File.Delete(path);
    }

    // ---- IBarcodeGenerator ----

    [Fact]
    public void Generator_matches_the_fluent_api()
    {
        IBarcodeGenerator gen = new BarcodeGenerator();
        Assert.Equal(Barcode.Code128("ABC").ToPng(), gen.Generate(BarcodeType.Code128, "ABC"));
        Assert.Equal(Barcode.QrCode("ABC").ToSvg(), gen.GenerateSvg(BarcodeType.QrCode, "ABC"));
        Assert.Equal(Barcode.Ean8("9638507").ToDataUri(BarcodeImageFormat.Jpeg), gen.GenerateDataUri(BarcodeType.Ean8, "9638507", BarcodeImageFormat.Jpeg));
    }

    [Fact]
    public void Generator_applies_options_and_ignores_ones_that_do_not_apply()
    {
        var options = new BarcodeOptions
        {
            ModuleSize = 3,
            Height = 40,
            HumanReadableText = true,
            QrErrorCorrection = QrErrorCorrectionLevel.H,
            Code39Checksum = true,
            Foreground = BarcodeColor.FromHex("#123456"),
        };
        var gen = new BarcodeGenerator(options);

        var expectedLinear = Barcode.Code128("ABC").ModuleSize(3).Height(40).HumanReadableText().Foreground("#123456").ToPng();
        Assert.Equal(expectedLinear, gen.Generate(BarcodeType.Code128, "ABC"));

        var expectedQr = Barcode.QrCode("ABC").ModuleSize(3).ErrorCorrection(QrErrorCorrectionLevel.H).Foreground("#123456").ToPng();
        Assert.Equal(expectedQr, gen.Generate(BarcodeType.QrCode, "ABC"));

        // Per-call options replace the defaults.
        Assert.Equal(Barcode.Code128("ABC").ToPng(), gen.Generate(BarcodeType.Code128, "ABC", options: new BarcodeOptions()));
    }

    [Fact]
    public async Task Generator_writes_to_a_stream()
    {
        var gen = new BarcodeGenerator();
        using var ms = new MemoryStream();
        await gen.WriteToAsync(BarcodeType.DataMatrix, "stream", ms);
        Assert.Equal(PngSignature, ms.ToArray().Take(8).ToArray());
    }

    [Fact]
    public void Generator_create_returns_a_builder_for_further_styling()
    {
        var gen = new BarcodeGenerator(new BarcodeOptions { ModuleSize = 5 });
        var builder = gen.Create(BarcodeType.QrCode, "x").Gradient("#000000", "#333366");
        Assert.Equal(Barcode.QrCode("x").ModuleSize(5).GetImageSize(), builder.GetImageSize());
    }

    private static bool ContainsAscii(byte[] data, string text)
    {
        var bytes = System.Text.Encoding.ASCII.GetBytes(text);
        for (var i = 0; i + bytes.Length <= data.Length; i++)
        {
            var match = true;
            for (var j = 0; j < bytes.Length && match; j++) match = data[i + j] == bytes[j];
            if (match) return true;
        }

        return false;
    }
}
