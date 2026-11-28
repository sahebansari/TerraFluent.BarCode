# TerraFluent.Barcode

TerraFluent.Barcode is a fluent, dependency-free .NET library that generates
linear barcodes and 2D codes (QR code, Data Matrix, PDF417) and gives you the result in whatever form you need:
PNG, JPEG, SVG, `byte[]`, `Stream`, base64 or a `data:` URI.

Every encoder and image writer is implemented in the library itself: no
System.Drawing, no SkiaSharp, no ImageSharp, no web service. It runs the same
on Windows, Linux, macOS, containers and serverless. The library targets
`netstandard2.0` (.NET Framework 4.6.1+, .NET Core 2.0+, Mono, Unity) and
`net10.0` (trimming and Native AOT compatible), and the assembly is strong-named.

## Install

```shell
dotnet add package TerraFluent.Barcode
```

## Quick Start

```csharp
using TerraFluent.Barcode;

// PNG bytes
byte[] png = Barcodes.Code128("ABC-12345").ToPng();

// SVG markup
string svg = Barcodes.QrCode("https://terrafluent.dev")
    .ErrorCorrection(QrErrorCorrectionLevel.H)
    .ModuleSize(6)
    .ToSvg();

// Base64 / data URI for HTML or JSON
string base64 = Barcodes.Ean13("590123412345").ToBase64();
string dataUri = Barcodes.UpcA("03600029145").ToDataUri(BarcodeImageFormat.Png);

// JPEG with custom colours
byte[] jpg = Barcodes.Code39("TERRAFLUENT")
    .WithChecksum()
    .Foreground("#1A3C8F")
    .Background("#FFFFFF")
    .JpegQuality(95)
    .ToJpeg();

// Files and streams
Barcodes.Ean8("9638507").Save("ean8.svg");          // format from extension
using Stream stream = Barcodes.QrCode("hello").ToStream(BarcodeImageFormat.Png);
await Barcodes.Code128("X1").WriteToAsync(response.Body, BarcodeImageFormat.Png);
```

## Symbologies

| Method | Standard | Input |
| --- | --- | --- |
| `Barcodes.Code128(value)` | ISO/IEC 15417 | ASCII 0-127, up to 256 characters. Code sets A/B/C chosen automatically. |
| `Barcodes.Code39(value)` | ISO/IEC 16388 | `0-9 A-Z`, space, `- . $ / + %`, up to 128 characters. Optional `.WithChecksum()`. |
| `Barcodes.Ean13(value)` | ISO/IEC 15420 | 12 digits (check digit added) or 13 (validated). |
| `Barcodes.Ean8(value)` | ISO/IEC 15420 | 7 digits (check digit added) or 8 (validated). |
| `Barcodes.UpcA(value)` | ISO/IEC 15420 | 11 digits (check digit added) or 12 (validated). |
| `Barcodes.QrCode(value)` | ISO/IEC 18004 | Any text. Split into numeric, alphanumeric and UTF-8 byte segments for the smallest code (non-ASCII text carries a UTF-8 ECI header so scanners decode it correctly); versions 1-40; levels L/M/Q/H. |
| `Barcodes.Gs1Code128(value)` | GS1 General Specifications | Element string such as `(01)09501101530003(17)250101(10)ABC123`. FNC1 inserted where needed. Every AI must be one GS1 defines, and its data is checked for length, digits, check digit and dates; up to 48 data characters. Use `Barcodes.Gs1Code128(("01", "09501101530003"), ("10", "LOT(A)"))` when data contains parentheses. |
| `Barcodes.DataMatrix(value)` | ISO/IEC 16022 | Any text (UTF-8 via ECI when needed). ECC 200, 30 square and rectangular sizes; `.DataMatrixShape(...)` picks square (default), rectangle or any. |
| `Barcodes.Pdf417(value)` | ISO/IEC 15438 | Any text (UTF-8 via ECI when needed). Text, numeric and byte compaction; `.Pdf417Columns(1-30)` and `.Pdf417ErrorCorrection(0-8)`. |

EAN-13 and UPC-A take a 2- or 5-digit add-on with `.AddOn("52495")`, and Code 39
encodes all 128 ASCII characters with `.FullAscii()`.

## Errors, Limits and Thread Safety

