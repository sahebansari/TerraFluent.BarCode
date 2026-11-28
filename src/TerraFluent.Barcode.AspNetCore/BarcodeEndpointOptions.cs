namespace TerraFluent.Barcode.AspNetCore;

/// <summary>
/// Settings for the endpoint that <see cref="BarcodeEndpointRouteBuilderExtensions.MapBarcodes"/> maps.
/// Configure them with <see cref="BarcodeServiceCollectionExtensions.AddBarcodes"/>.
/// The limits exist because the endpoint renders whatever a caller asks for: keep them as
/// low as your pages need.
/// </summary>
public sealed class BarcodeEndpointOptions
{
    /// <summary>Default for <see cref="MaxImagePixels"/>: 4 megapixels, for example 2048 x 2048.</summary>
    public const long DefaultMaxImagePixels = 4L * 1024 * 1024;

    /// <summary>
    /// Rendering defaults used by the <see cref="IBarcodeGenerator"/> that
    /// <see cref="BarcodeServiceCollectionExtensions.AddBarcodes"/> registers. Query parameters
    /// override them per request.
    /// </summary>
    public BarcodeOptions Defaults { get; set; } = new();

    /// <summary>
    /// The barcode types the endpoint serves. All types are allowed at first; remove the ones
    /// you do not need. A request for any other type gets 404 Not Found.
    /// </summary>
    public ISet<BarcodeType> AllowedTypes { get; } = new HashSet<BarcodeType>(Enum.GetValues<BarcodeType>());

    /// <summary>Format used when the path has no extension and the query has no <c>format</c>. Default PNG.</summary>
    public BarcodeImageFormat DefaultFormat { get; set; } = BarcodeImageFormat.Png;

    /// <summary>Longest <c>data</c> value accepted, in characters. Default 2048.</summary>
    public int MaxDataLength { get; set; } = 2048;

    /// <summary>Largest output image, in pixels (1 to <see cref="BarcodeBuilder.MaxImagePixels"/>). Default <see cref="DefaultMaxImagePixels"/>.</summary>
    public long MaxImagePixels { get; set; } = DefaultMaxImagePixels;

    /// <summary>Largest <c>moduleSize</c> query value, in pixels. Default 64.</summary>
    public int MaxModuleSize { get; set; } = 64;

    /// <summary>Largest <c>height</c> query value, in pixels. Default 2048.</summary>
    public int MaxBarHeight { get; set; } = 2048;

    /// <summary>Largest <c>quietZone</c> query value, in modules. Default 64.</summary>
    public int MaxQuietZone { get; set; } = 64;

    /// <summary>
    /// How long browsers and proxies may reuse a response without asking again. Default one day.
    /// <see cref="TimeSpan.Zero"/> sends <c>Cache-Control: no-cache</c>, so clients revalidate
    /// every time (the ETag still saves rendering and bandwidth).
    /// </summary>
    public TimeSpan CacheMaxAge { get; set; } = TimeSpan.FromDays(1);

    /// <summary>
    /// Whether shared caches (CDNs, proxies) may store responses (<c>public</c>) or only the
    /// browser (<c>private</c>). Default <see langword="true"/>; set it to <see langword="false"/>
    /// when the endpoint requires authorization.
    /// </summary>
    public bool PublicCache { get; set; } = true;

    internal void Validate()
    {
        if (Defaults is null) throw new InvalidOperationException("BarcodeEndpointOptions.Defaults must not be null.");
        if (MaxImagePixels < 1 || MaxImagePixels > BarcodeBuilder.MaxImagePixels)
        {
            throw new InvalidOperationException(FormattableString.Invariant($"BarcodeEndpointOptions.MaxImagePixels must be from 1 to {BarcodeBuilder.MaxImagePixels}, but was {MaxImagePixels}."));
        }

        if (MaxDataLength < 1) throw new InvalidOperationException("BarcodeEndpointOptions.MaxDataLength must be at least 1.");
        if (MaxModuleSize < 1) throw new InvalidOperationException("BarcodeEndpointOptions.MaxModuleSize must be at least 1.");
        if (MaxBarHeight < 1) throw new InvalidOperationException("BarcodeEndpointOptions.MaxBarHeight must be at least 1.");
        if (MaxQuietZone < 0) throw new InvalidOperationException("BarcodeEndpointOptions.MaxQuietZone must not be negative.");
        if (CacheMaxAge < TimeSpan.Zero) throw new InvalidOperationException("BarcodeEndpointOptions.CacheMaxAge must not be negative.");
        if (!Enum.IsDefined(DefaultFormat)) throw new InvalidOperationException("BarcodeEndpointOptions.DefaultFormat is not a known format.");
    }
}
