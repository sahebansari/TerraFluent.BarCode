using TerraFluent.BarCode.Encoding;

namespace TerraFluent.BarCode.Tests.Encoding;

public class Code39EncoderTests
{
    [Fact]
    public void Checksum_character_is_modulo_43()
    {
        Assert.Equal('W', Code39Encoder.ChecksumCharacter("CODE39"));
    }

    [Fact]
    public void Each_character_is_sixteen_modules_including_gap()
    {
        // 3 wide (x3) + 6 narrow = 15 modules per character, plus a 1-module gap between characters.
        var matrix = Code39Encoder.Encode("AB", includeChecksum: false);
        Assert.Equal(4 * 15 + 3, matrix.Width);
    }

    [Fact]
    public void Checksum_adds_one_character()
    {
        var without = Code39Encoder.Encode("ABC", false).Width;
        var with = Code39Encoder.Encode("ABC", true).Width;
        Assert.Equal(16, with - without);
    }

    [Fact]
    public void Lower_case_is_rejected_with_a_hint()
    {
        var ex = Assert.Throws<ArgumentException>(() => Code39Encoder.Encode("abc", false));
        Assert.Contains("FullAscii()", ex.Message);
    }

    [Fact]
    public void Asterisk_is_reserved_for_start_and_stop()
    {
        Assert.Throws<ArgumentException>(() => Code39Encoder.Encode("A*B", false));
    }
}
