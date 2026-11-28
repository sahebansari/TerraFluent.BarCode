# Class BarcodeGenerator

Namespace: [TerraFluent.Barcode](index.md)  
Assembly: TerraFluent.Barcode.dll  

Default [IBarcodeGenerator](ibarcodegenerator.md), built on [Barcodes](barcodes.md). It
holds no state besides its default options, so one instance can be shared
(register it as a singleton).

```csharp
public sealed class BarcodeGenerator : IBarcodeGenerator
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[BarcodeGenerator](barcodegenerator.md)

#### Implements

[IBarcodeGenerator](ibarcodegenerator.md)

## Constructors

### <a id="TerraFluent_Barcode_BarcodeGenerator__ctor_TerraFluent_Barcode_BarcodeOptions_"></a> BarcodeGenerator\(BarcodeOptions?\)

Creates a generator. <code class="paramref">defaults</code> is used whenever a call
passes no options of its own; it is read on every call, so do not change
it while the generator is in use.

```csharp
public BarcodeGenerator(BarcodeOptions? defaults = null)
```

#### Parameters

`defaults` [BarcodeOptions](barcodeoptions.md)?

## Methods

### <a id="TerraFluent_Barcode_BarcodeGenerator_Create_TerraFluent_Barcode_BarcodeType_System_String_TerraFluent_Barcode_BarcodeOptions_"></a> Create\(BarcodeType, string, BarcodeOptions?\)

Starts a [BarcodeBuilder](barcodebuilder.md) with <code class="paramref">options</code> (or the
generator's defaults) applied, for settings that [BarcodeOptions](barcodeoptions.md)
does not cover such as gradients and logos.

```csharp
public BarcodeBuilder Create(BarcodeType type, string value, BarcodeOptions? options = null)
```

#### Parameters

`type` [BarcodeType](barcodetype.md)

`value` [string](https://learn.microsoft.com/dotnet/api/system.string)

`options` [BarcodeOptions](barcodeoptions.md)?

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_BarcodeGenerator_Generate_TerraFluent_Barcode_BarcodeType_System_String_TerraFluent_Barcode_BarcodeImageFormat_TerraFluent_Barcode_BarcodeOptions_"></a> Generate\(BarcodeType, string, BarcodeImageFormat, BarcodeOptions?\)

Renders the barcode as an image in <code class="paramref">format</code>.

```csharp
public byte[] Generate(BarcodeType type, string value, BarcodeImageFormat format = BarcodeImageFormat.Png, BarcodeOptions? options = null)
```

#### Parameters

`type` [BarcodeType](barcodetype.md)

`value` [string](https://learn.microsoft.com/dotnet/api/system.string)

`format` [BarcodeImageFormat](barcodeimageformat.md)

`options` [BarcodeOptions](barcodeoptions.md)?

#### Returns

 [byte](https://learn.microsoft.com/dotnet/api/system.byte)\[\]

### <a id="TerraFluent_Barcode_BarcodeGenerator_GenerateDataUri_TerraFluent_Barcode_BarcodeType_System_String_TerraFluent_Barcode_BarcodeImageFormat_TerraFluent_Barcode_BarcodeOptions_"></a> GenerateDataUri\(BarcodeType, string, BarcodeImageFormat, BarcodeOptions?\)

Renders the barcode as a <code>data:</code> URI for an HTML <code>img</code> element.

```csharp
public string GenerateDataUri(BarcodeType type, string value, BarcodeImageFormat format = BarcodeImageFormat.Png, BarcodeOptions? options = null)
```

#### Parameters

`type` [BarcodeType](barcodetype.md)

`value` [string](https://learn.microsoft.com/dotnet/api/system.string)

`format` [BarcodeImageFormat](barcodeimageformat.md)

`options` [BarcodeOptions](barcodeoptions.md)?

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="TerraFluent_Barcode_BarcodeGenerator_GenerateSvg_TerraFluent_Barcode_BarcodeType_System_String_TerraFluent_Barcode_BarcodeOptions_"></a> GenerateSvg\(BarcodeType, string, BarcodeOptions?\)

Renders the barcode as an SVG document.

```csharp
public string GenerateSvg(BarcodeType type, string value, BarcodeOptions? options = null)
```

#### Parameters

`type` [BarcodeType](barcodetype.md)

`value` [string](https://learn.microsoft.com/dotnet/api/system.string)

`options` [BarcodeOptions](barcodeoptions.md)?

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="TerraFluent_Barcode_BarcodeGenerator_WriteToAsync_TerraFluent_Barcode_BarcodeType_System_String_System_IO_Stream_TerraFluent_Barcode_BarcodeImageFormat_TerraFluent_Barcode_BarcodeOptions_System_Threading_CancellationToken_"></a> WriteToAsync\(BarcodeType, string, Stream, BarcodeImageFormat, BarcodeOptions?, CancellationToken\)

Renders the barcode and writes it to <code class="paramref">destination</code> asynchronously.

```csharp
public Task WriteToAsync(BarcodeType type, string value, Stream destination, BarcodeImageFormat format = BarcodeImageFormat.Png, BarcodeOptions? options = null, CancellationToken cancellationToken = default)
```

#### Parameters

`type` [BarcodeType](barcodetype.md)

`value` [string](https://learn.microsoft.com/dotnet/api/system.string)

`destination` [Stream](https://learn.microsoft.com/dotnet/api/system.io.stream)

`format` [BarcodeImageFormat](barcodeimageformat.md)

`options` [BarcodeOptions](barcodeoptions.md)?

`cancellationToken` [CancellationToken](https://learn.microsoft.com/dotnet/api/system.threading.cancellationtoken)

#### Returns

 [Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task)
