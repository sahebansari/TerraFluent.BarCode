# Class QrPayload

Namespace: [TerraFluent.Barcode](index.md)  
Assembly: TerraFluent.Barcode.dll  

Builds the text that phone cameras and scanner apps act on: a contact to
save, a Wi-Fi network to join, a link, an email, a text message, a call or
a GS1 Digital Link. Each helper validates its input and escapes it the way
the format requires, then returns a string to pass to
<a href="barcodes.md#TerraFluent_Barcode_Barcodes_QrCode_System_String_">Barcodes.QrCode</a> (or any other 2D symbology):

<pre><code class="lang-csharp">byte[] png = Barcodes.QrCode(QrPayload.Wifi("Home", "correct horse battery")).ToPng();
string svg = Barcodes.QrCode(QrPayload.Email("hello@example.com", subject: "Hi")).ToSvg();
string url = QrPayload.Gs1DigitalLink(("01", "09501101530003"), ("10", "ABC123"));</code></pre>

```csharp
public static class QrPayload
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[QrPayload](qrpayload.md)

## Fields

### <a id="TerraFluent_Barcode_QrPayload_Gs1ResolverDomain"></a> Gs1ResolverDomain

The resolver GS1 runs, used by <a href="qrpayload.md#TerraFluent_Barcode_QrPayload_Gs1DigitalLink_System_ValueTuple_7bSystem_String_System_String_7d_5b_5d_">QrPayload.Gs1DigitalLink</a> when no domain is given.

```csharp
public const string Gs1ResolverDomain = "https://id.gs1.org"
```

#### Field Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="TerraFluent_Barcode_QrPayload_Contact_TerraFluent_Barcode_ContactCard_"></a> Contact\(ContactCard\)

A vCard 3.0 contact (RFC 2426) that phones offer to save. Text is
escaped as vCard requires (backslash, comma, semicolon and line breaks),
lines end in CRLF, and properties left <code>null</code> or empty are omitted.

```csharp
public static string Contact(ContactCard contact)
```

#### Parameters

`contact` [ContactCard](contactcard.md)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

<code class="paramref">contact</code> is <code>null</code>.

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

The contact has no name or organization, or an email, website or phone number is not valid.

### <a id="TerraFluent_Barcode_QrPayload_Email_System_String_System_String_System_String_"></a> Email\(string, string?, string?\)

A <code>mailto:</code> link (RFC 6068) that opens a new email to
<code class="paramref">address</code>, with an optional subject and body. Both are
percent-encoded as UTF-8, and line breaks in the body become CRLF.

```csharp
public static string Email(string address, string? subject = null, string? body = null)
```

#### Parameters

`address` [string](https://learn.microsoft.com/dotnet/api/system.string)

`subject` [string](https://learn.microsoft.com/dotnet/api/system.string)?

`body` [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

The address is not a single email address.

### <a id="TerraFluent_Barcode_QrPayload_Gs1DigitalLink_System_String_"></a> Gs1DigitalLink\(string\)

A GS1 Digital Link URI on GS1's resolver (<a href="qrpayload.md#TerraFluent_Barcode_QrPayload_Gs1ResolverDomain">QrPayload.Gs1ResolverDomain</a>)
from an element string with AIs in parentheses, for example
<code>(01)09501101530003(10)ABC123(17)251231</code>. See
<a href="qrpayload.md#TerraFluent_Barcode_QrPayload_Gs1DigitalLink_System_Uri_System_ValueTuple_7bSystem_String_System_String_7d_5b_5d_">QrPayload.Gs1DigitalLink</a> for the rules.

```csharp
public static string Gs1DigitalLink(string elementString)
```

#### Parameters

`elementString` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="TerraFluent_Barcode_QrPayload_Gs1DigitalLink_System_ValueTuple_System_String_System_String____"></a> Gs1DigitalLink\(params \(string Ai, string Data\)\[\]\)

A GS1 Digital Link URI on GS1's resolver (<a href="qrpayload.md#TerraFluent_Barcode_QrPayload_Gs1ResolverDomain">QrPayload.Gs1ResolverDomain</a>)
from application identifier and data pairs. See
<a href="qrpayload.md#TerraFluent_Barcode_QrPayload_Gs1DigitalLink_System_Uri_System_ValueTuple_7bSystem_String_System_String_7d_5b_5d_">QrPayload.Gs1DigitalLink</a> for the rules.

```csharp
public static string Gs1DigitalLink(params (string Ai, string Data)[] elements)
```

#### Parameters

`elements` \([string](https://learn.microsoft.com/dotnet/api/system.string) [Ai](https://learn.microsoft.com/dotnet/api/system.valuetuple-system.string,system.string-.ai), [string](https://learn.microsoft.com/dotnet/api/system.string) [Data](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.string,system.string\-.data)\)\[\]

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="TerraFluent_Barcode_QrPayload_Gs1DigitalLink_System_Uri_System_String_"></a> Gs1DigitalLink\(Uri, string\)

A GS1 Digital Link URI on your own <code class="paramref">domain</code>, from an
element string with AIs in parentheses. See
<a href="qrpayload.md#TerraFluent_Barcode_QrPayload_Gs1DigitalLink_System_Uri_System_ValueTuple_7bSystem_String_System_String_7d_5b_5d_">QrPayload.Gs1DigitalLink</a> for the rules.

```csharp
public static string Gs1DigitalLink(Uri domain, string elementString)
```

#### Parameters

`domain` [Uri](https://learn.microsoft.com/dotnet/api/system.uri)

`elementString` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="TerraFluent_Barcode_QrPayload_Gs1DigitalLink_System_Uri_System_ValueTuple_System_String_System_String____"></a> Gs1DigitalLink\(Uri, params \(string Ai, string Data\)\[\]\)

A GS1 Digital Link URI (GS1 Digital Link standard, uncompressed form) on
<code class="paramref">domain</code>, for example
<code>https://example.com/01/09501101530003/10/ABC123?17=251231</code>.

```csharp
public static string Gs1DigitalLink(Uri domain, params (string Ai, string Data)[] elements)
```

#### Parameters

`domain` [Uri](https://learn.microsoft.com/dotnet/api/system.uri)

`elements` \([string](https://learn.microsoft.com/dotnet/api/system.string) [Ai](https://learn.microsoft.com/dotnet/api/system.valuetuple-system.string,system.string-.ai), [string](https://learn.microsoft.com/dotnet/api/system.string) [Data](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.string,system.string\-.data)\)\[\]

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Remarks

Exactly one element must be a primary key: GTIN (01), SSCC (00), GLN (414)
and the other GS1 keys. Its key qualifiers, such as CPV (22), batch (10)
and serial (21) for a GTIN, go in the path in the order the standard
sets, whatever order they are given in; every other AI becomes a query
parameter. A GTIN of 8, 12 or 13 digits is padded to 14. Each AI's data is
checked as for GS1-128, and characters outside the URI unreserved set
are percent-encoded.

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

An AI is unknown or its data is invalid, there is not exactly one primary key, an AI repeats, or the domain is not an http or https address.

### <a id="TerraFluent_Barcode_QrPayload_Phone_System_String_"></a> Phone\(string\)

A <code>tel:</code> link that offers to call <code class="paramref">number</code>. Spaces,
dashes, dots and parentheses are removed; a leading <code>+</code> for the
international form is kept.

```csharp
public static string Phone(string number)
```

#### Parameters

`number` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

The number has characters other than digits and those separators.

### <a id="TerraFluent_Barcode_QrPayload_Sms_System_String_System_String_"></a> Sms\(string, string?\)

An <code>SMSTO:</code> payload that opens a text message to
<code class="paramref">number</code>, with <code class="paramref">message</code> filled in if
given. The number is normalized as in <a href="qrpayload.md#TerraFluent_Barcode_QrPayload_Phone_System_String_">QrPayload.Phone</a>.

```csharp
public static string Sms(string number, string? message = null)
```

#### Parameters

`number` [string](https://learn.microsoft.com/dotnet/api/system.string)

`message` [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

The number has characters other than digits and separators.

### <a id="TerraFluent_Barcode_QrPayload_Url_System_String_"></a> Url\(string\)

A link to a web page. A value without a scheme, such as
<code>terrafluent.dev/barcode</code>, gets <code>https://</code> in front; otherwise
the scheme must be <code>http</code> or <code>https</code>.

```csharp
public static string Url(string url)
```

#### Parameters

`url` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

The value is not an absolute web address or contains spaces.

### <a id="TerraFluent_Barcode_QrPayload_Wifi_System_String_System_String_TerraFluent_Barcode_WifiSecurity_System_Boolean_"></a> Wifi\(string, string?, WifiSecurity, bool\)

A Wi-Fi network that phones offer to join, in the <code>WIFI:</code> format
that Android and iOS cameras read. Backslash, semicolon, comma, colon and
double quote in the network name and password are escaped.

```csharp
public static string Wifi(string ssid, string? password, WifiSecurity security = WifiSecurity.Wpa, bool hidden = false)
```

#### Parameters

`ssid` [string](https://learn.microsoft.com/dotnet/api/system.string)

The network name, up to 32 bytes as UTF-8.

`password` [string](https://learn.microsoft.com/dotnet/api/system.string)?

The passphrase: 8 to 63 characters (or 64 hex digits) for
<a href="wifisecurity.md#TerraFluent_Barcode_WifiSecurity_Wpa">WifiSecurity.Wpa</a>; 5 or 13 characters (or 10 or 26 hex
digits) for <a href="wifisecurity.md#TerraFluent_Barcode_WifiSecurity_Wep">WifiSecurity.Wep</a>; <code>null</code> or empty for
<a href="wifisecurity.md#TerraFluent_Barcode_WifiSecurity_None">WifiSecurity.None</a>.

`security` [WifiSecurity](wifisecurity.md)

How the network is secured.

`hidden` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

<code>true</code> if the network does not broadcast its name.

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

The name or password does not suit the security type.