- An invalid value or setting throws `ArgumentException` (or one of its
  subclasses, `ArgumentNullException` and `ArgumentOutOfRangeException`;
  hex colours throw `FormatException`) with a message that says what was
  wrong, for example `Invalid EAN-13 check digit: '8' should be '7'.`
- Options that cannot be combined throw `InvalidOperationException`: an option
  for another symbology (such as `Height` on a QR code) when it is set, and a
  logo with too little error correction or colours with too little contrast
  when rendering.
- Images are limited to 32,768 pixels per side and 64 megapixels in total
  (`BarcodeBuilder.MaxImageDimension`, `BarcodeBuilder.MaxImagePixels`), so
  size settings taken from user input cannot exhaust memory.
- Foreground, gradient and eye colours need a WCAG contrast ratio of at least
  3:1 against the background. Light-on-dark is allowed. Call
  `AllowLowContrast()` to turn the check off for codes you have tested.
- The static `Barcodes` methods are thread-safe. Configure a `BarcodeBuilder`
  on one thread; after that, its output methods can be called concurrently.

## Outputs

| Method | Returns |
| --- | --- |
| `ToPng()` | `byte[]`, 1-bit indexed PNG (smallest lossless form); 8-bit RGB when styled |
| `ToJpeg()` | `byte[]`, baseline JPEG (`JpegQuality`, default 90) |
| `ToSvg()` | `string`, SVG document |
| `ToBytes(format)` | `byte[]` in any format |
| `ToStream(format)` | read-only `MemoryStream` at position 0 |
| `WriteTo(stream, format)` / `WriteToAsync(...)` | writes into your stream |
| `ToBase64(format)` | base64 `string` |
| `ToDataUri(format)` | `data:image/...;base64,...` |
| `Save(path)` / `Save(path, format)` | writes a file |
| `SaveAsync(path)` / `SaveAsync(path, format)` | writes a file asynchronously |
| `GetImageSize()` | output `(Width, Height)` in pixels |
| `Encode()` | the raw `BarcodeMatrix` for custom rendering |

`format` is `BarcodeImageFormat.Png`, `.Jpeg` or `.Svg`. Barcodes are
two-colour images, so prefer PNG or SVG: JPEG compression softens bar edges.

## Options

| Option | Default | Notes |
| --- | --- | --- |
| `ModuleSize(px)` | 2 (linear, PDF417), 4 (QR, Data Matrix) | Width of the narrowest bar, or one 2D square. |
| `Height(px)` | 80 | Linear barcodes only; excludes the text line. |
| `QuietZone(modules)` | per spec | 10 for Code 128/39, 11 for EAN-13, 9 for UPC-A, 7 for EAN-8, 4 for QR, 2 for Data Matrix and PDF417. |
| `Foreground(color)` / `Background(color)` | black / white | `BarcodeColor` or `"#RRGGBB"`/`"#RGB"`. Keep strong contrast. |
| `TransparentBackground()` | off | PNG and SVG only; JPEG output throws. Set `Background` to the surface colour for the contrast check. |
| `HumanReadableText()` | off | Linear barcodes. EAN/UPC digits in their standard positions with extended guard bars; the value centred under Code 128/39 and GS1-128. |
| `TextSize(px)` | 10 modules (EAN/UPC), 9 (others) | Font size of the human-readable text; long text shrinks to fit. |
| `ErrorCorrection(level)` | `M` | QR codes only. |
| `WithChecksum()` / `FullAscii()` | off | Code 39 only. |
| `AddOn(digits)` | none | EAN-13 and UPC-A: 2 or 5 digits. |
| `DataMatrixShape(shape)` | `Square` | Data Matrix only: `Square`, `Rectangle` or `Any`. |
| `Pdf417Columns(n)` / `Pdf417ErrorCorrection(n)` | automatic | PDF417 only. |
| `JpegQuality(1-100)` | 90 | JPEG output only. |
| `AllowLowContrast()` | off | Skips the contrast check. |

## Fancy Styles

Rounded and dotted modules, shaped eyes, gradients, logos and rounded bars
work in every output format: SVG gets real gradients and curves, PNG and JPEG
are anti-aliased.

