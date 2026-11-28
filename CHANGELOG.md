# Changelog

All notable changes to this project are documented in this file.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [Unreleased]

### Changed

- Renamed the package, assembly, root namespace, projects and solution from
  `TerraFluent.BarCode` to `TerraFluent.Barcode`, before the first NuGet.org publish.
  Update `using TerraFluent.BarCode;` to `using TerraFluent.Barcode;`.
- Package description and tags now name QR code and 2D codes (Data Matrix, PDF417).

## [1.1.0] - 2026-11-27

First release published to NuGet.org. Versions 0.1.0 and 1.0.0 below were
never published; their changes are included in this release.

### Added
- New symbologies: `Barcode.Gs1Code128` (GS1-128 with FNC1 and validated
  application identifiers), `Barcode.DataMatrix` (ECC 200, square and
  rectangular) and `Barcode.Pdf417`, with `DataMatrixShape(...)`,
  `Pdf417Columns(...)` and `Pdf417ErrorCorrection(...)`.
- `HumanReadableText()` and `TextSize(px)`: digits under EAN-13, EAN-8 and
  UPC-A in their standard positions with extended guard bars, and the value
  centred under Code 128, Code 39 and GS1-128. Text is drawn from embedded
  glyph outlines, so output is identical on every platform.
- `AddOn(digits)`: EAN-2 and EAN-5 add-ons for EAN-13 and UPC-A.
- `FullAscii()`: Code 39 full ASCII extension.
- `TransparentBackground()` for PNG and SVG output.
- `SaveAsync(path)` and `SaveAsync(path, format)`.
- `IBarcodeGenerator`, `BarcodeGenerator` and `BarcodeOptions` for dependency
  injection.
- `Barcode.Gs1Code128(params (string Ai, string Data)[])` for GS1 data that
  contains parentheses.
- GS1-128 validation against the GS1 application identifier table: unknown
  AIs, wrong lengths, non-digits in numeric fields, wrong check digits and
  impossible dates (such as month 13) throw `ArgumentException`.

### Changed
- QR codes are split into numeric, alphanumeric and byte segments, so mixed
  content such as a URL followed by a long number gives a smaller code.

## 1.0.0 - 2026-11-27 (not published)

### Added
- Fancy styling, in every output format (PNG, JPEG, SVG and everything built
  on them):
  - QR codes: `ModuleStyle(QrModuleStyle.Rounded | Dots | Connected)`,
    `EyeStyle(QrEyeStyle.Rounded | Circle)` for the corner eyes, `EyeColor(...)`
    and `Logo(png, scale)` to put a PNG logo in the centre (the code switches
    to error-correction level H unless one is set explicitly).
  - Linear barcodes: `RoundedBars()` for pill-shaped bars.
  - All symbologies: `Gradient(start, end, GradientDirection.Horizontal |
    Vertical | Diagonal | Radial)`.
- Styled raster output is anti-aliased (8-bit RGB PNG); plain barcodes keep
  the 1-bit PNG and are byte-for-byte unchanged.
- Contrast check: rendering throws `InvalidOperationException` when the
  foreground, a gradient stop or the eye colour has a WCAG contrast ratio
  below 3:1 against the background. `AllowLowContrast()` opts out.
  `BarcodeColor.RelativeLuminance` and `BarcodeColor.ContrastRatio` are public.
- Size limits: `BarcodeBuilder.MaxImageDimension` (32,768 px per side) and
  `BarcodeBuilder.MaxImagePixels` (64 megapixels).
- `Barcode.MaxCode128Length` (256) and `Barcode.MaxCode39Length` (128).
- Strong-named assembly, package icon, public API tracking and XML
  documentation for every public member.

### Changed
- `AssemblyVersion` is now `1.0.0.0` and will stay at `major.0.0.0`.
- `ModuleSize`, `Height` and `QuietZone` reject values above 32,768.
- A logo requires error-correction level H (the default with a logo) or Q
  with a scale up to 0.25; lower levels throw `InvalidOperationException`
  when rendering, because those codes often failed to scan.

### Fixed
- QR codes with non-ASCII text now start with a UTF-8 ECI header. Before,
  some scanners decoded such text as ISO-8859-1 or Shift JIS.
- JPEG output wider or taller than 65,535 pixels was written with a corrupt
  header; such sizes are now rejected.
- Huge `ModuleSize`, `Height` or `QuietZone` values could allocate gigabytes
  or throw `OverflowException` / `OutOfMemoryException`; they now throw
  `ArgumentException` before any allocation.
- A QR value with an unpaired surrogate threw `EncoderFallbackException`;
  it now throws `ArgumentException`.
- The PNG logo decoder uses 64-bit arithmetic for chunk bounds, so a
  malformed chunk length cannot overflow.

### Security
- Updated the build-only `Microsoft.SourceLink.GitHub` package to 10.0.404,
  which no longer pulls `Microsoft.Build.Tasks.Git` with advisory
  GHSA-23fw-v26w-5fgq. It never shipped to consumers.

## 0.1.0 - 2026-11-27 (not published)

### Added
- Fluent entry point `Barcode` with `Code128`, `Code39`, `Ean13`, `Ean8`,
  `UpcA`, `QrCode` and `Create(BarcodeType, value)`.
- Symbologies, implemented natively with no external libraries:
  - Code 128 with automatic code set A/B/C selection (digit runs packed into
    set C, control characters via set A).
  - Code 39 with optional modulo 43 check character (`WithChecksum()`).
  - EAN-13, EAN-8 and UPC-A: pass the value without its check digit to have
    it computed, or with it to have it validated.
  - QR code model 2, versions 1-40, error-correction levels L/M/Q/H, numeric,
    alphanumeric and byte (UTF-8) modes, automatic version and mask selection.
- Output formats: PNG (1-bit indexed), baseline JPEG, SVG, `byte[]`,
  `Stream`, `WriteTo`/`WriteToAsync`, base64, `data:` URI and `Save(path)`
  with the format inferred from the extension.
- Options: `ModuleSize`, `Height`, `QuietZone`, `Foreground`/`Background`
  (`BarcodeColor` or hex), `ErrorCorrection`, `JpegQuality`.
- `Encode()` returns the raw `BarcodeMatrix` for custom renderers.
- Targets `netstandard2.0` and `net10.0` (trimming and Native AOT compatible).

[Unreleased]: https://github.com/sahebansari/TerraFluent.Barcode/compare/v1.1.0...HEAD
[1.1.0]: https://github.com/sahebansari/TerraFluent.Barcode/releases/tag/v1.1.0
