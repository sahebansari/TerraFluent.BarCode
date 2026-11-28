using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace TerraFluent.Barcode.AspNetCore;

/// <summary>Maps the barcode endpoint.</summary>
public static class BarcodeEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps <c>GET</c> and <c>HEAD</c> on <c>{pattern}/{type}.{format?}</c>, which renders the
    /// barcode for the <c>data</c> query parameter, for example
    /// <c>/barcodes/qr.svg?data=https%3A%2F%2Fexample.com</c> or <c>/barcodes/code128.png?data=ABC-123&amp;text=true</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>type</c> is a <see cref="BarcodeType"/> name or alias such as <c>code128</c>, <c>gs1-128</c>,
    /// <c>ean13</c>, <c>qr</c> or <c>datamatrix</c> (case, dashes and underscores are ignored).
    /// <c>format</c> is <c>png</c>, <c>jpg</c> or <c>svg</c>; without it the <c>format</c> query
    /// parameter or <see cref="BarcodeEndpointOptions.DefaultFormat"/> applies.
    /// </para>
    /// <para>
    /// Optional query parameters: <c>moduleSize</c>, <c>height</c>, <c>quietZone</c>,
    /// <c>foreground</c> and <c>background</c> (<c>RRGGBB</c> or <c>%23RRGGBB</c>),
    /// <c>transparent</c>, <c>text</c> (human-readable text), <c>errorCorrection</c> (QR: L, M, Q, H)
    /// and <c>checksum</c> (Code 39, ITF). Invalid input, and options that do not apply to the
    /// type, get 400 Bad Request with a problem details body.
    /// </para>
    /// <para>
    /// Responses carry an ETag and <c>Cache-Control</c> from <see cref="BarcodeEndpointOptions"/>;
    /// a matching <c>If-None-Match</c> gets 304 Not Modified without rendering. Rendering is
    /// CPU-bound, so consider <c>RequireRateLimiting</c> on the returned builder for public sites.
    /// </para>
    /// </remarks>
    /// <param name="endpoints">The route builder.</param>
    /// <param name="pattern">The route prefix. Default <c>/barcodes</c>.</param>
    /// <returns>A builder for adding authorization, rate limiting, CORS and other conventions.</returns>
    public static IEndpointConventionBuilder MapBarcodes(this IEndpointRouteBuilder endpoints, string pattern = "/barcodes")
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(pattern);

        var options = endpoints.ServiceProvider.GetService<IOptions<BarcodeEndpointOptions>>()?.Value ?? new BarcodeEndpointOptions();
        options.Validate();
        var handler = new BarcodeEndpointHandler(options);

        var route = pattern.TrimEnd('/') + "/{type}.{format?}";
        return endpoints.MapMethods(route, ["GET", "HEAD"], handler.HandleAsync)
            .WithDisplayName("TerraFluent.Barcode " + route);
    }
}
