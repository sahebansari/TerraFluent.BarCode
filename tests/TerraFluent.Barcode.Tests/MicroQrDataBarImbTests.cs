using TerraFluent.Barcode.Encoding;
using ZXing;

namespace TerraFluent.Barcode.Tests;

/// <summary>
/// Micro QR, rMQR, GS1 DataBar (Omnidirectional, Limited, Expanded) and USPS
/// Intelligent Mail. Symbols are read back with ZXing.Net where it supports
/// them and with zxing-cpp otherwise; Intelligent Mail, which neither reads,
/// is checked against the USPS-B-3200 sample encoding.
/// </summary>
public class MicroQrDataBarImbTests
{
    // ---- Micro QR ----

    [Fact]
    public void Micro_qr_m1_matches_a_reference_symbol()
    {
        // "12345" at level L, as an independent encoder (zxing-cpp) draws it.
        const string expected =
            "#######.#.#\n#.....#.##.\n#.###.#.#..\n#.###.#....\n#.###.#.###\n#.....#..##\n" +
            "#######.#..\n.........##\n##..###..##\n.#.#...##..\n####.....##";
        Assert.Equal(expected, Barcodes.MicroQrCode("12345").ErrorCorrection(QrErrorCorrectionLevel.L).Encode().ToString());
    }

    [Theory]
    [InlineData("1", QrErrorCorrectionLevel.L, 11)]
    [InlineData("12345", QrErrorCorrectionLevel.M, 13)]
    [InlineData("HELLO WORLD", QrErrorCorrectionLevel.M, 15)]
    [InlineData("hello", QrErrorCorrectionLevel.L, 15)]
    [InlineData("ABC123", QrErrorCorrectionLevel.Q, 17)]
    [InlineData("12345678901234567890123456789012345", QrErrorCorrectionLevel.L, 17)]
    public void Micro_qr_picks_the_smallest_version(string value, QrErrorCorrectionLevel level, int size)
    {
        var matrix = Barcodes.MicroQrCode(value).ErrorCorrection(level).Encode();
        Assert.Equal(size, matrix.Width);
        Assert.Equal(size, matrix.Height);
        Assert.Equal(MicroQrEncoder.QuietZone, matrix.RecommendedQuietZone);
    }

    [Theory]
    [InlineData("A", QrErrorCorrectionLevel.L)]
    [InlineData("Micro QR", QrErrorCorrectionLevel.L)]
    [InlineData("HELLO WORLD", QrErrorCorrectionLevel.M)]
    [InlineData("hello", QrErrorCorrectionLevel.M)]
    [InlineData("ABC123", QrErrorCorrectionLevel.Q)]
    [InlineData("café", QrErrorCorrectionLevel.L)]
    [InlineData("123456789012345678901234567890", QrErrorCorrectionLevel.M)]
    [InlineData("AB12cd", QrErrorCorrectionLevel.L)]
    public void Micro_qr_scans(string value, QrErrorCorrectionLevel level)
    {
        var png = Barcodes.MicroQrCode(value).ErrorCorrection(level).ToPng();
        Assert.NotEmpty(png);
#if NET
        Assert.Equal(value, CppDecoderHelper.DecodeText(png, ZXingCpp.BarcodeFormat.MicroQRCode));
#endif
    }

    [Fact]
    public void Micro_qr_rejects_what_it_cannot_hold()
    {
        Assert.Throws<ArgumentException>(() => Barcodes.MicroQrCode(new string('1', 36)).ErrorCorrection(QrErrorCorrectionLevel.L).Encode());
        Assert.Throws<ArgumentException>(() => Barcodes.MicroQrCode("中文").Encode());
        Assert.Throws<ArgumentException>(() => Barcodes.MicroQrCode("1").ErrorCorrection(QrErrorCorrectionLevel.H));
    }

    // ---- rMQR ----

    [Fact]
    public void Rmqr_codeword_tables_match_the_symbol_layout()
    {
        // Every version's codewords fill its data area, leaving fewer than 8 remainder modules.
        for (var i = 0; i < 32; i++)
        {
            var modules = RmqrEncoder.DataModules(i);
            Assert.Equal(RmqrEncoder.TotalCodewords(i), modules / 8);
        }
    }

    public static TheoryData<string, QrErrorCorrectionLevel> RmqrValues => new()
    {
        { "1", QrErrorCorrectionLevel.M },
        { "12345678901234567890", QrErrorCorrectionLevel.M },
        { "HELLO WORLD 123", QrErrorCorrectionLevel.H },
        { "https://terrafluent.dev/barcode/", QrErrorCorrectionLevel.M },
        { "café 中文 \U0001F600", QrErrorCorrectionLevel.M },
        { string.Concat(Enumerable.Repeat("0123456789", 36)), QrErrorCorrectionLevel.M },
    };

