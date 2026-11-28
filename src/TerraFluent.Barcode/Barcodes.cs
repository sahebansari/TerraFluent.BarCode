using TerraFluent.Barcode.Compatibility;

namespace TerraFluent.Barcode;

/// <summary>
/// Entry point. Pick a symbology, configure it fluently, then ask for the
/// output you need:
/// <code>
/// byte[] png = Barcodes.Code128("ABC-12345").Height(60).ToPng();
/// string svg = Barcodes.QrCode("https://terrafluent.dev").ErrorCorrection(QrErrorCorrectionLevel.H).ToSvg();
/// string uri = Barcodes.Ean13("590123412345").ToDataUri(BarcodeImageFormat.Png);
/// </code>
/// </summary>
public static class Barcodes
{
    /// <summary>Longest value accepted by <see cref="Code128"/>, in characters.</summary>
    public const int MaxCode128Length = Encoding.Code128Encoder.MaxLength;

    /// <summary>Longest value accepted by <see cref="Code39"/>, in characters.</summary>
    public const int MaxCode39Length = Encoding.Code39Encoder.MaxLength;

    /// <summary>Longest value accepted by <see cref="Itf"/>, in digits (including a check digit added by <see cref="BarcodeBuilder.WithChecksum"/>).</summary>
    public const int MaxItfLength = Encoding.ItfEncoder.MaxLength;

    /// <summary>Longest value accepted by <see cref="Codabar"/>, in characters (including start and stop characters).</summary>
    public const int MaxCodabarLength = Encoding.CodabarEncoder.MaxLength;

    /// <summary>Starts a barcode of the given <paramref name="type"/>.</summary>
    public static BarcodeBuilder Create(BarcodeType type, string value) => new(type, Guard.NotNullOrEmpty(value, nameof(value)));

    /// <summary>
    /// Code 128 for any ASCII text, up to <see cref="MaxCode128Length"/> characters.
    /// Code sets are chosen automatically for the shortest symbol.
    /// </summary>
    public static BarcodeBuilder Code128(string value) => Create(BarcodeType.Code128, value);

    /// <summary>
    /// Code 39 for digits, upper-case letters, space and <c>- . $ / + %</c>, up to
    /// <see cref="MaxCode39Length"/> characters.
    /// Call <see cref="BarcodeBuilder.WithChecksum"/> to add the optional modulo 43 check character.
    /// </summary>
    public static BarcodeBuilder Code39(string value) => Create(BarcodeType.Code39, value);

    /// <summary>EAN-13 from 12 digits (check digit added) or 13 digits (check digit validated).</summary>
    public static BarcodeBuilder Ean13(string value) => Create(BarcodeType.Ean13, value);

    /// <summary>EAN-8 from 7 digits (check digit added) or 8 digits (check digit validated).</summary>
    public static BarcodeBuilder Ean8(string value) => Create(BarcodeType.Ean8, value);

    /// <summary>UPC-A from 11 digits (check digit added) or 12 digits (check digit validated).</summary>
    public static BarcodeBuilder UpcA(string value) => Create(BarcodeType.UpcA, value);

    /// <summary>QR code for any text (UTF-8). The smallest version that fits is picked automatically.</summary>
    public static BarcodeBuilder QrCode(string value) => Create(BarcodeType.QrCode, value);

    /// <summary>
    /// GS1-128 from an element string with application identifiers in parentheses,
    /// for example <c>(01)09501101530003(17)250101(10)ABC123</c>. FNC1 characters are
    /// inserted where GS1 requires them. Each AI must be one GS1 defines, and its data
    /// is checked against the AI's format: length, digits, check digit and date.
    /// </summary>
    public static BarcodeBuilder Gs1Code128(string value) => Create(BarcodeType.Gs1Code128, value);

