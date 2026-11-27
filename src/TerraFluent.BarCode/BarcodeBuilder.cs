using TerraFluent.BarCode.Compatibility;
using TerraFluent.BarCode.Encoding;
using TerraFluent.BarCode.Rendering;

namespace TerraFluent.BarCode;

/// <summary>
/// Fluent configuration and output for one barcode. Configuration methods
/// return the same builder; output methods (<c>To...</c>, <see cref="WriteTo"/>,
/// <see cref="Save(string)"/>) can be called any number of times.
/// </summary>
public sealed class BarcodeBuilder
{
    /// <summary>Default width of one module (the narrowest bar) for linear barcodes, in pixels.</summary>
    public const int DefaultLinearModuleSize = 2;

    /// <summary>Default size of one QR module, in pixels.</summary>
    public const int DefaultQrModuleSize = 4;

    /// <summary>Default bar height for linear barcodes, in pixels.</summary>
    public const int DefaultBarHeight = 80;

    /// <summary>Default JPEG quality (1-100).</summary>
    public const int DefaultJpegQuality = 90;

    private int? _moduleSize;
    private int _barHeight = DefaultBarHeight;
    private int? _quietZone;
    private BarcodeColor _foreground = BarcodeColor.Black;
    private BarcodeColor _background = BarcodeColor.White;
    private int _jpegQuality = DefaultJpegQuality;
    private QrErrorCorrectionLevel _errorCorrection = QrErrorCorrectionLevel.M;
    private bool _code39Checksum;
    private BarcodeMatrix? _matrix;

    internal BarcodeBuilder(BarcodeType type, string value)
    {
        Type = type;
        Value = value;
    }

    /// <summary>The symbology.</summary>
    public BarcodeType Type { get; }

    /// <summary>The value being encoded, as passed in.</summary>
    public string Value { get; }

    private bool IsLinear => Type != BarcodeType.QrCode;

    /// <summary>
    /// Size of one module in pixels: the narrowest bar of a linear barcode, or
    /// one square of a QR code. Defaults to 2 for linear barcodes and 4 for QR codes.
    /// </summary>
    public BarcodeBuilder ModuleSize(int pixels)
    {
        _moduleSize = Guard.Positive(pixels, nameof(pixels));
        return this;
    }

    /// <summary>Bar height of a linear barcode in pixels (default 80). Not allowed for QR codes, which are square.</summary>
    public BarcodeBuilder Height(int pixels)
    {
        if (!IsLinear) throw new InvalidOperationException("Height applies to linear barcodes only; a QR code's size follows from ModuleSize.");
        _barHeight = Guard.Positive(pixels, nameof(pixels));
        return this;
    }

    /// <summary>
    /// Blank margin in modules on each side (linear barcodes: left and right only).
    /// Defaults to what the symbology's specification asks for: 10 for Code 128/39,
    /// 11 for EAN-13, 9 for UPC-A, 7 for EAN-8 and 4 for QR codes. Scanners may
    /// fail with a smaller value.
    /// </summary>
    public BarcodeBuilder QuietZone(int modules)
    {
        _quietZone = Guard.NonNegative(modules, nameof(modules));
        return this;
    }

    /// <summary>Colour of bars/dark modules (default black).</summary>
    public BarcodeBuilder Foreground(BarcodeColor color)
    {
        _foreground = color;
        return this;
    }

    /// <summary>Colour of bars/dark modules as <c>#RRGGBB</c> or <c>#RGB</c>.</summary>
    public BarcodeBuilder Foreground(string hex) => Foreground(BarcodeColor.FromHex(hex));

    /// <summary>Background and quiet-zone colour (default white).</summary>
    public BarcodeBuilder Background(BarcodeColor color)
    {
        _background = color;
        return this;
    }

    /// <summary>Background and quiet-zone colour as <c>#RRGGBB</c> or <c>#RGB</c>.</summary>
    public BarcodeBuilder Background(string hex) => Background(BarcodeColor.FromHex(hex));

    /// <summary>QR error-correction level (default <see cref="QrErrorCorrectionLevel.M"/>). QR codes only.</summary>
    public BarcodeBuilder ErrorCorrection(QrErrorCorrectionLevel level)
    {
        if (Type != BarcodeType.QrCode) throw new InvalidOperationException("ErrorCorrection applies to QR codes only.");
        if (level < QrErrorCorrectionLevel.L || level > QrErrorCorrectionLevel.H) throw new ArgumentOutOfRangeException(nameof(level));
        _errorCorrection = level;
        _matrix = null;
        return this;
    }

    /// <summary>Adds the optional modulo 43 check character. Code 39 only.</summary>
    public BarcodeBuilder WithChecksum(bool include = true)
    {
        if (Type != BarcodeType.Code39) throw new InvalidOperationException("WithChecksum applies to Code 39 only; the other symbologies always carry their check digit.");
        _code39Checksum = include;
        _matrix = null;
        return this;
    }