    [Theory]
    [MemberData(nameof(RmqrValues))]
    public void Rmqr_scans(string value, QrErrorCorrectionLevel level)
    {
        var builder = Barcodes.RectangularMicroQrCode(value).ErrorCorrection(level);
        var matrix = builder.Encode();
        Assert.Contains(matrix.Height, RmqrEncoder.Heights);
        Assert.True(matrix.Width > matrix.Height);
        var png = builder.ToPng();
        Assert.NotEmpty(png);
#if NET
        Assert.Equal(value, CppDecoderHelper.DecodeText(png, ZXingCpp.BarcodeFormat.RMQRCode));
#endif
    }

#if NET
    [Theory]
    [InlineData(QrErrorCorrectionLevel.M)]
    [InlineData(QrErrorCorrectionLevel.H)]
    public void Every_rmqr_size_scans(QrErrorCorrectionLevel level)
    {
        // The longest digit string per size and level, so each of the 64 combinations is drawn and read once.
        var digits = string.Concat(Enumerable.Range(0, 400).Select(i => (char)('0' + (i * 7 + 3) % 10)));
        var seen = new Dictionary<(int, int), string>();
        foreach (var height in RmqrEncoder.Heights)
        {
            for (var n = 1; n <= 361; n++)
            {
                BarcodeMatrix matrix;
                try
                {
                    matrix = Barcodes.RectangularMicroQrCode(digits.Substring(0, n)).ErrorCorrection(level).RectangularMicroQrHeight(height).Encode();
                }
                catch (ArgumentException)
                {
                    break;
                }

                seen[(matrix.Height, matrix.Width)] = digits.Substring(0, n);
            }
        }

        Assert.Equal(32, seen.Count);
        foreach (var ((height, _), value) in seen)
        {
            var png = Barcodes.RectangularMicroQrCode(value).ErrorCorrection(level).RectangularMicroQrHeight(height).ToPng();
            Assert.Equal(value, CppDecoderHelper.DecodeText(png, ZXingCpp.BarcodeFormat.RMQRCode));
        }
    }
#endif

    [Fact]
    public void Rmqr_height_can_be_fixed()
    {
        var matrix = Barcodes.RectangularMicroQrCode("TERRAFLUENT").RectangularMicroQrHeight(7).Encode();
        Assert.Equal(7, matrix.Height);
        var largest = Barcodes.RectangularMicroQrCode(new string('9', 361)).Encode();
        Assert.Equal((17, 139), (largest.Height, largest.Width));
        Assert.Throws<ArgumentOutOfRangeException>(() => Barcodes.RectangularMicroQrCode("1").RectangularMicroQrHeight(8));
        Assert.Throws<ArgumentException>(() => Barcodes.RectangularMicroQrCode(new string('9', 200)).RectangularMicroQrHeight(7).Encode());
    }

    [Fact]
    public void Rmqr_rejects_unsupported_levels_and_long_values()
    {
        Assert.Throws<ArgumentException>(() => Barcodes.RectangularMicroQrCode("1").ErrorCorrection(QrErrorCorrectionLevel.L));
        Assert.Throws<ArgumentException>(() => Barcodes.RectangularMicroQrCode("1").ErrorCorrection(QrErrorCorrectionLevel.Q));
        Assert.Throws<ArgumentException>(() => Barcodes.RectangularMicroQrCode(new string('9', 362)).Encode());
        Assert.Throws<InvalidOperationException>(() => Barcodes.QrCode("1").RectangularMicroQrHeight(7));
    }

    // ---- GS1 DataBar Omnidirectional and Limited ----

    [Theory]
    [InlineData("0950110153000", "09501101530003")]
    [InlineData("09501101530003", "09501101530003")]
    [InlineData("(01)20012345678909", "20012345678909")]
    [InlineData("9999999999999", "99999999999997")]
    [InlineData("0000000000000", "00000000000000")]
    public void DataBar_omnidirectional_scans(string value, string gtin)
    {
        var builder = Barcodes.Gs1DataBar(value);
        Assert.Equal(96, builder.Encode().Width);
        var png = builder.ToPng();
        Assert.Equal(gtin, DecoderHelper.DecodeText(png, BarcodeFormat.RSS_14));
#if NET
        Assert.Equal("(01)" + gtin, CppDecoderHelper.DecodeText(png, ZXingCpp.BarcodeFormat.DataBarOmni));
#endif
    }

