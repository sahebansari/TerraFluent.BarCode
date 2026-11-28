# Errors, limits and thread safety

## Errors

- **An invalid value or setting** throws `ArgumentException`, or one of its subclasses
  `ArgumentNullException` and `ArgumentOutOfRangeException`, with a message that says what was
  wrong, for example `Invalid EAN-13 check digit: '8' should be '7'.` Invalid hex colours throw
  `FormatException`.
- **Options that cannot be combined** throw `InvalidOperationException`: an option for another
  symbology (such as `Height` on a QR code) when it is set, and, when rendering, a logo with too
  little error correction, colours with too little contrast, or JPEG output with a transparent
  background.

Values are checked when you call the `Barcodes` method, so you can validate user input by building
the code before you store it:

```csharp
try
{
    Barcodes.Ean13(input);
}
catch (ArgumentException ex)
{
    ModelState.AddModelError(nameof(input), ex.Message);
}
```

## Image size limits

Images are limited to 32,768 pixels per side (`BarcodeBuilder.MaxImageDimension`) and 64 megapixels
in total (`BarcodeBuilder.MaxImagePixels`).

When the module size, height or value come from untrusted input, lower the area limit with
`ImageSizeLimit(pixels)`, or `BarcodeOptions.MaxImagePixels` for the generator:

```csharp
Barcodes.QrCode(userText).ModuleSize(userModuleSize).ImageSizeLimit(4_000_000).ToPng();
```

Images are rendered a band of rows at a time, so memory grows with the image width, not its area.
CPU time and output size still grow with the area.

## Contrast check

Foreground, gradient and eye colours need a WCAG contrast ratio of at least 3:1 against the
background. Light-on-dark codes are allowed. Call `AllowLowContrast()` to turn the check off for
codes you have tested.

## Thread safety

- The static `Barcodes` methods and `BarcodeGenerator` are thread-safe.
- Configure a `BarcodeBuilder` on one thread. After that, its output methods can be called
  concurrently.
- `WriteToAsync` and `SaveAsync` encode on the calling thread and then write asynchronously; their
  cancellation token is also checked while encoding.
