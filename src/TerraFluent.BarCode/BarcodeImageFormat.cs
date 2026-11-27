namespace TerraFluent.BarCode;

/// <summary>Output image formats.</summary>
public enum BarcodeImageFormat
{
    /// <summary>Lossless PNG (8-bit RGB). The recommended raster format for barcodes.</summary>
    Png,

    /// <summary>Baseline JPEG. Lossy: prefer PNG or SVG unless JPEG is required.</summary>
    Jpeg,

    /// <summary>Scalable vector graphics (UTF-8 XML).</summary>
    Svg,
}

/// <summary>Helpers for <see cref="BarcodeImageFormat"/>.</summary>
public static class BarcodeImageFormatExtensions
{
    /// <summary>Gets the MIME type of the format, e.g. <c>image/png</c>.</summary>
    public static string ToMimeType(this BarcodeImageFormat format) => format switch
    {
        BarcodeImageFormat.Png => "image/png",
        BarcodeImageFormat.Jpeg => "image/jpeg",
        BarcodeImageFormat.Svg => "image/svg+xml",
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown image format."),
    };

    /// <summary>Gets the usual file extension of the format, including the dot, e.g. <c>.png</c>.</summary>
    public static string ToFileExtension(this BarcodeImageFormat format) => format switch
    {
        BarcodeImageFormat.Png => ".png",
        BarcodeImageFormat.Jpeg => ".jpg",
        BarcodeImageFormat.Svg => ".svg",
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown image format."),
    };

    /// <summary>
    /// Infers the format from a file path's extension (<c>.png</c>, <c>.jpg</c>/<c>.jpeg</c>, <c>.svg</c>).
    /// </summary>
    /// <exception cref="ArgumentException">The extension is missing or not recognised.</exception>
    public static BarcodeImageFormat FromFilePath(string path)
    {
        if (path is null) throw new ArgumentNullException(nameof(path));
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".png" => BarcodeImageFormat.Png,
            ".jpg" or ".jpeg" or ".jpe" => BarcodeImageFormat.Jpeg,
            ".svg" => BarcodeImageFormat.Svg,
            _ => throw new ArgumentException(
                $"Cannot infer an image format from extension '{extension}'. Use .png, .jpg, .jpeg or .svg, or pass a BarcodeImageFormat.",
                nameof(path)),
        };
    }
}
