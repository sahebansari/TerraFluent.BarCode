#if NET
using TerraFluent.Barcode.Rendering;
using ZXingCpp;

namespace TerraFluent.Barcode.Tests;

/// <summary>
/// Reads generated PNGs back with zxing-cpp, an independent decoder that knows
/// Micro QR, rMQR and DataBar Limited. Not available on .NET Framework.
/// </summary>
internal static class CppDecoderHelper
{
    public static string DecodeText(byte[] png, BarcodeFormat format)
    {
        var image = PngDecoder.Decode(png, "png");
        var luminance = new byte[image.Width * image.Height];
        var px = image.Pixels;
        for (var i = 0; i < luminance.Length; i++)
        {
            int r = px[i * 4], g = px[i * 4 + 1], b = px[i * 4 + 2], a = px[i * 4 + 3];
            var grey = (r * 299 + g * 587 + b * 114) / 1000;
            luminance[i] = (byte)((grey * a + 255 * (255 - a)) / 255);
        }

        var reader = new BarcodeReader { Formats = format, TryInvert = false };
        var results = reader.Read(luminance, image.Width, image.Height, ImageFormat.Lum, image.Width, 1);
        try
        {
            Assert.True(results.Length > 0, $"zxing-cpp could not read the {format} symbol.");
            Assert.True(results[0].IsValid, $"zxing-cpp found a {format} symbol but could not decode it: {results[0].ErrorMsg}");
            return results[0].Text;
        }
        finally
        {
            foreach (var result in results) result.Dispose();
        }
    }
}
#endif
