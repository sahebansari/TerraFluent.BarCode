using TerraFluent.Barcode.Rendering;
using ZXing;
using ZXing.Common;

namespace TerraFluent.Barcode.Tests;

/// <summary>Reads generated PNGs back with ZXing.Net, an independent decoder.</summary>
internal static class DecoderHelper
{
    public static Result? Decode(byte[] png, BarcodeFormat format, Action<DecodingOptions>? configure = null)
    {
        var image = PngDecoder.Decode(png, "png");
        var luminance = new byte[image.Width * image.Height];
        var px = image.Pixels;
        for (var i = 0; i < luminance.Length; i++)
        {
            int r = px[i * 4], g = px[i * 4 + 1], b = px[i * 4 + 2], a = px[i * 4 + 3];
            var grey = (r * 299 + g * 587 + b * 114) / 1000;
            luminance[i] = (byte)((grey * a + 255 * (255 - a)) / 255); // composite over white
        }

        var options = new DecodingOptions
        {
            PossibleFormats = new[] { format },
            TryHarder = true,
            PureBarcode = format is BarcodeFormat.MAXICODE or BarcodeFormat.AZTEC,
        };
        configure?.Invoke(options);
        var reader = new BarcodeReaderGeneric { AutoRotate = false, Options = options };
        return reader.Decode(new RGBLuminanceSource(luminance, image.Width, image.Height, RGBLuminanceSource.BitmapFormat.Gray8));
    }

    public static string DecodeText(byte[] png, BarcodeFormat format, Action<DecodingOptions>? configure = null)
    {
        var result = Decode(png, format, configure);
        Assert.True(result is not null, $"ZXing could not read the {format} symbol.");
        return result!.Text;
    }
}
