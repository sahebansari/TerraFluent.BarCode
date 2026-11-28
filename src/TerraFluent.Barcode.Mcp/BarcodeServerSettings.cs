using System.Globalization;
using System.Reflection;
using ModelContextProtocol.Protocol;

namespace TerraFluent.Barcode.Mcp;

/// <summary>Limits the server applies to every request, so a prompt cannot ask for an image that exhausts memory.</summary>
public sealed class BarcodeServerSettings
{
    /// <summary>Environment variable that overrides <see cref="MaxImagePixels"/>.</summary>
    public const string MaxImagePixelsVariable = "TERRAFLUENT_BARCODE_MCP_MAX_PIXELS";

    /// <summary>Default largest image area: 4 megapixels, for example 2048 x 2048.</summary>
    public const long DefaultMaxImagePixels = 4L * 1024 * 1024;

    /// <summary>Longest data value accepted, in characters. The encoders have their own, usually lower, capacity limits.</summary>
    public const int MaxDataLength = 4096;

    /// <summary>Largest module (bar or cell) size accepted, in pixels.</summary>
    public const int MaxModuleSize = 64;

    /// <summary>Tallest linear barcode accepted, in pixels.</summary>
    public const int MaxBarHeight = 2048;

    /// <summary>Widest quiet zone accepted, in modules.</summary>
    public const int MaxQuietZone = 64;

    /// <summary>Largest image area the server renders, in pixels.</summary>
    public long MaxImagePixels { get; }

    /// <summary>Creates settings with the given image area limit.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxImagePixels"/> is not between 1 and <see cref="BarcodeBuilder.MaxImagePixels"/>.</exception>
    public BarcodeServerSettings(long maxImagePixels = DefaultMaxImagePixels)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxImagePixels, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxImagePixels, BarcodeBuilder.MaxImagePixels);
        MaxImagePixels = maxImagePixels;
    }

    /// <summary>Reads <see cref="MaxImagePixelsVariable"/>, falling back to the default when it is unset.</summary>
    /// <exception cref="InvalidOperationException">The variable is set but is not a valid pixel count.</exception>
    public static BarcodeServerSettings FromEnvironment() => FromValue(Environment.GetEnvironmentVariable(MaxImagePixelsVariable));

    internal static BarcodeServerSettings FromValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return new BarcodeServerSettings();
        if (!long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var pixels) ||
            pixels < 1 || pixels > BarcodeBuilder.MaxImagePixels)
        {
            throw new InvalidOperationException(FormattableString.Invariant(
                $"{MaxImagePixelsVariable} must be a whole number from 1 to {BarcodeBuilder.MaxImagePixels}, but was '{value}'."));
        }

        return new BarcodeServerSettings(pixels);
    }

    /// <summary>Name and version the server reports to clients.</summary>
    public static Implementation ServerInfo { get; } = new()
    {
        Name = "terrafluent-barcode",
        Title = "TerraFluent Barcode",
        Version = typeof(BarcodeServerSettings).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "0.0.0",
        Description = "Generates barcodes and QR codes as PNG, JPEG or SVG.",
        WebsiteUrl = "https://terrafluent.dev/barcode/",
    };
}