    /// <summary>
    /// GS1-128 from application identifier and data pairs, for example
    /// <c>Gs1Code128(("01", "09501101530003"), ("10", "ABC(1)"))</c>. Use this
    /// overload when the data contains parentheses, which the element-string
    /// form cannot express. <see cref="BarcodeBuilder.Value"/> is the element
    /// string with AIs in parentheses.
    /// </summary>
    public static BarcodeBuilder Gs1Code128(params (string Ai, string Data)[] elements)
    {
        if (elements is null) throw new ArgumentNullException(nameof(elements));
        if (elements.Length == 0) throw new ArgumentException("At least one application identifier is needed.", nameof(elements));
        return new BarcodeBuilder(BarcodeType.Gs1Code128, ((string Ai, string Data)[])elements.Clone());
    }

    /// <summary>Data Matrix ECC 200 for any text (UTF-8). The smallest symbol that fits is picked automatically.</summary>
    public static BarcodeBuilder DataMatrix(string value) => Create(BarcodeType.DataMatrix, value);

    /// <summary>PDF417 for any text (UTF-8). Columns and error correction are chosen automatically unless set.</summary>
    public static BarcodeBuilder Pdf417(string value) => Create(BarcodeType.Pdf417, value);

    /// <summary>
    /// Interleaved 2 of 5 (ITF) for an even number of digits, up to <see cref="MaxItfLength"/>.
    /// Call <see cref="BarcodeBuilder.WithChecksum"/> to append the modulo 10 check digit;
    /// the value then needs an odd number of digits.
    /// </summary>
    public static BarcodeBuilder Itf(string value) => Create(BarcodeType.Itf, value);

    /// <summary>ITF-14 from 13 digits (check digit added) or 14 digits (check digit validated), for GTIN-14 on cartons.</summary>
    public static BarcodeBuilder Itf14(string value) => Create(BarcodeType.Itf14, value);

    /// <summary>
    /// Codabar for digits and <c>- $ : / . +</c>, up to <see cref="MaxCodabarLength"/> characters.
    /// Start and end the value with A, B, C or D (or T, N, * and E) to choose the start
    /// and stop characters, for example <c>A40156B</c>; otherwise A is used for both.
    /// </summary>
    public static BarcodeBuilder Codabar(string value) => Create(BarcodeType.Codabar, value);

    /// <summary>
    /// Aztec code for any text (UTF-8). The smallest compact or full-range symbol
    /// that fits is picked automatically; see <see cref="BarcodeBuilder.AztecErrorCorrection"/>.
    /// </summary>
    public static BarcodeBuilder Aztec(string value) => Create(BarcodeType.Aztec, value);

    /// <summary>
    /// MaxiCode in mode 4 for ISO-8859-1 (Latin-1) text of up to 93 characters
    /// (138 digits). Use <see cref="BarcodeBuilder.MaxiCodeMode"/> for mode 5 or 6.
    /// </summary>
    public static BarcodeBuilder MaxiCode(string value) => Create(BarcodeType.MaxiCode, value);

    /// <summary>
    /// MaxiCode with a structured carrier message, as parcel carriers use: mode 2
    /// for a numeric postal code of up to 9 digits, or mode 3 for up to 6 letters,
    /// digits and spaces. <paramref name="countryCode"/> (ISO 3166 numeric) and
    /// <paramref name="serviceClass"/> are 0 to 999. <paramref name="message"/> is the
    /// secondary message of up to 84 codewords (Latin-1), for example a
    /// <c>[)&gt;</c> ISO/IEC 15434 transport message; <see cref="BarcodeBuilder.Value"/> returns it.
    /// </summary>
    public static BarcodeBuilder MaxiCode(string postalCode, int countryCode, int serviceClass, string message)
    {
        if (postalCode is null) throw new ArgumentNullException(nameof(postalCode));
        if (message is null) throw new ArgumentNullException(nameof(message));
        Guard.InRange(countryCode, 0, 999, nameof(countryCode));
        Guard.InRange(serviceClass, 0, 999, nameof(serviceClass));
        return new BarcodeBuilder(postalCode, countryCode, serviceClass, message);
    }

    /// <summary>
    /// Micro QR code for short text: up to 35 digits, 21 alphanumeric characters
    /// (<c>0-9 A-Z</c>, space and <c>$ % * + - . / :</c>) or 15 ISO-8859-1 (Latin-1)
    /// characters at level L. The smallest of versions M1 to M4 that fits is
    /// picked; see <see cref="BarcodeBuilder.ErrorCorrection"/> for levels L, M and Q.
    /// </summary>
    public static BarcodeBuilder MicroQrCode(string value) => Create(BarcodeType.MicroQrCode, value);

