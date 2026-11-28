# ASP.NET Core

`TerraFluent.Barcode.AspNetCore` is a separate package for web apps on .NET 10. It adds three things
on top of the library:

- an endpoint that serves any barcode from a URL, with size limits, ETags and caching headers;
- `IResult` helpers for minimal APIs and MVC controllers;
- a Razor tag helper that renders a barcode as an `img` or as inline SVG.

The core library stays dependency-free; only this package references ASP.NET Core.

```shell
dotnet add package TerraFluent.Barcode.AspNetCore
```

## Register

```csharp
using TerraFluent.Barcode;
using TerraFluent.Barcode.AspNetCore;

builder.Services.AddBarcodes(options =>
{
    options.Defaults = new BarcodeOptions { ModuleSize = 3, HumanReadableText = true };
});
```

`AddBarcodes` registers a singleton `IBarcodeGenerator` that uses `options.Defaults`, unless you
registered one already. The endpoint, the results and the tag helper all use that generator, and
your own services can inject it as usual (see [Dependency injection](dependency-injection.md)).

## Serve barcodes from a URL

```csharp
app.MapBarcodes();               // GET and HEAD on /barcodes/{type}.{format?}
app.MapBarcodes("/api/codes");   // or under any prefix
```

Then point an `img` at it:

```html
<img src="/barcodes/qr.svg?data=https%3A%2F%2Fexample.com" alt="example.com">
<img src="/barcodes/code128.png?data=ABC-123&text=true&height=60" alt="ABC-123">
<img src="/barcodes/ean13.png?data=590123412345" alt="5901234123457">
```

The path names the type and, optionally, the format:

| Part | Values |
| --- | --- |
| `type` | Any `BarcodeType`, ignoring case, dashes and underscores (`code128`, `gs1-128`, `ean13`, `upca`, `itf14`, `qrcode`, `datamatrix`, `pdf417`, `aztec`, `maxicode`, `microqrcode`, `gs1databar`, `intelligentmail`), and the aliases `qr`, `upc`, `dm`, `ean128`, `i2of5`, `nw7`, `microqr`, `rmqr`, `databar`, `databar-limited`, `databar-expanded` and `imb`. |
| `format` | `png`, `jpg` / `jpeg` or `svg`. Without an extension the `format` query parameter applies, then `DefaultFormat` (PNG). |

Query parameters, all optional except `data`:

| Parameter | Meaning |
| --- | --- |
| `data` | The value to encode. Escape it with `Uri.EscapeDataString`. |
| `moduleSize` | Module size in pixels, 1 to `MaxModuleSize` (64). |
| `height` | Bar height in pixels for linear barcodes, 1 to `MaxBarHeight` (2048). |
| `quietZone` | Quiet zone in modules, 0 to `MaxQuietZone` (64). |
| `foreground`, `background` | Colours as `RRGGBB` or `RGB`, optionally after `%23` (an escaped `#`). |
| `transparent` | `true` for a transparent background (PNG and SVG). |
| `text` | `true` prints the human-readable text under a linear barcode; `false` turns off a default. |
| `errorCorrection` | QR codes: `L`, `M`, `Q` or `H`. |
| `checksum` | Code 39 and ITF: `true` adds the optional check character. |

Names are case-insensitive, and `module-size` or `module_size` work too. A boolean given without a
value (`&text`) means `true`.

### Responses

- **200** with the image, `Content-Length`, an `ETag`, `Cache-Control` and
  `X-Content-Type-Options: nosniff`. SVG also gets
  `Content-Security-Policy: default-src 'none'; style-src 'unsafe-inline'`, so an SVG opened on its
  own can never run script.
- **304 Not Modified** when `If-None-Match` matches. The ETag comes from the request and the
  configured defaults, so the endpoint answers without rendering anything.
- **400 Bad Request** with a problem details body for invalid input: a missing `data`, a number out
  of range, a value the symbology cannot encode (`"EAN-13 takes 12 or 13 digits"`), an option that
  does not apply to the type (`height` on a QR code), or an image larger than `MaxImagePixels`.
- **404 Not Found** for a type that is unknown or not in `AllowedTypes`.

Errors are sent with `Cache-Control: no-store`.

### Options

| `BarcodeEndpointOptions` | Default | |
| --- | --- | --- |
| `Defaults` | empty | `BarcodeOptions` for the registered generator; query parameters override them. |
| `AllowedTypes` | every type | Remove the types you do not need. |
| `DefaultFormat` | `Png` | Used when neither the path nor the query names a format. |
| `MaxDataLength` | 2048 | Longest `data`, in characters. |
| `MaxImagePixels` | 4 MP | Largest image area. Checked before rendering. |
| `MaxModuleSize`, `MaxBarHeight`, `MaxQuietZone` | 64, 2048, 64 | Limits for the query parameters. |
| `CacheMaxAge` | 1 day | `TimeSpan.Zero` sends `no-cache`, so clients revalidate with the ETag every time. |
| `PublicCache` | `true` | `false` sends `private`, for endpoints behind authorization. |

