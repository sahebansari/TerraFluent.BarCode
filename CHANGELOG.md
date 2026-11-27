# Changelog

All notable changes to this project are documented in this file.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [Unreleased]

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
