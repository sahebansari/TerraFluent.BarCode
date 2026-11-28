using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace TerraFluent.Barcode.Mcp;

/// <summary>The MCP tools: generate a barcode, generate a styled QR code, and list what is supported.</summary>
[McpServerToolType]
public sealed class BarcodeTools
{
    /// <summary>Largest logo accepted by <see cref="GenerateQrCode"/>, as base64 characters (about 768 KB of PNG).</summary>
    public const int MaxLogoBase64Length = 1024 * 1024;

    private readonly BarcodeServerSettings _settings;

    /// <summary>Creates the tools with the server's limits.</summary>
    public BarcodeTools(BarcodeServerSettings settings) => _settings = settings ?? throw new ArgumentNullException(nameof(settings));

    /// <summary>Generates any supported barcode or 2D code.</summary>
    [McpServerTool(Name = "generate_barcode", Title = "Generate barcode", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Generates a barcode or 2D code (Code 128, GS1-128, Code 39, EAN-13, EAN-8, UPC-A, QR code, Data Matrix, PDF417 and more; call list_barcode_formats for the full list and input rules). " +
                 "Returns a PNG or JPEG image, SVG markup, base64 or a data: URI. Options that do not apply to the chosen type are rejected with an explanation.")]
    public CallToolResult GenerateBarcode(
        [Description("Symbology, for example code128, gs1-128, code39, ean13, ean8, upca, qr, datamatrix, pdf417. Case, spaces, dashes and underscores are ignored.")] string type,
        [Description("The data to encode. GS1-128 takes an element string with AIs in parentheses, e.g. (01)09501101530003(10)ABC123.")] string data,
        [Description("Image format: png (default), jpeg or svg.")] string format = "png",
        [Description("How to return the result: image (default; PNG/JPEG as an image, SVG as markup), base64, or data_uri.")] string output = "image",
        [Description("Width of the narrowest bar or of one 2D cell, in pixels (1-64). Default depends on the type.")] int? moduleSize = null,
        [Description("Bar height in pixels for linear barcodes (1-2048). Default 80.")] int? height = null,
        [Description("Blank margin around the symbol, in modules (0-64). Default is the size the standard recommends.")] int? quietZone = null,
        [Description("Print the encoded text under a linear barcode.")] bool humanReadableText = false,
        [Description("Bar colour as #RRGGBB. Default #000000.")] string? foreground = null,
        [Description("Background colour as #RRGGBB. Default #FFFFFF.")] string? background = null,
        [Description("Transparent background (PNG and SVG only).")] bool transparentBackground = false,
        [Description("Code 39 only: add the optional modulo 43 check character.")] bool checksum = false,
        [Description("Code 39 only: encode full ASCII (lower case and symbols).")] bool fullAscii = false,
        [Description("EAN-13 and UPC-A only: 2 or 5 digit add-on.")] string? addOn = null,
        [Description("QR code only: error correction level L, M (default), Q or H.")] string? errorCorrection = null)
    {
        return Run(() =>
        {
            var barcodeType = ParseBarcodeType(type);
            var builder = Barcodes.Create(barcodeType, CheckData(data));
            ApplyCommon(builder, moduleSize, quietZone, foreground, background, transparentBackground);

            if (height is { } h) builder.Height(CheckRange(h, 1, BarcodeServerSettings.MaxBarHeight, nameof(height)));
            if (humanReadableText) builder.HumanReadableText();
            if (checksum) builder.WithChecksum();
            if (fullAscii) builder.FullAscii();
            if (!string.IsNullOrEmpty(addOn)) builder.AddOn(addOn);
            if (!string.IsNullOrEmpty(errorCorrection)) builder.ErrorCorrection(ParseEnum<QrErrorCorrectionLevel>(errorCorrection, nameof(errorCorrection)));

            return Render(builder, format, output);
        });
    }

