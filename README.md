# TerraFluent.BarCode

TerraFluent.BarCode is a fluent, dependency-free .NET library that generates
barcodes and QR codes and gives you the result in whatever form you need:
PNG, JPEG, SVG, `byte[]`, `Stream`, base64 or a `data:` URI.

Every encoder and image writer is implemented in the library itself: no
System.Drawing, no SkiaSharp, no ImageSharp, no web service. It runs the same
on Windows, Linux, macOS, containers and serverless. The library targets
`netstandard2.0` and `net10.0`.

## Install

```shell
dotnet add package TerraFluent.BarCode
```

## Quick Start

```csharp
using TerraFluent.BarCode;

// PNG bytes
byte[] png = Barcode.Code128("ABC-12345").ToPng();

// SVG markup
string svg = Barcode.QrCode("https://terrafluent.dev")
    .ErrorCorrection(QrErrorCorrectionLevel.H)
    .ModuleSize(6)
    .ToSvg();

// Base64 / data URI for HTML or JSON
string base64 = Barcode.Ean13("590123412345").ToBase64();
string dataUri = Barcode.UpcA("03600029145").ToDataUri(BarcodeImageFormat.Png);

// JPEG with custom colours
byte[] jpg = Barcode.Code39("TERRAFLUENT")
    .WithChecksum()
    .Foreground("#1A3C8F")
    .Background("#FFFFFF")
    .JpegQuality(95)
    .ToJpeg();

// Files and streams
Barcode.Ean8("9638507").Save("ean8.svg");          // format from extension
using Stream stream = Barcode.QrCode("hello").ToStream(BarcodeImageFormat.Png);
await Barcode.Code128("X1").WriteToAsync(response.Body, BarcodeImageFormat.Png);
```

## Symbologies

| Method | Standard | Input |
| --- | --- | --- |
| `Barcode.Code128(value)` | ISO/IEC 15417 | ASCII 0-127. Code sets A/B/C chosen automatically. |
| `Barcode.Code39(value)` | ISO/IEC 16388 | `0-9 A-Z`, space, `- . $ / + %`. Optional `.WithChecksum()`. |
| `Barcode.Ean13(value)` | ISO/IEC 15420 | 12 digits (check digit added) or 13 (validated). |
| `Barcode.Ean8(value)` | ISO/IEC 15420 | 7 digits (check digit added) or 8 (validated). |
| `Barcode.UpcA(value)` | ISO/IEC 15420 | 11 digits (check digit added) or 12 (validated). |
| `Barcode.QrCode(value)` | ISO/IEC 18004 | Any text. Numeric, alphanumeric or UTF-8 byte mode picked automatically; versions 1-40; levels L/M/Q/H. |

Invalid input throws `ArgumentException` with a message that says what was
wrong (for example `Invalid EAN-13 check digit: '8' should be '7'.`).

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
| `GetImageSize()` | output `(Width, Height)` in pixels |
| `Encode()` | the raw `BarcodeMatrix` for custom rendering |

`format` is `BarcodeImageFormat.Png`, `.Jpeg` or `.Svg`. Barcodes are
two-colour images, so prefer PNG or SVG: JPEG compression softens bar edges.

## Options

| Option | Default | Notes |
| --- | --- | --- |
| `ModuleSize(px)` | 2 (linear), 4 (QR) | Width of the narrowest bar, or one QR square. |
| `Height(px)` | 80 | Linear barcodes only. |
| `QuietZone(modules)` | per spec | 10 for Code 128/39, 11 for EAN-13, 9 for UPC-A, 7 for EAN-8, 4 for QR. |
| `Foreground(color)` / `Background(color)` | black / white | `BarcodeColor` or `"#RRGGBB"`/`"#RGB"`. Keep strong contrast. |
| `ErrorCorrection(level)` | `M` | QR codes only. |
| `WithChecksum()` | off | Code 39 only. |
| `JpegQuality(1-100)` | 90 | JPEG output only. |

## Fancy Styles

Rounded and dotted modules, shaped eyes, gradients, logos and rounded bars
work in every output format: SVG gets real gradients and curves, PNG and JPEG
are anti-aliased.

```csharp
// Dotted QR code with circular eyes and a diagonal gradient
byte[] dots = Barcode.QrCode("https://terrafluent.dev")
    .ModuleSize(8)
    .ModuleStyle(QrModuleStyle.Dots)
    .EyeStyle(QrEyeStyle.Circle)
    .Gradient("#1A3C8F", "#7B1FA2", GradientDirection.Diagonal)
    .ToPng();

// Logo in the centre (PNG bytes, transparency kept). Level H is used automatically.
string svg = Barcode.QrCode("https://terrafluent.dev")
    .ModuleStyle(QrModuleStyle.Connected)
    .EyeStyle(QrEyeStyle.Rounded)
    .EyeColor("#E65100")
    .Logo(File.ReadAllBytes("logo.png"), scale: 0.25)
    .ToSvg();

// Pill-shaped bars with a horizontal gradient
byte[] bars = Barcode.Code128("ABC-12345").ModuleSize(3).RoundedBars()
    .Gradient("#0D47A1", "#7B1FA2", GradientDirection.Horizontal)
    .ToPng();
```

| Option | Applies to | Values |
| --- | --- | --- |
| `ModuleStyle(style)` | QR | `Square` (default), `Rounded`, `Dots`, `Connected` |
| `EyeStyle(style)` | QR | `Square` (default), `Rounded`, `Circle` |
| `EyeColor(color)` | QR | colour of the three corner eyes |
| `Logo(png, scale)` | QR | PNG bytes; `scale` 0.05-0.3 of the code width (default 0.2) |
| `Gradient(start, end, direction)` | all | `Horizontal`, `Vertical`, `Diagonal` (default), `Radial` |
| `RoundedBars()` | linear | rounded bar ends |

Every style keeps each module's centre where a scanner reads it, and the
logo is kept clear of the eyes, timing and format modules. Keep gradient
colours dark against a light background, and test-scan styled codes on the
devices you target. Barcodes without any style produce exactly the same
output as before.

## Not yet supported

Human-readable text under linear barcodes, GS1-128 (FNC1), Code 39 full
ASCII, ITF, Data Matrix, PDF417, QR Kanji mode and structured append.

## Repository Layout

- `src/TerraFluent.BarCode` – the library.
- `tests/TerraFluent.BarCode.Tests` – xUnit tests.
- `samples/TerraFluent.BarCode.Sample` – writes every symbology in every format
  plus an `index.html` preview: `dotnet run --project samples/TerraFluent.BarCode.Sample`.

## License

MIT. See [LICENSE](LICENSE).
