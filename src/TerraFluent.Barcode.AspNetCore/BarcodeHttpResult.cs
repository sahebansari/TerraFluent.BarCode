using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace TerraFluent.Barcode.AspNetCore;

/// <summary>
/// An <see cref="IResult"/> that renders a barcode into the response, for minimal APIs and
/// MVC controllers. Create one with <see cref="BarcodeResultExtensions"/>.
/// </summary>
/// <remarks>
/// The barcode is rendered when the result executes. If the value cannot be encoded or the
/// image breaks a limit, the response is 400 Bad Request with a problem details body that
/// carries the library's message. Responses get <c>X-Content-Type-Options: nosniff</c>, and
/// SVG responses a <c>Content-Security-Policy</c> that blocks scripts.
/// </remarks>
public sealed class BarcodeHttpResult : IResult, IContentTypeHttpResult, IFileHttpResult, IEndpointMetadataProvider
{
    // Barcode SVG is static markup; the policy keeps an SVG opened directly from running script.
    internal const string SvgContentSecurityPolicy = "default-src 'none'; style-src 'unsafe-inline'";

    private readonly Func<HttpContext, BarcodeBuilder> _builder;

    internal BarcodeHttpResult(Func<HttpContext, BarcodeBuilder> builder, BarcodeImageFormat format, string? fileDownloadName)
    {
        if (!Enum.IsDefined(format)) throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown image format.");
        _builder = builder;
        Format = format;
        FileDownloadName = fileDownloadName;
    }

    /// <summary>The image format written.</summary>
    public BarcodeImageFormat Format { get; }

    /// <summary>The response content type, for example <c>image/png</c>.</summary>
    public string ContentType => Format.ToMimeType();

    /// <summary>
    /// When set, the response asks the browser to save the image under this name
    /// (<c>Content-Disposition: attachment</c>) instead of showing it.
    /// </summary>
    public string? FileDownloadName { get; }

    /// <inheritdoc/>
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        byte[] body;
        try
        {
            body = _builder(httpContext).ToBytes(Format);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or FormatException or NotSupportedException)
        {
            httpContext.Response.Headers.CacheControl = "no-store";
            await Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest).ExecuteAsync(httpContext).ConfigureAwait(false);
            return;
        }

        var response = httpContext.Response;
        response.StatusCode = StatusCodes.Status200OK;
        SetContentHeaders(response, Format, FileDownloadName);
        response.ContentLength = body.Length;
        if (HttpMethods.IsHead(httpContext.Request.Method)) return;
        await response.Body.WriteAsync(body, httpContext.RequestAborted).ConfigureAwait(false);
    }

    /// <summary>Describes the result for OpenAPI when an endpoint returns <see cref="BarcodeHttpResult"/>.</summary>
    /// <param name="method">The endpoint's handler method.</param>
    /// <param name="builder">The endpoint builder.</param>
    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(byte[]), ["image/png", "image/jpeg", "image/svg+xml"]));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status400BadRequest, typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), ["application/problem+json"]));
    }

    internal static void SetContentHeaders(HttpResponse response, BarcodeImageFormat format, string? fileDownloadName)
    {
        response.ContentType = format.ToMimeType();
        response.Headers.XContentTypeOptions = "nosniff";
        if (format == BarcodeImageFormat.Svg) response.Headers.ContentSecurityPolicy = SvgContentSecurityPolicy;
        if (!string.IsNullOrEmpty(fileDownloadName))
        {
            var disposition = new ContentDispositionHeaderValue("attachment");
            disposition.SetHttpFileName(fileDownloadName);
            response.Headers.ContentDisposition = disposition.ToString();
        }
    }

    internal static IBarcodeGenerator ResolveGenerator(HttpContext context) =>
        context.RequestServices?.GetService<IBarcodeGenerator>() ?? DefaultGenerator;

    private static readonly BarcodeGenerator DefaultGenerator = new();
}
