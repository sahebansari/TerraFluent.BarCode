# Barcode types

Each symbology has its own method on [Barcodes](api/barcodes.md). The method checks the value
straight away and throws `ArgumentException` with a clear message when it cannot be encoded, so a bad
value fails where you pass it, not when you render. `Barcodes.Create(BarcodeType, value)` does the
same for a type chosen at run time.

| Method | Kind | Standard | Typical use |
| --- | --- | --- | --- |
| [`Code128`](#code-128) | Linear | ISO/IEC 15417 | Shipping, logistics, internal labels |
| [`Gs1Code128`](#gs1-128) | Linear | GS1 General Specifications | GS1 logistics labels (SSCC, GTIN, lot, dates) |
| [`Code39`](#code-39) | Linear | ISO/IEC 16388 | Automotive, defence, older industrial systems |
| [`Ean13`](#ean-13) | Linear | ISO/IEC 15420 | Retail products worldwide, books (ISBN) |
| [`Ean8`](#ean-8) | Linear | ISO/IEC 15420 | Small retail packages |
| [`UpcA`](#upc-a) | Linear | ISO/IEC 15420 | Retail products in North America |
| [`QrCode`](#qr-code) | 2D | ISO/IEC 18004 | URLs, Wi-Fi, payments, tickets |
| [`DataMatrix`](#data-matrix) | 2D | ISO/IEC 16022 | Small parts marking, healthcare, electronics |
| [`Pdf417`](#pdf417) | Stacked 2D | ISO/IEC 15438 | ID cards, boarding passes, shipping documents |

Aztec, ITF-14, Codabar and MaxiCode are on the way; see the [Roadmap](roadmap.md).

## Code 128

```csharp
Barcodes.Code128("TerraFluent-2026").HumanReadableText().ToSvg();
```

![Code 128 barcode for TerraFluent-2026](images/code128.svg)

- **Input:** ASCII 0-127, up to 256 characters (`Barcodes.MaxCode128Length`).
- Code sets A, B and C are chosen automatically for the shortest symbol. Runs of digits are packed
  two per symbol in code set C:

```csharp
Barcodes.Code128("00123456789012").HumanReadableText();
```

![Code 128 barcode for 00123456789012](images/code128-digits.svg)

## GS1-128

```csharp
Barcodes.Gs1Code128("(01)09501101530003(17)250101(10)ABC123").HumanReadableText();
```

![GS1-128 barcode](images/gs1-128.svg)

- **Input:** a GS1 element string, each application identifier (AI) in parentheses.
- FNC1 is inserted at the start and after every variable-length field that is not last.
- Every AI must be one GS1 defines, and its data is checked for length, digits, check digit and
  dates. Up to 48 data characters.
- When the data itself contains parentheses, pass the elements as tuples instead:

```csharp
Barcodes.Gs1Code128(("01", "09501101530003"), ("10", "LOT(A)"));
```

## Code 39

```csharp
Barcodes.Code39("TERRAFLUENT").WithChecksum().HumanReadableText();
```

![Code 39 barcode for TERRAFLUENT](images/code39.svg)

- **Input:** `0-9`, `A-Z`, space and `- . $ / + %`, up to 128 characters (`Barcodes.MaxCode39Length`).
- `WithChecksum()` appends the optional modulo 43 check character.
- `FullAscii()` encodes all 128 ASCII characters, including lower case, as two-character sequences.
  The scanner must have Full ASCII mode turned on to read it back:

```csharp
Barcodes.Code39("Mixed Case").FullAscii().HumanReadableText();
```

![Code 39 Full ASCII barcode for Mixed Case](images/code39-full-ascii.svg)

## EAN-13

```csharp
Barcodes.Ean13("590123412345").HumanReadableText();
```

![EAN-13 barcode](images/ean13.svg)

- **Input:** 12 digits (the check digit is added) or 13 digits (the check digit is validated).
- `AddOn("52")` or `AddOn("52495")` adds an EAN-2 or EAN-5 supplement, used for periodicals and book
  prices:

```csharp
Barcodes.Ean13("978123456789").AddOn("52495").HumanReadableText();
```

![EAN-13 barcode with an EAN-5 add-on](images/ean13-addon.svg)

## EAN-8

```csharp
Barcodes.Ean8("9638507").HumanReadableText();
```

![EAN-8 barcode](images/ean8.svg)

- **Input:** 7 digits (the check digit is added) or 8 digits (the check digit is validated).

## UPC-A

```csharp
Barcodes.UpcA("03600029145").HumanReadableText();
```

![UPC-A barcode](images/upca.svg)

- **Input:** 11 digits (the check digit is added) or 12 digits (the check digit is validated).
- Takes an EAN-2 or EAN-5 add-on with `AddOn(...)`, like EAN-13.

## QR code

```csharp
Barcodes.QrCode("https://terrafluent.dev/barcode/");
```

![QR code](images/qr.svg)

- **Input:** any text. It is split into numeric, alphanumeric and byte segments to give the smallest
  code. Versions 1 to 40 are chosen automatically.
- Non-ASCII text is encoded as UTF-8 with an ECI header, so scanners decode it correctly:

```csharp
Barcodes.QrCode("Grüße — こんにちは");
```

![QR code with Unicode text](images/qr-unicode.svg)

- `ErrorCorrection(level)` picks how much of the code can be damaged and still read: `L` (about 7%),
  `M` (15%, the default), `Q` (25%) or `H` (30%). Higher levels make larger codes:

```csharp
Barcodes.QrCode("WIFI:T:WPA;S:Guest;P:welcome123;;").ErrorCorrection(QrErrorCorrectionLevel.H);
```

![QR code with error correction level H](images/qr-ecc-h.svg)

QR codes can also be styled with rounded modules, shaped eyes, gradients and a logo; see
[Styling](styling.md).

## Data Matrix

```csharp
Barcodes.DataMatrix("TerraFluent.Barcode Data Matrix");
```

![Data Matrix code](images/datamatrix.svg)

- **Input:** any text, UTF-8 via ECI when needed. ECC 200, in all 30 square and rectangular sizes.
- `DataMatrixShape(...)` chooses `Square` (the default), `Rectangle` or `Any` (the smallest of either):

```csharp
Barcodes.DataMatrix("TF-2026").DataMatrixShape(DataMatrixShape.Rectangle);
```

![Rectangular Data Matrix code](images/datamatrix-rect.svg)

## PDF417

```csharp
Barcodes.Pdf417("TerraFluent.Barcode PDF417 sample");
```

![PDF417 code](images/pdf417.svg)

- **Input:** any text, UTF-8 via ECI when needed. Text, numeric and byte compaction are chosen
  automatically.
- `Pdf417Columns(1-30)` sets the number of data columns, and `Pdf417ErrorCorrection(0-8)` the error
  correction level. Both are chosen automatically when not set:

```csharp
Barcodes.Pdf417("TerraFluent.Barcode PDF417 sample").Pdf417Columns(3).Pdf417ErrorCorrection(4);
```

![PDF417 code with 3 columns](images/pdf417-columns.svg)

## Not yet supported

QR Kanji mode, structured append, and Data Matrix C40, Text and EDIFACT compaction (Data Matrix
uses ASCII encodation, which still packs digit pairs). New symbologies are listed in the
[Roadmap](roadmap.md).
