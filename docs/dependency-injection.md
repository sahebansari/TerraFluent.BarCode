# Dependency injection

[IBarcodeGenerator](api/ibarcodegenerator.md) wraps the fluent API in a service you can inject into
controllers and services, and replace with a mock in tests. The default implementation,
[BarcodeGenerator](api/barcodegenerator.md), is thread-safe, so register one instance with your
defaults.

## Register the generator

```csharp
using TerraFluent.Barcode;

builder.Services.AddSingleton<IBarcodeGenerator>(new BarcodeGenerator(new BarcodeOptions
{
    ModuleSize = 3,
    HumanReadableText = true,
}));
```

No extra package is needed: the library depends on nothing, including
`Microsoft.Extensions.DependencyInjection`.

## Use it

```csharp
public class LabelController(IBarcodeGenerator barcodes) : ControllerBase
{
    [HttpGet("labels/{sku}.png")]
    public IActionResult Get(string sku) =>
        File(barcodes.Generate(BarcodeType.Code128, sku), "image/png");

    [HttpGet("labels/{sku}.svg")]
    public IActionResult GetSvg(string sku) =>
        Content(barcodes.GenerateSvg(BarcodeType.Code128, sku), "image/svg+xml");
}
```

| Method | Returns |
| --- | --- |
| `Generate(type, value, format, options)` | `byte[]` image, PNG by default |
| `GenerateSvg(type, value, options)` | SVG `string` |
| `GenerateDataUri(type, value, format, options)` | `data:` URI `string` |
| `WriteToAsync(type, value, stream, format, options, ct)` | writes into a stream |
| `Create(type, value, options)` | a [BarcodeBuilder](api/barcodebuilder.md) with the options applied |

## Defaults and per-call options

[BarcodeOptions](api/barcodeoptions.md) holds the defaults. Options that do not apply to a
symbology, such as `Height` for a QR code, are ignored, so one `BarcodeOptions` serves every type.

Options passed to a call replace the generator's defaults for that call. They are not merged, so
include every setting you want:

```csharp
byte[] big = barcodes.Generate(BarcodeType.QrCode, url, BarcodeImageFormat.Png,
    new BarcodeOptions { ModuleSize = 8, QrErrorCorrection = QrErrorCorrectionLevel.H });
// HumanReadableText from the defaults does not carry over (and does not apply to QR codes anyway).
```

| `BarcodeOptions` property | Builder method |
| --- | --- |
| `ModuleSize` | `ModuleSize(px)` |
| `Height` | `Height(px)` |
| `QuietZone` | `QuietZone(modules)` |
| `Foreground` / `Background` | `Foreground(color)` / `Background(color)` |
| `TransparentBackground` | `TransparentBackground()` |
| `HumanReadableText` / `TextSize` | `HumanReadableText()` / `TextSize(px)` |
| `QrErrorCorrection` | `ErrorCorrection(level)` |
| `Code39Checksum` / `Code39FullAscii` | `WithChecksum()` / `FullAscii()` |
| `DataMatrixShape` | `DataMatrixShape(shape)` |
| `Pdf417Columns` / `Pdf417ErrorCorrection` | `Pdf417Columns(n)` / `Pdf417ErrorCorrection(n)` |
| `JpegQuality` | `JpegQuality(quality)` |
| `MaxImagePixels` | `ImageSizeLimit(pixels)` |

## Styles through the generator

Gradients, logos and the other [styles](styling.md) are not part of `BarcodeOptions`. Start a builder
with `Create`, which applies your defaults, and continue fluently:

```csharp
string svg = barcodes.Create(BarcodeType.QrCode, url)
    .ModuleStyle(QrModuleStyle.Dots)
    .Gradient("#1A3C8F", "#7B1FA2")
    .ToSvg();
```

## Testing with a mock

Because callers depend on the interface, tests can substitute it, for example with Moq:

```csharp
var barcodes = new Mock<IBarcodeGenerator>();
barcodes.Setup(b => b.Generate(BarcodeType.Code128, "SKU-1", BarcodeImageFormat.Png, null))
        .Returns([0x89, 0x50, 0x4E, 0x47]);
```
