using System.Net;
using TerraFluent.BarCode;
using TerraFluent.BarCode.Sample;

// Writes every symbology in every output format to ./barcode-samples and an
// index.html that shows them side by side using data URIs.
var outputDir = args.Length > 0 ? args[0] : Path.Combine(Environment.CurrentDirectory, "barcode-samples");
Directory.CreateDirectory(outputDir);

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

    // Styled ("fancy") codes.
    ("qr-dots", Barcode.QrCode("https://terrafluent.dev").ModuleSize(8)
        .Style(s => s.ModuleShape(QrModuleShape.Circle).FinderShape(QrFinderShape.Rounded))),
    ("qr-gradient", Barcode.QrCode("https://terrafluent.dev").ModuleSize(8)
        .Style(s => s.ModuleShape(QrModuleShape.RoundedSquare).FinderShape(QrFinderShape.Rounded)
                     .Gradient("#1A3C8F", "#00897B", GradientDirection.Diagonal).FinderColor("#E91E63"))),
    ("qr-radial", Barcode.QrCode("https://terrafluent.dev").ModuleSize(8)
        .Style(s => s.ModuleShape(QrModuleShape.Diamond).FinderShape(QrFinderShape.Circle)
                     .Gradient("#6A1B9A", "#1A237E", GradientDirection.Radial))),
    ("qr-logo", Barcode.QrCode("https://github.com/sahebansari/TerraFluent.BarCode").ModuleSize(8)
        .Style(s => s.Logo(SampleLogo.Create(), 0.22).ModuleShape(QrModuleShape.Circle)
                     .FinderShape(QrFinderShape.Rounded).FinderColor("#1A3C8F"))),
    ("qr-transparent", Barcode.QrCode("Transparent background").ModuleSize(8)
        .Style(s => s.TransparentBackground().ModuleShape(QrModuleShape.RoundedSquare))),
    ("code128-rounded", Barcode.Code128("TerraFluent-2026").ModuleSize(3)
        .Style(s => s.BarShape(BarShape.Rounded).Gradient("#1A3C8F", "#6A1B9A", GradientDirection.Horizontal))),
    ("ean13-gradient", Barcode.Ean13("590123412345").ModuleSize(3)
        .Style(s => s.Gradient("#004D40", "#1A237E", GradientDirection.Vertical))),
};

var html = new System.Text.StringBuilder()
    .Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>TerraFluent.BarCode samples</title>")
    .Append("<style>body{font-family:sans-serif;margin:2rem}td{padding:.5rem 1rem;vertical-align:middle}")
    .Append("tr.transparent td+td{background:repeating-conic-gradient(#ddd 0 25%,#fff 0 50%) 0 0/16px 16px}</style></head><body>")
    .Append("<h1>TerraFluent.BarCode samples</h1><table><tr><th>Sample</th><th>PNG</th><th>JPEG</th><th>SVG</th></tr>");

foreach (var (name, builder) in samples)
{
    builder.Save(Path.Combine(outputDir, name + ".png"));
    builder.Save(Path.Combine(outputDir, name + ".jpg"));
    builder.Save(Path.Combine(outputDir, name + ".svg"));

    var (width, height) = builder.GetImageSize();
    Console.WriteLine($"{name,-16} {builder.Type,-8} {width}x{height}px  \"{builder.Value}\"");

    html.Append(name.Contains("transparent") ? "<tr class=\"transparent\"><td>" : "<tr><td>").Append(WebUtility.HtmlEncode($"{name} ({builder.Type})")).Append("</td>");
    foreach (var format in new[] { BarcodeImageFormat.Png, BarcodeImageFormat.Jpeg, BarcodeImageFormat.Svg })
    {
        html.Append("<td><img alt=\"").Append(format).Append("\" src=\"").Append(builder.ToDataUri(format)).Append("\"></td>");
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

Console.WriteLine($"Base64 (first 40 chars): {Barcode.QrCode("base64").ToBase64()[..40]}...");
Console.WriteLine($"Wrote samples to {outputDir}");
