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
