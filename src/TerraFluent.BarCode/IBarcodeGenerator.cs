namespace TerraFluent.BarCode;

/// <summary>
/// Barcode generation as a service, for dependency injection and mocking in tests.
/// Register the default implementation once:
/// <code>
/// services.AddSingleton&lt;IBarcodeGenerator&gt;(new BarcodeGenerator(new BarcodeOptions { HumanReadableText = true }));
/// </code>
/// Implementations are thread-safe.
/// </summary>
public interface IBarcodeGenerator
{
    /// <summary>
    /// Starts a <see cref="BarcodeBuilder"/> with <paramref name="options"/> (or the
    /// generator's defaults) applied, for settings that <see cref="BarcodeOptions"/>
    /// does not cover such as gradients and logos.
    /// </summary>
    BarcodeBuilder Create(BarcodeType type, string value, BarcodeOptions? options = null);

    /// <summary>Renders the barcode as an image in <paramref name="format"/>.</summary>
    byte[] Generate(BarcodeType type, string value, BarcodeImageFormat format = BarcodeImageFormat.Png, BarcodeOptions? options = null);

    /// <summary>Renders the barcode as an SVG document.</summary>
    string GenerateSvg(BarcodeType type, string value, BarcodeOptions? options = null);

    /// <summary>Renders the barcode as a <c>data:</c> URI for an HTML <c>img</c> element.</summary>
    string GenerateDataUri(BarcodeType type, string value, BarcodeImageFormat format = BarcodeImageFormat.Png, BarcodeOptions? options = null);

    /// <summary>Renders the barcode and writes it to <paramref name="destination"/> asynchronously.</summary>
    Task WriteToAsync(BarcodeType type, string value, Stream destination, BarcodeImageFormat format = BarcodeImageFormat.Png, BarcodeOptions? options = null, CancellationToken cancellationToken = default);
}
