# Class BarcodeImageFormatExtensions

Namespace: [TerraFluent.Barcode](index.md)  
Assembly: TerraFluent.Barcode.dll  

Helpers for [BarcodeImageFormat](barcodeimageformat.md).

```csharp
public static class BarcodeImageFormatExtensions
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[BarcodeImageFormatExtensions](barcodeimageformatextensions.md)

## Methods

### <a id="TerraFluent_Barcode_BarcodeImageFormatExtensions_FromFilePath_System_String_"></a> FromFilePath\(string\)

Infers the format from a file path's extension (<code>.png</code>, <code>.jpg</code>/<code>.jpeg</code>, <code>.svg</code>).

```csharp
public static BarcodeImageFormat FromFilePath(string path)
```

#### Parameters

`path` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [BarcodeImageFormat](barcodeimageformat.md)

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

The extension is missing or not recognised.

### <a id="TerraFluent_Barcode_BarcodeImageFormatExtensions_ToFileExtension_TerraFluent_Barcode_BarcodeImageFormat_"></a> ToFileExtension\(BarcodeImageFormat\)

Gets the usual file extension of the format, including the dot, e.g. <code>.png</code>.

```csharp
public static string ToFileExtension(this BarcodeImageFormat format)
```

#### Parameters

`format` [BarcodeImageFormat](barcodeimageformat.md)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="TerraFluent_Barcode_BarcodeImageFormatExtensions_ToMimeType_TerraFluent_Barcode_BarcodeImageFormat_"></a> ToMimeType\(BarcodeImageFormat\)

Gets the MIME type of the format, e.g. <code>image/png</code>.

```csharp
public static string ToMimeType(this BarcodeImageFormat format)
```

#### Parameters

`format` [BarcodeImageFormat](barcodeimageformat.md)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)
