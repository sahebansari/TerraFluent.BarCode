# Class ContactCard

Namespace: [TerraFluent.Barcode](index.md)  
Assembly: TerraFluent.Barcode.dll  

The details of a person or business, written as a vCard by
<a href="qrpayload.md#TerraFluent_Barcode_QrPayload_Contact_TerraFluent_Barcode_ContactCard_">QrPayload.Contact</a> so that a phone's camera offers to save the
contact. Set at least <a href="contactcard.md#TerraFluent_Barcode_ContactCard_FirstName">ContactCard.FirstName</a>, <a href="contactcard.md#TerraFluent_Barcode_ContactCard_LastName">ContactCard.LastName</a> or
<a href="contactcard.md#TerraFluent_Barcode_ContactCard_Organization">ContactCard.Organization</a>; every other property is optional.

```csharp
public sealed class ContactCard
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ContactCard](contactcard.md)

## Examples

<pre><code class="lang-csharp">var card = new ContactCard
{
    FirstName = "Ada",
    LastName = "Lovelace",
    Organization = "Analytical Engines Ltd",
    MobilePhone = "+44 20 7946 0958",
    Email = "ada@example.com",
};
byte[] png = Barcodes.QrCode(QrPayload.Contact(card)).ToPng();</code></pre>

## Properties

### <a id="TerraFluent_Barcode_ContactCard_Birthday"></a> Birthday

Date of birth. Only the date part is written.

```csharp
public DateTime? Birthday { get; set; }
```

#### Property Value

 [DateTime](https://learn.microsoft.com/dotnet/api/system.datetime)?

### <a id="TerraFluent_Barcode_ContactCard_City"></a> City

City or locality.

```csharp
public string? City { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="TerraFluent_Barcode_ContactCard_Country"></a> Country

Country name.

```csharp
public string? Country { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="TerraFluent_Barcode_ContactCard_Email"></a> Email

Email address.

```csharp
public string? Email { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="TerraFluent_Barcode_ContactCard_FirstName"></a> FirstName

Given name.

```csharp
public string? FirstName { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="TerraFluent_Barcode_ContactCard_JobTitle"></a> JobTitle

Job title, for example <code>Head of Engineering</code>.

```csharp
public string? JobTitle { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="TerraFluent_Barcode_ContactCard_LastName"></a> LastName

Family name.

```csharp
public string? LastName { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="TerraFluent_Barcode_ContactCard_MobilePhone"></a> MobilePhone

Mobile phone number.

```csharp
public string? MobilePhone { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="TerraFluent_Barcode_ContactCard_Note"></a> Note

Free-text note. May span several lines.

```csharp
public string? Note { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="TerraFluent_Barcode_ContactCard_Organization"></a> Organization

Company or organization.

```csharp
public string? Organization { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="TerraFluent_Barcode_ContactCard_Phone"></a> Phone

Main (voice) phone number.

```csharp
public string? Phone { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="TerraFluent_Barcode_ContactCard_PostalCode"></a> PostalCode

Postal or ZIP code.

```csharp
public string? PostalCode { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="TerraFluent_Barcode_ContactCard_Region"></a> Region

State, province or region.

```csharp
public string? Region { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="TerraFluent_Barcode_ContactCard_Street"></a> Street

Street address. May span several lines.

```csharp
public string? Street { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="TerraFluent_Barcode_ContactCard_Website"></a> Website

Website, for example <code>https://terrafluent.dev</code>.

```csharp
public string? Website { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="TerraFluent_Barcode_ContactCard_WorkPhone"></a> WorkPhone

Work phone number.

```csharp
public string? WorkPhone { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?