    [Theory]
    [InlineData("0950110153000", "09501101530003")]
    [InlineData("1999999999999", "19999999999991")]
    [InlineData("(01)00000000000000", "00000000000000")]
    [InlineData("0123456789012", "01234567890128")]
    public void DataBar_limited_scans(string value, string gtin)
    {
        var builder = Barcodes.Gs1DataBarLimited(value);
        Assert.Equal(79, builder.Encode().Width);
        Assert.Equal("(01)" + gtin, builder.Encode().Layout!.CentredText);
        var png = builder.ToPng();
        Assert.NotEmpty(png);
#if NET
        Assert.Equal("(01)" + gtin, CppDecoderHelper.DecodeText(png, ZXingCpp.BarcodeFormat.DataBarLtd));
#endif
    }

    [Fact]
    public void DataBar_omnidirectional_matches_a_reference_symbol()
    {
        // Bar and space widths of (01)09501101530003 as an independent encoder (zxing-cpp) draws them.
        var expected = new[] { 1, 1, 5, 1, 1, 1, 5, 1, 1, 1, 3, 8, 2, 1, 1, 4, 1, 2, 1, 1, 1, 3, 2, 1, 3, 2, 1, 2, 1, 2, 3, 1, 1, 6, 5, 2, 2, 3, 1, 2, 1, 3, 2, 2, 1, 1 };
        Assert.Equal(expected, Runs(Barcodes.Gs1DataBar("0950110153000").Encode()));
    }

    [Theory]
    [InlineData("09501101530004")]         // wrong check digit
    [InlineData("095011015300")]           // too short
    [InlineData("09501101530A0")]          // not a digit
    public void Invalid_databar_values_throw(string value)
    {
        Assert.Throws<ArgumentException>(() => Barcodes.Gs1DataBar(value).Encode());
        Assert.Throws<ArgumentException>(() => Barcodes.Gs1DataBarLimited(value).Encode());
    }

    [Fact]
    public void DataBar_limited_needs_indicator_digit_0_or_1() =>
        Assert.Throws<ArgumentException>(() => Barcodes.Gs1DataBarLimited("2001234567890").Encode());

    [Fact]
    public void DataBar_prints_the_gtin_with_its_ai()
    {
        var builder = Barcodes.Gs1DataBar("0950110153000").HumanReadableText();
        Assert.Equal("(01)09501101530003", builder.Encode().Layout!.CentredText);
        Assert.NotEmpty(builder.ToPng());
        Assert.Contains("<path", builder.ToSvg(), StringComparison.Ordinal);
    }

    // ---- GS1 DataBar Expanded ----

    [Theory]
    [InlineData("(01)09501101530003")]
    [InlineData("(01)09501101530003(3103)000450(15)261231")]
    [InlineData("(01)09501101530003(10)12345(21)ABCabc-./")]
    [InlineData("(10)ABC123")]
    [InlineData("(21)12345678901234567890")]
    [InlineData("(01)09501101530003(3922)1123(17)260630(10)LOT-7")]
    public void DataBar_expanded_scans(string value)
    {
        var png = Barcodes.Gs1DataBarExpanded(value).ToPng();
        Assert.Equal(value, DecoderHelper.DecodeText(png, BarcodeFormat.RSS_EXPANDED));
#if NET
        Assert.Equal(value, CppDecoderHelper.DecodeText(png, ZXingCpp.BarcodeFormat.DataBarExp));
#endif
    }

#if NET
    // A variable-length field ending in alphanumeric or ISO 646 mode is followed by FNC1, which
    // latches back to numeric. ZXing.Net 0.16 stays in the old mode and drops the rest (it does the
    // same with another encoder's symbols), so these are read with zxing-cpp only.
    [Theory]
    [InlineData("(8020)ab&c%+_?(21)1")]
    [InlineData("(10)ab(21)12")]
    [InlineData("(10)A(21)12")]
    public void DataBar_expanded_latches_back_to_numeric_after_fnc1(string value) =>
        Assert.Equal(value, CppDecoderHelper.DecodeText(Barcodes.Gs1DataBarExpanded(value).ToPng(), ZXingCpp.BarcodeFormat.DataBarExp));
#endif

    [Fact]
    public void DataBar_expanded_takes_ai_pairs()
    {
        var builder = Barcodes.Gs1DataBarExpanded(("01", "09501101530003"), ("10", "A(1)"));
        Assert.Equal("(01)09501101530003(10)A(1)", builder.Value);
        Assert.Equal(BarcodeType.Gs1DataBarExpanded, builder.Type);
        Assert.Equal("(01)09501101530003(10)A(1)", DecoderHelper.DecodeText(builder.ToPng(), BarcodeFormat.RSS_EXPANDED));
    }

