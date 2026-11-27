using TerraFluent.BarCode.Compatibility;
using TerraFluent.BarCode.Encoding;
using TerraFluent.BarCode.Rendering;

namespace TerraFluent.BarCode;

/// <summary>
/// Fluent configuration and output for one barcode. Configuration methods
/// return the same builder; output methods (<c>To...</c>, <see cref="WriteTo"/>,
/// <see cref="Save(string)"/>) can be called any number of times.
/// </summary>
/// <remarks>
/// <para>
/// <b>Thread safety:</b> configure a builder on one thread. Once configured, its
/// output methods may be called from several threads at the same time. The
/// static <see cref="Barcode"/> methods are thread-safe, so the simplest pattern
/// is one builder per barcode.
/// </para>
/// <para>
/// <b>Errors:</b> an invalid value or argument throws <see cref="ArgumentException"/>
/// (or <see cref="ArgumentNullException"/>, <see cref="ArgumentOutOfRangeException"/>
/// and <see cref="FormatException"/> for hex colours). Options that cannot be
/// combined throw <see cref="InvalidOperationException"/>, either when set (an
/// option for another symbology) or when rendering (a logo with too little error
/// correction, or colours with too little contrast).
/// </para>
/// </remarks>
public sealed class BarcodeBuilder
{
    /// <summary>Default width of one module (the narrowest bar) for linear barcodes, in pixels.</summary>
    public const int DefaultLinearModuleSize = 2;

    /// <summary>Default size of one QR or Data Matrix module, in pixels.</summary>
    public const int DefaultQrModuleSize = 4;

    /// <summary>Default width of one PDF417 module, in pixels (rows are three modules high).</summary>
    public const int DefaultPdf417ModuleSize = 2;

    /// <summary>Default bar height for linear barcodes, in pixels.</summary>
    public const int DefaultBarHeight = 80;

    /// <summary>Default JPEG quality (1-100).</summary>
    public const int DefaultJpegQuality = 90;

    /// <summary>Default logo size as a fraction of the QR code's width.</summary>
    public const double DefaultLogoScale = 0.2;

    /// <summary>Largest allowed logo size as a fraction of the QR code's width.</summary>
    public const double MaxLogoScale = 0.3;

    /// <summary>
    /// Largest allowed output width or height, in pixels. Rendering a bigger
    /// image throws <see cref="ArgumentException"/>, so untrusted size settings
    /// cannot exhaust memory.
    /// </summary>
    public const int MaxImageDimension = 32_768;

    /// <summary>Largest allowed output area (width times height), in pixels: 64 megapixels.</summary>
    public const long MaxImagePixels = 64L * 1024 * 1024;

    /// <summary>
    /// Smallest WCAG contrast ratio accepted between the background and the
    /// bars, gradient stops or eye colour, unless <see cref="AllowLowContrast"/> is set.
    /// </summary>
    public const double MinContrastRatio = 3.0;

    /// <summary>Largest logo scale allowed at error-correction level Q; level H allows up to <see cref="MaxLogoScale"/>.</summary>
    public const double MaxLogoScaleAtQ = 0.25;

    private int? _moduleSize;
    private int _barHeight = DefaultBarHeight;
    private int? _quietZone;
    private BarcodeColor _foreground = BarcodeColor.Black;
    private BarcodeColor _background = BarcodeColor.White;
    private int _jpegQuality = DefaultJpegQuality;
    private QrErrorCorrectionLevel _errorCorrection = QrErrorCorrectionLevel.M;
    private bool _errorCorrectionSet;
    private bool _code39Checksum;
    private BarcodeMatrix? _matrix;
    private QrModuleStyle _moduleStyle;
    private QrEyeStyle _eyeStyle;
    private BarcodeColor? _eyeColor;
    private (BarcodeColor Start, BarcodeColor End, GradientDirection Direction)? _gradient;
    private bool _roundedBars;
    private RgbaImage? _logo;
    private byte[]? _logoPng;
    private double _logoScale = DefaultLogoScale;
    private bool _allowLowContrast;
    private bool _fullAscii;
    private string? _addOn;
    private bool _humanReadable;
    private int? _fontSize;
    private bool _transparent;
    private DataMatrixShape _dataMatrixShape;
    private int? _pdf417Columns;
    private int? _pdf417ErrorCorrection;

    internal BarcodeBuilder(BarcodeType type, string value)
    {
        Type = type;
        Value = value;
    }

    /// <summary>The symbology.</summary>
    public BarcodeType Type { get; }

    /// <summary>The value being encoded, as passed in.</summary>
    public string Value { get; }