    /// <summary>Generates a QR code with optional styling.</summary>
    [McpServerTool(Name = "generate_qr_code", Title = "Generate QR code", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Generates a QR code for any text, URL, Wi-Fi string (WIFI:T:WPA;S:name;P:password;;), vCard and so on, with optional styling: " +
                 "rounded or dot modules, rounded or circular eyes, colours, a gradient and a centre logo. Returns a PNG or JPEG image, SVG markup, base64 or a data: URI.")]
    public CallToolResult GenerateQrCode(
        [Description("The text to encode (UTF-8).")] string data,
        [Description("Image format: png (default), jpeg or svg.")] string format = "png",
        [Description("How to return the result: image (default; PNG/JPEG as an image, SVG as markup), base64, or data_uri.")] string output = "image",
        [Description("Error correction level: L (7%), M (15%, default), Q (25%) or H (30%). A logo needs Q or H.")] string? errorCorrection = null,
        [Description("Size of one cell in pixels (1-64). Default 4.")] int? moduleSize = null,
        [Description("Blank margin in cells (0-64). Default 4.")] int? quietZone = null,
        [Description("Cell shape: square (default), rounded, dots or connected.")] string? moduleStyle = null,
        [Description("Finder pattern (eye) shape: square (default), rounded or circle.")] string? eyeStyle = null,
        [Description("Cell colour as #RRGGBB. Default #000000.")] string? foreground = null,
        [Description("Background colour as #RRGGBB. Default #FFFFFF.")] string? background = null,
        [Description("Eye colour as #RRGGBB. Default: the foreground colour.")] string? eyeColor = null,
        [Description("Gradient start colour as #RRGGBB; needs gradientEnd. Replaces the foreground colour.")] string? gradientStart = null,
        [Description("Gradient end colour as #RRGGBB; needs gradientStart.")] string? gradientEnd = null,
        [Description("Gradient direction: horizontal, vertical, diagonal (default) or radial.")] string? gradientDirection = null,
        [Description("Transparent background (PNG and SVG only).")] bool transparentBackground = false,
        [Description("Centre logo as a base64-encoded PNG. Needs errorCorrection Q or H.")] string? logoPngBase64 = null,
        [Description("Logo width as a fraction of the QR code width, up to 0.3 (0.25 at level Q). Default 0.2.")] double? logoScale = null)
    {
        return Run(() =>
        {
            var builder = Barcodes.QrCode(CheckData(data));
            ApplyCommon(builder, moduleSize, quietZone, foreground, background, transparentBackground);

            if (!string.IsNullOrEmpty(errorCorrection)) builder.ErrorCorrection(ParseEnum<QrErrorCorrectionLevel>(errorCorrection, nameof(errorCorrection)));
            if (!string.IsNullOrEmpty(moduleStyle)) builder.ModuleStyle(ParseEnum<QrModuleStyle>(moduleStyle, nameof(moduleStyle)));
            if (!string.IsNullOrEmpty(eyeStyle)) builder.EyeStyle(ParseEnum<QrEyeStyle>(eyeStyle, nameof(eyeStyle)));
            if (!string.IsNullOrEmpty(eyeColor)) builder.EyeColor(eyeColor);

            if (!string.IsNullOrEmpty(gradientStart) || !string.IsNullOrEmpty(gradientEnd))
            {
                if (string.IsNullOrEmpty(gradientStart) || string.IsNullOrEmpty(gradientEnd))
                {
                    throw new ArgumentException("A gradient needs both gradientStart and gradientEnd.");
                }

                var direction = string.IsNullOrEmpty(gradientDirection)
                    ? GradientDirection.Diagonal
                    : ParseEnum<GradientDirection>(gradientDirection, nameof(gradientDirection));
                builder.Gradient(gradientStart, gradientEnd, direction);
            }
            else if (!string.IsNullOrEmpty(gradientDirection))
            {
                throw new ArgumentException("gradientDirection needs gradientStart and gradientEnd.");
            }

            if (!string.IsNullOrEmpty(logoPngBase64))
            {
                builder.Logo(DecodeLogo(logoPngBase64), logoScale ?? BarcodeBuilder.DefaultLogoScale);
            }
            else if (logoScale is not null)
            {
                throw new ArgumentException("logoScale needs logoPngBase64.");
            }

            return Render(builder, format, output);
        });
    }

