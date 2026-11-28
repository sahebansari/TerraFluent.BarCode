# Roadmap

These are planned or in progress. The pages for each will appear here once they ship.

## New symbologies

| Symbology | Kind | Typical use | Status |
| --- | --- | --- | --- |
| Aztec | 2D | Transport tickets, boarding passes | In progress |
| ITF-14 | Linear | Outer cartons and cases (GTIN-14) | In progress |
| Codabar | Linear | Libraries, blood banks, courier forms | In progress |
| MaxiCode | 2D | Parcel sorting (UPS) | In progress |

## MCP server

`TerraFluent.Barcode.Mcp`, a separate package, will expose barcode generation as a
[Model Context Protocol](https://modelcontextprotocol.io) server, so AI assistants and agents can
create barcodes and QR codes as tools. Planned.

## Smaller items

- QR code Kanji mode and structured append.
- Data Matrix C40, Text and EDIFACT compaction.

Changes in each release are listed in the
[changelog](https://github.com/sahebansari/TerraFluent.Barcode/blob/main/CHANGELOG.md). Ideas and
requests are welcome as [GitHub issues](https://github.com/sahebansari/TerraFluent.Barcode/issues).
