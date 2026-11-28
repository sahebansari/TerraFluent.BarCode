using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace TerraFluent.Barcode.AspNetCore.Tests;

public class EndpointTests
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    [Fact]
    public async Task Serves_a_png_identical_to_the_library_output()
    {
        await using var app = await TestApp.StartEndpointAsync();
        var response = await app.Client.GetAsync("/barcodes/code128.png?data=ABC-123");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal(PngSignature, body[..8]);
        Assert.Equal(Barcodes.Code128("ABC-123").ToPng(), body);
        Assert.Equal(body.Length, response.Content.Headers.ContentLength);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
    }

    [Theory]
    [InlineData("/barcodes/qr.svg?data=hello", "image/svg+xml")]
    [InlineData("/barcodes/qr.jpg?data=hello", "image/jpeg")]
    [InlineData("/barcodes/qr.jpeg?data=hello", "image/jpeg")]
    [InlineData("/barcodes/qr?data=hello&format=svg", "image/svg+xml")]
    [InlineData("/barcodes/QR-Code?data=hello", "image/png")]
    [InlineData("/barcodes/gs1-128.png?data=(01)09501101530003", "image/png")]
    [InlineData("/barcodes/data_matrix.svg?data=hello", "image/svg+xml")]
    public async Task Reads_type_and_format_from_the_path_or_query(string url, string contentType)
    {
        await using var app = await TestApp.StartEndpointAsync();
        var response = await app.Client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(contentType, response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Svg_responses_carry_a_script_blocking_policy()
    {
        await using var app = await TestApp.StartEndpointAsync();
        var response = await app.Client.GetAsync("/barcodes/qr.svg?data=hello");

        Assert.Equal(Barcodes.QrCode("hello").ToSvg(), await response.Content.ReadAsStringAsync());
        Assert.Contains("default-src 'none'", response.Headers.GetValues("Content-Security-Policy").Single(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Applies_query_options()
    {
        await using var app = await TestApp.StartEndpointAsync();
        var response = await app.Client.GetAsync("/barcodes/code128.png?data=ABC&moduleSize=3&height=40&quietZone=5&text=true&foreground=%23112233&background=eeeeee");

        var expected = Barcodes.Code128("ABC").ModuleSize(3).Height(40).QuietZone(5).HumanReadableText()
            .Foreground("#112233").Background("#EEEEEE").ToPng();
        Assert.Equal(expected, await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Applies_qr_error_correction_and_kebab_case_names()
    {
        await using var app = await TestApp.StartEndpointAsync();
        var response = await app.Client.GetAsync("/barcodes/qr.png?data=hello&error-correction=h&module-size=2");

        Assert.Equal(Barcodes.QrCode("hello").ErrorCorrection(QrErrorCorrectionLevel.H).ModuleSize(2).ToPng(), await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Uses_configured_defaults_and_lets_false_switch_them_off()
    {
        await using var app = await TestApp.StartEndpointAsync(o => o.Defaults = new BarcodeOptions { ModuleSize = 3, HumanReadableText = true });

        var withDefaults = await app.Client.GetByteArrayAsync("/barcodes/code128.png?data=ABC");
        Assert.Equal(Barcodes.Code128("ABC").ModuleSize(3).HumanReadableText().ToPng(), withDefaults);

        var textOff = await app.Client.GetByteArrayAsync("/barcodes/code128.png?data=ABC&text=false");
        Assert.Equal(Barcodes.Code128("ABC").ModuleSize(3).ToPng(), textOff);

        // text=false on a 2D code is a no-op rather than an error.
        var qr = await app.Client.GetAsync("/barcodes/qr.png?data=ABC&text=false");
        Assert.Equal(HttpStatusCode.OK, qr.StatusCode);
    }

    [Fact]
    public async Task Sends_cache_headers_and_answers_if_none_match_with_304()
    {
        await using var app = await TestApp.StartEndpointAsync();
        var first = await app.Client.GetAsync("/barcodes/qr.png?data=hello");
        var etag = first.Headers.ETag;

        Assert.NotNull(etag);
        Assert.False(etag.IsWeak);
        Assert.True(first.Headers.CacheControl?.Public);
        Assert.Equal(TimeSpan.FromDays(1), first.Headers.CacheControl?.MaxAge);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/barcodes/qr.png?data=hello");
        request.Headers.IfNoneMatch.Add(etag);
        var second = await app.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotModified, second.StatusCode);
        Assert.Equal(etag, second.Headers.ETag);
        Assert.Empty(await second.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task ETag_changes_with_any_input()
    {
        await using var app = await TestApp.StartEndpointAsync();
        string?[] urls =
        [
            "/barcodes/qr.png?data=hello",
            "/barcodes/qr.png?data=hello2",
            "/barcodes/qr.svg?data=hello",
            "/barcodes/qr.png?data=hello&moduleSize=5",
            "/barcodes/qr.png?data=hello&errorCorrection=H",
            "/barcodes/datamatrix.png?data=hello",
        ];
        var etags = new List<string>();
        foreach (var url in urls) etags.Add((await app.Client.GetAsync(url)).Headers.ETag!.Tag);

        Assert.Equal(etags.Count, etags.Distinct().Count());

        // Equivalent spellings give the same tag.
        var same = (await app.Client.GetAsync("/barcodes/QR?data=hello&format=PNG")).Headers.ETag!.Tag;
        Assert.Equal(etags[0], same);
    }

    [Fact]
    public async Task Cache_settings_are_configurable()
    {
        await using var app = await TestApp.StartEndpointAsync(o =>
        {
            o.CacheMaxAge = TimeSpan.FromMinutes(5);
            o.PublicCache = false;
        });
        var response = await app.Client.GetAsync("/barcodes/qr.png?data=hello");
        Assert.True(response.Headers.CacheControl?.Private);
        Assert.False(response.Headers.CacheControl?.Public);
        Assert.Equal(TimeSpan.FromMinutes(5), response.Headers.CacheControl?.MaxAge);

        await using var noCache = await TestApp.StartEndpointAsync(o => o.CacheMaxAge = TimeSpan.Zero);
        var revalidate = await noCache.Client.GetAsync("/barcodes/qr.png?data=hello");
        Assert.True(revalidate.Headers.CacheControl?.NoCache);
    }

    [Fact]
    public async Task Head_returns_headers_without_a_body()
    {
        await using var app = await TestApp.StartEndpointAsync();
        using var request = new HttpRequestMessage(HttpMethod.Head, "/barcodes/qr.png?data=hello");
        var response = await app.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(Barcodes.QrCode("hello").ToPng().Length, response.Content.Headers.ContentLength);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
    }

    [Theory]
    [InlineData("/barcodes/qr.png", "'data' query parameter is required")]
    [InlineData("/barcodes/qr.gif?data=x", "Unknown image format 'gif'")]
    [InlineData("/barcodes/qr?data=x&format=bmp", "Unknown image format 'bmp'")]
    [InlineData("/barcodes/qr.png?data=x&moduleSize=0", "'moduleSize' must be a whole number from 1 to 64")]
    [InlineData("/barcodes/qr.png?data=x&moduleSize=65", "from 1 to 64")]
    [InlineData("/barcodes/qr.png?data=x&moduleSize=abc", "but was 'abc'")]
    [InlineData("/barcodes/code128.png?data=x&height=5000", "'height' must be a whole number from 1 to 2048")]
    [InlineData("/barcodes/qr.png?data=x&quietZone=-1", "'quietZone'")]
    [InlineData("/barcodes/qr.png?data=x&foreground=red", "'foreground' must be a colour")]
    [InlineData("/barcodes/qr.png?data=x&text=maybe", "'text' must be true or false")]
    [InlineData("/barcodes/qr.png?data=x&errorCorrection=Z", "Unknown errorCorrection 'Z'")]
    [InlineData("/barcodes/qr.png?data=x&data=y", "given 2 times")]
    [InlineData("/barcodes/ean13.png?data=ABC", "EAN-13")]
    [InlineData("/barcodes/qr.png?data=x&height=40", "Height applies to linear barcodes only")]
    [InlineData("/barcodes/code128.png?data=x&errorCorrection=H", "ErrorCorrection applies to QR codes, Micro QR and rMQR only")]
    [InlineData("/barcodes/qr.png?data=x&text=true", "HumanReadableText applies to linear barcodes only")]
    public async Task Rejects_bad_input_with_problem_details(string url, string message)
    {
        await using var app = await TestApp.StartEndpointAsync();
        var response = await app.Client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.Null(response.Headers.ETag);
        var detail = await ReadDetailAsync(response);
        Assert.Contains(message, detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Unknown_or_disallowed_types_are_not_found()
    {
        await using var app = await TestApp.StartEndpointAsync(o =>
        {
            o.AllowedTypes.Clear();
            o.AllowedTypes.Add(BarcodeType.QrCode);
        });

        var unknown = await app.Client.GetAsync("/barcodes/nope.png?data=x");
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Contains("This endpoint serves: QrCode.", await ReadDetailAsync(unknown), StringComparison.Ordinal);

        var disallowed = await app.Client.GetAsync("/barcodes/code128.png?data=x");
        Assert.Equal(HttpStatusCode.NotFound, disallowed.StatusCode);

        var allowed = await app.Client.GetAsync("/barcodes/qr.png?data=x");
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    [Fact]
    public async Task Enforces_data_length_and_image_size_limits()
    {
        await using var app = await TestApp.StartEndpointAsync(o =>
        {
            o.MaxDataLength = 10;
            o.MaxImagePixels = 100 * 100;
        });

        var tooLong = await app.Client.GetAsync("/barcodes/qr.png?data=12345678901");
        Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);
        Assert.Contains("accepts up to 10", await ReadDetailAsync(tooLong), StringComparison.Ordinal);

        var tooBig = await app.Client.GetAsync("/barcodes/qr.png?data=hello&moduleSize=20");
        Assert.Equal(HttpStatusCode.BadRequest, tooBig.StatusCode);

        var fits = await app.Client.GetAsync("/barcodes/qr.png?data=hello&moduleSize=2");
        Assert.Equal(HttpStatusCode.OK, fits.StatusCode);
    }

    [Fact]
    public async Task Default_image_limit_is_four_megapixels()
    {
        await using var app = await TestApp.StartEndpointAsync();
        // Version 40 would be 177 + 8 = 185 modules; at 64 px that is about 140 MP.
        var response = await app.Client.GetAsync("/barcodes/qr.png?data=" + new string('A', 2000) + "&moduleSize=64");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Maps_under_a_custom_prefix_and_returns_a_convention_builder()
    {
        await using var app = await TestApp.StartAsync(a => a.MapBarcodes("/api/codes/").WithMetadata("marker"), s => s.AddBarcodes());
        var response = await app.Client.GetAsync("/api/codes/qr.png?data=x");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Works_without_AddBarcodes()
    {
        await using var app = await TestApp.StartAsync(a => a.MapBarcodes());
        var response = await app.Client.GetAsync("/barcodes/qr.png?data=x");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Rejects_invalid_options_when_mapping()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => TestApp.StartEndpointAsync(o => o.MaxImagePixels = 0));
        await Assert.ThrowsAsync<InvalidOperationException>(() => TestApp.StartEndpointAsync(o => o.MaxImagePixels = BarcodeBuilder.MaxImagePixels + 1));
        await Assert.ThrowsAsync<InvalidOperationException>(() => TestApp.StartEndpointAsync(o => o.CacheMaxAge = TimeSpan.FromSeconds(-1)));
    }

    [Fact]
    public async Task Uses_a_registered_generator()
    {
        await using var app = await TestApp.StartAsync(
            a => a.MapBarcodes(),
            s => s.AddSingleton<IBarcodeGenerator>(new BarcodeGenerator(new BarcodeOptions { ModuleSize = 7 })).AddBarcodes());
        var body = await app.Client.GetByteArrayAsync("/barcodes/qr.png?data=x");
        Assert.Equal(Barcodes.QrCode("x").ModuleSize(7).ToPng(), body);
    }

    internal static async Task<string> ReadDetailAsync(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("detail").GetString() ?? string.Empty;
    }
}
