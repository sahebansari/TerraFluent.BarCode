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

### <a id="TerraFluent_Barcode_Barcodes_MaxCode128Length"></a> MaxCode128Length

Longest value accepted by [Barcodes.Code128](barcodes.md#TerraFluent_Barcode_Barcodes_Code128_System_String_), in characters.

```csharp
public const int MaxCode128Length = 256
```

#### Field Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="TerraFluent_Barcode_Barcodes_MaxCode39Length"></a> MaxCode39Length

Longest value accepted by [Barcodes.Code39](barcodes.md#TerraFluent_Barcode_Barcodes_Code39_System_String_), in characters.

```csharp
public const int MaxCode39Length = 128
```

#### Field Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="TerraFluent_Barcode_Barcodes_Code128_System_String_"></a> Code128\(string\)

Code 128 for any ASCII text, up to [Barcodes.MaxCode128Length](barcodes.md#TerraFluent_Barcode_Barcodes_MaxCode128Length) characters.
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
[Barcodes.MaxCode39Length](barcodes.md#TerraFluent_Barcode_Barcodes_MaxCode39Length) characters.
Call [BarcodeBuilder.WithChecksum](barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_WithChecksum_System_Boolean_) to add the optional modulo 43 check character.

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
form cannot express. [BarcodeBuilder.Value](barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_Value) is the element
string with AIs in parentheses.

```csharp
public static BarcodeBuilder Gs1Code128(params (string Ai, string Data)[] elements)
```

#### Parameters

`elements` \([string](https://learn.microsoft.com/dotnet/api/system.string) [Ai](https://learn.microsoft.com/dotnet/api/system.valuetuple-system.string,system.string-.ai), [string](https://learn.microsoft.com/dotnet/api/system.string) [Data](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.string,system.string\-.data)\)\[\]

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

### <a id="TerraFluent_Barcode_Barcodes_UpcA_System_String_"></a> UpcA\(string\)

UPC-A from 11 digits (check digit added) or 12 digits (check digit validated).

```csharp
public static BarcodeBuilder UpcA(string value)
```

#### Parameters

`value` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)
