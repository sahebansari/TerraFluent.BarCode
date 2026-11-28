# Class BarcodeOptions

Namespace: [TerraFluent.Barcode](index.md)  
Assembly: TerraFluent.Barcode.dll  

Settings used by <a href="ibarcodegenerator.md">IBarcodeGenerator</a>. Every property is optional;
a <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a> or <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">false</a> value keeps the default
that <a href="barcodebuilder.md">BarcodeBuilder</a> uses. Settings that only apply to some
symbologies (for example <a href="barcodeoptions.md#TerraFluent_Barcode_BarcodeOptions_Height">BarcodeOptions.Height</a> or <a href="barcodeoptions.md#TerraFluent_Barcode_BarcodeOptions_QrErrorCorrection">BarcodeOptions.QrErrorCorrection</a>)
are ignored for the others, so one instance can serve every type.

```csharp
public sealed class BarcodeOptions
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[BarcodeOptions](barcodeoptions.md)

## Properties

### <a id="TerraFluent_Barcode_BarcodeOptions_AztecErrorCorrection"></a> AztecErrorCorrection

Aztec minimum error correction in percent. See <a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_AztecErrorCorrection_System_Int32_">BarcodeBuilder.AztecErrorCorrection</a>.

```csharp
public int? AztecErrorCorrection { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)?

### <a id="TerraFluent_Barcode_BarcodeOptions_Background"></a> Background

Background colour (default white).

```csharp
public BarcodeColor? Background { get; set; }
```

#### Property Value

 [BarcodeColor](barcodecolor.md)?

### <a id="TerraFluent_Barcode_BarcodeOptions_Code39Checksum"></a> Code39Checksum

Adds the Code 39 modulo 43 check character. See <a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_WithChecksum_System_Boolean_">BarcodeBuilder.WithChecksum</a>.

```csharp
public bool Code39Checksum { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="TerraFluent_Barcode_BarcodeOptions_Code39FullAscii"></a> Code39FullAscii

Uses the Code 39 full ASCII extension. See <a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_FullAscii_System_Boolean_">BarcodeBuilder.FullAscii</a>.

```csharp
public bool Code39FullAscii { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="TerraFluent_Barcode_BarcodeOptions_DataMatrixShape"></a> DataMatrixShape

Data Matrix symbol shapes. See <a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_DataMatrixShape_TerraFluent_Barcode_DataMatrixShape_">BarcodeBuilder.DataMatrixShape</a>.

```csharp
public DataMatrixShape? DataMatrixShape { get; set; }
```

#### Property Value

 [DataMatrixShape](datamatrixshape.md)?

### <a id="TerraFluent_Barcode_BarcodeOptions_Foreground"></a> Foreground

Colour of bars and dark modules (default black).

```csharp
public BarcodeColor? Foreground { get; set; }
```

#### Property Value

 [BarcodeColor](barcodecolor.md)?

### <a id="TerraFluent_Barcode_BarcodeOptions_Height"></a> Height

Bar height in pixels for linear barcodes. See <a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_Height_System_Int32_">BarcodeBuilder.Height</a>.

```csharp
public int? Height { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)?

### <a id="TerraFluent_Barcode_BarcodeOptions_HumanReadableText"></a> HumanReadableText

Prints human-readable text under linear barcodes. See <a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_HumanReadableText_System_Boolean_">BarcodeBuilder.HumanReadableText</a>.

```csharp
public bool HumanReadableText { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="TerraFluent_Barcode_BarcodeOptions_ItfChecksum"></a> ItfChecksum

Appends the Interleaved 2 of 5 check digit. See <a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_WithChecksum_System_Boolean_">BarcodeBuilder.WithChecksum</a>.

```csharp
public bool ItfChecksum { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="TerraFluent_Barcode_BarcodeOptions_JpegQuality"></a> JpegQuality

JPEG quality, 1 to 100. See <a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_JpegQuality_System_Int32_">BarcodeBuilder.JpegQuality</a>.

```csharp
public int? JpegQuality { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)?

### <a id="TerraFluent_Barcode_BarcodeOptions_MaxImagePixels"></a> MaxImagePixels

Largest output area in pixels. See <a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_ImageSizeLimit_System_Int64_">BarcodeBuilder.ImageSizeLimit</a>.

```csharp
public long? MaxImagePixels { get; set; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)?

### <a id="TerraFluent_Barcode_BarcodeOptions_MaxiCodeMode"></a> MaxiCodeMode

MaxiCode mode 4, 5 or 6. See <a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_MaxiCodeMode_System_Int32_">BarcodeBuilder.MaxiCodeMode</a>.

```csharp
public int? MaxiCodeMode { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)?

### <a id="TerraFluent_Barcode_BarcodeOptions_ModuleSize"></a> ModuleSize

Module size in pixels. See <a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_ModuleSize_System_Int32_">BarcodeBuilder.ModuleSize</a>.

```csharp
public int? ModuleSize { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)?

### <a id="TerraFluent_Barcode_BarcodeOptions_Pdf417Columns"></a> Pdf417Columns

PDF417 data columns. See <a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_Pdf417Columns_System_Int32_">BarcodeBuilder.Pdf417Columns</a>.

```csharp
public int? Pdf417Columns { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)?

### <a id="TerraFluent_Barcode_BarcodeOptions_Pdf417ErrorCorrection"></a> Pdf417ErrorCorrection

PDF417 error-correction level. See <a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_Pdf417ErrorCorrection_System_Int32_">BarcodeBuilder.Pdf417ErrorCorrection</a>.

```csharp
public int? Pdf417ErrorCorrection { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)?

### <a id="TerraFluent_Barcode_BarcodeOptions_QrErrorCorrection"></a> QrErrorCorrection

QR, Micro QR and rMQR error-correction level. See <a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_ErrorCorrection_TerraFluent_Barcode_QrErrorCorrectionLevel_">BarcodeBuilder.ErrorCorrection</a>.
Ignored for Micro QR when it is H, and for rMQR when it is L or Q.

```csharp
public QrErrorCorrectionLevel? QrErrorCorrection { get; set; }
```

#### Property Value

 [QrErrorCorrectionLevel](qrerrorcorrectionlevel.md)?

### <a id="TerraFluent_Barcode_BarcodeOptions_QuietZone"></a> QuietZone

Quiet zone in modules. See <a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_QuietZone_System_Int32_">BarcodeBuilder.QuietZone</a>.

```csharp
public int? QuietZone { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)?

### <a id="TerraFluent_Barcode_BarcodeOptions_TextSize"></a> TextSize

Font size of the human-readable text in pixels. See <a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_TextSize_System_Int32_">BarcodeBuilder.TextSize</a>.

```csharp
public int? TextSize { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)?

### <a id="TerraFluent_Barcode_BarcodeOptions_TransparentBackground"></a> TransparentBackground

Leaves the background transparent in PNG and SVG output. See <a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_TransparentBackground_System_Boolean_">BarcodeBuilder.TransparentBackground</a>.

```csharp
public bool TransparentBackground { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)
