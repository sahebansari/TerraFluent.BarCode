# Changelog

All notable changes to this project are documented in this file.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [Unreleased]

## [1.0.0] - 2026-11-27

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

## [0.1.0] - 2026-11-27

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

[Unreleased]: https://github.com/sahebansari/TerraFluent.BarCode/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/sahebansari/TerraFluent.BarCode/compare/v0.1.0...v1.0.0
[0.1.0]: https://github.com/sahebansari/TerraFluent.BarCode/releases/tag/v0.1.0
