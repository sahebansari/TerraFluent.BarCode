# Class BarcodeOptions

Namespace: [TerraFluent.Barcode](index.md)  
Assembly: TerraFluent.Barcode.dll  

Settings used by [IBarcodeGenerator](ibarcodegenerator.md). Every property is optional;
a <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a> or <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">false</a> value keeps the default
that [BarcodeBuilder](barcodebuilder.md) uses. Settings that only apply to some
symbologies (for example [BarcodeOptions.Height](barcodeoptions.md#TerraFluent_Barcode_BarcodeOptions_Height) or [BarcodeOptions.QrErrorCorrection](barcodeoptions.md#TerraFluent_Barcode_BarcodeOptions_QrErrorCorrection))
are ignored for the others, so one instance can serve every type.

```csharp
public sealed class BarcodeOptions
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[BarcodeOptions](barcodeoptions.md)

## Properties

### <a id="TerraFluent_Barcode_BarcodeOptions_Background"></a> Background

Background colour (default white).

```csharp
public BarcodeColor? Background { get; set; }
```

#### Property Value

 [BarcodeColor](barcodecolor.md)?

### <a id="TerraFluent_Barcode_BarcodeOptions_Code39Checksum"></a> Code39Checksum

Adds the Code 39 modulo 43 check character. See [BarcodeBuilder.WithChecksum](barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_WithChecksum_System_Boolean_).

```csharp
public bool Code39Checksum { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="TerraFluent_Barcode_BarcodeOptions_Code39FullAscii"></a> Code39FullAscii

Uses the Code 39 full ASCII extension. See [BarcodeBuilder.FullAscii](barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_FullAscii_System_Boolean_).

```csharp
public bool Code39FullAscii { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="TerraFluent_Barcode_BarcodeOptions_DataMatrixShape"></a> DataMatrixShape

Data Matrix symbol shapes. See [BarcodeBuilder.DataMatrixShape](barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_DataMatrixShape_TerraFluent_Barcode_DataMatrixShape_).

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

Bar height in pixels for linear barcodes. See [BarcodeBuilder.Height](barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_Height_System_Int32_).

```csharp
public int? Height { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)?

### <a id="TerraFluent_Barcode_BarcodeOptions_HumanReadableText"></a> HumanReadableText

Prints human-readable text under linear barcodes. See [BarcodeBuilder.HumanReadableText](barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_HumanReadableText_System_Boolean_).

```csharp
public bool HumanReadableText { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="TerraFluent_Barcode_BarcodeOptions_JpegQuality"></a> JpegQuality

JPEG quality, 1 to 100. See [BarcodeBuilder.JpegQuality](barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_JpegQuality_System_Int32_).

```csharp
public int? JpegQuality { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)?

### <a id="TerraFluent_Barcode_BarcodeOptions_MaxImagePixels"></a> MaxImagePixels

Largest output area in pixels. See [BarcodeBuilder.ImageSizeLimit](barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_ImageSizeLimit_System_Int64_).

```csharp
public long? MaxImagePixels { get; set; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)?

### <a id="TerraFluent_Barcode_BarcodeOptions_ModuleSize"></a> ModuleSize

Module size in pixels. See [BarcodeBuilder.ModuleSize](barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_ModuleSize_System_Int32_).

```csharp
public int? ModuleSize { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)?

### <a id="TerraFluent_Barcode_BarcodeOptions_Pdf417Columns"></a> Pdf417Columns

PDF417 data columns. See [BarcodeBuilder.Pdf417Columns](barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_Pdf417Columns_System_Int32_).

```csharp
public int? Pdf417Columns { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)?

### <a id="TerraFluent_Barcode_BarcodeOptions_Pdf417ErrorCorrection"></a> Pdf417ErrorCorrection

PDF417 error-correction level. See [BarcodeBuilder.Pdf417ErrorCorrection](barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_Pdf417ErrorCorrection_System_Int32_).

```csharp
public int? Pdf417ErrorCorrection { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)?

### <a id="TerraFluent_Barcode_BarcodeOptions_QrErrorCorrection"></a> QrErrorCorrection

QR error-correction level. See [BarcodeBuilder.ErrorCorrection](barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_ErrorCorrection_TerraFluent_Barcode_QrErrorCorrectionLevel_).

```csharp
public QrErrorCorrectionLevel? QrErrorCorrection { get; set; }
```

#### Property Value

 [QrErrorCorrectionLevel](qrerrorcorrectionlevel.md)?

### <a id="TerraFluent_Barcode_BarcodeOptions_QuietZone"></a> QuietZone

Quiet zone in modules. See [BarcodeBuilder.QuietZone](barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_QuietZone_System_Int32_).

```csharp
public int? QuietZone { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)?

### <a id="TerraFluent_Barcode_BarcodeOptions_TextSize"></a> TextSize

Font size of the human-readable text in pixels. See [BarcodeBuilder.TextSize](barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_TextSize_System_Int32_).

```csharp
public int? TextSize { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)?

### <a id="TerraFluent_Barcode_BarcodeOptions_TransparentBackground"></a> TransparentBackground

Leaves the background transparent in PNG and SVG output. See [BarcodeBuilder.TransparentBackground](barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_TransparentBackground_System_Boolean_).

```csharp
public bool TransparentBackground { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)
