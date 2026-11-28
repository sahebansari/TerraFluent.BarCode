using System.Buffers.Binary;
using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace TerraFluent.Barcode.Mcp.Tests;

public sealed class McpServerTests(McpServerFixture server) : IClassFixture<McpServerFixture>
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    [Fact]
    public async Task Lists_the_three_tools_as_read_only()
    {
        var tools = await server.Client.ListToolsAsync();

        Assert.Equal(["generate_barcode", "generate_qr_code", "list_barcode_formats"], tools.Select(t => t.Name).Order());
        foreach (var tool in tools)
        {
            Assert.False(string.IsNullOrWhiteSpace(tool.Description));
            Assert.True(tool.ProtocolTool.Annotations?.ReadOnlyHint);
            Assert.False(tool.ProtocolTool.Annotations?.OpenWorldHint);
        }
    }

    [Fact]
    public async Task Generate_barcode_schema_requires_only_type_and_data()
    {
        var tools = await server.Client.ListToolsAsync();
        var schema = tools.Single(t => t.Name == "generate_barcode").JsonSchema;

        var required = schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).Order();
        Assert.Equal(["data", "type"], required);
        Assert.True(schema.GetProperty("properties").TryGetProperty("moduleSize", out _));
    }

    [Fact]
    public async Task Server_reports_its_name_and_version()
    {
        Assert.Equal("terrafluent-barcode", server.Client.ServerInfo!.Name);
        Assert.Matches(@"^\d+\.\d+\.\d+", server.Client.ServerInfo.Version);
        await Task.CompletedTask;
    }

    [Theory]
    [InlineData("code128", "ABC-12345")]
    [InlineData("Code 128", "ABC-12345")]
    [InlineData("gs1-128", "(01)09501101530003(10)ABC123")]
    [InlineData("code39", "TERRAFLUENT")]
    [InlineData("EAN-13", "590123412345")]
    [InlineData("ean8", "9638507")]
    [InlineData("upc_a", "03600029145")]
    [InlineData("qr", "https://terrafluent.dev")]
    [InlineData("QrCode", "hello")]
    [InlineData("datamatrix", "hello")]
    [InlineData("pdf417", "hello")]
    public async Task Generate_barcode_returns_a_png_image(string type, string data)
    {
        var result = await server.CallAsync("generate_barcode", new() { ["type"] = type, ["data"] = data });

        Assert.NotEqual(true, result.IsError);
        var image = Assert.IsType<ImageContentBlock>(result.Content[0]);
        Assert.Equal("image/png", image.MimeType);
        Assert.Equal(PngSignature, image.DecodedData.Span[..8].ToArray());

        var summary = Assert.IsType<TextContentBlock>(result.Content[1]).Text;
        var (width, height) = PngSize(image.DecodedData.Span);
        Assert.Contains($"{width} x {height} px PNG", summary, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Linear_options_change_the_image()
    {
        var result = await server.CallAsync("generate_barcode", new()
        {
            ["type"] = "code39",
            ["data"] = "ABC",
            ["moduleSize"] = 3,
            ["height"] = 40,
            ["quietZone"] = 0,
            ["checksum"] = true,
            ["humanReadableText"] = true,
            ["foreground"] = "#1A3C8F",
        });

        Assert.NotEqual(true, result.IsError);
        var expected = Barcodes.Code39("ABC").ModuleSize(3).Height(40).QuietZone(0).WithChecksum().HumanReadableText().Foreground("#1A3C8F").ToPng();
        Assert.Equal(expected, Assert.IsType<ImageContentBlock>(result.Content[0]).DecodedData.ToArray());
    }

    [Fact]
    public async Task Svg_is_returned_as_markup()
    {
        var result = await server.CallAsync("generate_barcode", new() { ["type"] = "ean13", ["data"] = "590123412345", ["format"] = "svg" });

        var svg = Assert.IsType<TextContentBlock>(result.Content[0]).Text;
        Assert.StartsWith("<svg", svg.TrimStart(), StringComparison.Ordinal);
        Assert.Equal(Barcodes.Ean13("590123412345").ToSvg(), svg);
    }

    [Fact]
    public async Task Jpeg_image_has_jpeg_mime_type()
    {
        var result = await server.CallAsync("generate_barcode", new() { ["type"] = "code128", ["data"] = "X1", ["format"] = "jpg" });

        var image = Assert.IsType<ImageContentBlock>(result.Content[0]);
        Assert.Equal("image/jpeg", image.MimeType);
        Assert.Equal([0xFF, 0xD8], image.DecodedData.Span[..2].ToArray());
    }

    [Theory]
    [InlineData("png", "base64")]
    [InlineData("svg", "base64")]
    [InlineData("jpeg", "data_uri")]
    [InlineData("svg", "data-uri")]
    public async Task Text_outputs_match_the_library(string format, string output)
    {
        var result = await server.CallAsync("generate_barcode", new() { ["type"] = "qr", ["data"] = "hello", ["format"] = format, ["output"] = output });

        var text = Assert.IsType<TextContentBlock>(result.Content[0]).Text;
        var imageFormat = BarcodeTools.ParseImageFormat(format);
        var expected = output == "base64"
            ? Barcodes.QrCode("hello").ToBase64(imageFormat)
            : Barcodes.QrCode("hello").ToDataUri(imageFormat);
        Assert.Equal(expected, text);
    }

    [Fact]
    public async Task Styled_qr_code_matches_the_library()
    {
        var result = await server.CallAsync("generate_qr_code", new()
        {
            ["data"] = "https://terrafluent.dev",
            ["errorCorrection"] = "h",
            ["moduleSize"] = 6,
            ["moduleStyle"] = "dots",
            ["eyeStyle"] = "Rounded",
            ["eyeColor"] = "#C2185B",
            ["gradientStart"] = "#1A3C8F",
            ["gradientEnd"] = "#6A1B9A",
            ["gradientDirection"] = "radial",
        });

        Assert.NotEqual(true, result.IsError);
        var expected = Barcodes.QrCode("https://terrafluent.dev")
            .ErrorCorrection(QrErrorCorrectionLevel.H)
            .ModuleSize(6)
            .ModuleStyle(QrModuleStyle.Dots)
            .EyeStyle(QrEyeStyle.Rounded)
            .EyeColor("#C2185B")
            .Gradient("#1A3C8F", "#6A1B9A", GradientDirection.Radial)
            .ToPng();
        Assert.Equal(expected, Assert.IsType<ImageContentBlock>(result.Content[0]).DecodedData.ToArray());
    }

    [Fact]
    public async Task Qr_code_accepts_a_base64_logo()
    {
        var logo = Barcodes.QrCode("logo").ModuleSize(2).ToPng();
        var result = await server.CallAsync("generate_qr_code", new()
        {
            ["data"] = "https://terrafluent.dev",
            ["errorCorrection"] = "H",
            ["logoPngBase64"] = "data:image/png;base64," + Convert.ToBase64String(logo),
            ["logoScale"] = 0.25,
        });

        Assert.NotEqual(true, result.IsError);
        var expected = Barcodes.QrCode("https://terrafluent.dev").ErrorCorrection(QrErrorCorrectionLevel.H).Logo(logo, 0.25).ToPng();
        Assert.Equal(expected, Assert.IsType<ImageContentBlock>(result.Content[0]).DecodedData.ToArray());
    }

    [Theory]
    [InlineData("generate_barcode", "{\"type\":\"aztecx\",\"data\":\"1\"}", "Unknown barcode type 'aztecx'")]
    [InlineData("generate_barcode", "{\"type\":\"ean13\",\"data\":\"5901234123458\"}", "check digit")]
    [InlineData("generate_barcode", "{\"type\":\"qr\",\"data\":\"x\",\"height\":50}", "Height applies to linear barcodes only")]
    [InlineData("generate_barcode", "{\"type\":\"code128\",\"data\":\"x\",\"format\":\"gif\"}", "Unknown image format 'gif'")]
    [InlineData("generate_barcode", "{\"type\":\"code128\",\"data\":\"x\",\"output\":\"file\"}", "Unknown output 'file'")]
    [InlineData("generate_barcode", "{\"type\":\"code128\",\"data\":\"x\",\"moduleSize\":65}", "moduleSize must be from 1 to 64")]
    [InlineData("generate_barcode", "{\"type\":\"code128\",\"data\":\"x\",\"format\":\"jpeg\",\"transparentBackground\":true}", "JPEG has no transparency")]
    [InlineData("generate_qr_code", "{\"data\":\"x\",\"moduleStyle\":\"hearts\"}", "Use one of: square, rounded, dots, connected")]
    [InlineData("generate_qr_code", "{\"data\":\"x\",\"gradientStart\":\"#000000\"}", "needs both gradientStart and gradientEnd")]
    [InlineData("generate_qr_code", "{\"data\":\"x\",\"logoPngBase64\":\"not base64!\",\"errorCorrection\":\"H\"}", "not valid base64")]
    [InlineData("generate_qr_code", "{\"data\":\"x\",\"foreground\":\"#EEEEEE\"}", "contrast")]
    public async Task Invalid_input_is_a_tool_error_with_the_reason(string tool, string argumentsJson, string expectedMessage)
    {
        var arguments = JsonSerializer.Deserialize<Dictionary<string, object?>>(argumentsJson)!;
        var result = await server.CallAsync(tool, arguments);

        Assert.True(result.IsError);
        var message = Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text;
        Assert.Contains(expectedMessage, message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Images_larger_than_the_server_limit_are_refused()
    {
        // The fixture allows 1,000,000 pixels; a version 40 QR code at 10 px per module is about 1,850 x 1,850.
        var result = await server.CallAsync("generate_qr_code", new() { ["data"] = new string('7', 7000 / 2), ["moduleSize"] = 10, ["errorCorrection"] = "L" });

        Assert.True(result.IsError);
        Assert.Contains("1000000", Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text.Replace(",", "", StringComparison.Ordinal), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Data_longer_than_the_server_limit_is_refused()
    {
        var result = await server.CallAsync("generate_barcode", new() { ["type"] = "qr", ["data"] = new string('a', BarcodeServerSettings.MaxDataLength + 1) });

        Assert.True(result.IsError);
        Assert.Contains("4096", Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task List_formats_returns_every_type_and_the_limits()
    {
        var result = await server.CallAsync("list_barcode_formats", []);

        Assert.NotEqual(true, result.IsError);
        var json = result.StructuredContent!.Value;
        var ids = json.GetProperty("barcodeTypes").EnumerateArray().Select(t => t.GetProperty("id").GetString()).ToList();
        Assert.Equal(Enum.GetValues<BarcodeType>().Length, ids.Count);
        Assert.Contains("qr", ids);
        Assert.Contains("gs1-128", ids);
        Assert.Equal(1_000_000, json.GetProperty("limits").GetProperty("maxImagePixels").GetInt64());
        Assert.Equal(["png", "jpeg", "svg"], json.GetProperty("imageFormats").EnumerateArray().Select(e => e.GetString()));

        // Every listed id is accepted back by generate_barcode.
        foreach (var id in ids)
        {
            Assert.Equal(id, FormatCatalogId(BarcodeTools.ParseBarcodeType(id!)));
        }
    }

    private static string FormatCatalogId(BarcodeType type) =>
        FormatCatalog.Create(new BarcodeServerSettings()).BarcodeTypes[Array.IndexOf(Enum.GetValues<BarcodeType>(), type)].Id;

    private static (int Width, int Height) PngSize(ReadOnlySpan<byte> png) =>
        (BinaryPrimitives.ReadInt32BigEndian(png[16..]), BinaryPrimitives.ReadInt32BigEndian(png[20..]));
}
