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
| [`Itf`](#interleaved-2-of-5) | Linear | ISO/IEC 16390 | Warehousing, distribution, numeric IDs |
| [`Itf14`](#itf-14) | Linear | GS1 General Specifications | Outer cartons and cases (GTIN-14) |
| [`Codabar`](#codabar) | Linear | ANSI/AIM BC3 | Libraries, blood banks, courier forms |
| [`Pdf417`](#pdf417) | Stacked 2D | ISO/IEC 15438 | ID cards, boarding passes, shipping documents |
| [`Aztec`](#aztec) | 2D | ISO/IEC 24778 | Transport tickets, boarding passes |
| [`MaxiCode`](#maxicode) | 2D | ISO/IEC 16023 | Parcel sorting |
| [`MicroQrCode`](#micro-qr) | 2D | ISO/IEC 18004 | Tiny items: electronic parts, small labels |
| [`RectangularMicroQrCode`](#rmqr) | 2D | ISO/IEC 23941 | Narrow print areas: cable tags, edges of packaging |
| [`Gs1DataBar`](#gs1-databar) | Linear | ISO/IEC 24724 | Retail point of sale, fresh produce (GTIN) |
| [`Gs1DataBarLimited`](#gs1-databar-limited) | Linear | ISO/IEC 24724 | Very small retail items (GTIN starting with 0 or 1) |
| [`Gs1DataBarExpanded`](#gs1-databar-expanded) | Linear | ISO/IEC 24724 | Variable-weight foods, coupons (GTIN plus weight, price, dates) |
| [`IntelligentMail`](#usps-intelligent-mail) | 4-state | USPS-B-3200 | US mail pieces |

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

## Interleaved 2 of 5

```csharp
Barcodes.Itf("12345678").HumanReadableText();
```

![Interleaved 2 of 5 barcode for 12345678](images/itf.svg)

- **Input:** digits only, up to 80 (`Barcodes.MaxItfLength`). Digits are encoded in pairs, so the
  count must be even, or odd with `WithChecksum()`, which adds a modulo 10 check digit.

## ITF-14

```csharp
Barcodes.Itf14("1540014128876").HumanReadableText();
```

![ITF-14 barcode](images/itf14.svg)

- **Input:** 13 digits (the check digit is added) or 14 digits (the check digit is validated).
- Bearer bars are not drawn.

## Codabar

```csharp
Barcodes.Codabar("A40156B").HumanReadableText();
```

![Codabar barcode for A40156B](images/codabar.svg)

- **Input:** digits and `- $ : / . +`, up to 60 characters (`Barcodes.MaxCodabarLength`), including
  the start and stop characters.
- Start and stop characters are `A`, `B`, `C` or `D` (or `T`, `N`, `*`, `E`). When the value has
  none, `A` is used at both ends.

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

## Aztec

```csharp
Barcodes.Aztec("TerraFluent.Barcode Aztec sample");
```

![Aztec code](images/aztec.svg)

- **Input:** any text; non-ASCII text is sent as UTF-8 with an ECI. Upper, lower, mixed,
  punctuation, digit and byte modes are chosen automatically for the shortest symbol.
- The smallest compact (1-4 layers) or full-range (up to 32 layers) symbol that fits is used.
- `AztecErrorCorrection(5-90)` sets the share of the symbol used for error correction, plus 3 check
  words. The default is 23%:

```csharp
Barcodes.Aztec("TerraFluent.Barcode Aztec sample").AztecErrorCorrection(50);
```

![Aztec code with 50% error correction](images/aztec-ecc-50.svg)

## MaxiCode

```csharp
Barcodes.MaxiCode("152382802", 840, 1, "TerraFluent.Barcode MaxiCode");
```

![MaxiCode](images/maxicode.svg)

- A fixed-size symbol of 33 rows of hexagons around a bullseye, about 1 inch square at the default
  module size of 6 px.
- `MaxiCode(postalCode, countryCode, serviceClass, message)` writes a structured carrier message:
  mode 2 for a numeric postal code of up to 9 digits, mode 3 for an alphanumeric one of up to 6
  characters. The country (ISO 3166 numeric) and service class are numbers from 0 to 999.
- `MaxiCode(value)` uses mode 4 for Latin-1 text of up to 93 characters (138 digits).
  `MaxiCodeMode(5)` uses enhanced error correction (up to 77 characters) and `MaxiCodeMode(6)` marks
  a reader programming symbol:

```csharp
Barcodes.MaxiCode("TerraFluent.Barcode").MaxiCodeMode(5);
```

## Micro QR

```csharp
Barcodes.MicroQrCode("TF-2026");
```

![Micro QR code](images/micro-qr.svg)

- **Input:** up to 35 digits, 21 alphanumeric characters (`0-9 A-Z`, space, `$ % * + - . / :`) or
  15 ISO-8859-1 (Latin-1) characters at level L. Micro QR has no ECI, so characters outside Latin-1
  are rejected; use a QR code or rMQR for them.
- One finder pattern and a 2-module quiet zone. The smallest of versions M1 (11 x 11) to
  M4 (17 x 17) that fits is used.
- `ErrorCorrection` takes L, M (default) or Q. Q needs version M4. At level L the value may fit
  M1, which detects errors but cannot correct them.

## rMQR

```csharp
Barcodes.RectangularMicroQrCode("https://terrafluent.dev/barcode/");
```

![rMQR code](images/rmqr.svg)

- **Input:** any text, up to 361 digits or 150 bytes. Non-ASCII text is sent as UTF-8 with an ECI.
- 32 sizes, 7 to 17 modules high and 27 to 139 wide. The symbol with the smallest area that fits
  is used; `RectangularMicroQrHeight(7, 9, 11, 13, 15 or 17)` fixes the height and picks the
  narrowest width that fits:

```csharp
Barcodes.RectangularMicroQrCode("TERRAFLUENT 2026").RectangularMicroQrHeight(7);
```

![rMQR code 7 modules high](images/rmqr-height-7.svg)

- `ErrorCorrection` takes M (default) or H.

## GS1 DataBar

```csharp
Barcodes.Gs1DataBar("0950110153000").HumanReadableText();
```

![GS1 DataBar Omnidirectional](images/databar.svg)

- **Input:** a GTIN: 13 digits (the check digit is added) or 14 digits (validated), optionally
  after `(01)`. The human-readable text is `(01)` and the 14 digits.
- DataBar Omnidirectional, 96 modules wide. Keep the bars at least 33 modules high (66 px at the
  default 2 px module) for omnidirectional scanning; the default height of 80 px is enough.

## GS1 DataBar Limited

```csharp
Barcodes.Gs1DataBarLimited("1501234567890").HumanReadableText();
```

![GS1 DataBar Limited](images/databar-limited.svg)

- **Input:** as for GS1 DataBar, but the GTIN's first digit (the indicator) must be 0 or 1.
- 79 modules wide, the narrowest DataBar; read by linear scanners only, not omnidirectionally.

## GS1 DataBar Expanded

```csharp
Barcodes.Gs1DataBarExpanded("(01)09501101530003(3103)000450(15)261231").HumanReadableText();
```

![GS1 DataBar Expanded](images/databar-expanded.svg)

- **Input:** a GS1 element string, validated as for [GS1-128](#gs1-128), or AI and data pairs.
  Up to 74 digits or 41 letters, depending on the mix.
- A leading `(01)` GTIN is compressed (its check digit is implied); the rest uses numeric,
  alphanumeric and ISO 646 compaction. Drawn in a single row (the stacked variant is not supported).

## USPS Intelligent Mail

```csharp
Barcodes.IntelligentMail("01234567094987654321", "01234567891");
```

![USPS Intelligent Mail barcode](images/intelligent-mail.svg)

- **Input:** a 20-digit tracking code (barcode identifier, service type, mailer ID and serial
  number; the second digit is 0 to 4) and a routing code of 0, 5, 9 or 11 digits (the delivery
  point ZIP code). `Barcodes.Create(BarcodeType.IntelligentMail, value)` takes the two run together.
- 65 bars of four states (full, ascender, descender, tracker), drawn as a 129 x 6 module matrix
  with bars one module wide. At the default 2 px module and 300 dpi this is close to the
  specification's 22 bars per inch and 0.125 to 0.165 inch height; print at 300 dpi or scale the
  module size for your printer.

## Not yet supported

QR Kanji mode, structured append, and Data Matrix C40, Text and EDIFACT compaction (Data Matrix
uses ASCII encodation, which still packs digit pairs). ITF-14 bearer bars, Aztec runes and
structured append, MaxiCode ECI and structured append, Micro QR and rMQR Kanji mode, and the
stacked DataBar variants (Stacked, Stacked Omnidirectional, Expanded Stacked). Planned work is listed in the
[Roadmap](roadmap.md).
