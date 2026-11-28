using Microsoft.AspNetCore.Http;

namespace TerraFluent.Barcode.AspNetCore;

/// <summary>
/// Creates <see cref="BarcodeHttpResult"/>s. In a minimal API:
/// <code>
/// app.MapGet("/labels/{sku}.png", (string sku) => Results.Extensions.Barcode(BarcodeType.Code128, sku));
/// app.MapGet("/tickets/{id}/qr", (string id) => Barcodes.QrCode(TicketUrl(id)).ModuleSize(6).ToHttpResult(BarcodeImageFormat.Svg));
/// </code>
/// MVC controller actions can return the same results.
/// </summary>
public static class BarcodeResultExtensions
{
    /// <summary>
    /// Renders <paramref name="value"/> with the <see cref="IBarcodeGenerator"/> from dependency
    /// injection (or a default one), using <paramref name="options"/> or the generator's defaults.
    /// </summary>
    /// <param name="resultExtensions">The <see cref="Results.Extensions"/> marker.</param>
    /// <param name="type">The symbology.</param>
    /// <param name="value">The data to encode.</param>
    /// <param name="format">The image format. Default PNG.</param>
    /// <param name="options">Options for this call; they replace the generator's defaults.</param>
    /// <param name="fileDownloadName">Optional file name that makes the browser download the image.</param>
    public static BarcodeHttpResult Barcode(
        this IResultExtensions resultExtensions,
        BarcodeType type,
        string value,
        BarcodeImageFormat format = BarcodeImageFormat.Png,
        BarcodeOptions? options = null,
        string? fileDownloadName = null)
    {
        ArgumentNullException.ThrowIfNull(resultExtensions);
        ArgumentNullException.ThrowIfNull(value);
        return new BarcodeHttpResult(context => BarcodeHttpResult.ResolveGenerator(context).Create(type, value, options), format, fileDownloadName);
    }

    /// <summary>Renders a configured <see cref="BarcodeBuilder"/>.</summary>
    /// <param name="resultExtensions">The <see cref="Results.Extensions"/> marker.</param>
    /// <param name="builder">The barcode to render.</param>
    /// <param name="format">The image format. Default PNG.</param>
    /// <param name="fileDownloadName">Optional file name that makes the browser download the image.</param>
    public static BarcodeHttpResult Barcode(
        this IResultExtensions resultExtensions,
        BarcodeBuilder builder,
        BarcodeImageFormat format = BarcodeImageFormat.Png,
        string? fileDownloadName = null)
    {
        ArgumentNullException.ThrowIfNull(resultExtensions);
        return builder.ToHttpResult(format, fileDownloadName);
    }

    /// <summary>Wraps a configured <see cref="BarcodeBuilder"/> in an <see cref="IResult"/>.</summary>
    /// <param name="builder">The barcode to render.</param>
    /// <param name="format">The image format. Default PNG.</param>
    /// <param name="fileDownloadName">Optional file name that makes the browser download the image.</param>
    public static BarcodeHttpResult ToHttpResult(this BarcodeBuilder builder, BarcodeImageFormat format = BarcodeImageFormat.Png, string? fileDownloadName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return new BarcodeHttpResult(_ => builder, format, fileDownloadName);
    }
}
