using System.Net;
using TerraFluent.BarCode;

// Writes every symbology in every output format to ./barcode-samples and an
// index.html that shows them side by side using data URIs.
var outputDir = args.Length > 0 ? args[0] : Path.Combine(Environment.CurrentDirectory, "barcode-samples");
Directory.CreateDirectory(outputDir);
const string RepoUrl = "https://github.com/sahebansari/TerraFluent.BarCode";

var samples = new (string Name, BarcodeBuilder Builder)[]
{
    ("code128", Barcode.Code128("TerraFluent-2026")),
    ("code128-digits", Barcode.Code128("00123456789012").Height(60)),
    ("code39", Barcode.Code39("TERRAFLUENT").WithChecksum()),
    ("ean13", Barcode.Ean13("590123412345")),
    ("ean8", Barcode.Ean8("9638507")),
    ("upca", Barcode.UpcA("03600029145")),
    ("qr", Barcode.QrCode("https://github.com/sahebansari/TerraFluent.BarCode")),
    ("qr-high-ecc", Barcode.QrCode("WIFI:T:WPA;S:Guest;P:welcome123;;").ErrorCorrection(QrErrorCorrectionLevel.H).ModuleSize(6)),
    ("qr-coloured", Barcode.QrCode("Grüße — こんにちは").Foreground("#1A3C8F").Background("#FFF8E0")),

    // Fancy styles: rounded/dotted/connected modules, eye shapes, gradients, logos and rounded bars.
    ("qr-dots", Barcode.QrCode(RepoUrl).ModuleSize(8).ModuleStyle(QrModuleStyle.Dots).EyeStyle(QrEyeStyle.Circle)
        .Gradient("#1A3C8F", "#7B1FA2", GradientDirection.Diagonal)),
    ("qr-rounded", Barcode.QrCode(RepoUrl).ModuleSize(8).ModuleStyle(QrModuleStyle.Rounded).EyeStyle(QrEyeStyle.Rounded)
        .Foreground("#00695C").EyeColor("#E65100").Background("#F1F8E9")),
    ("qr-connected", Barcode.QrCode(RepoUrl).ModuleSize(8).ModuleStyle(QrModuleStyle.Connected).EyeStyle(QrEyeStyle.Rounded)
        .Gradient("#0D47A1", "#00838F", GradientDirection.Radial)),
    ("qr-logo", Barcode.QrCode(RepoUrl).ModuleSize(8).ModuleStyle(QrModuleStyle.Dots).EyeStyle(QrEyeStyle.Rounded)
        .Gradient("#1A3C8F", "#7B1FA2").Logo(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "logo.png")), 0.25)),
    ("code128-fancy", Barcode.Code128("TerraFluent-2026").ModuleSize(3).Height(90).RoundedBars()
        .Gradient("#0D47A1", "#7B1FA2", GradientDirection.Horizontal)),
    ("ean13-fancy", Barcode.Ean13("590123412345").ModuleSize(3).RoundedBars().Gradient("#B71C1C", "#E65100", GradientDirection.Vertical)),
    ("ean13-text", Barcode.Ean13("590123412345").HumanReadableText()),
    ("ean13-addon", Barcode.Ean13("978123456789").AddOn("52495").HumanReadableText()),
    ("upca-text", Barcode.UpcA("03600029145").HumanReadableText()),
    ("ean8-text", Barcode.Ean8("9638507").HumanReadableText()),
    ("code128-text", Barcode.Code128("TerraFluent-2026").HumanReadableText()),
    ("code39-full-ascii", Barcode.Code39("Mixed Case").FullAscii().HumanReadableText()),
    ("gs1-128", Barcode.Gs1Code128("(01)09501101530003(17)250101(10)ABC123").HumanReadableText()),
    ("datamatrix", Barcode.DataMatrix("TerraFluent.BarCode Data Matrix")),
    ("datamatrix-rect", Barcode.DataMatrix("TF-2026").DataMatrixShape(DataMatrixShape.Rectangle)),
    ("pdf417", Barcode.Pdf417("TerraFluent.BarCode PDF417 sample: https://github.com/sahebansari/TerraFluent.BarCode")),
    ("qr-mixed", Barcode.QrCode("https://example.com/track/" + "123456789012345678901234567890")),
    ("qr-transparent", Barcode.QrCode(RepoUrl).TransparentBackground()),
};

var html = new System.Text.StringBuilder()
    .Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>TerraFluent.BarCode samples</title>")
    .Append("<style>body{font-family:sans-serif;margin:2rem}td{padding:.5rem 1rem;vertical-align:middle}</style></head><body>")
    .Append("<h1>TerraFluent.BarCode samples</h1><table><tr><th>Sample</th><th>PNG</th><th>JPEG</th><th>SVG</th></tr>");

foreach (var (name, builder) in samples)
{
    // JPEG has no transparency, so transparent samples skip it.
    var formats = name.EndsWith("transparent", StringComparison.Ordinal)
        ? new[] { BarcodeImageFormat.Png, BarcodeImageFormat.Svg }
        : new[] { BarcodeImageFormat.Png, BarcodeImageFormat.Jpeg, BarcodeImageFormat.Svg };
    foreach (var format in formats)
    {
        await builder.SaveAsync(Path.Combine(outputDir, name + format.ToFileExtension()));
    }

    var (width, height) = builder.GetImageSize();
    Console.WriteLine($"{name,-18} {builder.Type,-11} {width}x{height}px  \"{builder.Value}\"");

    html.Append("<tr><td>").Append(WebUtility.HtmlEncode($"{name} ({builder.Type})")).Append("</td>");
    foreach (var format in new[] { BarcodeImageFormat.Png, BarcodeImageFormat.Jpeg, BarcodeImageFormat.Svg })
    {
        html.Append(formats.Contains(format)
            ? $"<td><img alt=\"{format}\" src=\"{builder.ToDataUri(format)}\"></td>"
            : "<td></td>");
    }

    html.Append("</tr>");
}

html.Append("</table></body></html>");
File.WriteAllText(Path.Combine(outputDir, "index.html"), html.ToString());

// Streams and base64 for APIs:
using (var stream = Barcode.Code128("STREAM").ToStream(BarcodeImageFormat.Png))
{
    Console.WriteLine($"Stream output: {stream.Length} bytes");
}

// Dependency injection: register one BarcodeGenerator as IBarcodeGenerator.
IBarcodeGenerator generator = new BarcodeGenerator(new BarcodeOptions { HumanReadableText = true });
Console.WriteLine($"IBarcodeGenerator PNG: {generator.Generate(BarcodeType.Code128, "DI-123").Length} bytes");
Console.WriteLine($"Base64 (first 40 chars): {Barcode.QrCode("base64").ToBase64()[..40]}...");
Console.WriteLine($"Wrote samples to {outputDir}");
