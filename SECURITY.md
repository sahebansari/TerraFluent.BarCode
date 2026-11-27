# Security Policy

## Supported Versions

| Version | Supported |
| --- | --- |
| 1.x | Yes |
| < 1.0 | No |

## Reporting a Vulnerability

Please do not open a public issue for security problems. Report them
privately through GitHub's
[private vulnerability reporting](https://github.com/sahebansari/TerraFluent.BarCode/security/advisories/new)
with a description, the affected version and, if possible, a minimal
reproduction.

You can expect an acknowledgement within 5 working days and a fix or
mitigation plan within 30 days for confirmed issues. Fixes are released as a
patch version and noted in the CHANGELOG under "Security".

## Hardening Notes

- The library has no runtime dependencies and performs no I/O except
  `Save(path)`, which writes to the path you pass.
- Output size is capped (`BarcodeBuilder.MaxImageDimension`,
  `BarcodeBuilder.MaxImagePixels`), so size settings from untrusted input
  cannot exhaust memory.
- Logo PNGs are parsed by a built-in decoder that limits the image to
  4096 x 4096 pixels and bounds the decompressed size; malformed files throw
  `ArgumentException`.
- SVG output contains only numbers, `#RRGGBB` colours and base64 data, so no
  user-supplied text is written into the markup.
