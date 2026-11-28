namespace TerraFluent.Barcode.Mcp;

/// <summary>What the server can generate, as returned by the <c>list_barcode_formats</c> tool.</summary>
/// <param name="BarcodeTypes">Every supported symbology.</param>
/// <param name="ImageFormats">Image formats: png, jpeg, svg.</param>
/// <param name="OutputModes">How a result can be returned: image, base64, data_uri.</param>
/// <param name="Limits">The server's size limits.</param>
public sealed record FormatCatalog(
    IReadOnlyList<BarcodeTypeInfo> BarcodeTypes,
    IReadOnlyList<string> ImageFormats,
    IReadOnlyList<string> OutputModes,
    ServerLimits Limits)
{
    private static readonly Dictionary<BarcodeType, (string Id, string Name, string Kind, string Input)> Known = new()
    {
        [BarcodeType.Code128] = ("code128", "Code 128", "linear", "ASCII 0-127, up to 256 characters. Code sets chosen automatically."),
        [BarcodeType.Gs1Code128] = ("gs1-128", "GS1-128", "linear", "GS1 element string with AIs in parentheses, e.g. (01)09501101530003(17)250101(10)ABC123. Each AI's format and check digit is validated."),
        [BarcodeType.Code39] = ("code39", "Code 39", "linear", "0-9, A-Z, space and - . $ / + %, up to 128 characters. Optional checksum; fullAscii for lower case and symbols."),
        [BarcodeType.Ean13] = ("ean13", "EAN-13", "linear", "12 digits (check digit added) or 13 digits (check digit validated). Optional 2 or 5 digit add-on."),
        [BarcodeType.Ean8] = ("ean8", "EAN-8", "linear", "7 digits (check digit added) or 8 digits (check digit validated)."),
        [BarcodeType.UpcA] = ("upca", "UPC-A", "linear", "11 digits (check digit added) or 12 digits (check digit validated). Optional 2 or 5 digit add-on."),
        [BarcodeType.QrCode] = ("qr", "QR code", "2d", "Any text (UTF-8), up to about 2,900 bytes at level L. Use generate_qr_code for styling."),
        [BarcodeType.DataMatrix] = ("datamatrix", "Data Matrix", "2d", "Any text (UTF-8), up to about 1,500 characters."),
        [BarcodeType.Pdf417] = ("pdf417", "PDF417", "stacked", "Any text (UTF-8), up to about 1,800 characters."),
        [BarcodeType.Itf] = ("itf", "Interleaved 2 of 5", "linear", "An even number of digits, up to 80. With checksum, an odd number (the modulo 10 check digit is appended)."),
        [BarcodeType.Itf14] = ("itf14", "ITF-14", "linear", "13 digits (check digit added) or 14 digits (check digit validated): a GTIN-14 for cartons."),
        [BarcodeType.Codabar] = ("codabar", "Codabar", "linear", "0-9 and - $ : / . +, up to 60 characters. Start and end with A, B, C or D to choose the start/stop characters; otherwise A is used."),
        [BarcodeType.Aztec] = ("aztec", "Aztec", "2d", "Any text (UTF-8), up to about 1,900 bytes. Optional aztecErrorCorrection percent (5-90, default 23)."),
        [BarcodeType.MaxiCode] = ("maxicode", "MaxiCode", "2d", "Latin-1 text, up to 93 characters (138 digits); mode 4 by default, maxiCodeMode 5 or 6. For parcels, give maxiCodePostalCode, maxiCodeCountry and maxiCodeService (modes 2/3)."),
    };

    /// <summary>Extra spellings accepted for a type, keyed by their normalized form.</summary>
    internal static readonly IReadOnlyDictionary<string, BarcodeType> Aliases = new Dictionary<string, BarcodeType>
    {
        ["qr"] = BarcodeType.QrCode,
        ["gs1128"] = BarcodeType.Gs1Code128,
        ["ean128"] = BarcodeType.Gs1Code128,
        ["upc"] = BarcodeType.UpcA,
        ["dm"] = BarcodeType.DataMatrix,
        ["interleaved2of5"] = BarcodeType.Itf,
        ["i2of5"] = BarcodeType.Itf,
        ["nw7"] = BarcodeType.Codabar,
        ["azteccode"] = BarcodeType.Aztec,
    };

    /// <summary>The type ids shown to clients.</summary>
    internal static IEnumerable<string> TypeIds => Enum.GetValues<BarcodeType>().Select(Id);

    internal static string DisplayName(BarcodeType type) => Known.TryGetValue(type, out var k) ? k.Name : type.ToString();

    private static string Id(BarcodeType type) => Known.TryGetValue(type, out var k) ? k.Id : BarcodeTools.Normalize(type.ToString());

    internal static FormatCatalog Create(BarcodeServerSettings settings)
    {
        // Types added to the library later are listed automatically, with a generic description until one is written here.
        var types = Enum.GetValues<BarcodeType>()
            .Select(t => Known.TryGetValue(t, out var k)
                ? new BarcodeTypeInfo(k.Id, k.Name, k.Kind, k.Input)
                : new BarcodeTypeInfo(Id(t), t.ToString(), "unknown", "See https://terrafluent.dev/barcode/ for the accepted input."))
            .ToList();

        return new FormatCatalog(
            types,
            ["png", "jpeg", "svg"],
            ["image", "base64", "data_uri"],
            new ServerLimits(
                settings.MaxImagePixels,
                BarcodeServerSettings.MaxDataLength,
                BarcodeServerSettings.MaxModuleSize,
                BarcodeServerSettings.MaxBarHeight,
                BarcodeServerSettings.MaxQuietZone));
    }
}

/// <summary>One supported symbology.</summary>
/// <param name="Id">Value to pass as <c>type</c> to <c>generate_barcode</c>.</param>
/// <param name="Name">Display name.</param>
/// <param name="Kind">linear, stacked or 2d.</param>
/// <param name="Input">What data the type accepts.</param>
public sealed record BarcodeTypeInfo(string Id, string Name, string Kind, string Input);

/// <summary>The server's size limits.</summary>
/// <param name="MaxImagePixels">Largest image area (width x height) in pixels.</param>
/// <param name="MaxDataLength">Longest data value in characters.</param>
/// <param name="MaxModuleSize">Largest module size in pixels.</param>
/// <param name="MaxBarHeight">Tallest linear barcode in pixels.</param>
/// <param name="MaxQuietZone">Widest quiet zone in modules.</param>
public sealed record ServerLimits(long MaxImagePixels, int MaxDataLength, int MaxModuleSize, int MaxBarHeight, int MaxQuietZone);
