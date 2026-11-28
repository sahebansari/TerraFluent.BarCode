# TerraFluent.Barcode.AspNetCore

ASP.NET Core integration for [TerraFluent.Barcode](https://www.nuget.org/packages/TerraFluent.Barcode):
serve barcodes and QR codes from a URL, return them from your own endpoints, and render them in
Razor views. Requires .NET 10.

```shell
dotnet add package TerraFluent.Barcode.AspNetCore
```

## Endpoint

```csharp
using TerraFluent.Barcode;
using TerraFluent.Barcode.AspNetCore;

builder.Services.AddBarcodes(options =>
{
    options.Defaults = new BarcodeOptions { ModuleSize = 3 };
    options.MaxImagePixels = 1024 * 1024;   // default 4 MP
});

app.MapBarcodes();   // GET/HEAD /barcodes/{type}.{format?}
```

```html
<img src="/barcodes/qr.svg?data=https%3A%2F%2Fexample.com" alt="example.com">
<img src="/barcodes/code128.png?data=ABC-123&text=true" alt="ABC-123">
```

Query options: `moduleSize`, `height`, `quietZone`, `foreground`, `background`, `transparent`,
`text`, `errorCorrection`, `checksum`. Responses carry an ETag (a matching `If-None-Match` gets 304
without rendering), `Cache-Control` (one day, public, by default), `nosniff`, and a script-blocking
CSP on SVG. Bad input gets 400 problem details; unknown or disallowed types get 404. Add
`.RequireRateLimiting(...)` or `.RequireAuthorization()` to the returned builder as needed.

## Results

```csharp
app.MapGet("/labels/{sku}.png", (string sku) =>
    Results.Extensions.Barcode(BarcodeType.Code128, sku));

app.MapGet("/tickets/{id}/qr", (string id) =>
    Barcodes.QrCode($"https://example.com/t/{id}").ModuleSize(8).ToHttpResult(BarcodeImageFormat.Svg));
```

## Tag helper

```cshtml
@addTagHelper *, TerraFluent.Barcode.AspNetCore

<barcode type="QrCode" value="@Model.Url" module-size="6" class="qr" />
<barcode type="Code128" value="@Model.Sku" human-readable="true" inline-svg="true" />
```

Full guide: [docs/aspnetcore.md](https://github.com/sahebansari/TerraFluent.Barcode/blob/main/docs/aspnetcore.md).
