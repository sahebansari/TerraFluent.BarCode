using System.Buffers.Text;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace TerraFluent.Barcode.AspNetCore;

/// <summary>Request handling for <see cref="BarcodeEndpointRouteBuilderExtensions.MapBarcodes"/>.</summary>
internal sealed class BarcodeEndpointHandler
{
    private static readonly string LibraryVersion =
        typeof(Barcodes).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? string.Empty;

    private readonly BarcodeEndpointOptions _options;
    private readonly BarcodeGenerator _fallbackGenerator;
    private readonly string _cacheControl;
    private readonly string _fingerprint;
    private readonly string _allowedTypes;

    public BarcodeEndpointHandler(BarcodeEndpointOptions options)
    {
        _options = options;
        _fallbackGenerator = new BarcodeGenerator(options.Defaults);
        var maxAge = (long)options.CacheMaxAge.TotalSeconds;
        _cacheControl = maxAge <= 0
            ? "no-cache"
            : (options.PublicCache ? "public" : "private") + ", max-age=" + maxAge.ToString(CultureInfo.InvariantCulture);
        _fingerprint = LibraryVersion + "|" + Fingerprint(options.Defaults);
        _allowedTypes = string.Join(", ", options.AllowedTypes.Order().Select(t => t.ToString()));
    }

    public async Task HandleAsync(HttpContext context)
    {
        if (!TryParseRequest(context, out var request, out var status, out var error))
        {
            await Problem(context, status, error).ConfigureAwait(false);
            return;
        }

        var etag = new EntityTagHeaderValue(ComputeETag(request));
        if (IsNotModified(context.Request, etag))
        {
            SetCacheHeaders(context.Response, etag);
            context.Response.StatusCode = StatusCodes.Status304NotModified;
            return;
        }

        byte[] body;
        try
        {
            var generator = context.RequestServices.GetService<IBarcodeGenerator>() ?? _fallbackGenerator;
            var builder = generator.Create(request.Type, request.Data);
            Apply(builder, request);
            builder.ImageSizeLimit(_options.MaxImagePixels);
            body = builder.ToBytes(request.Format);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or FormatException or NotSupportedException)
        {
            // The library's validation messages say exactly what is wrong with the input.
            await Problem(context, StatusCodes.Status400BadRequest, ex.Message).ConfigureAwait(false);
            return;
        }

        var response = context.Response;
        response.StatusCode = StatusCodes.Status200OK;
        BarcodeHttpResult.SetContentHeaders(response, request.Format, fileDownloadName: null);
        SetCacheHeaders(response, etag);
        response.ContentLength = body.Length;
        if (HttpMethods.IsHead(context.Request.Method)) return;
        await response.Body.WriteAsync(body, context.RequestAborted).ConfigureAwait(false);
    }

    private bool TryParseRequest(HttpContext context, out BarcodeRequest request, out int status, out string error)
    {
        request = default;
        status = StatusCodes.Status400BadRequest;
        var query = context.Request.Query;

        var typeText = context.GetRouteValue("type") as string;
        if (!BarcodeParsing.TryParseType(typeText, out var type) || !_options.AllowedTypes.Contains(type))
        {
            status = StatusCodes.Status404NotFound;
            error = $"Unknown barcode type '{typeText}'. This endpoint serves: {_allowedTypes}.";
            return false;
        }

        var format = _options.DefaultFormat;
        var formatText = context.GetRouteValue("format") as string;
        if (string.IsNullOrEmpty(formatText) && !TryGetSingle(query, "format", out formatText, out error)) return false;
        if (!string.IsNullOrEmpty(formatText) && !BarcodeParsing.TryParseFormat(formatText, out format))
        {
            error = $"Unknown image format '{formatText}'. Use png, jpg or svg.";
            return false;
        }

        if (!TryGetSingle(query, "data", out var data, out error)) return false;
        if (string.IsNullOrEmpty(data))
        {
            error = "The 'data' query parameter is required.";
            return false;
        }

        if (data.Length > _options.MaxDataLength)
        {
            error = FormattableString.Invariant($"'data' is {data.Length} characters; this endpoint accepts up to {_options.MaxDataLength}.");
            return false;
        }

        if (!TryGetInt(query, "moduleSize", 1, _options.MaxModuleSize, out var moduleSize, out error)) return false;
        if (!TryGetInt(query, "height", 1, _options.MaxBarHeight, out var height, out error)) return false;
        if (!TryGetInt(query, "quietZone", 0, _options.MaxQuietZone, out var quietZone, out error)) return false;
        if (!TryGetColor(query, "foreground", out var foreground, out error)) return false;
        if (!TryGetColor(query, "background", out var background, out error)) return false;
        if (!TryGetBool(query, "transparent", out var transparent, out error)) return false;
        if (!TryGetBool(query, "text", out var text, out error)) return false;
        if (!TryGetBool(query, "checksum", out var checksum, out error)) return false;
        if (!TryGetSingle(query, "errorCorrection", out var eccText, out error)) return false;
        QrErrorCorrectionLevel? errorCorrection = null;
        if (!string.IsNullOrEmpty(eccText))
        {
            if (!BarcodeParsing.TryParseEnum<QrErrorCorrectionLevel>(eccText, out var level))
            {
                error = $"Unknown errorCorrection '{eccText}'. Use L, M, Q or H.";
                return false;
            }

            errorCorrection = level;
        }

        request = new BarcodeRequest(type, format, data, moduleSize, height, quietZone, foreground, background, transparent, text, checksum, errorCorrection);
        return true;
    }

