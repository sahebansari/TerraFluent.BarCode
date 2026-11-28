namespace TerraFluent.Barcode;

/// <summary>
/// Default <see cref="IBarcodeGenerator"/>, built on <see cref="Barcode"/>. It
/// holds no state besides its default options, so one instance can be shared
/// (register it as a singleton).
/// </summary>
public sealed class BarcodeGenerator : IBarcodeGenerator
{
    private readonly BarcodeOptions? _defaults;

    /// <summary>
    /// Creates a generator. <paramref name="defaults"/> is used whenever a call
    /// passes no options of its own; it is read on every call, so do not change
    /// it while the generator is in use.
    /// </summary>
    public BarcodeGenerator(BarcodeOptions? defaults = null) => _defaults = defaults;

    /// <inheritdoc/>
    public BarcodeBuilder Create(BarcodeType type, string value, BarcodeOptions? options = null)
    {
        var builder = Barcode.Create(type, value);
        var o = options ?? _defaults;
        if (o is null) return builder;

        bool linear = type is not (BarcodeType.QrCode or BarcodeType.DataMatrix or BarcodeType.Pdf417);
        if (o.ModuleSize is { } moduleSize) builder.ModuleSize(moduleSize);
        if (o.Height is { } height && linear) builder.Height(height);
        if (o.QuietZone is { } quietZone) builder.QuietZone(quietZone);
        if (o.Foreground is { } foreground) builder.Foreground(foreground);
        if (o.Background is { } background) builder.Background(background);
        if (o.TransparentBackground) builder.TransparentBackground();
        if (o.HumanReadableText && linear) builder.HumanReadableText();
        if (o.TextSize is { } textSize && linear) builder.TextSize(textSize);
        if (o.QrErrorCorrection is { } level && type == BarcodeType.QrCode) builder.ErrorCorrection(level);
        if (o.Code39Checksum && type == BarcodeType.Code39) builder.WithChecksum();
        if (o.Code39FullAscii && type == BarcodeType.Code39) builder.FullAscii();
        if (o.DataMatrixShape is { } shape && type == BarcodeType.DataMatrix) builder.DataMatrixShape(shape);
        if (o.Pdf417Columns is { } columns && type == BarcodeType.Pdf417) builder.Pdf417Columns(columns);
        if (o.Pdf417ErrorCorrection is { } pdfLevel && type == BarcodeType.Pdf417) builder.Pdf417ErrorCorrection(pdfLevel);
        if (o.JpegQuality is { } quality) builder.JpegQuality(quality);
        return builder;
    }

    /// <inheritdoc/>
    public byte[] Generate(BarcodeType type, string value, BarcodeImageFormat format = BarcodeImageFormat.Png, BarcodeOptions? options = null) =>
        Create(type, value, options).ToBytes(format);

    /// <inheritdoc/>
    public string GenerateSvg(BarcodeType type, string value, BarcodeOptions? options = null) =>
        Create(type, value, options).ToSvg();

    /// <inheritdoc/>
    public string GenerateDataUri(BarcodeType type, string value, BarcodeImageFormat format = BarcodeImageFormat.Png, BarcodeOptions? options = null) =>
        Create(type, value, options).ToDataUri(format);

    /// <inheritdoc/>
    public Task WriteToAsync(BarcodeType type, string value, Stream destination, BarcodeImageFormat format = BarcodeImageFormat.Png, BarcodeOptions? options = null, CancellationToken cancellationToken = default) =>
        Create(type, value, options).WriteToAsync(destination, format, cancellationToken);
}
