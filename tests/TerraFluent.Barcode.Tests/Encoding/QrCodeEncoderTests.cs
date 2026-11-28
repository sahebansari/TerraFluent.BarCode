using TerraFluent.Barcode.Encoding;

namespace TerraFluent.Barcode.Tests.Encoding;

public class QrCodeEncoderTests
{
    [Theory]
    [InlineData(1, QrErrorCorrectionLevel.L, 19)]
    [InlineData(1, QrErrorCorrectionLevel.H, 9)]
    [InlineData(10, QrErrorCorrectionLevel.M, 216)]
    [InlineData(40, QrErrorCorrectionLevel.L, 2956)]
    [InlineData(40, QrErrorCorrectionLevel.H, 1276)]
    public void Data_capacity_matches_iso_18004_table_7(int version, QrErrorCorrectionLevel level, int codewords)
    {
        Assert.Equal(codewords, QrCodeEncoder.DataCodewords(version, (int)level));
    }

    [Theory]
    [InlineData(41, "0", 21)]  // numeric capacity of 1-L
    [InlineData(42, "0", 25)]
    [InlineData(25, "A", 21)]  // alphanumeric capacity of 1-L
    [InlineData(26, "A", 25)]
    [InlineData(17, "a", 21)]  // byte capacity of 1-L
    [InlineData(18, "a", 25)]
    public void Smallest_version_that_fits_is_chosen(int length, string character, int expectedSize)
    {
        var matrix = QrCodeEncoder.Encode(new string(character[0], length), QrErrorCorrectionLevel.L);
        Assert.Equal(expectedSize, matrix.Width);
        Assert.Equal(expectedSize, matrix.Height);
    }

    [Fact]
    public void Version_40_byte_capacity_is_the_limit()
    {
        Assert.Equal(177, QrCodeEncoder.Encode(new string('a', 2953), QrErrorCorrectionLevel.L).Width);
        Assert.Throws<ArgumentException>(() => QrCodeEncoder.Encode(new string('a', 2954), QrErrorCorrectionLevel.L));
    }

    [Theory]
    [InlineData("0123456789", QrCodeEncoder.Mode.Numeric)]
    [InlineData("HELLO WORLD", QrCodeEncoder.Mode.Alphanumeric)]
    [InlineData("Hello world", QrCodeEncoder.Mode.Byte)]
    public void Most_compact_mode_is_selected(string value, object expected)
    {
        Assert.Equal((QrCodeEncoder.Mode)expected, QrCodeEncoder.SelectMode(value).Mode);
    }

    [Fact]
    public void Finder_patterns_are_in_three_corners()
    {
        var m = QrCodeEncoder.Encode("HELLO WORLD", QrErrorCorrectionLevel.Q);
        var s = m.Width;
        foreach (var (x0, y0) in new[] { (0, 0), (s - 7, 0), (0, s - 7) })
        {
            for (var i = 0; i < 7; i++)
            {
                Assert.True(m[x0 + i, y0]);       // outer ring
                Assert.True(m[x0 + i, y0 + 6]);
                Assert.True(m[x0, y0 + i]);
                Assert.True(m[x0 + 6, y0 + i]);
            }

            Assert.False(m[x0 + 1, y0 + 1]);      // light ring
            Assert.True(m[x0 + 3, y0 + 3]);       // centre
        }

        Assert.True(m[8, s - 8]); // dark module
    }

    [Fact]
    public void Utf8_text_is_supported()
    {
        var (mode, _, count, _) = QrCodeEncoder.SelectMode("こんにちは");
        Assert.Equal(QrCodeEncoder.Mode.Byte, mode);
        Assert.Equal(15, count);
    }

    [Fact]
    public void Alignment_positions_match_table_e1()
    {
        Assert.Empty(QrCodeEncoder.AlignmentPositions(1));
        Assert.Equal(new[] { 6, 18 }, QrCodeEncoder.AlignmentPositions(2));
        Assert.Equal(new[] { 6, 34, 60, 86, 112, 138 }, QrCodeEncoder.AlignmentPositions(32));
        Assert.Equal(new[] { 6, 30, 58, 86, 114, 142, 170 }, QrCodeEncoder.AlignmentPositions(40));
    }
}
