# Struct BarcodeColor

Namespace: [TerraFluent.Barcode](index.md)  
Assembly: TerraFluent.Barcode.dll  

An opaque RGB colour used for bars/modules and the background.

```csharp
public readonly struct BarcodeColor : IEquatable<BarcodeColor>
```

#### Implements

[IEquatable<BarcodeColor\>](https://learn.microsoft.com/dotnet/api/system.iequatable-1)

## Constructors

### <a id="TerraFluent_Barcode_BarcodeColor__ctor_System_Byte_System_Byte_System_Byte_"></a> BarcodeColor\(byte, byte, byte\)

Creates a colour from red, green and blue components (0-255).

```csharp
public BarcodeColor(byte r, byte g, byte b)
```

#### Parameters

`r` [byte](https://learn.microsoft.com/dotnet/api/system.byte)

`g` [byte](https://learn.microsoft.com/dotnet/api/system.byte)

`b` [byte](https://learn.microsoft.com/dotnet/api/system.byte)

## Properties

### <a id="TerraFluent_Barcode_BarcodeColor_B"></a> B

Blue component (0-255).

```csharp
public byte B { get; }
```

#### Property Value

 [byte](https://learn.microsoft.com/dotnet/api/system.byte)

### <a id="TerraFluent_Barcode_BarcodeColor_Black"></a> Black

Black (<code>#000000</code>).

```csharp
public static BarcodeColor Black { get; }
```

#### Property Value

 [BarcodeColor](barcodecolor.md)

### <a id="TerraFluent_Barcode_BarcodeColor_G"></a> G

Green component (0-255).

```csharp
public byte G { get; }
```

#### Property Value

 [byte](https://learn.microsoft.com/dotnet/api/system.byte)

### <a id="TerraFluent_Barcode_BarcodeColor_R"></a> R

Red component (0-255).

```csharp
public byte R { get; }
```

#### Property Value

 [byte](https://learn.microsoft.com/dotnet/api/system.byte)

### <a id="TerraFluent_Barcode_BarcodeColor_RelativeLuminance"></a> RelativeLuminance

Relative luminance from 0 (black) to 1 (white), as defined by WCAG 2.x
for sRGB colours.

```csharp
public double RelativeLuminance { get; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="TerraFluent_Barcode_BarcodeColor_White"></a> White

White (<code>#FFFFFF</code>).

```csharp
public static BarcodeColor White { get; }
```

#### Property Value

 [BarcodeColor](barcodecolor.md)

## Methods

### <a id="TerraFluent_Barcode_BarcodeColor_ContrastRatio_TerraFluent_Barcode_BarcodeColor_TerraFluent_Barcode_BarcodeColor_"></a> ContrastRatio\(BarcodeColor, BarcodeColor\)

WCAG contrast ratio between two colours, from 1 (identical) to 21 (black on white).
Scanners need strong contrast between bars and background.

```csharp
public static double ContrastRatio(BarcodeColor a, BarcodeColor b)
```

#### Parameters

`a` [BarcodeColor](barcodecolor.md)

`b` [BarcodeColor](barcodecolor.md)

#### Returns

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="TerraFluent_Barcode_BarcodeColor_Equals_TerraFluent_Barcode_BarcodeColor_"></a> Equals\(BarcodeColor\)

Indicates whether the current object is equal to another object of the same type.

```csharp
public bool Equals(BarcodeColor other)
```

#### Parameters

`other` [BarcodeColor](barcodecolor.md)

An object to compare with this object.

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

<a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">true</a> if the current object is equal to the <code class="paramref">other</code> parameter; otherwise, <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">false</a>.

### <a id="TerraFluent_Barcode_BarcodeColor_Equals_System_Object_"></a> Equals\(object?\)

Indicates whether this instance and a specified object are equal.

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

The object to compare with the current instance.

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

<a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">true</a> if <code class="paramref">obj</code> and this instance are the same type and represent the same value; otherwise, <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">false</a>.

### <a id="TerraFluent_Barcode_BarcodeColor_FromHex_System_String_"></a> FromHex\(string\)

Parses <code>#RGB</code> or <code>#RRGGBB</code> (the leading <code>#</code> is optional).

```csharp
public static BarcodeColor FromHex(string hex)
```

#### Parameters

`hex` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [BarcodeColor](barcodecolor.md)

#### Exceptions

 [FormatException](https://learn.microsoft.com/dotnet/api/system.formatexception)

The text is not a hex colour.

### <a id="TerraFluent_Barcode_BarcodeColor_GetHashCode"></a> GetHashCode\(\)

Returns the hash code for this instance.

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

A 32-bit signed integer that is the hash code for this instance.

### <a id="TerraFluent_Barcode_BarcodeColor_ToHex"></a> ToHex\(\)

Formats the colour as <code>#RRGGBB</code>.

```csharp
public string ToHex()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="TerraFluent_Barcode_BarcodeColor_ToString"></a> ToString\(\)

Formats the colour as <code>#RRGGBB</code>.

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="TerraFluent_Barcode_BarcodeColor_op_Equality_TerraFluent_Barcode_BarcodeColor_TerraFluent_Barcode_BarcodeColor_"></a> operator ==\(BarcodeColor, BarcodeColor\)

Whether two colours are equal.

```csharp
public static bool operator ==(BarcodeColor left, BarcodeColor right)
```

#### Parameters

`left` [BarcodeColor](barcodecolor.md)

`right` [BarcodeColor](barcodecolor.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="TerraFluent_Barcode_BarcodeColor_op_Inequality_TerraFluent_Barcode_BarcodeColor_TerraFluent_Barcode_BarcodeColor_"></a> operator \!=\(BarcodeColor, BarcodeColor\)

Whether two colours differ.

```csharp
public static bool operator !=(BarcodeColor left, BarcodeColor right)
```

#### Parameters

`left` [BarcodeColor](barcodecolor.md)

`right` [BarcodeColor](barcodecolor.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)
