using TerraFluent.Barcode.Rendering;

namespace TerraFluent.Barcode.Tests.Rendering;

public class JpegEncoderTests
{
    [Fact]
    public void Ac_huffman_table_covers_every_baseline_symbol()
    {
        var expected = new HashSet<byte> { 0x00, 0xF0 };
        for (var run = 0; run < 16; run++)
        {
            for (var size = 1; size <= 10; size++) expected.Add((byte)(run << 4 | size));
        }

        Assert.Equal(162, JpegEncoder.AcValues.Length);
        Assert.Equal(162, JpegEncoder.AcBits.Sum(b => b));
        Assert.True(expected.SetEquals(JpegEncoder.AcValues));
    }

    [Fact]
    public void Dc_huffman_table_covers_categories_0_to_11()
    {
        Assert.Equal(12, JpegEncoder.DcBits.Sum(b => b));
        var table = JpegEncoder.BuildHuffman(JpegEncoder.DcBits, JpegEncoder.DcValues);
        for (var i = 0; i < 12; i++) Assert.True(table[i].Length > 0);
    }
}
