# Enum BarcodeImageFormat

Namespace: [TerraFluent.Barcode](index.md)  
Assembly: TerraFluent.Barcode.dll  

Output image formats.

```csharp
public enum BarcodeImageFormat
```

#### Extension Methods

[BarcodeImageFormatExtensions.ToFileExtension\(BarcodeImageFormat\)](TerraFluent.Barcode.BarcodeImageFormatExtensions.md\#TerraFluent\_Barcode\_BarcodeImageFormatExtensions\_ToFileExtension\_TerraFluent\_Barcode\_BarcodeImageFormat\_), 
[BarcodeImageFormatExtensions.ToMimeType\(BarcodeImageFormat\)](TerraFluent.Barcode.BarcodeImageFormatExtensions.md\#TerraFluent\_Barcode\_BarcodeImageFormatExtensions\_ToMimeType\_TerraFluent\_Barcode\_BarcodeImageFormat\_)

## Fields

<a id="TerraFluent_Barcode_BarcodeImageFormat_Png"></a>`Png = 0` 

Lossless PNG (8-bit RGB). The recommended raster format for barcodes.

<a id="TerraFluent_Barcode_BarcodeImageFormat_Jpeg"></a>`Jpeg = 1` 

Baseline JPEG. Lossy: prefer PNG or SVG unless JPEG is required.

<a id="TerraFluent_Barcode_BarcodeImageFormat_Svg"></a>`Svg = 2` 

Scalable vector graphics (UTF-8 XML).