    private static void Apply(BarcodeBuilder builder, BarcodeRequest r)
    {
        if (r.ModuleSize is { } moduleSize) builder.ModuleSize(moduleSize);
        if (r.Height is { } height) builder.Height(height);
        if (r.QuietZone is { } quietZone) builder.QuietZone(quietZone);
        if (r.Foreground is { } foreground) builder.Foreground(foreground);
        if (r.Background is { } background) builder.Background(background);
        if (r.Transparent is { } transparent && transparent) builder.TransparentBackground();
        // true asks for something and must apply to the type; false only switches a default off where it can be on.
        if (r.Text == true || (r.Text == false && BarcodeParsing.IsLinear(builder.Type))) builder.HumanReadableText(r.Text.Value);
        if (r.Checksum == true || (r.Checksum == false && builder.Type is BarcodeType.Code39 or BarcodeType.Itf)) builder.WithChecksum(r.Checksum.Value);
        if (r.ErrorCorrection is { } level) builder.ErrorCorrection(level);
    }

    private string ComputeETag(BarcodeRequest r)
    {
        // Rendering is deterministic, so the inputs identify the output. The data goes last and
        // length-prefixed so no value can imitate another field.
        var key = new StringBuilder(128 + r.Data.Length)
            .Append(_fingerprint).Append('|')
            .Append(r.Type).Append('|').Append(r.Format).Append('|')
            .Append(r.ModuleSize).Append('|').Append(r.Height).Append('|').Append(r.QuietZone).Append('|')
            .Append(r.Foreground?.ToHex()).Append('|').Append(r.Background?.ToHex()).Append('|')
            .Append(r.Transparent).Append('|').Append(r.Text).Append('|').Append(r.Checksum).Append('|')
            .Append(r.ErrorCorrection).Append('|')
            .Append(r.Data.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(r.Data)
            .ToString();
        Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(key), hash);
        return "\"" + Base64Url.EncodeToString(hash[..18]) + "\"";
    }

    private static bool IsNotModified(HttpRequest request, EntityTagHeaderValue etag)
    {
        if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method)) return false;
        var ifNoneMatch = request.GetTypedHeaders().IfNoneMatch;
        foreach (var candidate in ifNoneMatch)
        {
            if (candidate.Equals(EntityTagHeaderValue.Any) || candidate.Compare(etag, useStrongComparison: false)) return true;
        }

        return false;
    }

    private void SetCacheHeaders(HttpResponse response, EntityTagHeaderValue etag)
    {
        response.Headers.ETag = etag.ToString();
        response.Headers.CacheControl = _cacheControl;
    }

    private static Task Problem(HttpContext context, int status, string detail)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Problem(detail: detail, statusCode: status).ExecuteAsync(context);
    }

    private static bool TryGetSingle(IQueryCollection query, string name, out string? value, out string error)
    {
        error = string.Empty;
        var values = GetValues(query, name);
        if (values.Count > 1)
        {
            value = null;
            error = $"The '{name}' query parameter was given {values.Count} times.";
            return false;
        }

        value = values.Count == 1 ? values[0] : null;
        return true;
    }

    // Query keys are case-insensitive already; this also accepts module-size and module_size for moduleSize.
    private static StringValues GetValues(IQueryCollection query, string name)
    {
        if (query.TryGetValue(name, out var values)) return values;
        var key = BarcodeParsing.Normalize(name);
        foreach (var pair in query)
        {
            if (BarcodeParsing.Normalize(pair.Key) == key) return pair.Value;
        }

        return StringValues.Empty;
    }

    private static bool TryGetInt(IQueryCollection query, string name, int min, int max, out int? value, out string error)
    {
        value = null;
        if (!TryGetSingle(query, name, out var text, out error) || string.IsNullOrEmpty(text)) return error.Length == 0;
        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var number) || number < min || number > max)
        {
            error = FormattableString.Invariant($"'{name}' must be a whole number from {min} to {max}, but was '{text}'.");
            return false;
        }

        value = number;
        return true;
    }

    private static bool TryGetBool(IQueryCollection query, string name, out bool? value, out string error)
    {
        value = null;
        var present = GetValues(query, name).Count > 0;
        if (!TryGetSingle(query, name, out var text, out error)) return false;
        if (!present) return true;
        switch (text?.Trim().ToLowerInvariant())
        {
            case "" or null or "true" or "1" or "yes" or "on":
                value = true;
                return true;
            case "false" or "0" or "no" or "off":
                value = false;
                return true;
            default:
                error = $"'{name}' must be true or false, but was '{text}'.";
                return false;
        }
    }

    private static bool TryGetColor(IQueryCollection query, string name, out BarcodeColor? value, out string error)
    {
        value = null;
        if (!TryGetSingle(query, name, out var text, out error) || string.IsNullOrEmpty(text)) return error.Length == 0;
        try
        {
            value = BarcodeColor.FromHex(text);
            return true;
        }
        catch (FormatException)
        {
            error = $"'{name}' must be a colour as RRGGBB or RGB, optionally after %23 (#), but was '{text}'.";
            return false;
        }
    }

    private static string Fingerprint(BarcodeOptions defaults)
    {
        var sb = new StringBuilder();
        foreach (var property in typeof(BarcodeOptions).GetProperties(BindingFlags.Public | BindingFlags.Instance).OrderBy(p => p.Name, StringComparer.Ordinal))
        {
            sb.Append(property.Name).Append('=').Append(Convert.ToString(property.GetValue(defaults), CultureInfo.InvariantCulture)).Append(';');
        }

        return sb.ToString();
    }

    private readonly record struct BarcodeRequest(
        BarcodeType Type,
        BarcodeImageFormat Format,
        string Data,
        int? ModuleSize,
        int? Height,
        int? QuietZone,
        BarcodeColor? Foreground,
        BarcodeColor? Background,
        bool? Transparent,
        bool? Text,
        bool? Checksum,
        QrErrorCorrectionLevel? ErrorCorrection);
}
