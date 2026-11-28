# Enum DataMatrixShape

Namespace: [TerraFluent.Barcode](index.md)  
Assembly: TerraFluent.Barcode.dll  

Which Data Matrix symbol shapes may be chosen.

```csharp
public enum DataMatrixShape
```

## Fields

<a id="TerraFluent_Barcode_DataMatrixShape_Square"></a>`Square = 0` 

Square symbols only, 10x10 to 144x144 (the default, and what most scanners expect).

<a id="TerraFluent_Barcode_DataMatrixShape_Rectangle"></a>`Rectangle = 1` 

Rectangular symbols only, 8x18 to 16x48, for narrow labels.

<a id="TerraFluent_Barcode_DataMatrixShape_Any"></a>`Any = 2` 

The smallest symbol of either shape.