    /// <summary>
    /// Rectangular Micro QR code (rMQR) for any text (UTF-8), up to 361 digits or
    /// 150 bytes. The symbol with the smallest area that fits is picked; use
    /// <see cref="BarcodeBuilder.RectangularMicroQrHeight"/> to fix the height, and
    /// <see cref="BarcodeBuilder.ErrorCorrection"/> for level M (default) or H.
    /// </summary>
    public static BarcodeBuilder RectangularMicroQrCode(string value) => Create(BarcodeType.RectangularMicroQrCode, value);

    /// <summary>
    /// GS1 DataBar Omnidirectional for a GTIN: 13 digits (check digit added),
    /// 14 digits (check digit validated), or either written as <c>(01)</c> and the
    /// digits. Printed with the human-readable text <c>(01)</c> and the 14 digits.
    /// </summary>
    public static BarcodeBuilder Gs1DataBar(string gtin) => Create(BarcodeType.Gs1DataBar, gtin);

    /// <summary>
    /// GS1 DataBar Limited for a GTIN whose first digit (the indicator) is 0 or 1:
    /// 13 digits (check digit added) or 14 digits (validated), optionally after <c>(01)</c>.
    /// </summary>
    public static BarcodeBuilder Gs1DataBarLimited(string gtin) => Create(BarcodeType.Gs1DataBarLimited, gtin);

    /// <summary>
    /// GS1 DataBar Expanded from an element string with application identifiers in
    /// parentheses, for example <c>(01)09501101530003(3103)000450(15)261231</c>,
    /// validated as for <see cref="Gs1Code128(string)"/>. Holds up to 74 digits or
    /// 41 letters.
    /// </summary>
    public static BarcodeBuilder Gs1DataBarExpanded(string value) => Create(BarcodeType.Gs1DataBarExpanded, value);

    /// <summary>
    /// GS1 DataBar Expanded from application identifier and data pairs; use it when
    /// the data contains parentheses. <see cref="BarcodeBuilder.Value"/> is the element
    /// string with AIs in parentheses.
    /// </summary>
    public static BarcodeBuilder Gs1DataBarExpanded(params (string Ai, string Data)[] elements)
    {
        if (elements is null) throw new ArgumentNullException(nameof(elements));
        if (elements.Length == 0) throw new ArgumentException("At least one application identifier is needed.", nameof(elements));
        return new BarcodeBuilder(BarcodeType.Gs1DataBarExpanded, ((string Ai, string Data)[])elements.Clone());
    }

    /// <summary>
    /// USPS Intelligent Mail barcode. <paramref name="trackingCode"/> is 20 digits:
    /// barcode identifier (2 digits, the second 0 to 4), service type (3), mailer ID
    /// and serial number (15). <paramref name="routingCode"/> is the delivery point
    /// ZIP code: empty, 5, 9 or 11 digits. <see cref="BarcodeBuilder.Value"/> is the
    /// two run together, the form <see cref="Create"/> also accepts.
    /// </summary>
    public static BarcodeBuilder IntelligentMail(string trackingCode, string routingCode = "")
    {
        if (trackingCode is null) throw new ArgumentNullException(nameof(trackingCode));
        if (routingCode is null) throw new ArgumentNullException(nameof(routingCode));
        if (trackingCode.Length != Encoding.IntelligentMailEncoder.TrackingLength)
        {
            throw new ArgumentException($"The tracking code must be {Encoding.IntelligentMailEncoder.TrackingLength} digits; it has {trackingCode.Length} characters.", nameof(trackingCode));
        }

        if (routingCode.Length is not (0 or 5 or 9 or 11))
        {
            throw new ArgumentException($"The routing code must be empty or 5, 9 or 11 digits; it has {routingCode.Length} characters.", nameof(routingCode));
        }

        return Create(BarcodeType.IntelligentMail, trackingCode + routingCode);
    }
}
