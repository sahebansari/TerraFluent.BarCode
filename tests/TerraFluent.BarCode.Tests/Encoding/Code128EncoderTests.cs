using TerraFluent.BarCode.Encoding;

namespace TerraFluent.BarCode.Tests.Encoding;

public class Code128EncoderTests
{
    [Fact]
    public void Digits_only_start_in_code_set_C()
    {
        var values = Code128Encoder.EncodeValues("1234567890");
        Assert.Equal(105, values[0]); // Start C
        Assert.Equal(new[] { 12, 34, 56, 78, 90 }, values.Skip(1).Take(5));
        Assert.Equal(106, values[^1]);
    }

    [Fact]
    public void Text_starts_in_code_set_B_and_computes_checksum()
    {
        // Start B (104) + "AB" (33, 34): (104 + 33*1 + 34*2) % 103 = 102
        var values = Code128Encoder.EncodeValues("AB");
        Assert.Equal(new[] { 104, 33, 34, 102, 106 }, values);
    }

    [Fact]
    public void Long_trailing_digit_run_switches_to_code_set_C()
    {
        var values = Code128Encoder.EncodeValues("AB1234");
        Assert.Equal(new[] { 104, 33, 34, 99, 12, 34 }, values.Take(6));
    }

    [Fact]
    public void Odd_digit_run_keeps_first_digit_in_current_set()
    {
        var values = Code128Encoder.EncodeValues("A12345");
        Assert.Equal(new[] { 104, 33, 17, 99, 23, 45 }, values.Take(6));
    }

    [Fact]
    public void Short_digit_run_in_the_middle_stays_in_code_set_B()
    {
        var values = Code128Encoder.EncodeValues("x12345y");
        Assert.DoesNotContain(99, values.Skip(1).Take(values.Count - 3));
    }

    [Fact]
    public void Control_characters_use_code_set_A()
    {
        var values = Code128Encoder.EncodeValues("A\tB");
        Assert.Equal(new[] { 103, 33, 73, 34 }, values.Take(4)); // Start A, 'A', TAB (9 + 64), 'B'
    }

    [Fact]
    public void Matrix_width_is_eleven_modules_per_symbol_plus_stop()
    {
        // Start + 5 data + checksum = 7 symbols of 11 modules, stop is 13.
        var matrix = Code128Encoder.Encode("HELLO");
        Assert.Equal(7 * 11 + 13, matrix.Width);
        Assert.True(matrix.IsLinear);
        Assert.Equal(10, matrix.RecommendedQuietZone);
    }

    [Fact]
    public void Non_ascii_is_rejected()
    {
        var ex = Assert.Throws<ArgumentException>(() => Code128Encoder.Encode("café"));
        Assert.Contains("index 3", ex.Message);
    }

    [Fact]
    public void Every_pattern_is_eleven_modules_wide()
    {
        for (var i = 0; i < 106; i++) Assert.Equal(11, Code128Encoder.Patterns[i].Sum(c => c - '0'));
        Assert.Equal(13, Code128Encoder.Patterns[106].Sum(c => c - '0'));
    }
}