```csharp
builder.Services.AddBarcodes(options =>
{
    options.AllowedTypes.Clear();
    options.AllowedTypes.Add(BarcodeType.QrCode);
    options.AllowedTypes.Add(BarcodeType.Code128);
    options.MaxDataLength = 512;
    options.MaxImagePixels = 1024 * 1024;
});
```

### Securing the endpoint

The endpoint renders whatever a caller asks for, and rendering is CPU-bound. Keep the limits as low
as your pages need, and on a public site add rate limiting. `MapBarcodes` returns an
`IEndpointConventionBuilder`, so the usual conventions apply:

```csharp
builder.Services.AddRateLimiter(o => o.AddFixedWindowLimiter("barcodes", w =>
{
    w.PermitLimit = 60;
    w.Window = TimeSpan.FromMinutes(1);
}));

app.UseRateLimiter();
app.MapBarcodes().RequireRateLimiting("barcodes");

// Or only for signed-in users, with private caching:
app.MapBarcodes("/secure/barcodes").RequireAuthorization();
```

## Return a barcode from your own endpoint

`Results.Extensions.Barcode` renders with the registered generator:

```csharp
app.MapGet("/labels/{sku}.png", (string sku) =>
    Results.Extensions.Barcode(BarcodeType.Code128, sku));

app.MapGet("/labels/{sku}.svg", (string sku) =>
    Results.Extensions.Barcode(BarcodeType.Code128, sku, BarcodeImageFormat.Svg,
        new BarcodeOptions { HumanReadableText = true }));
```

Options passed to a call replace the generator's defaults, as they do for `IBarcodeGenerator`.

For styles that `BarcodeOptions` does not cover, build the barcode yourself and call
`ToHttpResult`:

```csharp
app.MapGet("/tickets/{id}/qr", (string id) =>
    Barcodes.QrCode($"https://example.com/t/{id}")
        .ModuleSize(8)
        .ModuleStyle(QrModuleStyle.Rounded)
        .ToHttpResult(BarcodeImageFormat.Png, fileDownloadName: $"ticket-{id}.png"));
```

`fileDownloadName` makes the browser save the file instead of showing it. The result type,
`BarcodeHttpResult`, describes itself for OpenAPI (200 with `image/png`, `image/jpeg` or
`image/svg+xml`, 400 with problem details) when an endpoint declares it as its return type.

MVC controllers can return the same results:

```csharp
[HttpGet("labels/{sku}")]
public IResult Get(string sku) => Results.Extensions.Barcode(BarcodeType.Code128, sku);
```

These results do not add caching headers: the right policy depends on your data. A value that
cannot be encoded becomes a 400 problem details response when the result runs.

## Tag helper

Register the tag helper in `_ViewImports.cshtml`:

```cshtml
@addTagHelper *, TerraFluent.Barcode.AspNetCore
```

Then use `<barcode>` in any view or page:

```cshtml
<barcode type="QrCode" value="@Model.Url" module-size="6" class="qr" />
<barcode type="Code128" value="@Model.Sku" human-readable="true" inline-svg="true" width="240" />
<barcode type="Ean13" value="@Model.Gtin" format="Png" alt="GTIN @Model.Gtin" />
```

By default the tag helper writes an `img` whose `src` is a `data:` URI, SVG unless `format` says
otherwise, so the page needs no extra request. `inline-svg="true"` writes the `<svg>` markup into
the page instead, so CSS can style it.

| Attribute | |
| --- | --- |
| `type` | The `BarcodeType`. Default `Code128`. |
| `value` | The data to encode. A null or empty value renders nothing. |
| `format` | `Svg` (default), `Png` or `Jpeg`, for the `data:` URI. |
| `inline-svg` | Writes inline SVG instead of an `img`. |
| `module-size`, `height`, `quiet-zone` | Size, as in [Options](options.md). |
| `human-readable` | Text under a linear barcode. |
| `foreground`, `background` | Colours as `#RRGGBB`. |
| `transparent` | Transparent background. |
| `error-correction` | QR level: `L`, `M`, `Q` or `H`. |

Other attributes (`class`, `style`, `id`, `width`, ...) are copied to the `img` or `svg` element.
`alt` defaults to the value; on inline SVG it becomes `aria-label` with `role="img"`. A `width` or
`height` on inline SVG replaces the image's own size, and the `viewBox` keeps the proportions.

The tag helper renders while the page renders, so a value the symbology cannot encode throws, as
everywhere else in the library. Validate user input first, or use the endpoint, which answers 400.
