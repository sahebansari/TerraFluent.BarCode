using Microsoft.AspNetCore.Routing;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace TerraFluent.Barcode.AspNetCore.Tests;

public class ResultTests
{
    [Fact]
    public async Task Results_extension_renders_with_the_registered_generator()
    {
        await using var app = await TestApp.StartAsync(
            a => a.MapGet("/labels/{sku}", (string sku) => Results.Extensions.Barcode(BarcodeType.Code128, sku)),
            s => s.AddBarcodes(o => o.Defaults = new BarcodeOptions { ModuleSize = 3 }));

        var response = await app.Client.GetAsync("/labels/SKU-42");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(Barcodes.Code128("SKU-42").ModuleSize(3).ToPng(), await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Per_call_options_replace_the_defaults()
    {
        await using var app = await TestApp.StartAsync(
            a => a.MapGet("/qr", () => Results.Extensions.Barcode(BarcodeType.QrCode, "x", BarcodeImageFormat.Svg, new BarcodeOptions { ModuleSize = 9 })),
            s => s.AddBarcodes(o => o.Defaults = new BarcodeOptions { ModuleSize = 3 }));

        Assert.Equal(Barcodes.QrCode("x").ModuleSize(9).ToSvg(), await app.Client.GetStringAsync("/qr"));
    }

    [Fact]
    public async Task Builder_results_work_without_any_registration()
    {
        await using var app = await TestApp.StartAsync(a =>
        {
            a.MapGet("/a", () => Barcodes.QrCode("hello").ModuleSize(5).ToHttpResult(BarcodeImageFormat.Jpeg));
            a.MapGet("/b", () => Results.Extensions.Barcode(Barcodes.DataMatrix("hello"), BarcodeImageFormat.Svg));
            a.MapGet("/c", () => Results.Extensions.Barcode(BarcodeType.Ean13, "590123412345"));
        });

        var jpeg = await app.Client.GetAsync("/a");
        Assert.Equal("image/jpeg", jpeg.Content.Headers.ContentType?.MediaType);
        Assert.Equal(Barcodes.QrCode("hello").ModuleSize(5).ToJpeg(), await jpeg.Content.ReadAsByteArrayAsync());

        var svg = await app.Client.GetAsync("/b");
        Assert.Equal("image/svg+xml", svg.Content.Headers.ContentType?.MediaType);
        Assert.True(svg.Headers.Contains("Content-Security-Policy"));

        Assert.Equal(Barcodes.Ean13("590123412345").ToPng(), await app.Client.GetByteArrayAsync("/c"));
    }

    [Fact]
    public async Task File_download_name_sets_content_disposition()
    {
        await using var app = await TestApp.StartAsync(a =>
            a.MapGet("/d", () => Results.Extensions.Barcode(BarcodeType.QrCode, "x", fileDownloadName: "ticket 1.png")));

        var response = await app.Client.GetAsync("/d");
        var disposition = response.Content.Headers.ContentDisposition;
        Assert.Equal("attachment", disposition?.DispositionType);
        Assert.Equal("ticket 1.png", disposition?.FileName?.Trim('"'));
    }

    [Fact]
    public async Task Invalid_values_become_400_problem_details()
    {
        await using var app = await TestApp.StartAsync(a =>
            a.MapGet("/bad", () => Results.Extensions.Barcode(BarcodeType.Ean13, "not digits")));

        var response = await app.Client.GetAsync("/bad");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("EAN-13", await EndpointTests.ReadDetailAsync(response), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Typed_results_describe_themselves_for_openapi()
    {
        await using var app = await TestApp.StartAsync(a =>
            a.MapGet("/typed", () => Barcodes.QrCode("x").ToHttpResult()));

        var endpoint = app.Endpoints.OfType<RouteEndpoint>().Single(e => e.RoutePattern.RawText == "/typed");
        var produces = endpoint.Metadata.OfType<Microsoft.AspNetCore.Http.Metadata.IProducesResponseTypeMetadata>().ToList();
        Assert.Contains(produces, p => p.StatusCode == 200 && p.ContentTypes.Contains("image/svg+xml"));
        Assert.Contains(produces, p => p.StatusCode == 400 && p.Type == typeof(ProblemDetails));
    }

    [Fact]
    public async Task Controllers_can_return_the_result()
    {
        await using var app = await TestApp.StartAsync(
            a => a.MapControllers(),
            s => s.AddControllers().AddApplicationPart(typeof(LabelsController).Assembly));

        var response = await app.Client.GetAsync("/mvc/labels/ABC");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(Barcodes.Code39("ABC").ToSvg(), await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public void Properties_describe_the_output()
    {
        var result = Barcodes.QrCode("x").ToHttpResult(BarcodeImageFormat.Svg, "x.svg");
        Assert.Equal(BarcodeImageFormat.Svg, result.Format);
        Assert.Equal("image/svg+xml", result.ContentType);
        Assert.Equal("x.svg", result.FileDownloadName);
        Assert.Throws<ArgumentOutOfRangeException>(() => Barcodes.QrCode("x").ToHttpResult((BarcodeImageFormat)42));
    }
}

[ApiController]
[Route("mvc/labels")]
public sealed class LabelsController : ControllerBase
{
    [HttpGet("{code}")]
    public IResult Get(string code) => Barcodes.Code39(code).ToHttpResult(BarcodeImageFormat.Svg);
}
