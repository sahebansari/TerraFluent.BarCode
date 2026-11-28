# Class BarcodeMatrix

Namespace: [TerraFluent.Barcode](index.md)  
Assembly: TerraFluent.Barcode.dll  

The encoded symbol as a grid of dark/light modules, before any scaling,
colouring or quiet zone. A linear (1D) barcode is a single row; a QR code
is a square. Useful for custom renderers.

```csharp
public sealed class BarcodeMatrix
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[BarcodeMatrix](barcodematrix.md)

## Properties

### <a id="TerraFluent_Barcode_BarcodeMatrix_Height"></a> Height

Height in modules (1 for linear barcodes).

```csharp
public int Height { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="TerraFluent_Barcode_BarcodeMatrix_IsLinear"></a> IsLinear

<code>true</code> for 1D barcodes, whose single row is stretched to the bar height when rendered.

```csharp
public bool IsLinear { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="TerraFluent_Barcode_BarcodeMatrix_RecommendedQuietZone"></a> RecommendedQuietZone

The quiet zone, in modules, that the symbology's specification asks for on each side.

```csharp
public int RecommendedQuietZone { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="TerraFluent_Barcode_BarcodeMatrix_Width"></a> Width

Width in modules.

```csharp
public int Width { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="TerraFluent_Barcode_BarcodeMatrix_Item_System_Int32_System_Int32_"></a> this\[int, int\]

Whether the module at (<code class="paramref">x</code>, <code class="paramref">y</code>) is dark.

```csharp
public bool this[int x, int y] { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="TerraFluent_Barcode_BarcodeMatrix_ToString"></a> ToString\(\)

Renders the matrix as text, one line per row, for debugging (<code>#</code> = dark).

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)
