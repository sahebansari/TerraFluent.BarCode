# Interface IBarcodeGenerator

Namespace: [TerraFluent.Barcode](index.md)  
Assembly: TerraFluent.Barcode.dll  

Barcode generation as a service, for dependency injection and mocking in tests.
Register the default implementation once:

<pre><code class="lang-csharp">services.AddSingleton&lt;IBarcodeGenerator&gt;(new BarcodeGenerator(new BarcodeOptions { HumanReadableText = true }));</code></pre>

Implementations are thread-safe.

```csharp
public interface IBarcodeGenerator
```

## Methods

### <a id="TerraFluent_Barcode_IBarcodeGenerator_Create_TerraFluent_Barcode_BarcodeType_System_String_TerraFluent_Barcode_BarcodeOptions_"></a> Create\(BarcodeType, string, BarcodeOptions?\)

Starts a <a href="barcodebuilder.md">BarcodeBuilder</a> with <code class="paramref">options</code> (or the
generator's defaults) applied, for settings that <a href="barcodeoptions.md">BarcodeOptions</a>
does not cover such as gradients and logos.

```csharp
BarcodeBuilder Create(BarcodeType type, string value, BarcodeOptions? options = null)
```

#### Parameters

`type` [BarcodeType](barcodetype.md)

`value` [string](https://learn.microsoft.com/dotnet/api/system.string)

`options` [BarcodeOptions](barcodeoptions.md)?

#### Returns

 [BarcodeBuilder](barcodebuilder.md)

### <a id="TerraFluent_Barcode_IBarcodeGenerator_Generate_TerraFluent_Barcode_BarcodeType_System_String_TerraFluent_Barcode_BarcodeImageFormat_TerraFluent_Barcode_BarcodeOptions_"></a> Generate\(BarcodeType, string, BarcodeImageFormat, BarcodeOptions?\)

Renders the barcode as an image in <code class="paramref">format</code>.

```csharp
byte[] Generate(BarcodeType type, string value, BarcodeImageFormat format = BarcodeImageFormat.Png, BarcodeOptions? options = null)
```

#### Parameters

`type` [BarcodeType](barcodetype.md)

`value` [string](https://learn.microsoft.com/dotnet/api/system.string)

`format` [BarcodeImageFormat](barcodeimageformat.md)

`options` [BarcodeOptions](barcodeoptions.md)?

#### Returns

 [byte](https://learn.microsoft.com/dotnet/api/system.byte)\[\]

### <a id="TerraFluent_Barcode_IBarcodeGenerator_GenerateDataUri_TerraFluent_Barcode_BarcodeType_System_String_TerraFluent_Barcode_BarcodeImageFormat_TerraFluent_Barcode_BarcodeOptions_"></a> GenerateDataUri\(BarcodeType, string, BarcodeImageFormat, BarcodeOptions?\)

Renders the barcode as a <code>data:</code> URI for an HTML <code>img</code> element.

```csharp
string GenerateDataUri(BarcodeType type, string value, BarcodeImageFormat format = BarcodeImageFormat.Png, BarcodeOptions? options = null)
```

#### Parameters

`type` [BarcodeType](barcodetype.md)

`value` [string](https://learn.microsoft.com/dotnet/api/system.string)

`format` [BarcodeImageFormat](barcodeimageformat.md)

`options` [BarcodeOptions](barcodeoptions.md)?

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="TerraFluent_Barcode_IBarcodeGenerator_GenerateSvg_TerraFluent_Barcode_BarcodeType_System_String_TerraFluent_Barcode_BarcodeOptions_"></a> GenerateSvg\(BarcodeType, string, BarcodeOptions?\)

Renders the barcode as an SVG document.

```csharp
string GenerateSvg(BarcodeType type, string value, BarcodeOptions? options = null)
```

#### Parameters

`type` [BarcodeType](barcodetype.md)

`value` [string](https://learn.microsoft.com/dotnet/api/system.string)

`options` [BarcodeOptions](barcodeoptions.md)?

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="TerraFluent_Barcode_IBarcodeGenerator_WriteToAsync_TerraFluent_Barcode_BarcodeType_System_String_System_IO_Stream_TerraFluent_Barcode_BarcodeImageFormat_TerraFluent_Barcode_BarcodeOptions_System_Threading_CancellationToken_"></a> WriteToAsync\(BarcodeType, string, Stream, BarcodeImageFormat, BarcodeOptions?, CancellationToken\)

Renders the barcode and writes it to <code class="paramref">destination</code> asynchronously.

```csharp
Task WriteToAsync(BarcodeType type, string value, Stream destination, BarcodeImageFormat format = BarcodeImageFormat.Png, BarcodeOptions? options = null, CancellationToken cancellationToken = default)
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
