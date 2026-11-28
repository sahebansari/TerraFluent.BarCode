# Class BarcodeBuilder

Namespace: [TerraFluent.Barcode](index.md)  
Assembly: TerraFluent.Barcode.dll  

Fluent configuration and output for one barcode. Configuration methods
return the same builder; output methods (<code>To...</code>, <a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_WriteTo_System_IO_Stream_TerraFluent_Barcode_BarcodeImageFormat_">BarcodeBuilder.WriteTo</a>,
<a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_Save_System_String_">BarcodeBuilder.Save</a>) can be called any number of times.

```csharp
public sealed class BarcodeBuilder
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[BarcodeBuilder](barcodebuilder.md)

## Remarks

<p>
<b>Thread safety:</b> configure a builder on one thread. Once configured, its
output methods may be called from several threads at the same time. The
static <a href="barcodes.md">Barcodes</a> methods are thread-safe, so the simplest pattern
is one builder per barcode.
</p>
<p>
<b>Errors:</b> an invalid value or argument throws <a href="https://learn.microsoft.com/dotnet/api/system.argumentexception">ArgumentException</a>
(or <a href="https://learn.microsoft.com/dotnet/api/system.argumentnullexception">ArgumentNullException</a>, <a href="https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception">ArgumentOutOfRangeException</a>
and <a href="https://learn.microsoft.com/dotnet/api/system.formatexception">FormatException</a> for hex colours). Options that cannot be
combined throw <a href="https://learn.microsoft.com/dotnet/api/system.invalidoperationexception">InvalidOperationException</a>, either when set (an
option for another symbology) or when rendering (a logo with too little error
correction, or colours with too little contrast).
</p>

## Fields

### <a id="TerraFluent_Barcode_BarcodeBuilder_DefaultBarHeight"></a> DefaultBarHeight

Default bar height for linear barcodes, in pixels.

```csharp
public const int DefaultBarHeight = 80
```

#### Field Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="TerraFluent_Barcode_BarcodeBuilder_DefaultJpegQuality"></a> DefaultJpegQuality

Default JPEG quality (1-100).

```csharp
public const int DefaultJpegQuality = 90
```

#### Field Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="TerraFluent_Barcode_BarcodeBuilder_DefaultLinearModuleSize"></a> DefaultLinearModuleSize

Default width of one module (the narrowest bar) for linear barcodes, in pixels.

```csharp
public const int DefaultLinearModuleSize = 2
```

#### Field Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="TerraFluent_Barcode_BarcodeBuilder_DefaultLogoScale"></a> DefaultLogoScale

Default logo size as a fraction of the QR code's width.

```csharp
public const double DefaultLogoScale = 0.2
```

#### Field Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="TerraFluent_Barcode_BarcodeBuilder_DefaultMaxiCodeModuleSize"></a> DefaultMaxiCodeModuleSize

Default width of one MaxiCode hexagon, in pixels.

```csharp
public const int DefaultMaxiCodeModuleSize = 6
```

#### Field Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="TerraFluent_Barcode_BarcodeBuilder_DefaultPdf417ModuleSize"></a> DefaultPdf417ModuleSize

Default width of one PDF417 module, in pixels (rows are three modules high).

```csharp
public const int DefaultPdf417ModuleSize = 2
```

#### Field Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="TerraFluent_Barcode_BarcodeBuilder_DefaultQrModuleSize"></a> DefaultQrModuleSize

Default size of one QR or Data Matrix module, in pixels.

```csharp
public const int DefaultQrModuleSize = 4
```

#### Field Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="TerraFluent_Barcode_BarcodeBuilder_MaxImageDimension"></a> MaxImageDimension

Largest allowed output width or height, in pixels. Rendering a bigger
image throws <a href="https://learn.microsoft.com/dotnet/api/system.argumentexception">ArgumentException</a>, so untrusted size settings
cannot exhaust memory.

```csharp
public const int MaxImageDimension = 32768
```

#### Field Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="TerraFluent_Barcode_BarcodeBuilder_MaxImagePixels"></a> MaxImagePixels

Largest allowed output area (width times height), in pixels: 64 megapixels.
Use <a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_ImageSizeLimit_System_Int64_">BarcodeBuilder.ImageSizeLimit</a> to set a lower limit, for example when
sizes come from untrusted input on a server.

```csharp
public const long MaxImagePixels = 67108864
```

#### Field Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="TerraFluent_Barcode_BarcodeBuilder_MaxLogoScale"></a> MaxLogoScale

Largest allowed logo size as a fraction of the QR code's width.

```csharp
public const double MaxLogoScale = 0.3
```

#### Field Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="TerraFluent_Barcode_BarcodeBuilder_MaxLogoScaleAtQ"></a> MaxLogoScaleAtQ

Largest logo scale allowed at error-correction level Q; level H allows up to <a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_MaxLogoScale">BarcodeBuilder.MaxLogoScale</a>.

```csharp
public const double MaxLogoScaleAtQ = 0.25
```

#### Field Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="TerraFluent_Barcode_BarcodeBuilder_MinContrastRatio"></a> MinContrastRatio

Smallest WCAG contrast ratio accepted between the background and the
bars, gradient stops or eye colour, unless <a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_AllowLowContrast_System_Boolean_">BarcodeBuilder.AllowLowContrast</a> is set.

```csharp
public const double MinContrastRatio = 3
```

#### Field Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

## Properties

### <a id="TerraFluent_Barcode_BarcodeBuilder_Type"></a> Type

The symbology.

```csharp
public BarcodeType Type { get; }
```

#### Property Value

 [BarcodeType](barcodetype.md)

### <a id="TerraFluent_Barcode_BarcodeBuilder_Value"></a> Value

The value being encoded, as passed in.

```csharp
public string Value { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="TerraFluent_Barcode_BarcodeBuilder_AddOn_System_String_"></a> AddOn\(string\)

Appends a 2-digit (EAN-2) or 5-digit (EAN-5) add-on, as used for periodical
issue numbers and book prices. EAN-13 and UPC-A only.

```csharp
public BarcodeBuilder AddOn(string digits)
```

#### Parameters

`digits` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

<code class="paramref">digits</code> is not 2 or 5 digits.

### <a id="TerraFluent_Barcode_BarcodeBuilder_AllowLowContrast_System_Boolean_"></a> AllowLowContrast\(bool\)

Turns off the contrast check, which otherwise throws
<a href="https://learn.microsoft.com/dotnet/api/system.invalidoperationexception">InvalidOperationException</a> when rendering if the background and
the bars, gradient stops or eye colour have a contrast ratio below
<a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_MinContrastRatio">BarcodeBuilder.MinContrastRatio</a>. Only use this for decorative output that
you have tested with your scanners.

```csharp
public BarcodeBuilder AllowLowContrast(bool allow = true)
```

#### Parameters

`allow` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_BarcodeBuilder_AztecErrorCorrection_System_Int32_"></a> AztecErrorCorrection\(int\)

Minimum share of the symbol's codewords used for error correction, in percent,
5 to 90; three more check words are always added (default 23, as ISO/IEC 24778
recommends). More error correction makes a bigger symbol that survives more damage.
Aztec only.

```csharp
public BarcodeBuilder AztecErrorCorrection(int percent)
```

#### Parameters

`percent` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_BarcodeBuilder_Background_TerraFluent_Barcode_BarcodeColor_"></a> Background\(BarcodeColor\)

Background and quiet-zone colour (default white).

```csharp
public BarcodeBuilder Background(BarcodeColor color)
```

#### Parameters

`color` [BarcodeColor](barcodecolor.md)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_BarcodeBuilder_Background_System_String_"></a> Background\(string\)

Background and quiet-zone colour as <code>#RRGGBB</code> or <code>#RGB</code>.

```csharp
public BarcodeBuilder Background(string hex)
```

#### Parameters

`hex` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_BarcodeBuilder_DataMatrixShape_TerraFluent_Barcode_DataMatrixShape_"></a> DataMatrixShape\(DataMatrixShape\)

Which symbol shapes may be used (default <a href="datamatrixshape.md#TerraFluent_Barcode_DataMatrixShape_Square">DataMatrixShape.Square</a>). Data Matrix only.

```csharp
public BarcodeBuilder DataMatrixShape(DataMatrixShape shape)
```

#### Parameters

`shape` [DataMatrixShape](datamatrixshape.md)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_BarcodeBuilder_Encode"></a> Encode\(\)

Encodes the value into its module matrix without rendering it. The
result is cached until an encoding option changes.

```csharp
public BarcodeMatrix Encode()
```

#### Returns

 [BarcodeMatrix](barcodematrix.md)

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

The value cannot be encoded in this symbology.

### <a id="TerraFluent_Barcode_BarcodeBuilder_ErrorCorrection_TerraFluent_Barcode_QrErrorCorrectionLevel_"></a> ErrorCorrection\(QrErrorCorrectionLevel\)

QR error-correction level (default <a href="qrerrorcorrectionlevel.md#TerraFluent_Barcode_QrErrorCorrectionLevel_M">QrErrorCorrectionLevel.M</a>). QR codes only.

```csharp
public BarcodeBuilder ErrorCorrection(QrErrorCorrectionLevel level)
```

#### Parameters

`level` [QrErrorCorrectionLevel](qrerrorcorrectionlevel.md)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_BarcodeBuilder_EyeColor_TerraFluent_Barcode_BarcodeColor_"></a> EyeColor\(BarcodeColor\)

Colour of the three corner "eyes", if different from the foreground or gradient. QR codes only.

```csharp
public BarcodeBuilder EyeColor(BarcodeColor color)
```

#### Parameters

`color` [BarcodeColor](barcodecolor.md)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_BarcodeBuilder_EyeColor_System_String_"></a> EyeColor\(string\)

Colour of the three corner "eyes" as <code>#RRGGBB</code> or <code>#RGB</code>. QR codes only.

```csharp
public BarcodeBuilder EyeColor(string hex)
```

#### Parameters

`hex` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_BarcodeBuilder_EyeStyle_TerraFluent_Barcode_QrEyeStyle_"></a> EyeStyle\(QrEyeStyle\)

Shape of the three corner "eyes" (default <a href="qreyestyle.md#TerraFluent_Barcode_QrEyeStyle_Square">QrEyeStyle.Square</a>). QR codes only.

```csharp
public BarcodeBuilder EyeStyle(QrEyeStyle style)
```

#### Parameters

`style` [QrEyeStyle](qreyestyle.md)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_BarcodeBuilder_Foreground_TerraFluent_Barcode_BarcodeColor_"></a> Foreground\(BarcodeColor\)

Colour of bars/dark modules (default black).

```csharp
public BarcodeBuilder Foreground(BarcodeColor color)
```

#### Parameters

`color` [BarcodeColor](barcodecolor.md)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_BarcodeBuilder_Foreground_System_String_"></a> Foreground\(string\)

Colour of bars/dark modules as <code>#RRGGBB</code> or <code>#RGB</code>.

```csharp
public BarcodeBuilder Foreground(string hex)
```

#### Parameters

`hex` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_BarcodeBuilder_FullAscii_System_Boolean_"></a> FullAscii\(bool\)

Encodes all 128 ASCII characters, including lower case, using the Code 39
full ASCII extension (each extra character takes two symbol characters).
The scanner must have full ASCII mode turned on. Code 39 only.

```csharp
public BarcodeBuilder FullAscii(bool enable = true)
```

#### Parameters

`enable` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_BarcodeBuilder_GetImageSize"></a> GetImageSize\(\)

The output image size in pixels with the current settings.

```csharp
public (int Width, int Height) GetImageSize()
```

#### Returns

 \([int](https://learn.microsoft.com/dotnet/api/system.int32) [Width](https://learn.microsoft.com/dotnet/api/system.valuetuple-system.int32,system.int32-.width), [int](https://learn.microsoft.com/dotnet/api/system.int32) [Height](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32\-.height)\)

### <a id="TerraFluent_Barcode_BarcodeBuilder_Gradient_TerraFluent_Barcode_BarcodeColor_TerraFluent_Barcode_BarcodeColor_TerraFluent_Barcode_GradientDirection_"></a> Gradient\(BarcodeColor, BarcodeColor, GradientDirection\)

Fills bars/dark modules with a gradient from <code class="paramref">start</code> to
<code class="paramref">end</code> instead of the flat foreground colour. Keep both
colours dark against the background so scanners see enough contrast.

```csharp
public BarcodeBuilder Gradient(BarcodeColor start, BarcodeColor end, GradientDirection direction = GradientDirection.Diagonal)
```

#### Parameters

`start` [BarcodeColor](barcodecolor.md)

`end` [BarcodeColor](barcodecolor.md)

`direction` [GradientDirection](gradientdirection.md)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_BarcodeBuilder_Gradient_System_String_System_String_TerraFluent_Barcode_GradientDirection_"></a> Gradient\(string, string, GradientDirection\)

Gradient fill with colours given as <code>#RRGGBB</code> or <code>#RGB</code>.

```csharp
public BarcodeBuilder Gradient(string startHex, string endHex, GradientDirection direction = GradientDirection.Diagonal)
```

#### Parameters

`startHex` [string](https://learn.microsoft.com/dotnet/api/system.string)

`endHex` [string](https://learn.microsoft.com/dotnet/api/system.string)

`direction` [GradientDirection](gradientdirection.md)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_BarcodeBuilder_Height_System_Int32_"></a> Height\(int\)

Bar height of a linear barcode in pixels (default 80), not counting
human-readable text. Not allowed for 2D codes, whose size follows from ModuleSize.

```csharp
public BarcodeBuilder Height(int pixels)
```

#### Parameters

`pixels` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_BarcodeBuilder_HumanReadableText_System_Boolean_"></a> HumanReadableText\(bool\)

Prints the human-readable text under a linear barcode: EAN/UPC digits in
their standard positions with extended guard bars, or the value centred
under Code 128, Code 39, GS1-128 (with AIs in parentheses), Interleaved
2 of 5 and ITF-14 (with the check digit) and Codabar. The image
grows by about one text line. Linear barcodes only.

```csharp
public BarcodeBuilder HumanReadableText(bool show = true)
```

#### Parameters

`show` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_BarcodeBuilder_ImageSizeLimit_System_Int64_"></a> ImageSizeLimit\(long\)

Lowers the largest output area this builder renders, in pixels (default and
maximum <a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_MaxImagePixels">BarcodeBuilder.MaxImagePixels</a>). Rendering a bigger image throws
<a href="https://learn.microsoft.com/dotnet/api/system.argumentexception">ArgumentException</a> before any work is done. Styled output
renders in bands, so memory use grows with the image width, but CPU time
and the size of the result grow with the area: set a limit when module size,
height or the value come from untrusted input.

```csharp
public BarcodeBuilder ImageSizeLimit(long maxPixels)
```

#### Parameters

`maxPixels` [long](https://learn.microsoft.com/dotnet/api/system.int64)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_BarcodeBuilder_JpegQuality_System_Int32_"></a> JpegQuality\(int\)

JPEG quality from 1 to 100 (default 90). Only affects <a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_ToJpeg">BarcodeBuilder.ToJpeg</a> and JPEG outputs.

```csharp
public BarcodeBuilder JpegQuality(int quality)
```

#### Parameters

`quality` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_BarcodeBuilder_Logo_System_Byte___System_Double_"></a> Logo\(byte\[\], double\)

Places a logo (a PNG file's bytes; transparency is kept) in the centre of
a QR code and clears the modules behind it. <code class="paramref">scale</code> is the
logo's size as a fraction of the code's width, from 0.05 to 0.3 (default 0.2);
it is shrunk if needed so the corner eyes and format modules stay visible.
Unless <a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_ErrorCorrection_TerraFluent_Barcode_QrErrorCorrectionLevel_">BarcodeBuilder.ErrorCorrection</a> is set explicitly, the code switches to
level H so it still scans with the covered modules missing. Rendering throws
<a href="https://learn.microsoft.com/dotnet/api/system.invalidoperationexception">InvalidOperationException</a> if the error correction is set
explicitly to L or M, or to Q with a scale above <a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_MaxLogoScaleAtQ">BarcodeBuilder.MaxLogoScaleAtQ</a>,
because such codes often fail to scan. QR codes only.

```csharp
public BarcodeBuilder Logo(byte[] png, double scale = 0.2)
```

#### Parameters

`png` [byte](https://learn.microsoft.com/dotnet/api/system.byte)\[\]

`scale` [double](https://learn.microsoft.com/dotnet/api/system.double)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

<code class="paramref">png</code> is not a readable PNG file.

### <a id="TerraFluent_Barcode_BarcodeBuilder_MaxiCodeMode_System_Int32_"></a> MaxiCodeMode\(int\)

MaxiCode mode: 4 (default, standard error correction, 93 codewords of data),
5 (enhanced error correction, 77 codewords) or 6 (reader programming). Modes 2
and 3 are chosen by the <a href="barcodes.md#TerraFluent_Barcode_Barcodes_MaxiCode_System_String_System_Int32_System_Int32_System_String_">Barcodes.MaxiCode</a>
overload that takes a postal code. MaxiCode only.

```csharp
public BarcodeBuilder MaxiCodeMode(int mode)
```

#### Parameters

`mode` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_BarcodeBuilder_ModuleSize_System_Int32_"></a> ModuleSize\(int\)

Size of one module in pixels: the narrowest bar of a linear barcode, or
one square of a 2D code (for MaxiCode, the width of one hexagon). Defaults
to 2 for linear barcodes and PDF417, 4 for QR codes, Data Matrix and Aztec,
and 6 for MaxiCode.

```csharp
public BarcodeBuilder ModuleSize(int pixels)
```

#### Parameters

`pixels` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_BarcodeBuilder_ModuleStyle_TerraFluent_Barcode_QrModuleStyle_"></a> ModuleStyle\(QrModuleStyle\)

Shape of the QR code's data modules (default <a href="qrmodulestyle.md#TerraFluent_Barcode_QrModuleStyle_Square">QrModuleStyle.Square</a>).
QR codes only. Every style keeps each module's centre where a scanner expects it.

```csharp
public BarcodeBuilder ModuleStyle(QrModuleStyle style)
```

#### Parameters

`style` [QrModuleStyle](qrmodulestyle.md)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_BarcodeBuilder_Pdf417Columns_System_Int32_"></a> Pdf417Columns\(int\)

Number of data columns, 1 to 30 (default: chosen for a roughly 3:1 symbol). PDF417 only.

```csharp
public BarcodeBuilder Pdf417Columns(int columns)
```

#### Parameters

`columns` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_BarcodeBuilder_Pdf417ErrorCorrection_System_Int32_"></a> Pdf417ErrorCorrection\(int\)

Error-correction level 0 to 8; level n adds 2^(n+1) error-correction
codewords (default: the level ISO/IEC 15438 recommends for the data size,
2 to 6). PDF417 only.

```csharp
public BarcodeBuilder Pdf417ErrorCorrection(int level)
```

#### Parameters

`level` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_BarcodeBuilder_QuietZone_System_Int32_"></a> QuietZone\(int\)

Blank margin in modules on each side (linear barcodes: left and right only).
Defaults to what the symbology's specification asks for: 10 for Code 128/39,
Interleaved 2 of 5, ITF-14 and Codabar, 11 for EAN-13, 9 for UPC-A, 7 for
EAN-8, 4 for QR codes, 2 for Data Matrix, PDF417 and Aztec, and 1 for
MaxiCode. Scanners may fail with a smaller value.

```csharp
public BarcodeBuilder QuietZone(int modules)
```

#### Parameters

`modules` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_BarcodeBuilder_RoundedBars_System_Boolean_"></a> RoundedBars\(bool\)

Gives the bars of a linear barcode rounded, pill-shaped ends. Linear barcodes only.

```csharp
public BarcodeBuilder RoundedBars(bool rounded = true)
```

#### Parameters

`rounded` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_BarcodeBuilder_Save_System_String_"></a> Save\(string\)

Saves the image to <code class="paramref">path</code>, choosing the format from its extension (.png, .jpg, .jpeg, .svg).

```csharp
public void Save(string path)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="TerraFluent_Barcode_BarcodeBuilder_Save_System_String_TerraFluent_Barcode_BarcodeImageFormat_"></a> Save\(string, BarcodeImageFormat\)

Saves the image to <code class="paramref">path</code> in <code class="paramref">format</code>.

```csharp
public void Save(string path, BarcodeImageFormat format)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

`format` [BarcodeImageFormat](barcodeimageformat.md)

### <a id="TerraFluent_Barcode_BarcodeBuilder_SaveAsync_System_String_System_Threading_CancellationToken_"></a> SaveAsync\(string, CancellationToken\)

Saves the image to <code class="paramref">path</code> asynchronously, choosing the
format from its extension (.png, .jpg, .jpeg, .svg).

```csharp
public Task SaveAsync(string path, CancellationToken cancellationToken = default)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

`cancellationToken` [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken)

#### Returns

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task)

### <a id="TerraFluent_Barcode_BarcodeBuilder_SaveAsync_System_String_TerraFluent_Barcode_BarcodeImageFormat_System_Threading_CancellationToken_"></a> SaveAsync\(string, BarcodeImageFormat, CancellationToken\)

Saves the image to <code class="paramref">path</code> in <code class="paramref">format</code> asynchronously.
Encoding is CPU-bound and runs on the calling thread before the file is
written; <code class="paramref">cancellationToken</code> is checked while encoding too.

```csharp
public Task SaveAsync(string path, BarcodeImageFormat format, CancellationToken cancellationToken = default)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

`format` [BarcodeImageFormat](barcodeimageformat.md)

`cancellationToken` [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken)

#### Returns

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task)

### <a id="TerraFluent_Barcode_BarcodeBuilder_TextSize_System_Int32_"></a> TextSize\(int\)

Font size of the human-readable text, in pixels per em. Defaults to 10
modules for EAN/UPC and 9 modules for other linear barcodes. Text that is
wider than the bars is shrunk to fit. Linear barcodes only.

```csharp
public BarcodeBuilder TextSize(int pixels)
```

#### Parameters

`pixels` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_BarcodeBuilder_ToBase64_TerraFluent_Barcode_BarcodeImageFormat_"></a> ToBase64\(BarcodeImageFormat\)

Renders the image as a base64 string.

```csharp
public string ToBase64(BarcodeImageFormat format = BarcodeImageFormat.Png)
```

#### Parameters

`format` [BarcodeImageFormat](barcodeimageformat.md)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="TerraFluent_Barcode_BarcodeBuilder_ToBytes_TerraFluent_Barcode_BarcodeImageFormat_"></a> ToBytes\(BarcodeImageFormat\)

Renders the image in <code class="paramref">format</code> as bytes (SVG as UTF-8 without BOM).

```csharp
public byte[] ToBytes(BarcodeImageFormat format = BarcodeImageFormat.Png)
```

#### Parameters

`format` [BarcodeImageFormat](barcodeimageformat.md)

#### Returns

 [byte](https://learn.microsoft.com/dotnet/api/system.byte)\[\]

### <a id="TerraFluent_Barcode_BarcodeBuilder_ToDataUri_TerraFluent_Barcode_BarcodeImageFormat_"></a> ToDataUri\(BarcodeImageFormat\)

Renders the image as a <code>data:</code> URI, ready for an HTML <code>img src</code> or CSS <code>url()</code>.

```csharp
public string ToDataUri(BarcodeImageFormat format = BarcodeImageFormat.Png)
```

#### Parameters

`format` [BarcodeImageFormat](barcodeimageformat.md)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="TerraFluent_Barcode_BarcodeBuilder_ToJpeg"></a> ToJpeg\(\)

Renders a baseline JPEG at the configured <a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_JpegQuality_System_Int32_">BarcodeBuilder.JpegQuality</a>.

```csharp
public byte[] ToJpeg()
```

#### Returns

 [byte](https://learn.microsoft.com/dotnet/api/system.byte)\[\]

### <a id="TerraFluent_Barcode_BarcodeBuilder_ToPng"></a> ToPng\(\)

Renders a PNG: 1-bit indexed for plain barcodes, 8-bit RGB with
anti-aliasing when a style (gradient, rounded shapes, logo) is set.

```csharp
public byte[] ToPng()
```

#### Returns

 [byte](https://learn.microsoft.com/dotnet/api/system.byte)\[\]

### <a id="TerraFluent_Barcode_BarcodeBuilder_ToStream_TerraFluent_Barcode_BarcodeImageFormat_"></a> ToStream\(BarcodeImageFormat\)

Renders the image into a new <a href="https://learn.microsoft.com/dotnet/api/system.io.memorystream">MemoryStream</a> positioned at the start. The caller disposes it.

```csharp
public Stream ToStream(BarcodeImageFormat format = BarcodeImageFormat.Png)
```

#### Parameters

`format` [BarcodeImageFormat](barcodeimageformat.md)

#### Returns

 [Stream](https://learn.microsoft.com/dotnet/api/system.io.stream)

### <a id="TerraFluent_Barcode_BarcodeBuilder_ToSvg"></a> ToSvg\(\)

Renders an SVG document.

```csharp
public string ToSvg()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="TerraFluent_Barcode_BarcodeBuilder_TransparentBackground_System_Boolean_"></a> TransparentBackground\(bool\)

Leaves the background transparent in PNG and SVG output. The contrast
check still uses <a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_Background_TerraFluent_Barcode_BarcodeColor_">BarcodeBuilder.Background</a>, so set it to the
colour of the surface the code will be placed on. JPEG has no
transparency, so JPEG output then throws <a href="https://learn.microsoft.com/dotnet/api/system.invalidoperationexception">InvalidOperationException</a>.

```csharp
public BarcodeBuilder TransparentBackground(bool transparent = true)
```

#### Parameters

`transparent` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_BarcodeBuilder_WithChecksum_System_Boolean_"></a> WithChecksum\(bool\)

Adds the optional check character: modulo 43 for Code 39, or the modulo 10
check digit (weights 3 and 1, as GS1 uses) for Interleaved 2 of 5, which then
needs an odd number of digits. Code 39 and Interleaved 2 of 5 only.

```csharp
public BarcodeBuilder WithChecksum(bool include = true)
```

#### Parameters

`include` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_BarcodeBuilder_WriteTo_System_IO_Stream_TerraFluent_Barcode_BarcodeImageFormat_"></a> WriteTo\(Stream, BarcodeImageFormat\)

Writes the image to <code class="paramref">destination</code>, encoding straight into
the stream without an intermediate copy. The stream is left open.

```csharp
public void WriteTo(Stream destination, BarcodeImageFormat format = BarcodeImageFormat.Png)
```

#### Parameters

`destination` [Stream](https://learn.microsoft.com/dotnet/api/system.io.stream)

`format` [BarcodeImageFormat](barcodeimageformat.md)

### <a id="TerraFluent_Barcode_BarcodeBuilder_WriteToAsync_System_IO_Stream_TerraFluent_Barcode_BarcodeImageFormat_System_Threading_CancellationToken_"></a> WriteToAsync\(Stream, BarcodeImageFormat, CancellationToken\)

Writes the image to <code class="paramref">destination</code> asynchronously. The stream is left open.
Encoding is CPU-bound and runs on the calling thread before the write is
awaited; <code class="paramref">cancellationToken</code> is checked while encoding too.

```csharp
public Task WriteToAsync(Stream destination, BarcodeImageFormat format = BarcodeImageFormat.Png, CancellationToken cancellationToken = default)
```

#### Parameters

`destination` [Stream](https://learn.microsoft.com/dotnet/api/system.io.stream)

`format` [BarcodeImageFormat](barcodeimageformat.md)

`cancellationToken` [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken)

#### Returns

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task)