    private bool IsLinear => Type is not (BarcodeType.QrCode or BarcodeType.DataMatrix or BarcodeType.Pdf417);

    private bool IsEanUpc => Type is BarcodeType.Ean13 or BarcodeType.Ean8 or BarcodeType.UpcA;

    /// <summary>
    /// Size of one module in pixels: the narrowest bar of a linear barcode, or
    /// one square of a 2D code. Defaults to 2 for linear barcodes and PDF417,
    /// and 4 for QR codes and Data Matrix.
    /// </summary>
    public BarcodeBuilder ModuleSize(int pixels)
    {
        _moduleSize = Guard.InRange(pixels, 1, MaxImageDimension, nameof(pixels));
        return this;
    }

    /// <summary>
    /// Bar height of a linear barcode in pixels (default 80), not counting
    /// human-readable text. Not allowed for 2D codes, whose size follows from ModuleSize.
    /// </summary>
    public BarcodeBuilder Height(int pixels)
    {
        if (!IsLinear) throw new InvalidOperationException("Height applies to linear barcodes only; a 2D code's size follows from ModuleSize.");
        _barHeight = Guard.InRange(pixels, 1, MaxImageDimension, nameof(pixels));
        return this;
    }

    /// <summary>
    /// Blank margin in modules on each side (linear barcodes: left and right only).
    /// Defaults to what the symbology's specification asks for: 10 for Code 128/39,
    /// 11 for EAN-13, 9 for UPC-A, 7 for EAN-8, 4 for QR codes and 2 for Data
    /// Matrix and PDF417. Scanners may fail with a smaller value.
    /// </summary>
    public BarcodeBuilder QuietZone(int modules)
    {
        _quietZone = Guard.InRange(modules, 0, MaxImageDimension, nameof(modules));
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
        _errorCorrectionSet = true;
        _matrix = null;
        return this;
    }

    /// <summary>
    /// Encodes all 128 ASCII characters, including lower case, using the Code 39
    /// full ASCII extension (each extra character takes two symbol characters).
    /// The scanner must have full ASCII mode turned on. Code 39 only.
    /// </summary>
    public BarcodeBuilder FullAscii(bool enable = true)
    {
        if (Type != BarcodeType.Code39) throw new InvalidOperationException("FullAscii applies to Code 39 only.");
        _fullAscii = enable;
        _matrix = null;
        return this;
    }

    /// <summary>
    /// Appends a 2-digit (EAN-2) or 5-digit (EAN-5) add-on, as used for periodical
    /// issue numbers and book prices. EAN-13 and UPC-A only.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="digits"/> is not 2 or 5 digits.</exception>
    public BarcodeBuilder AddOn(string digits)
    {
        if (Type is not (BarcodeType.Ean13 or BarcodeType.UpcA)) throw new InvalidOperationException("AddOn applies to EAN-13 and UPC-A only.");
        _addOn = EanUpcEncoder.ValidateAddOn(digits, nameof(digits));
        _matrix = null;
        return this;
    }

    /// <summary>
    /// Prints the human-readable text under a linear barcode: EAN/UPC digits in
    /// their standard positions with extended guard bars, or the value centred
    /// under Code 128, Code 39 and GS1-128 (with AIs in parentheses). The image
    /// grows by about one text line. Linear barcodes only.
    /// </summary>
    public BarcodeBuilder HumanReadableText(bool show = true)
    {
        if (!IsLinear) throw new InvalidOperationException("HumanReadableText applies to linear barcodes only.");
        _humanReadable = show;
        return this;
    }

    /// <summary>
    /// Font size of the human-readable text, in pixels per em. Defaults to 10
    /// modules for EAN/UPC and 9 modules for other linear barcodes. Text that is
    /// wider than the bars is shrunk to fit. Linear barcodes only.
    /// </summary>
    public BarcodeBuilder TextSize(int pixels)
    {
        if (!IsLinear) throw new InvalidOperationException("TextSize applies to linear barcodes only.");
        _fontSize = Guard.InRange(pixels, 4, 1000, nameof(pixels));
        return this;
    }

    /// <summary>
    /// Leaves the background transparent in PNG and SVG output. The contrast
    /// check still uses <see cref="Background(BarcodeColor)"/>, so set it to the
    /// colour of the surface the code will be placed on. JPEG has no
    /// transparency, so JPEG output then throws <see cref="InvalidOperationException"/>.
    /// </summary>
    public BarcodeBuilder TransparentBackground(bool transparent = true)
    {
        _transparent = transparent;
        return this;
    }