    [Fact]
    public void DataBar_expanded_rejects_bad_and_long_data()
    {
        Assert.Throws<ArgumentException>(() => Barcodes.Gs1DataBarExpanded("(01)09501101530004").Encode());
        Assert.Throws<ArgumentException>(() => Barcodes.Gs1DataBarExpanded("(99)" + new string('A', 60)).Encode());
        // 74 digits fit; a few more do not.
        Assert.NotNull(Barcodes.Gs1DataBarExpanded("(01)09501101530003(91)" + new string('7', 56)).Encode());
        Assert.Throws<ArgumentException>(() => Barcodes.Gs1DataBarExpanded("(01)09501101530003(91)" + new string('7', 70)).Encode());
    }

    [Fact]
    public void Shared_generator_options_skip_levels_a_type_lacks()
    {
        var generator = new BarcodeGenerator(new BarcodeOptions { QrErrorCorrection = QrErrorCorrectionLevel.H });
        Assert.NotEmpty(generator.Generate(BarcodeType.MicroQrCode, "12345"));
        Assert.NotEmpty(generator.Generate(BarcodeType.RectangularMicroQrCode, "12345"));
        Assert.NotEmpty(generator.Generate(BarcodeType.IntelligentMail, "00040123456200800001"));
        Assert.NotEmpty(generator.Generate(BarcodeType.Gs1DataBarExpanded, "(10)ABC"));
    }

    // ---- USPS Intelligent Mail ----

    [Fact]
    public void Intelligent_mail_matches_the_usps_sample()
    {
        // USPS-B-3200 sample: tracking code 01 234 567094 987654321, routing code 01234-5678-91.
        Assert.Equal(
            "AADTFFDFTDADTAADAATFDTDDAAADDTDTTDAFADADDDTFFFDDTTTADFAAADFTDAADA",
            IntelligentMailEncoder.BarStates("0123456709498765432101234567891"));
    }

    [Fact]
    public void Intelligent_mail_bar_table_uses_every_character_bit_once() =>
        Assert.True(IntelligentMailEncoder.BarMapIsPermutation());

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("123456789")]
    [InlineData("12345678901")]
    public void Intelligent_mail_draws_65_four_state_bars(string routing)
    {
        var builder = Barcodes.IntelligentMail("00040123456200800001", routing);
        Assert.Equal("00040123456200800001" + routing, builder.Value);
        var matrix = builder.Encode();
        Assert.Equal(129, matrix.Width);
        Assert.Equal(IntelligentMailEncoder.Height, matrix.Height);
        Assert.False(matrix.IsLinear);

        var states = IntelligentMailEncoder.BarStates(builder.Value);
        for (var bar = 0; bar < 65; bar++)
        {
            Assert.Equal(states[bar] is 'F' or 'A', matrix[2 * bar, 0]);
            Assert.Equal(states[bar] is 'F' or 'D', matrix[2 * bar, IntelligentMailEncoder.Height - 1]);
            Assert.True(matrix[2 * bar, 2] && matrix[2 * bar, 3], "every bar has the tracker part");
            if (bar < 64) Assert.False(matrix[2 * bar + 1, 2]);
        }

        Assert.NotEmpty(builder.ToPng());
        Assert.NotEmpty(builder.ToSvg());
    }

    [Theory]
    [InlineData("0004012345620080000")]       // 19 digits
    [InlineData("09040123456200800001")]      // second digit above 4
    [InlineData("000401234562008000011234")]  // 4-digit routing
    [InlineData("0004012345620080000A")]      // not a digit
    public void Invalid_intelligent_mail_values_throw(string value) =>
        Assert.Throws<ArgumentException>(() => Barcodes.Create(BarcodeType.IntelligentMail, value).Encode());

    [Fact]
    public void Intelligent_mail_overload_checks_its_parts()
    {
        Assert.Throws<ArgumentException>(() => Barcodes.IntelligentMail("123", "12345"));
        Assert.Throws<ArgumentException>(() => Barcodes.IntelligentMail("00040123456200800001", "1234"));
        Assert.Throws<InvalidOperationException>(() => Barcodes.IntelligentMail("00040123456200800001").Height(40));
    }

    private static int[] Runs(BarcodeMatrix matrix)
    {
        var runs = new List<int>();
        var current = false;
        var length = 0;
        for (var x = 0; x < matrix.Width; x++)
        {
            if (matrix[x, 0] == current)
            {
                length++;
                continue;
            }

            runs.Add(length);
            current = !current;
            length = 1;
        }

        runs.Add(length);
        return runs.ToArray();
    }
}
