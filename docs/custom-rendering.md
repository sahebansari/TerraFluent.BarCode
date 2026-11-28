# Custom rendering

When you need to draw a code yourself, for example onto a PDF canvas, a label printer's command
language or a game texture, `Encode()` returns the raw modules as a
[BarcodeMatrix](api/barcodematrix.md), before any scaling, colouring or quiet zone.

```csharp
BarcodeMatrix matrix = Barcodes.QrCode("TF").Encode();

for (var y = 0; y < matrix.Height; y++)
{
    for (var x = 0; x < matrix.Width; x++)
    {
        if (matrix[x, y])
        {
            canvas.FillRectangle(x * moduleSize, y * moduleSize, moduleSize, moduleSize);
        }
    }
}
```

| Member | Meaning |
| --- | --- |
| `Width` | Width in modules. |
| `Height` | Height in modules; 1 for linear barcodes. |
| `IsLinear` | `true` for 1D barcodes, whose single row is stretched to the bar height. |
| `RecommendedQuietZone` | The standard's quiet zone, in modules, to leave around the code. |
| `this[x, y]` | `true` for a dark module. |
| `ToString()` | The matrix as text, `#` for dark and `.` for light, one line per row. |

For a linear barcode, draw each dark module of the single row as a bar of the height you want.
Leave `RecommendedQuietZone` modules of space around the code (on the left and right for linear
barcodes) so scanners can find it.

`ToString()` is handy in tests and for quick looks in a console:

```csharp
Console.WriteLine(Barcodes.QrCode("TF").Encode());
```
