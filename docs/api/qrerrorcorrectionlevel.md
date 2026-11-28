# Enum QrErrorCorrectionLevel

Namespace: [TerraFluent.Barcode](index.md)  
Assembly: TerraFluent.Barcode.dll  

QR code error-correction level: how much of the symbol can be damaged and still read.

```csharp
public enum QrErrorCorrectionLevel
```

## Fields

<a id="TerraFluent_Barcode_QrErrorCorrectionLevel_L"></a>`L = 0` 

About 7% of codewords can be restored. Smallest symbol.

<a id="TerraFluent_Barcode_QrErrorCorrectionLevel_M"></a>`M = 1` 

About 15% of codewords can be restored. The default.

<a id="TerraFluent_Barcode_QrErrorCorrectionLevel_Q"></a>`Q = 2` 

About 25% of codewords can be restored.

<a id="TerraFluent_Barcode_QrErrorCorrectionLevel_H"></a>`H = 3` 

About 30% of codewords can be restored. Use when printing a logo over the code or on rough surfaces.