    /// <summary>Lists the supported symbologies, image formats and limits.</summary>
    [McpServerTool(Name = "list_barcode_formats", Title = "List barcode formats", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Lists the barcode types the server can generate, what data each accepts, the image formats and output modes, and the server's size limits.")]
    public FormatCatalog ListBarcodeFormats() => FormatCatalog.Create(_settings);

    private void ApplyCommon(BarcodeBuilder builder, int? moduleSize, int? quietZone, string? foreground, string? background, bool transparentBackground)
    {
        builder.ImageSizeLimit(_settings.MaxImagePixels);
        if (moduleSize is { } m) builder.ModuleSize(CheckRange(m, 1, BarcodeServerSettings.MaxModuleSize, nameof(moduleSize)));
        if (quietZone is { } q) builder.QuietZone(CheckRange(q, 0, BarcodeServerSettings.MaxQuietZone, nameof(quietZone)));
        if (!string.IsNullOrEmpty(foreground)) builder.Foreground(foreground);
        if (!string.IsNullOrEmpty(background)) builder.Background(background);
        if (transparentBackground) builder.TransparentBackground();
    }

    private static CallToolResult Render(BarcodeBuilder builder, string format, string output)
    {
        var imageFormat = ParseImageFormat(format);
        var mode = ParseOutputMode(output);
        var (width, height) = builder.GetImageSize();
        var mime = imageFormat.ToMimeType();
        var name = FormatCatalog.DisplayName(builder.Type);

        ContentBlock payload;
        string size;
        if (imageFormat == BarcodeImageFormat.Svg && mode == OutputMode.Image)
        {
            var svg = builder.ToSvg();
            payload = new TextContentBlock { Text = svg };
            size = FormattableString.Invariant($"{svg.Length:N0} characters");
        }
        else
        {
            var bytes = builder.ToBytes(imageFormat);
            size = FormattableString.Invariant($"{bytes.Length:N0} bytes");
            payload = mode switch
            {
                OutputMode.Image => ImageContentBlock.FromBytes(bytes, mime),
                OutputMode.Base64 => new TextContentBlock { Text = Convert.ToBase64String(bytes) },
                _ => new TextContentBlock { Text = "data:" + mime + ";base64," + Convert.ToBase64String(bytes) },
            };
        }

        var summary = FormattableString.Invariant(
            $"{name} for \"{Shorten(builder.Value)}\": {width} x {height} px {imageFormat.ToString().ToUpperInvariant()} ({mime}), {size}.");

        return new CallToolResult { Content = [payload, new TextContentBlock { Text = summary }] };
    }

    // Library validation errors explain exactly what is wrong with the input, so they are passed to the
    // client as tool errors the model can act on. Anything else stays a generic server error.
    private static CallToolResult Run(Func<CallToolResult> generate)
    {
        try
        {
            return generate();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or FormatException or NotSupportedException)
        {
            throw new McpException(ex.Message, ex);
        }
    }

    private static string CheckData(string data)
    {
        if (string.IsNullOrEmpty(data)) throw new ArgumentException("data must not be empty.");
        if (data.Length > BarcodeServerSettings.MaxDataLength)
        {
            throw new ArgumentException(FormattableString.Invariant(
                $"data is {data.Length} characters; this server accepts up to {BarcodeServerSettings.MaxDataLength}."));
        }

        return data;
    }

    private static int CheckRange(int value, int min, int max, string name)
    {
        if (value < min || value > max)
        {
            throw new ArgumentException(FormattableString.Invariant($"{name} must be from {min} to {max}, but was {value}."));
        }

        return value;
    }

    private static byte[] DecodeLogo(string base64)
    {
        var comma = base64.IndexOf(',', StringComparison.Ordinal);
        if (base64.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma > 0) base64 = base64[(comma + 1)..];
        if (base64.Length > MaxLogoBase64Length)
        {
            throw new ArgumentException(FormattableString.Invariant(
                $"logoPngBase64 is {base64.Length} characters; this server accepts up to {MaxLogoBase64Length}."));
        }

        try
        {
            return Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            throw new ArgumentException("logoPngBase64 is not valid base64.");
        }
    }

    internal static BarcodeType ParseBarcodeType(string type)
    {
        var key = Normalize(type);
        if (FormatCatalog.Aliases.TryGetValue(key, out var aliased)) return aliased;
        foreach (var value in Enum.GetValues<BarcodeType>())
        {
            if (Normalize(value.ToString()) == key) return value;
        }

        throw new ArgumentException($"Unknown barcode type '{type}'. Supported types: {string.Join(", ", FormatCatalog.TypeIds)}.");
    }

    internal static BarcodeImageFormat ParseImageFormat(string format) => Normalize(format) switch
    {
        "png" => BarcodeImageFormat.Png,
        "jpeg" or "jpg" => BarcodeImageFormat.Jpeg,
        "svg" => BarcodeImageFormat.Svg,
        _ => throw new ArgumentException($"Unknown image format '{format}'. Use png, jpeg or svg."),
    };

    private static OutputMode ParseOutputMode(string output) => Normalize(output) switch
    {
        "image" or "" => OutputMode.Image,
        "base64" => OutputMode.Base64,
        "datauri" or "uri" => OutputMode.DataUri,
        _ => throw new ArgumentException($"Unknown output '{output}'. Use image, base64 or data_uri."),
    };

    private static T ParseEnum<T>(string value, string name) where T : struct, Enum
    {
        var key = Normalize(value);
        foreach (var candidate in Enum.GetValues<T>())
        {
            if (Normalize(candidate.ToString()) == key) return candidate;
        }

        var allowed = string.Join(", ", Enum.GetNames<T>().Select(n => n.ToLowerInvariant()));
        throw new ArgumentException($"Unknown {name} '{value}'. Use one of: {allowed}.");
    }

    internal static string Normalize(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var chars = new char[value.Length];
        var n = 0;
        foreach (var c in value)
        {
            if (c is '-' or '_' or ' ' or '.') continue;
            chars[n++] = char.ToLowerInvariant(c);
        }

        return new string(chars, 0, n);
    }

    private static string Shorten(string value) => value.Length <= 60 ? value : string.Concat(value.AsSpan(0, 57), "...");

    private enum OutputMode
    {
        Image,
        Base64,
        DataUri,
    }
}
