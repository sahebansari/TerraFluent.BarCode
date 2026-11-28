# TerraFluent.Barcode.Mcp

An [MCP](https://modelcontextprotocol.io) server that lets AI assistants such as
Claude generate barcodes and QR codes. It runs locally over stdio, is built on
[TerraFluent.Barcode](https://www.nuget.org/packages/TerraFluent.Barcode) and the
official C# MCP SDK, and needs no network access or API key.

## Install

Requires the .NET 10 SDK or runtime.

```shell
dotnet tool install --global TerraFluent.Barcode.Mcp
```

Or run it without installing (.NET 10 SDK):

```shell
dnx TerraFluent.Barcode.Mcp --yes
```

## Configure your client

Claude Desktop (`claude_desktop_config.json`), Cursor, VS Code and most other
clients take the same shape:

```json
{
  "mcpServers": {
    "terrafluent-barcode": {
      "command": "terrafluent-barcode-mcp"
    }
  }
}
```

Without a global install, use `"command": "dnx"` and
`"args": ["TerraFluent.Barcode.Mcp", "--yes"]`.

Claude Code:

```shell
claude mcp add terrafluent-barcode -- terrafluent-barcode-mcp
```

## Tools

| Tool | What it does |
| --- | --- |
| `generate_barcode` | Any supported type (Code 128, GS1-128, Code 39, EAN-13, EAN-8, UPC-A, QR, Data Matrix, PDF417, Interleaved 2 of 5, ITF-14, Codabar, Aztec, MaxiCode). Options: format, output, module size, bar height, quiet zone, human-readable text, colours, transparent background, Code 39 / ITF checksum and Code 39 full ASCII, EAN/UPC add-on, QR error correction, Aztec error correction, MaxiCode mode and structured carrier message (postal code, country, service class). |
| `generate_qr_code` | QR code with styling: module style (square, rounded, dots, connected), eye style (square, rounded, circle), colours, eye colour, gradient, centre logo (base64 PNG). |
| `list_barcode_formats` | The supported types and their input rules, image formats, output modes and the server's limits, as JSON. |

Every generate tool takes:

- `format`: `png` (default), `jpeg` or `svg`.
- `output`: `image` (default) returns PNG and JPEG as MCP image content, which
  the assistant can show and see, and SVG as markup; `base64` returns the file
  bytes as base64 text; `data_uri` returns a `data:image/...;base64,...` URI for
  HTML, Markdown or JSON.

Each result ends with a one-line summary: type, data, pixel size, format and size.
Invalid input, such as a wrong EAN check digit or an option that does not apply
to the type, comes back as a tool error that explains the problem, so the
assistant can correct the request.

Example prompts:

- "Make a QR code for https://terrafluent.dev with rounded dots and a blue to purple gradient."
- "Give me an EAN-13 for 590123412345 as SVG."
- "Create a GS1-128 label barcode for GTIN 09501101530003, batch ABC123, as a data URI."

## Limits

Requests come from a model, so the server caps them:

| Limit | Value |
| --- | --- |
| Image area | 4,194,304 pixels (2048 x 2048) by default; set `TERRAFLUENT_BARCODE_MCP_MAX_PIXELS` to change it, up to 67,108,864 |
| Data length | 4,096 characters (the symbology's own capacity usually applies first) |
| Module size | 1 to 64 pixels |
| Bar height | 1 to 2,048 pixels |
| Quiet zone | 0 to 64 modules |
| Logo | 1,048,576 base64 characters |

The tools only compute images: they read no files, write no files and make no
network calls.

## License

MIT. See the [repository](https://github.com/sahebansari/TerraFluent.Barcode).