```csharp
// Dotted QR code with circular eyes and a diagonal gradient
byte[] dots = Barcodes.QrCode("https://terrafluent.dev")
    .ModuleSize(8)
    .ModuleStyle(QrModuleStyle.Dots)
    .EyeStyle(QrEyeStyle.Circle)
    .Gradient("#1A3C8F", "#7B1FA2", GradientDirection.Diagonal)
    .ToPng();

// Logo in the centre (PNG bytes, transparency kept). Level H is used automatically.
string svg = Barcodes.QrCode("https://terrafluent.dev")
    .ModuleStyle(QrModuleStyle.Connected)
    .EyeStyle(QrEyeStyle.Rounded)
    .EyeColor("#E65100")
    .Logo(File.ReadAllBytes("logo.png"), scale: 0.25)
    .ToSvg();

// Pill-shaped bars with a horizontal gradient
byte[] bars = Barcodes.Code128("ABC-12345").ModuleSize(3).RoundedBars()
    .Gradient("#0D47A1", "#7B1FA2", GradientDirection.Horizontal)
    .ToPng();
```

| Option | Applies to | Values |
| --- | --- | --- |
| `ModuleStyle(style)` | QR | `Square` (default), `Rounded`, `Dots`, `Connected` |
| `EyeStyle(style)` | QR | `Square` (default), `Rounded`, `Circle` |
| `EyeColor(color)` | QR | colour of the three corner eyes |
| `Logo(png, scale)` | QR | PNG bytes; `scale` 0.05-0.3 of the code width (default 0.2). Needs level H (automatic) or Q with scale up to 0.25. |
| `Gradient(start, end, direction)` | all | `Horizontal`, `Vertical`, `Diagonal` (default), `Radial` |
| `RoundedBars()` | linear | rounded bar ends |

Every style keeps each module's centre where a scanner reads it, and the
logo is kept clear of the eyes, timing and format modules. Keep gradient
colours dark against a light background, and test-scan styled codes on the
devices you target. Barcodes without any style produce exactly the same
output as before.

## Dependency Injection

`IBarcodeGenerator` wraps the fluent API in a service you can inject and mock.
`BarcodeGenerator` is thread-safe; register one instance with your defaults:

```csharp
services.AddSingleton<IBarcodeGenerator>(new BarcodeGenerator(new BarcodeOptions
{
    ModuleSize = 3,
    HumanReadableText = true,
}));

public class LabelController(IBarcodeGenerator barcodes)
{
    public IActionResult Get(string sku) =>
        File(barcodes.Generate(BarcodeType.Code128, sku), "image/png");
}
```

Options that do not apply to a symbology (such as `Height` for a QR code) are
ignored, so one `BarcodeOptions` serves every type. Options passed to a call
replace the defaults. `Create(type, value)` returns a `BarcodeBuilder` with
the options applied, for gradients, logos and other styles.

## Not yet supported

ITF, Aztec, QR Kanji mode, structured append and Data Matrix C40/Text/EDIFACT
compaction (Data Matrix uses ASCII encodation, which pairs digits).

## Versioning

The library follows [Semantic Versioning](https://semver.org). The public API
is tracked in `src/TerraFluent.Barcode/PublicAPI.*.txt` and checked on every
build, so it cannot change by accident. `AssemblyVersion` stays at
`major.0.0.0` so upgrades within a major version need no binding redirects.
Changes are listed in [CHANGELOG.md](https://github.com/sahebansari/TerraFluent.Barcode/blob/main/CHANGELOG.md).

## Repository Layout

- `src/TerraFluent.Barcode` – the library.
- `tests/TerraFluent.Barcode.Tests` – xUnit tests.
- `samples/TerraFluent.Barcode.Sample` – writes every symbology in every format
  plus an `index.html` preview: `dotnet run --project samples/TerraFluent.Barcode.Sample`.

## Contributing and Security

See [CONTRIBUTING.md](https://github.com/sahebansari/TerraFluent.Barcode/blob/main/CONTRIBUTING.md). Please report vulnerabilities privately
as described in [SECURITY.md](https://github.com/sahebansari/TerraFluent.Barcode/blob/main/SECURITY.md).

## License

MIT. See [LICENSE](https://github.com/sahebansari/TerraFluent.Barcode/blob/main/LICENSE). The human-readable text uses glyph outlines from
JetBrains Mono (SIL Open Font License 1.1) and the PDF417 tables come from
pdf417gen (MIT); see [THIRD-PARTY-NOTICES.md](https://github.com/sahebansari/TerraFluent.Barcode/blob/main/THIRD-PARTY-NOTICES.md).
