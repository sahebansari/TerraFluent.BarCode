# API reference

Everything lives in the `TerraFluent.Barcode` namespace. These pages are generated from the
XML documentation comments in the source, so they always match the released package.

The types you will use most:

| Type | What it is |
| --- | --- |
| [Barcodes](barcodes.md) | Static entry point: one method per symbology, each returning a builder. |
| [BarcodeBuilder](barcodebuilder.md) | Fluent builder: options, styles and every output method. |
| [IBarcodeGenerator](ibarcodegenerator.md) / [BarcodeGenerator](barcodegenerator.md) | Injectable, mockable service for dependency injection. |
| [BarcodeOptions](barcodeoptions.md) | Default options for `BarcodeGenerator`. |
| [BarcodeColor](barcodecolor.md) | An RGB colour, with hex parsing and the WCAG contrast check. |
| [BarcodeMatrix](barcodematrix.md) | The encoded modules, for custom rendering. |

Every type in the namespace is listed below.

### Classes

 [BarcodeBuilder](barcodebuilder.md)

Fluent configuration and output for one barcode. Configuration methods
return the same builder; output methods (<code>To...</code>, <a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_WriteTo_System_IO_Stream_TerraFluent_Barcode_BarcodeImageFormat_">BarcodeBuilder.WriteTo</a>,
<a href="barcodebuilder.md#TerraFluent_Barcode_BarcodeBuilder_Save_System_String_">BarcodeBuilder.Save</a>) can be called any number of times.

 [BarcodeGenerator](barcodegenerator.md)

Default <a href="ibarcodegenerator.md">IBarcodeGenerator</a>, built on <a href="barcodes.md">Barcodes</a>. It
holds no state besides its default options, so one instance can be shared
(register it as a singleton).

 [BarcodeImageFormatExtensions](barcodeimageformatextensions.md)

Helpers for <a href="barcodeimageformat.md">BarcodeImageFormat</a>.

 [BarcodeMatrix](barcodematrix.md)

The encoded symbol as a grid of dark/light modules, before any scaling,
colouring or quiet zone. A linear (1D) barcode is a single row; a QR code
is a square. Useful for custom renderers.

 [BarcodeOptions](barcodeoptions.md)

Settings used by <a href="ibarcodegenerator.md">IBarcodeGenerator</a>. Every property is optional;
a <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a> or <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool">false</a> value keeps the default
that <a href="barcodebuilder.md">BarcodeBuilder</a> uses. Settings that only apply to some
symbologies (for example <a href="barcodeoptions.md#TerraFluent_Barcode_BarcodeOptions_Height">BarcodeOptions.Height</a> or <a href="barcodeoptions.md#TerraFluent_Barcode_BarcodeOptions_QrErrorCorrection">BarcodeOptions.QrErrorCorrection</a>)
are ignored for the others, so one instance can serve every type.

 [Barcodes](barcodes.md)

Entry point. Pick a symbology, configure it fluently, then ask for the
output you need:

<pre><code class="lang-csharp">byte[] png = Barcodes.Code128("ABC-12345").Height(60).ToPng();
string svg = Barcodes.QrCode("https://terrafluent.dev").ErrorCorrection(QrErrorCorrectionLevel.H).ToSvg();
string uri = Barcodes.Ean13("590123412345").ToDataUri(BarcodeImageFormat.Png);</code></pre>

 [ContactCard](contactcard.md)

The details of a person or business, written as a vCard by
<a href="qrpayload.md#TerraFluent_Barcode_QrPayload_Contact_TerraFluent_Barcode_ContactCard_">QrPayload.Contact</a> so that a phone's camera offers to save the
contact. Set at least <a href="contactcard.md#TerraFluent_Barcode_ContactCard_FirstName">ContactCard.FirstName</a>, <a href="contactcard.md#TerraFluent_Barcode_ContactCard_LastName">ContactCard.LastName</a> or
<a href="contactcard.md#TerraFluent_Barcode_ContactCard_Organization">ContactCard.Organization</a>; every other property is optional.

 [QrPayload](qrpayload.md)

Builds the text that phone cameras and scanner apps act on: a contact to
save, a Wi-Fi network to join, a link, an email, a text message, a call or
a GS1 Digital Link. Each helper validates its input and escapes it the way
the format requires, then returns a string to pass to
<a href="barcodes.md#TerraFluent_Barcode_Barcodes_QrCode_System_String_">Barcodes.QrCode</a> (or any other 2D symbology):

<pre><code class="lang-csharp">byte[] png = Barcodes.QrCode(QrPayload.Wifi("Home", "correct horse battery")).ToPng();
string svg = Barcodes.QrCode(QrPayload.Email("hello@example.com", subject: "Hi")).ToSvg();
string url = QrPayload.Gs1DigitalLink(("01", "09501101530003"), ("10", "ABC123"));</code></pre>

### Structs

 [BarcodeColor](barcodecolor.md)

An opaque RGB colour used for bars/modules and the background.

### Interfaces

 [IBarcodeGenerator](ibarcodegenerator.md)

Barcode generation as a service, for dependency injection and mocking in tests.
Register the default implementation once:

<pre><code class="lang-csharp">services.AddSingleton&lt;IBarcodeGenerator&gt;(new BarcodeGenerator(new BarcodeOptions { HumanReadableText = true }));</code></pre>

Implementations are thread-safe.

### Enums

 [BarcodeImageFormat](barcodeimageformat.md)

Output image formats.

 [BarcodeType](barcodetype.md)

The barcode symbologies supported by TerraFluent.Barcode.

 [DataMatrixShape](datamatrixshape.md)

Which Data Matrix symbol shapes may be chosen.

 [GradientDirection](gradientdirection.md)

Direction of a foreground colour gradient, measured across the symbol (quiet zone excluded).

 [QrErrorCorrectionLevel](qrerrorcorrectionlevel.md)

QR code error-correction level: how much of the symbol can be damaged and still read.

 [QrEyeStyle](qreyestyle.md)

How the three large corner squares (finder patterns, or "eyes") of a QR code are drawn.

 [QrModuleStyle](qrmodulestyle.md)

How the data modules (the small squares) of a QR code are drawn.

 [WifiSecurity](wifisecurity.md)

How a Wi-Fi network is secured, for <a href="qrpayload.md#TerraFluent_Barcode_QrPayload_Wifi_System_String_System_String_TerraFluent_Barcode_WifiSecurity_System_Boolean_">QrPayload.Wifi</a>.