    /// <summary>JPEG quality from 1 to 100 (default 90). Only affects <see cref="ToJpeg"/> and JPEG outputs.</summary>
    public BarcodeBuilder JpegQuality(int quality)
    {
        _jpegQuality = Guard.InRange(quality, 1, 100, nameof(quality));
        return this;
    }

    /// <summary>
    /// Encodes the value into its module matrix without rendering it. The
    /// result is cached until an encoding option changes.
    /// </summary>
    /// <exception cref="ArgumentException">The value cannot be encoded in this symbology.</exception>
    public BarcodeMatrix Encode() => _matrix ??= Type switch
    {
        BarcodeType.Code128 => Code128Encoder.Encode(Value),
        BarcodeType.Code39 => Code39Encoder.Encode(Value, _code39Checksum),
        BarcodeType.Ean13 => EanUpcEncoder.EncodeEan13(Value),
        BarcodeType.Ean8 => EanUpcEncoder.EncodeEan8(Value),
        BarcodeType.UpcA => EanUpcEncoder.EncodeUpcA(Value),
        BarcodeType.QrCode => QrCodeEncoder.Encode(Value, _errorCorrection),
        _ => throw new InvalidOperationException($"Unsupported barcode type {Type}."),
    };

    /// <summary>The output image size in pixels with the current settings.</summary>
    public (int Width, int Height) GetImageSize() => CreateRenderOptions().ImageSize(Encode());

    /// <summary>Renders a 1-bit indexed PNG.</summary>
    public byte[] ToPng() => PngEncoder.Encode(Encode(), CreateRenderOptions());

    /// <summary>Renders a baseline JPEG at the configured <see cref="JpegQuality"/>.</summary>
    public byte[] ToJpeg() => JpegEncoder.Encode(Encode(), CreateRenderOptions(), _jpegQuality);

    /// <summary>Renders an SVG document.</summary>
    public string ToSvg() => SvgRenderer.Render(Encode(), CreateRenderOptions());

    /// <summary>Renders the image in <paramref name="format"/> as bytes (SVG as UTF-8 without BOM).</summary>
    public byte[] ToBytes(BarcodeImageFormat format = BarcodeImageFormat.Png) => format switch
    {
        BarcodeImageFormat.Png => ToPng(),
        BarcodeImageFormat.Jpeg => ToJpeg(),
        BarcodeImageFormat.Svg => new System.Text.UTF8Encoding(false).GetBytes(ToSvg()),
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown image format."),
    };

    /// <summary>Renders the image into a new <see cref="MemoryStream"/> positioned at the start. The caller disposes it.</summary>
    public Stream ToStream(BarcodeImageFormat format = BarcodeImageFormat.Png) => new MemoryStream(ToBytes(format), writable: false);

    /// <summary>Writes the image to <paramref name="destination"/>. The stream is left open.</summary>
    public void WriteTo(Stream destination, BarcodeImageFormat format = BarcodeImageFormat.Png)
    {
        if (destination is null) throw new ArgumentNullException(nameof(destination));
        var bytes = ToBytes(format);
        destination.Write(bytes, 0, bytes.Length);
    }

    /// <summary>Writes the image to <paramref name="destination"/> asynchronously. The stream is left open.</summary>
    public Task WriteToAsync(Stream destination, BarcodeImageFormat format = BarcodeImageFormat.Png, CancellationToken cancellationToken = default)
    {
        if (destination is null) throw new ArgumentNullException(nameof(destination));
        var bytes = ToBytes(format);
        return destination.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
    }

    /// <summary>Renders the image as a base64 string.</summary>
    public string ToBase64(BarcodeImageFormat format = BarcodeImageFormat.Png) => Convert.ToBase64String(ToBytes(format));

    /// <summary>Renders the image as a <c>data:</c> URI, ready for an HTML <c>img src</c> or CSS <c>url()</c>.</summary>
    public string ToDataUri(BarcodeImageFormat format = BarcodeImageFormat.Png) => $"data:{format.ToMimeType()};base64,{ToBase64(format)}";

    /// <summary>Saves the image to <paramref name="path"/>, choosing the format from its extension (.png, .jpg, .jpeg, .svg).</summary>
    public void Save(string path) => Save(path, BarcodeImageFormatExtensions.FromFilePath(path));

    /// <summary>Saves the image to <paramref name="path"/> in <paramref name="format"/>.</summary>
    public void Save(string path, BarcodeImageFormat format)
    {
        if (path is null) throw new ArgumentNullException(nameof(path));
        File.WriteAllBytes(path, ToBytes(format));
    }

    private RenderOptions CreateRenderOptions()
    {
        var matrix = Encode();
        return new RenderOptions(
            _moduleSize ?? (IsLinear ? DefaultLinearModuleSize : DefaultQrModuleSize),
            _barHeight,
            _quietZone ?? matrix.RecommendedQuietZone,
            _foreground,
            _background);
    }
}
