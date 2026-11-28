using System.Net;
using TerraFluent.Barcode;

// Writes every symbology in every output format to ./barcode-samples and an
// index.html that shows them side by side using data URIs.
var outputDir = args.Length > 0 ? args[0] : Path.Combine(Environment.CurrentDirectory, "barcode-samples");
Directory.CreateDirectory(outputDir);
const string RepoUrl = "https://github.com/sahebansari/TerraFluent.Barcode";

var samples = new (string Name, BarcodeBuilder Builder)[]
{
    ("code128", Barcodes.Code128("TerraFluent-2026")),
    ("code128-digits", Barcodes.Code128("00123456789012").Height(60)),
    ("code39", Barcodes.Code39("TERRAFLUENT").WithChecksum()),
    ("ean13", Barcodes.Ean13("590123412345")),
    ("ean8", Barcodes.Ean8("9638507")),
    ("upca", Barcodes.UpcA("03600029145")),
    ("qr", Barcodes.QrCode("https://github.com/sahebansari/TerraFluent.Barcode")),
    ("qr-high-ecc", Barcodes.QrCode("WIFI:T:WPA;S:Guest;P:welcome123;;").ErrorCorrection(QrErrorCorrectionLevel.H).ModuleSize(6)),
    ("qr-coloured", Barcodes.QrCode("Grüße — こんにちは").Foreground("#1A3C8F").Background("#FFF8E0")),

    // Fancy styles: rounded/dotted/connected modules, eye shapes, gradients, logos and rounded bars.
    ("qr-dots", Barcodes.QrCode(RepoUrl).ModuleSize(8).ModuleStyle(QrModuleStyle.Dots).EyeStyle(QrEyeStyle.Circle)
        .Gradient("#1A3C8F", "#7B1FA2", GradientDirection.Diagonal)),
    ("qr-rounded", Barcodes.QrCode(RepoUrl).ModuleSize(8).ModuleStyle(QrModuleStyle.Rounded).EyeStyle(QrEyeStyle.Rounded)
        .Foreground("#00695C").EyeColor("#E65100").Background("#F1F8E9")),
    ("qr-connected", Barcodes.QrCode(RepoUrl).ModuleSize(8).ModuleStyle(QrModuleStyle.Connected).EyeStyle(QrEyeStyle.Rounded)
        .Gradient("#0D47A1", "#00838F", GradientDirection.Radial)),
    ("qr-logo", Barcodes.QrCode(RepoUrl).ModuleSize(8).ModuleStyle(QrModuleStyle.Dots).EyeStyle(QrEyeStyle.Rounded)
        .Gradient("#1A3C8F", "#7B1FA2").Logo(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "logo.png")), 0.25)),
    ("code128-fancy", Barcodes.Code128("TerraFluent-2026").ModuleSize(3).Height(90).RoundedBars()
        .Gradient("#0D47A1", "#7B1FA2", GradientDirection.Horizontal)),
    ("ean13-fancy", Barcodes.Ean13("590123412345").ModuleSize(3).RoundedBars().Gradient("#B71C1C", "#E65100", GradientDirection.Vertical)),
    ("ean13-text", Barcodes.Ean13("590123412345").HumanReadableText()),
    ("ean13-addon", Barcodes.Ean13("978123456789").AddOn("52495").HumanReadableText()),
    ("upca-text", Barcodes.UpcA("03600029145").HumanReadableText()),
    ("ean8-text", Barcodes.Ean8("9638507").HumanReadableText()),
    ("code128-text", Barcodes.Code128("TerraFluent-2026").HumanReadableText()),
    ("code39-full-ascii", Barcodes.Code39("Mixed Case").FullAscii().HumanReadableText()),
    ("gs1-128", Barcodes.Gs1Code128("(01)09501101530003(17)250101(10)ABC123").HumanReadableText()),
    ("datamatrix", Barcodes.DataMatrix("TerraFluent.Barcode Data Matrix")),
    ("datamatrix-rect", Barcodes.DataMatrix("TF-2026").DataMatrixShape(DataMatrixShape.Rectangle)),
    ("pdf417", Barcodes.Pdf417("TerraFluent.Barcode PDF417 sample: https://github.com/sahebansari/TerraFluent.Barcode")),
    ("itf14", Barcodes.Itf14("1540014128876").HumanReadableText()),
    ("codabar", Barcodes.Codabar("A40156B").HumanReadableText()),
    ("aztec", Barcodes.Aztec("TerraFluent.Barcode Aztec: https://github.com/sahebansari/TerraFluent.Barcode")),
    ("maxicode", Barcodes.MaxiCode("152382802", 840, 1, "TerraFluent.Barcode MaxiCode")),
    ("qr-mixed", Barcodes.QrCode("https://example.com/track/" + "123456789012345678901234567890")),
    ("qr-transparent", Barcodes.QrCode(RepoUrl).TransparentBackground()),
    ("microqr", Barcodes.MicroQrCode("TF-2026")),
    ("rmqr", Barcodes.RectangularMicroQrCode("https://terrafluent.dev/barcode/")),
    ("databar", Barcodes.Gs1DataBar("09501101530003")),
    ("databar-limited", Barcodes.Gs1DataBarLimited("01501101530001")),
    ("databar-expanded", Barcodes.Gs1DataBarExpanded("(01)09501101530003(17)250101(10)ABC123")),
    ("imb", Barcodes.IntelligentMail("01234567094987654321", "01234567891")),

    // QR payload helpers: text that phone cameras act on.
    ("qr-wifi", Barcodes.QrCode(QrPayload.Wifi("Guest", "welcome123"))),
    ("qr-contact", Barcodes.QrCode(QrPayload.Contact(new ContactCard
    {
        FirstName = "Ada", LastName = "Lovelace", Organization = "TerraFluent",
        Email = "ada@example.com", Phone = "+44 20 7946 0000", Website = "https://terrafluent.dev",
    }))),
    ("qr-url", Barcodes.QrCode(QrPayload.Url("terrafluent.dev/barcode/"))),
    ("qr-email", Barcodes.QrCode(QrPayload.Email("hello@example.com", "Hi", "Sent from a QR code"))),
    ("qr-phone", Barcodes.QrCode(QrPayload.Phone("+1 555 0100"))),
    ("qr-sms", Barcodes.QrCode(QrPayload.Sms("+1 555 0100", "Hello"))),
    ("qr-digital-link", Barcodes.QrCode(QrPayload.Gs1DigitalLink("(01)09501101530003(10)ABC123"))),
};

var html = new System.Text.StringBuilder()
    .Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>TerraFluent.Barcode samples</title>")
    .Append("<style>body{font-family:sans-serif;margin:2rem}td{padding:.5rem 1rem;vertical-align:middle}</style></head><body>")
    .Append("<h1>TerraFluent.Barcode samples</h1><table><tr><th>Sample</th><th>PNG</th><th>JPEG</th><th>SVG</th></tr>");

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
using (var stream = Barcodes.Code128("STREAM").ToStream(BarcodeImageFormat.Png))
{
    Console.WriteLine($"Stream output: {stream.Length} bytes");
}

// Dependency injection: register one BarcodeGenerator as IBarcodeGenerator.
IBarcodeGenerator generator = new BarcodeGenerator(new BarcodeOptions { HumanReadableText = true });
Console.WriteLine($"IBarcodeGenerator PNG: {generator.Generate(BarcodeType.Code128, "DI-123").Length} bytes");
Console.WriteLine($"Base64 (first 40 chars): {Barcodes.QrCode("base64").ToBase64()[..40]}...");
Console.WriteLine($"Wrote samples to {outputDir}");
