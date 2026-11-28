# TerraFluent.Barcode documentation

These pages are the source of the documentation at
[terrafluent.dev/docs/barcode](https://terrafluent.dev/docs/barcode/getting-started/). They are plain
Markdown, so they also read well right here on GitHub.

## Guides

1. [Introduction](introduction.md)
2. [Getting started](getting-started.md)
3. [Barcode types](barcode-types.md): every symbology, its input rules and examples
4. [Output formats](output-formats.md): PNG, JPEG, SVG, bytes, streams, base64, data URIs and files
5. [Options](options.md): size, quiet zone, colours and human-readable text
6. [Styling](styling.md): module and eye shapes, gradients, logos and rounded bars
7. [Dependency injection](dependency-injection.md): `IBarcodeGenerator` and `BarcodeOptions`
8. [Custom rendering](custom-rendering.md): drawing a `BarcodeMatrix` yourself
9. [Errors, limits and thread safety](errors-and-limits.md)
10. [Performance](performance.md)
11. [Roadmap](roadmap.md)

[API reference](api/index.md): every public type and member, generated from the XML documentation
comments.

## Maintaining the docs

The guides are written by hand. Two folders are generated, and must not be edited by hand:

- `images/` is rendered by [`tools/DocImages`](tools/DocImages/Program.cs). Each image there is
  produced by the same builder chain the guide shows next to it. To add an example, add an entry to
  `Program.cs` and reference `images/<name>.svg` (or `.png`) from the guide.
- `api/` is generated from the XML documentation comments by DocFX (`docfx metadata` with Markdown
  output) and tidied by [`tools/api-markdown.py`](tools/api-markdown.py). The introduction at the top
  of `api/index.md` comes from `tools/api-index.md`.

After changing the public API, its XML comments, or `DocImages`, regenerate both folders and commit
the result:

```shell
docs/build.sh
```

The [Docs workflow](../.github/workflows/docs.yml) runs the same script on every pull request and
fails if the committed files are out of date.

### Publishing to terrafluent.dev

terrafluent.dev is a Jekyll site in
[sahebansari/terrafluent.dev](https://github.com/sahebansari/terrafluent.dev). Its
`scripts/mirror-docs.sh` copies these pages into `docs/barcode/` (guides) and `docs/barcode/api/`
(API reference), and the images into `assets/img/barcode/`. Pages are listed in the site's sidebar
in the order of the `BARCODE_ORDER` list in that script, so add new guides there too.
