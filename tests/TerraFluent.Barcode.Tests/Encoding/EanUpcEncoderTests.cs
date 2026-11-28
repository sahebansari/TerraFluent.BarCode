using TerraFluent.Barcode.Encoding;

namespace TerraFluent.Barcode.Tests.Encoding;

public class EanUpcEncoderTests
{
    [Theory]
    [InlineData("590123412345", '7')]
    [InlineData("400638133393", '1')]
    [InlineData("978030640615", '7')]
    [InlineData("03600029145", '2')]
    [InlineData("9638507", '4')]
    public void Check_digit_matches_gs1(string data, char expected)
    {
        Assert.Equal(expected, EanUpcEncoder.CheckDigit(data));
    }

    [Fact]
    public void Ean13_is_95_modules()
    {
        var matrix = EanUpcEncoder.EncodeEan13("5901234123457");
        Assert.Equal(95, matrix.Width);
        Assert.Equal(11, matrix.RecommendedQuietZone);
    }

    [Fact]
    public void Ean8_is_67_modules()
    {
        Assert.Equal(67, EanUpcEncoder.EncodeEan8("9638507").Width);
    }

    [Fact]
    public void UpcA_matches_ean13_with_leading_zero()
    {
        var upc = EanUpcEncoder.EncodeUpcA("036000291452");
        var ean = EanUpcEncoder.EncodeEan13("0036000291452");
        Assert.Equal(ean.ToString(), upc.ToString());
    }

    [Fact]
    public void Missing_check_digit_is_added()
    {
        Assert.Equal(
            EanUpcEncoder.EncodeEan13("5901234123457").ToString(),
            EanUpcEncoder.EncodeEan13("590123412345").ToString());
    }

    [Fact]
    public void Wrong_check_digit_is_rejected()
    {
        var ex = Assert.Throws<ArgumentException>(() => EanUpcEncoder.EncodeEan13("5901234123458"));
        Assert.Contains("should be '7'", ex.Message);
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("59012341234A")]
    public void Wrong_length_or_non_digits_are_rejected(string value)
    {
        Assert.Throws<ArgumentException>(() => EanUpcEncoder.EncodeEan13(value));
    }
}
