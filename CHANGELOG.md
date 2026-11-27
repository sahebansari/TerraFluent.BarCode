# Changelog

All notable changes to this project are documented in this file.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [Unreleased]

## [0.2.0] - 2026-11-27

### Added
- `Style(s => ...)` on `BarcodeBuilder` for styled ("fancy") codes. Every
  option is off by default, and an unstyled code renders byte-for-byte as in 0.1.0.
  - QR module shapes: `QrModuleShape.Square`, `RoundedSquare`, `Circle`, `Diamond`.
  - QR corner (finder) shapes: `QrFinderShape.Square`, `Rounded`, `Circle`,
    and a separate `FinderColor`.
  - QR centre logo: `Logo(bytes or path, sizeRatio, clearBehind)`. The logo
    raises the default error-correction level to H. The encoder checks
    exactly which codewords the logo hides and moves to a larger QR version
    until every block can still be corrected. SVG output embeds PNG, JPEG,
    GIF, WebP or SVG logos; PNG and JPEG output need a PNG logo.
  - Gradients for bars and modules: `Gradient(start, end, direction)` with
    `Horizontal`, `Vertical`, `Diagonal` and `Radial` directions.
  - `BarShape.Rounded` for linear barcodes.
  - `TransparentBackground()` for PNG and SVG (JPEG uses the background colour).
- Styled PNG output is 8-bit RGBA with 4x4 anti-aliasing; unstyled PNG stays 1-bit.
- Sample gallery now includes dots, gradient, radial, logo, transparent and
  rounded-bar examples.

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
