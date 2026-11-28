# Class Barcodes

Namespace: [TerraFluent.Barcode](index.md)  
Assembly: TerraFluent.Barcode.dll  

Entry point. Pick a symbology, configure it fluently, then ask for the
output you need:

<pre><code class="lang-csharp">byte[] png = Barcodes.Code128("ABC-12345").Height(60).ToPng();
string svg = Barcodes.QrCode("https://terrafluent.dev").ErrorCorrection(QrErrorCorrectionLevel.H).ToSvg();
string uri = Barcodes.Ean13("590123412345").ToDataUri(BarcodeImageFormat.Png);</code></pre>

```csharp
public static class Barcodes
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Barcodes](barcodes.md)

## Fields

### <a id="TerraFluent_Barcode_Barcodes_MaxCodabarLength"></a> MaxCodabarLength

Longest value accepted by <a href="barcodes.md#TerraFluent_Barcode_Barcodes_Codabar_System_String_">Barcodes.Codabar</a>, in characters (including start and stop characters).

```csharp
public const int MaxCodabarLength = 60
```

#### Field Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="TerraFluent_Barcode_Barcodes_MaxCode128Length"></a> MaxCode128Length

Longest value accepted by <a href="barcodes.md#TerraFluent_Barcode_Barcodes_Code128_System_String_">Barcodes.Code128</a>, in characters.

```csharp
public const int MaxCode128Length = 256
```

#### Field Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="TerraFluent_Barcode_Barcodes_MaxCode39Length"></a> MaxCode39Length

Longest value accepted by <a href="barcodes.md#TerraFluent_Barcode_Barcodes_Code39_System_String_">Barcodes.Code39</a>, in characters.

```csharp
public const int MaxCode39Length = 128
```

#### Field Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="TerraFluent_Barcode_Barcodes_MaxItfLength"></a> MaxItfLength

Longest value accepted by <a href="barcodes.md#TerraFluent_Barcode_Barcodes_Itf_System_String_">Barcodes.Itf</a>, in digits (including a check digit added by <a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_WithChecksum_System_Boolean_">BarcodeBuilder.WithChecksum</a>).

```csharp
public const int MaxItfLength = 80
```

#### Field Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="TerraFluent_Barcode_Barcodes_Aztec_System_String_"></a> Aztec\(string\)

Aztec code for any text (UTF-8). The smallest compact or full-range symbol
that fits is picked automatically; see <a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_AztecErrorCorrection_System_Int32_">BarcodeBuilder.AztecErrorCorrection</a>.

```csharp
public static BarcodeBuilder Aztec(string value)
```

#### Parameters

`value` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_Barcodes_Codabar_System_String_"></a> Codabar\(string\)

Codabar for digits and <code>- $ : / . +</code>, up to <a href="barcodes.md#TerraFluent_Barcode_Barcodes_MaxCodabarLength">Barcodes.MaxCodabarLength</a> characters.
Start and end the value with A, B, C or D (or T, N, * and E) to choose the start
and stop characters, for example <code>A40156B</code>; otherwise A is used for both.

```csharp
public static BarcodeBuilder Codabar(string value)
```

#### Parameters

`value` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_Barcodes_Code128_System_String_"></a> Code128\(string\)

Code 128 for any ASCII text, up to <a href="barcodes.md#TerraFluent_Barcode_Barcodes_MaxCode128Length">Barcodes.MaxCode128Length</a> characters.
Code sets are chosen automatically for the shortest symbol.

```csharp
public static BarcodeBuilder Code128(string value)
```

#### Parameters

`value` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_Barcodes_Code39_System_String_"></a> Code39\(string\)

Code 39 for digits, upper-case letters, space and <code>- . $ / + %</code>, up to
<a href="barcodes.md#TerraFluent_Barcode_Barcodes_MaxCode39Length">Barcodes.MaxCode39Length</a> characters.
Call <a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_WithChecksum_System_Boolean_">BarcodeBuilder.WithChecksum</a> to add the optional modulo 43 check character.

```csharp
public static BarcodeBuilder Code39(string value)
```

#### Parameters

`value` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_Barcodes_Create_TerraFluent_Barcode_BarcodeType_System_String_"></a> Create\(BarcodeType, string\)

Starts a barcode of the given <code class="paramref">type</code>.

```csharp
public static BarcodeBuilder Create(BarcodeType type, string value)
```

#### Parameters

`type` [BarcodeType](barcodetype.md)

`value` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_Barcodes_DataMatrix_System_String_"></a> DataMatrix\(string\)

Data Matrix ECC 200 for any text (UTF-8). The smallest symbol that fits is picked automatically.

```csharp
public static BarcodeBuilder DataMatrix(string value)
```

#### Parameters

`value` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_Barcodes_Ean13_System_String_"></a> Ean13\(string\)

EAN-13 from 12 digits (check digit added) or 13 digits (check digit validated).

```csharp
public static BarcodeBuilder Ean13(string value)
```

#### Parameters

`value` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_Barcodes_Ean8_System_String_"></a> Ean8\(string\)

EAN-8 from 7 digits (check digit added) or 8 digits (check digit validated).

```csharp
public static BarcodeBuilder Ean8(string value)
```

#### Parameters

`value` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_Barcodes_Gs1Code128_System_String_"></a> Gs1Code128\(string\)

GS1-128 from an element string with application identifiers in parentheses,
for example <code>(01)09501101530003(17)250101(10)ABC123</code>. FNC1 characters are
inserted where GS1 requires them. Each AI must be one GS1 defines, and its data
is checked against the AI's format: length, digits, check digit and date.

```csharp
public static BarcodeBuilder Gs1Code128(string value)
```

#### Parameters

`value` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_Barcodes_Gs1Code128_System_ValueTuple_System_String_System_String____"></a> Gs1Code128\(params \(string Ai, string Data\)\[\]\)

GS1-128 from application identifier and data pairs, for example
<code>Gs1Code128(("01", "09501101530003"), ("10", "ABC(1)"))</code>. Use this
overload when the data contains parentheses, which the element-string
form cannot express. <a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_Value">BarcodeBuilder.Value</a> is the element
string with AIs in parentheses.

```csharp
public static BarcodeBuilder Gs1Code128(params (string Ai, string Data)[] elements)
```

#### Parameters

`elements` \([string](https://learn.microsoft.com/dotnet/api/system.string) [Ai](https://learn.microsoft.com/dotnet/api/system.valuetuple-system.string,system.string-.ai), [string](https://learn.microsoft.com/dotnet/api/system.string) [Data](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.string,system.string\-.data)\)\[\]

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_Barcodes_Gs1DataBar_System_String_"></a> Gs1DataBar\(string\)

GS1 DataBar Omnidirectional for a GTIN: 13 digits (check digit added),
14 digits (check digit validated), or either written as <code>(01)</code> and the
digits. Printed with the human-readable text <code>(01)</code> and the 14 digits.

```csharp
public static BarcodeBuilder Gs1DataBar(string gtin)
```

#### Parameters

`gtin` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_Barcodes_Gs1DataBarExpanded_System_String_"></a> Gs1DataBarExpanded\(string\)

GS1 DataBar Expanded from an element string with application identifiers in
parentheses, for example <code>(01)09501101530003(3103)000450(15)261231</code>,
validated as for <a href="barcodes.md#TerraFluent_Barcode_Barcodes_Gs1Code128_System_String_">Barcodes.Gs1Code128</a>. Holds up to 74 digits or
41 letters.

```csharp
public static BarcodeBuilder Gs1DataBarExpanded(string value)
```

#### Parameters

`value` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_Barcodes_Gs1DataBarExpanded_System_ValueTuple_System_String_System_String____"></a> Gs1DataBarExpanded\(params \(string Ai, string Data\)\[\]\)

GS1 DataBar Expanded from application identifier and data pairs; use it when
the data contains parentheses. <a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_Value">BarcodeBuilder.Value</a> is the element
string with AIs in parentheses.

```csharp
public static BarcodeBuilder Gs1DataBarExpanded(params (string Ai, string Data)[] elements)
```

#### Parameters

`elements` \([string](https://learn.microsoft.com/dotnet/api/system.string) [Ai](https://learn.microsoft.com/dotnet/api/system.valuetuple-system.string,system.string-.ai), [string](https://learn.microsoft.com/dotnet/api/system.string) [Data](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.string,system.string\-.data)\)\[\]

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_Barcodes_Gs1DataBarLimited_System_String_"></a> Gs1DataBarLimited\(string\)

GS1 DataBar Limited for a GTIN whose first digit (the indicator) is 0 or 1:
13 digits (check digit added) or 14 digits (validated), optionally after <code>(01)</code>.

```csharp
public static BarcodeBuilder Gs1DataBarLimited(string gtin)
```

#### Parameters

`gtin` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_Barcodes_IntelligentMail_System_String_System_String_"></a> IntelligentMail\(string, string\)

USPS Intelligent Mail barcode. <code class="paramref">trackingCode</code> is 20 digits:
barcode identifier (2 digits, the second 0 to 4), service type (3), mailer ID
and serial number (15). <code class="paramref">routingCode</code> is the delivery point
ZIP code: empty, 5, 9 or 11 digits. <a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_Value">BarcodeBuilder.Value</a> is the
two run together, the form <a href="barcodes.md#TerraFluent_Barcode_Barcodes_Create_TerraFluent_Barcode_BarcodeType_System_String_">Barcodes.Create</a> also accepts.

```csharp
public static BarcodeBuilder IntelligentMail(string trackingCode, string routingCode = "")
```

#### Parameters

`trackingCode` [string](https://learn.microsoft.com/dotnet/api/system.string)

`routingCode` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_Barcodes_Itf_System_String_"></a> Itf\(string\)

Interleaved 2 of 5 (ITF) for an even number of digits, up to <a href="barcodes.md#TerraFluent_Barcode_Barcodes_MaxItfLength">Barcodes.MaxItfLength</a>.
Call <a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_WithChecksum_System_Boolean_">BarcodeBuilder.WithChecksum</a> to append the modulo 10 check digit;
the value then needs an odd number of digits.

```csharp
public static BarcodeBuilder Itf(string value)
```

#### Parameters

`value` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_Barcodes_Itf14_System_String_"></a> Itf14\(string\)

ITF-14 from 13 digits (check digit added) or 14 digits (check digit validated), for GTIN-14 on cartons.

```csharp
public static BarcodeBuilder Itf14(string value)
```

#### Parameters

`value` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_Barcodes_MaxiCode_System_String_"></a> MaxiCode\(string\)

MaxiCode in mode 4 for ISO-8859-1 (Latin-1) text of up to 93 characters
(138 digits). Use <a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_MaxiCodeMode_System_Int32_">BarcodeBuilder.MaxiCodeMode</a> for mode 5 or 6.

```csharp
public static BarcodeBuilder MaxiCode(string value)
```

#### Parameters

`value` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_Barcodes_MaxiCode_System_String_System_Int32_System_Int32_System_String_"></a> MaxiCode\(string, int, int, string\)

MaxiCode with a structured carrier message, as parcel carriers use: mode 2
for a numeric postal code of up to 9 digits, or mode 3 for up to 6 letters,
digits and spaces. <code class="paramref">countryCode</code> (ISO 3166 numeric) and
<code class="paramref">serviceClass</code> are 0 to 999. <code class="paramref">message</code> is the
secondary message of up to 84 codewords (Latin-1), for example a
<code>[)&gt;</code> ISO/IEC 15434 transport message; <a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_Value">BarcodeBuilder.Value</a> returns it.

```csharp
public static BarcodeBuilder MaxiCode(string postalCode, int countryCode, int serviceClass, string message)
```

#### Parameters

`postalCode` [string](https://learn.microsoft.com/dotnet/api/system.string)

`countryCode` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`serviceClass` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`message` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_Barcodes_MicroQrCode_System_String_"></a> MicroQrCode\(string\)

Micro QR code for short text: up to 35 digits, 21 alphanumeric characters
(<code>0-9 A-Z</code>, space and <code>$ % * + - . / :</code>) or 15 ISO-8859-1 (Latin-1)
characters at level L. The smallest of versions M1 to M4 that fits is
picked; see <a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_ErrorCorrection_TerraFluent_Barcode_QrErrorCorrectionLevel_">BarcodeBuilder.ErrorCorrection</a> for levels L, M and Q.

```csharp
public static BarcodeBuilder MicroQrCode(string value)
```

#### Parameters

`value` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_Barcodes_Pdf417_System_String_"></a> Pdf417\(string\)

PDF417 for any text (UTF-8). Columns and error correction are chosen automatically unless set.

```csharp
public static BarcodeBuilder Pdf417(string value)
```

#### Parameters

`value` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_Barcodes_QrCode_System_String_"></a> QrCode\(string\)

QR code for any text (UTF-8). The smallest version that fits is picked automatically.

```csharp
public static BarcodeBuilder QrCode(string value)
```

#### Parameters

`value` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_Barcodes_RectangularMicroQrCode_System_String_"></a> RectangularMicroQrCode\(string\)

Rectangular Micro QR code (rMQR) for any text (UTF-8), up to 361 digits or
150 bytes. The symbol with the smallest area that fits is picked; use
<a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_RectangularMicroQrHeight_System_Int32_">BarcodeBuilder.RectangularMicroQrHeight</a> to fix the height, and
<a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_ErrorCorrection_TerraFluent_Barcode_QrErrorCorrectionLevel_">BarcodeBuilder.ErrorCorrection</a> for level M (default) or H.

```csharp
public static BarcodeBuilder RectangularMicroQrCode(string value)
```

#### Parameters

`value` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_Barcodes_UpcA_System_String_"></a> UpcA\(string\)

UPC-A from 11 digits (check digit added) or 12 digits (check digit validated).

```csharp
public static BarcodeBuilder UpcA(string value)
```

#### Parameters

`value` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)
