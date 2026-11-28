using TerraFluent.Barcode;

// Renders every image the documentation site shows. Each entry here is referenced from a page under
// docs/articles, and the code in that page matches the builder below, so the pictures always show
// what the documented code produces. Run from the repository root:
//   dotnet run --project docs/tools/DocImages -- docs/images
var outputDir = args.Length > 0 ? args[0] : Path.Combine(Environment.CurrentDirectory, "images");
Directory.CreateDirectory(outputDir);

const string Url = "https://terrafluent.dev/barcode/";
var logo = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "logo.png"));
var svg = BarcodeImageFormat.Svg;
var png = BarcodeImageFormat.Png;
var jpeg = BarcodeImageFormat.Jpeg;

var images = new (string Name, BarcodeBuilder Builder, BarcodeImageFormat[] Formats)[]
{
    // Landing page and getting started
    ("hero-qr", Barcodes.QrCode(Url).ModuleSize(8).ModuleStyle(QrModuleStyle.Dots).EyeStyle(QrEyeStyle.Circle)
        .Gradient("#1A3C8F", "#7B1FA2"), [svg]),
    ("quick-code128", Barcodes.Code128("ABC-12345").HumanReadableText(), [svg, png]),
    ("quick-qr", Barcodes.QrCode(Url).ModuleSize(6), [svg, png]),

    // Symbologies
    ("code128", Barcodes.Code128("TerraFluent-2026").HumanReadableText(), [svg, png]),
    ("code128-digits", Barcodes.Code128("00123456789012").HumanReadableText(), [svg]),
    ("gs1-128", Barcodes.Gs1Code128("(01)09501101530003(17)250101(10)ABC123").HumanReadableText(), [svg, png]),
    ("code39", Barcodes.Code39("TERRAFLUENT").WithChecksum().HumanReadableText(), [svg, png]),
    ("code39-full-ascii", Barcodes.Code39("Mixed Case").FullAscii().HumanReadableText(), [svg]),
    ("ean13", Barcodes.Ean13("590123412345").HumanReadableText(), [svg, png]),
    ("ean13-addon", Barcodes.Ean13("978123456789").AddOn("52495").HumanReadableText(), [svg]),
    ("ean8", Barcodes.Ean8("9638507").HumanReadableText(), [svg, png]),
    ("upca", Barcodes.UpcA("03600029145").HumanReadableText(), [svg, png]),
    ("qr", Barcodes.QrCode(Url), [svg, png]),
    ("qr-ecc-h", Barcodes.QrCode("WIFI:T:WPA;S:Guest;P:welcome123;;").ErrorCorrection(QrErrorCorrectionLevel.H), [svg]),
    ("qr-unicode", Barcodes.QrCode("Grüße — こんにちは"), [svg]),
    ("datamatrix", Barcodes.DataMatrix("TerraFluent.Barcode Data Matrix"), [svg, png]),
    ("datamatrix-rect", Barcodes.DataMatrix("TF-2026").DataMatrixShape(DataMatrixShape.Rectangle), [svg]),
    ("pdf417", Barcodes.Pdf417("TerraFluent.Barcode PDF417 sample"), [svg, png]),
    ("pdf417-columns", Barcodes.Pdf417("TerraFluent.Barcode PDF417 sample").Pdf417Columns(3).Pdf417ErrorCorrection(4), [svg]),

    // Output formats: the same code in each format
    ("format-sample", Barcodes.QrCode(Url).ModuleSize(5), [png, jpeg, svg]),

    // Styling
    ("style-colours", Barcodes.QrCode(Url).ModuleSize(6).Foreground("#1A3C8F").Background("#FFF8E0"), [svg]),
    ("style-transparent", Barcodes.QrCode(Url).ModuleSize(6).TransparentBackground(), [svg, png]),
    ("style-module-square", Barcodes.QrCode(Url).ModuleSize(6), [svg]),
    ("style-module-rounded", Barcodes.QrCode(Url).ModuleSize(6).ModuleStyle(QrModuleStyle.Rounded), [svg]),
    ("style-module-dots", Barcodes.QrCode(Url).ModuleSize(6).ModuleStyle(QrModuleStyle.Dots), [svg]),
    ("style-module-connected", Barcodes.QrCode(Url).ModuleSize(6).ModuleStyle(QrModuleStyle.Connected), [svg]),
    ("style-eye-square", Barcodes.QrCode(Url).ModuleSize(6).EyeStyle(QrEyeStyle.Square).EyeColor("#E65100"), [svg]),
    ("style-eye-rounded", Barcodes.QrCode(Url).ModuleSize(6).EyeStyle(QrEyeStyle.Rounded).EyeColor("#E65100"), [svg]),
    ("style-eye-circle", Barcodes.QrCode(Url).ModuleSize(6).EyeStyle(QrEyeStyle.Circle).EyeColor("#E65100"), [svg]),
    ("style-gradient-horizontal", Barcodes.QrCode(Url).ModuleSize(6).Gradient("#0D47A1", "#7B1FA2", GradientDirection.Horizontal), [svg]),
    ("style-gradient-vertical", Barcodes.QrCode(Url).ModuleSize(6).Gradient("#0D47A1", "#7B1FA2", GradientDirection.Vertical), [svg]),
    ("style-gradient-diagonal", Barcodes.QrCode(Url).ModuleSize(6).Gradient("#0D47A1", "#7B1FA2", GradientDirection.Diagonal), [svg]),
    ("style-gradient-radial", Barcodes.QrCode(Url).ModuleSize(6).Gradient("#0D47A1", "#00838F", GradientDirection.Radial), [svg]),
    ("style-logo", Barcodes.QrCode(Url).ModuleSize(8).ModuleStyle(QrModuleStyle.Connected).EyeStyle(QrEyeStyle.Rounded)
        .EyeColor("#E65100").Logo(logo, scale: 0.25), [svg, png]),
    ("style-rounded-bars", Barcodes.Code128("ABC-12345").ModuleSize(3).RoundedBars()
        .Gradient("#0D47A1", "#7B1FA2", GradientDirection.Horizontal), [svg, png]),
    ("style-ean13-fancy", Barcodes.Ean13("590123412345").ModuleSize(3).RoundedBars()
        .Gradient("#B71C1C", "#E65100", GradientDirection.Vertical).HumanReadableText(), [svg]),

    // Options
    ("option-module-size-2", Barcodes.Code128("ABC-12345").ModuleSize(2).Height(50), [svg]),
    ("option-module-size-4", Barcodes.Code128("ABC-12345").ModuleSize(4).Height(50), [svg]),
    ("option-quiet-zone-0", Barcodes.QrCode("TF").ModuleSize(6).QuietZone(0), [svg]),
    ("option-quiet-zone-4", Barcodes.QrCode("TF").ModuleSize(6), [svg]),
};

foreach (var (name, builder, formats) in images)
{
    foreach (var format in formats)
    {
        await builder.SaveAsync(Path.Combine(outputDir, name + format.ToFileExtension()));
    }

    var (width, height) = builder.GetImageSize();
    Console.WriteLine($"{name,-28} {width}x{height}px");
}
