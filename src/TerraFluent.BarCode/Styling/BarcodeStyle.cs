using TerraFluent.BarCode.Compatibility;

namespace TerraFluent.BarCode;

/// <summary>
/// Decorative options for a barcode, configured through
/// <see cref="BarcodeBuilder.Style(Action{BarcodeStyle})"/>. Every option is
/// off by default; an unstyled barcode renders exactly as before.
/// </summary>
/// <remarks>
/// Styling trades some scanning robustness for looks. Keep strong contrast
/// between the gradient colours and the background, and keep the quiet zone.
/// </remarks>
public sealed class BarcodeStyle
{
    /// <summary>Largest logo size, as a fraction of the QR symbol width.</summary>
    public const double MaxLogoSizeRatio = 0.3;

    private readonly bool _isQr;

    internal BarcodeStyle(bool isQr) => _isQr = isQr;

    internal QrModuleShape ModuleShapeValue { get; private set; } = QrModuleShape.Square;
    internal QrFinderShape FinderShapeValue { get; private set; } = QrFinderShape.Square;
    internal BarcodeColor? FinderColorValue { get; private set; }
    internal global::TerraFluent.BarCode.BarShape BarShapeValue { get; private set; } = global::TerraFluent.BarCode.BarShape.Square;
    internal BarcodeColor? GradientStart { get; private set; }
    internal BarcodeColor? GradientEnd { get; private set; }
    internal GradientDirection GradientDirectionValue { get; private set; }
    internal bool Transparent { get; private set; }
    internal byte[]? LogoBytes { get; private set; }
    internal double LogoSizeRatio { get; private set; }
    internal bool LogoClearBehind { get; private set; }

    internal bool HasGradient => GradientStart.HasValue;

    /// <summary>Whether anything differs from a plain black-and-white-style render.</summary>
    internal bool IsPlain =>
        ModuleShapeValue == QrModuleShape.Square &&
        FinderShapeValue == QrFinderShape.Square &&
        FinderColorValue is null &&
        BarShapeValue == global::TerraFluent.BarCode.BarShape.Square &&
        !HasGradient &&
        !Transparent &&
        LogoBytes is null;

    /// <summary>Shape of the QR data modules. QR codes only.</summary>
    public BarcodeStyle ModuleShape(QrModuleShape shape)
    {
        RequireQr(nameof(ModuleShape));
        ModuleShapeValue = shape;
        return this;
    }

    /// <summary>Shape of the three QR corner squares. QR codes only.</summary>
    public BarcodeStyle FinderShape(QrFinderShape shape)
    {
        RequireQr(nameof(FinderShape));
        FinderShapeValue = shape;
        return this;
    }

    /// <summary>A separate colour for the three QR corner squares. QR codes only.</summary>
    public BarcodeStyle FinderColor(BarcodeColor color)
    {
        RequireQr(nameof(FinderColor));
        FinderColorValue = color;
        return this;
    }

    /// <summary>A separate colour for the three QR corner squares, as <c>#RRGGBB</c>. QR codes only.</summary>
    public BarcodeStyle FinderColor(string hex) => FinderColor(BarcodeColor.FromHex(hex));

    /// <summary>Shape of the bars. Linear barcodes only.</summary>
    public BarcodeStyle BarShape(global::TerraFluent.BarCode.BarShape shape)
    {
        if (_isQr) throw new InvalidOperationException("BarShape applies to linear barcodes only; use ModuleShape for QR codes.");
        BarShapeValue = shape;
        return this;
    }

    /// <summary>
    /// Fills bars/modules with a gradient from <paramref name="start"/> to
    /// <paramref name="end"/> instead of the foreground colour. Both colours
    /// should be dark against the background.
    /// </summary>
    public BarcodeStyle Gradient(BarcodeColor start, BarcodeColor end, GradientDirection direction = GradientDirection.Diagonal)
    {
        if (direction < GradientDirection.Horizontal || direction > GradientDirection.Radial) throw new ArgumentOutOfRangeException(nameof(direction));
        GradientStart = start;
        GradientEnd = end;
        GradientDirectionValue = direction;
        return this;
    }

    /// <summary>Gradient from two <c>#RRGGBB</c> colours.</summary>
    public BarcodeStyle Gradient(string startHex, string endHex, GradientDirection direction = GradientDirection.Diagonal) =>
        Gradient(BarcodeColor.FromHex(startHex), BarcodeColor.FromHex(endHex), direction);

    /// <summary>
    /// Leaves the background transparent in PNG and SVG output. JPEG has no
    /// transparency and uses the background colour instead.
    /// </summary>
    public BarcodeStyle TransparentBackground(bool transparent = true)
    {
        Transparent = transparent;
        return this;
    }

    /// <summary>
    /// Places an image in the centre of a QR code. QR codes only.
    /// </summary>
    /// <param name="image">
    /// The image file's bytes. SVG output accepts PNG, JPEG, GIF, WebP or SVG.
    /// PNG and JPEG output need a PNG logo (8- or 16-bit, not interlaced).
    /// </param>
    /// <param name="sizeRatio">Logo width and height as a fraction of the symbol width, up to 0.3 (default 0.2).</param>
    /// <param name="clearBehind">Removes the modules behind the logo (default true) so it sits on a clean background.</param>
    /// <remarks>
    /// The logo hides part of the symbol, which error correction must restore,
    /// so a logo raises the default error-correction level to H. Setting L or
    /// M explicitly together with a logo is rejected.
    /// </remarks>
    public BarcodeStyle Logo(byte[] image, double sizeRatio = 0.2, bool clearBehind = true)
    {
        RequireQr(nameof(Logo));
        if (image is null) throw new ArgumentNullException(nameof(image));
        if (image.Length == 0) throw new ArgumentException("Logo image is empty.", nameof(image));
        if (double.IsNaN(sizeRatio) || sizeRatio <= 0 || sizeRatio > MaxLogoSizeRatio)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeRatio), sizeRatio, $"Logo size must be greater than 0 and at most {MaxLogoSizeRatio} of the symbol width.");
        }

        LogoBytes = (byte[])image.Clone();
        LogoSizeRatio = sizeRatio;
        LogoClearBehind = clearBehind;
        return this;
    }

    /// <summary>Places the image file at <paramref name="path"/> in the centre of a QR code. See <see cref="Logo(byte[], double, bool)"/>.</summary>
    public BarcodeStyle Logo(string path, double sizeRatio = 0.2, bool clearBehind = true)
    {
        Guard.NotNullOrEmpty(path, nameof(path));
        return Logo(File.ReadAllBytes(path), sizeRatio, clearBehind);
    }

    private void RequireQr(string option)
    {
        if (!_isQr) throw new InvalidOperationException($"{option} applies to QR codes only.");
    }
}
