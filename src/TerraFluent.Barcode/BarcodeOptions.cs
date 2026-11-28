namespace TerraFluent.Barcode;

/// <summary>
/// Settings used by <see cref="IBarcodeGenerator"/>. Every property is optional;
/// a <see langword="null"/> or <see langword="false"/> value keeps the default
/// that <see cref="BarcodeBuilder"/> uses. Settings that only apply to some
/// symbologies (for example <see cref="Height"/> or <see cref="QrErrorCorrection"/>)
/// are ignored for the others, so one instance can serve every type.
/// </summary>
public sealed class BarcodeOptions
{
    /// <summary>Module size in pixels. See <see cref="BarcodeBuilder.ModuleSize"/>.</summary>
    public int? ModuleSize { get; set; }

    /// <summary>Bar height in pixels for linear barcodes. See <see cref="BarcodeBuilder.Height"/>.</summary>
    public int? Height { get; set; }

    /// <summary>Quiet zone in modules. See <see cref="BarcodeBuilder.QuietZone"/>.</summary>
    public int? QuietZone { get; set; }

    /// <summary>Colour of bars and dark modules (default black).</summary>
    public BarcodeColor? Foreground { get; set; }

    /// <summary>Background colour (default white).</summary>
    public BarcodeColor? Background { get; set; }

    /// <summary>Leaves the background transparent in PNG and SVG output. See <see cref="BarcodeBuilder.TransparentBackground"/>.</summary>
    public bool TransparentBackground { get; set; }

    /// <summary>Prints human-readable text under linear barcodes. See <see cref="BarcodeBuilder.HumanReadableText"/>.</summary>
    public bool HumanReadableText { get; set; }

    /// <summary>Font size of the human-readable text in pixels. See <see cref="BarcodeBuilder.TextSize"/>.</summary>
    public int? TextSize { get; set; }

    /// <summary>
    /// QR, Micro QR and rMQR error-correction level. See <see cref="BarcodeBuilder.ErrorCorrection"/>.
    /// Ignored for Micro QR when it is H, and for rMQR when it is L or Q.
    /// </summary>
    public QrErrorCorrectionLevel? QrErrorCorrection { get; set; }

    /// <summary>Adds the Code 39 modulo 43 check character. See <see cref="BarcodeBuilder.WithChecksum"/>.</summary>
    public bool Code39Checksum { get; set; }

    /// <summary>Uses the Code 39 full ASCII extension. See <see cref="BarcodeBuilder.FullAscii"/>.</summary>
    public bool Code39FullAscii { get; set; }

    /// <summary>Appends the Interleaved 2 of 5 check digit. See <see cref="BarcodeBuilder.WithChecksum"/>.</summary>
    public bool ItfChecksum { get; set; }

    /// <summary>Aztec minimum error correction in percent. See <see cref="BarcodeBuilder.AztecErrorCorrection"/>.</summary>
    public int? AztecErrorCorrection { get; set; }

    /// <summary>MaxiCode mode 4, 5 or 6. See <see cref="BarcodeBuilder.MaxiCodeMode"/>.</summary>
    public int? MaxiCodeMode { get; set; }

    /// <summary>Data Matrix symbol shapes. See <see cref="BarcodeBuilder.DataMatrixShape"/>.</summary>
    public DataMatrixShape? DataMatrixShape { get; set; }

    /// <summary>PDF417 data columns. See <see cref="BarcodeBuilder.Pdf417Columns"/>.</summary>
    public int? Pdf417Columns { get; set; }

    /// <summary>PDF417 error-correction level. See <see cref="BarcodeBuilder.Pdf417ErrorCorrection"/>.</summary>
    public int? Pdf417ErrorCorrection { get; set; }

    /// <summary>JPEG quality, 1 to 100. See <see cref="BarcodeBuilder.JpegQuality"/>.</summary>
    public int? JpegQuality { get; set; }

    /// <summary>Largest output area in pixels. See <see cref="BarcodeBuilder.ImageSizeLimit"/>.</summary>
    public long? MaxImagePixels { get; set; }
}