    /// <summary>Which symbol shapes may be used (default <see cref="BarCode.DataMatrixShape.Square"/>). Data Matrix only.</summary>
    public BarcodeBuilder DataMatrixShape(DataMatrixShape shape)
    {
        if (Type != BarcodeType.DataMatrix) throw new InvalidOperationException("DataMatrixShape applies to Data Matrix only.");
        if (shape < BarCode.DataMatrixShape.Square || shape > BarCode.DataMatrixShape.Any) throw new ArgumentOutOfRangeException(nameof(shape));
        _dataMatrixShape = shape;
        _matrix = null;
        return this;
    }

    /// <summary>Number of data columns, 1 to 30 (default: chosen for a roughly 3:1 symbol). PDF417 only.</summary>
    public BarcodeBuilder Pdf417Columns(int columns)
    {
        if (Type != BarcodeType.Pdf417) throw new InvalidOperationException("Pdf417Columns applies to PDF417 only.");
        _pdf417Columns = Guard.InRange(columns, Pdf417Encoder.MinColumns, Pdf417Encoder.MaxColumns, nameof(columns));
        _matrix = null;
        return this;
    }

    /// <summary>
    /// Error-correction level 0 to 8; level n adds 2^(n+1) error-correction
    /// codewords (default: the level ISO/IEC 15438 recommends for the data size,
    /// 2 to 6). PDF417 only.
    /// </summary>
    public BarcodeBuilder Pdf417ErrorCorrection(int level)
    {
        if (Type != BarcodeType.Pdf417) throw new InvalidOperationException("Pdf417ErrorCorrection applies to PDF417 only.");
        _pdf417ErrorCorrection = Guard.InRange(level, 0, 8, nameof(level));
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

    /// <summary>
    /// Shape of the QR code's data modules (default <see cref="QrModuleStyle.Square"/>).
    /// QR codes only. Every style keeps each module's centre where a scanner expects it.
    /// </summary>
    public BarcodeBuilder ModuleStyle(QrModuleStyle style)
    {
        RequireQr(nameof(ModuleStyle));
        if (style < QrModuleStyle.Square || style > QrModuleStyle.Connected) throw new ArgumentOutOfRangeException(nameof(style));
        _moduleStyle = style;
        return this;
    }

    /// <summary>Shape of the three corner "eyes" (default <see cref="QrEyeStyle.Square"/>). QR codes only.</summary>
    public BarcodeBuilder EyeStyle(QrEyeStyle style)
    {
        RequireQr(nameof(EyeStyle));
        if (style < QrEyeStyle.Square || style > QrEyeStyle.Circle) throw new ArgumentOutOfRangeException(nameof(style));
        _eyeStyle = style;
        return this;
    }

    /// <summary>Colour of the three corner "eyes", if different from the foreground or gradient. QR codes only.</summary>
    public BarcodeBuilder EyeColor(BarcodeColor color)
    {
        RequireQr(nameof(EyeColor));
        _eyeColor = color;
        return this;
    }

    /// <summary>Colour of the three corner "eyes" as <c>#RRGGBB</c> or <c>#RGB</c>. QR codes only.</summary>
    public BarcodeBuilder EyeColor(string hex) => EyeColor(BarcodeColor.FromHex(hex));

    /// <summary>
    /// Fills bars/dark modules with a gradient from <paramref name="start"/> to
    /// <paramref name="end"/> instead of the flat foreground colour. Keep both
    /// colours dark against the background so scanners see enough contrast.
    /// </summary>
    public BarcodeBuilder Gradient(BarcodeColor start, BarcodeColor end, GradientDirection direction = GradientDirection.Diagonal)
    {
        if (direction < GradientDirection.Horizontal || direction > GradientDirection.Radial) throw new ArgumentOutOfRangeException(nameof(direction));
        _gradient = (start, end, direction);
        return this;
    }

    /// <summary>Gradient fill with colours given as <c>#RRGGBB</c> or <c>#RGB</c>.</summary>
    public BarcodeBuilder Gradient(string startHex, string endHex, GradientDirection direction = GradientDirection.Diagonal) =>
        Gradient(BarcodeColor.FromHex(startHex), BarcodeColor.FromHex(endHex), direction);

    /// <summary>Gives the bars of a linear barcode rounded, pill-shaped ends. Linear barcodes only.</summary>
    public BarcodeBuilder RoundedBars(bool rounded = true)
    {
        if (!IsLinear) throw new InvalidOperationException("RoundedBars applies to linear barcodes only; use ModuleStyle for QR codes.");
        _roundedBars = rounded;
        return this;
    }

    /// <summary>
    /// Places a logo (a PNG file's bytes; transparency is kept) in the centre of
    /// a QR code and clears the modules behind it. <paramref name="scale"/> is the
    /// logo's size as a fraction of the code's width, from 0.05 to 0.3 (default 0.2);
    /// it is shrunk if needed so the corner eyes and format modules stay visible.
    /// Unless <see cref="ErrorCorrection"/> is set explicitly, the code switches to
    /// level H so it still scans with the covered modules missing. Rendering throws
    /// <see cref="InvalidOperationException"/> if the error correction is set
    /// explicitly to L or M, or to Q with a scale above <see cref="MaxLogoScaleAtQ"/>,
    /// because such codes often fail to scan. QR codes only.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="png"/> is not a readable PNG file.</exception>
    public BarcodeBuilder Logo(byte[] png, double scale = DefaultLogoScale)
    {
        RequireQr(nameof(Logo));
        if (double.IsNaN(scale) || scale < 0.05 || scale > MaxLogoScale)
        {
            throw new ArgumentOutOfRangeException(nameof(scale), scale, FormattableString.Invariant($"Logo scale must be between 0.05 and {MaxLogoScale}."));
        }

        _logo = PngDecoder.Decode(png, nameof(png));
        _logoPng = (byte[])png.Clone();
        _logoScale = scale;
        _matrix = null;
        return this;
    }

    /// <summary>
    /// Turns off the contrast check, which otherwise throws
    /// <see cref="InvalidOperationException"/> when rendering if the background and
    /// the bars, gradient stops or eye colour have a contrast ratio below
    /// <see cref="MinContrastRatio"/>. Only use this for decorative output that
    /// you have tested with your scanners.
    /// </summary>
    public BarcodeBuilder AllowLowContrast(bool allow = true)
    {
        _allowLowContrast = allow;
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
    public BarcodeMatrix Encode() => _matrix ??= EncodeCore();

    private BarcodeMatrix EncodeCore()
    {
        switch (Type)
        {
            case BarcodeType.Code128:
            {
                var matrix = Code128Encoder.Encode(Value);
                matrix.Layout!.CentredText = new string(Value.Select(c => c < ' ' || c == 127 ? ' ' : c).ToArray());
                return matrix;
            }

            case BarcodeType.Gs1Code128:
            {
                var gs1 = Gs1.Parse(Value, "value");
                var matrix = Code128Encoder.Encode(gs1.Encoded, allowFnc1: true);
                matrix.Layout!.CentredText = gs1.HumanReadable;
                return matrix;
            }

            case BarcodeType.Code39:
            {
                var matrix = Code39Encoder.Encode(Value, _code39Checksum, _fullAscii);
                matrix.Layout!.CentredText = new string(Value.Select(c => c < ' ' || c == 127 ? ' ' : c).ToArray());
                return matrix;
            }

            case BarcodeType.Ean13: return EanUpcEncoder.EncodeEan13(Value, _addOn);
            case BarcodeType.Ean8: return EanUpcEncoder.EncodeEan8(Value);
            case BarcodeType.UpcA: return EanUpcEncoder.EncodeUpcA(Value, _addOn);
            case BarcodeType.QrCode: return QrCodeEncoder.Encode(Value, EffectiveErrorCorrection);
            case BarcodeType.DataMatrix: return DataMatrixEncoder.Encode(Value, _dataMatrixShape);
            case BarcodeType.Pdf417: return Pdf417Encoder.Encode(Value, _pdf417Columns, _pdf417ErrorCorrection);
            default: throw new InvalidOperationException($"Unsupported barcode type {Type}.");
        }
    }

    /// <summary>The output image size in pixels with the current settings.</summary>
    public (int Width, int Height) GetImageSize() => CreateRenderOptions().ImageSize(Encode());

    /// <summary>
    /// Renders a PNG: 1-bit indexed for plain barcodes, 8-bit RGB with
    /// anti-aliasing when a style (gradient, rounded shapes, logo) is set.
    /// </summary>
    public byte[] ToPng() => CreateScene() is { } scene
        ? PngEncoder.Encode(Rasterizer.Render(scene))
        : PngEncoder.Encode(Encode(), CreateRenderOptions(), _transparent);

    /// <summary>Renders a baseline JPEG at the configured <see cref="JpegQuality"/>.</summary>
    public byte[] ToJpeg()
    {
        if (_transparent) throw new InvalidOperationException("JPEG has no transparency; use PNG or SVG, or remove TransparentBackground().");
        return CreateScene() is { } scene
            ? JpegEncoder.Encode(Rasterizer.Render(scene), _jpegQuality)
            : JpegEncoder.Encode(Encode(), CreateRenderOptions(), _jpegQuality);
    }

    /// <summary>Renders an SVG document.</summary>
    public string ToSvg() => CreateScene() is { } scene
        ? SvgRenderer.Render(scene)
        : SvgRenderer.Render(Encode(), CreateRenderOptions(), _transparent);

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

    /// <summary>
    /// Saves the image to <paramref name="path"/> asynchronously, choosing the
    /// format from its extension (.png, .jpg, .jpeg, .svg).
    /// </summary>
    public Task SaveAsync(string path, CancellationToken cancellationToken = default) =>
        SaveAsync(path, BarcodeImageFormatExtensions.FromFilePath(path), cancellationToken);

    /// <summary>Saves the image to <paramref name="path"/> in <paramref name="format"/> asynchronously.</summary>
    public async Task SaveAsync(string path, BarcodeImageFormat format, CancellationToken cancellationToken = default)
    {
        if (path is null) throw new ArgumentNullException(nameof(path));
        var bytes = ToBytes(format);
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
        await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
    }

    private QrErrorCorrectionLevel EffectiveErrorCorrection =>
        _errorCorrectionSet || _logo is null ? _errorCorrection : QrErrorCorrectionLevel.H;

    private bool IsStyled =>
        _moduleStyle != QrModuleStyle.Square || _eyeStyle != QrEyeStyle.Square || _eyeColor is not null
        || _gradient is not null || _roundedBars || _logo is not null || _humanReadable;

    private void RequireQr(string option)
    {
        if (Type != BarcodeType.QrCode) throw new InvalidOperationException($"{option} applies to QR codes only.");
    }

    private void ValidateCombination()
    {
        if (_logo is not null)
        {
            var level = EffectiveErrorCorrection;
            if (level is QrErrorCorrectionLevel.L or QrErrorCorrectionLevel.M)
            {
                throw new InvalidOperationException(
                    $"A logo needs error-correction level Q or H to scan reliably; level {level} is set. Remove the ErrorCorrection call (a logo selects H) or set Q or H.");
            }

            if (level == QrErrorCorrectionLevel.Q && _logoScale > MaxLogoScaleAtQ)
            {
                throw new InvalidOperationException(FormattableString.Invariant(
                    $"At error-correction level Q the logo scale can be at most {MaxLogoScaleAtQ}; it is {_logoScale}. Use level H or a smaller logo."));
            }
        }

        if (_allowLowContrast) return;
        CheckContrast(_gradient is null ? _foreground : _gradient.Value.Start, _gradient is null ? "Foreground" : "Gradient start");
        if (_gradient is { } g) CheckContrast(g.End, "Gradient end");
        if (_eyeColor is { } eye) CheckContrast(eye, "EyeColor");
    }

    private void CheckContrast(BarcodeColor color, string option)
    {
        var ratio = BarcodeColor.ContrastRatio(color, _background);
        if (ratio < MinContrastRatio)
        {
            throw new InvalidOperationException(FormattableString.Invariant(
                $"{option} {color} on background {_background} has a contrast ratio of {ratio:0.0}:1, below the {MinContrastRatio:0.0}:1 scanners need. ") +
                "Use a darker or lighter colour, or call AllowLowContrast() if you have tested it.");
        }
    }

    /// <summary>The vector scene for styled output, or <c>null</c> for a plain barcode.</summary>
    internal Scene? CreateScene()
    {
        ValidateCombination();
        if (!IsStyled) return null;
        return SceneBuilder.Build(Encode(), CreateRenderOptions(), new StyleOptions
        {
            ModuleStyle = _moduleStyle,
            EyeStyle = _eyeStyle,
            EyeColor = _eyeColor,
            Gradient = _gradient,
            RoundedBars = _roundedBars,
            Logo = _logo,
            LogoPng = _logoPng,
            LogoScale = _logoScale,
            IsQr = Type == BarcodeType.QrCode,
            Transparent = _transparent,
        });
    }

    private RenderOptions CreateRenderOptions()
    {
        var matrix = Encode();
        var moduleSize = _moduleSize ?? Type switch
        {
            BarcodeType.QrCode or BarcodeType.DataMatrix => DefaultQrModuleSize,
            BarcodeType.Pdf417 => DefaultPdf417ModuleSize,
            _ => DefaultLinearModuleSize,
        };
        var fontSize = !_humanReadable ? 0 : _fontSize ?? Math.Max(8, moduleSize * (IsEanUpc ? 10 : 9));
        return new RenderOptions(
            moduleSize,
            _barHeight,
            _quietZone ?? matrix.RecommendedQuietZone,
            _foreground,
            _background,
            fontSize);
    }
}
