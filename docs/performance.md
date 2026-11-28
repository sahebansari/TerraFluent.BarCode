# Performance

Typical timings on one core (.NET 10, 2.3 GHz Xeon), per call, from a new builder:

| Output | Time | Allocated |
| --- | --- | --- |
| Code 128 / EAN-13 PNG | about 25 µs | about 70 KB |
| QR code (URL) PNG | about 0.1 ms | about 80 KB |
| QR code (URL) JPEG | about 0.3 ms | about 55 KB |
| Code 128 with human-readable text, PNG | about 1 ms | about 250 KB |
| Styled QR code (rounded, gradient) PNG | about 1.5 ms | about 150 KB |

Tips:

- **Reuse a builder for several formats.** The value is encoded once per builder.
- **Prefer `WriteTo` and `WriteToAsync`** for responses and files: they write straight into the
  destination stream.
- **Plain codes are cheapest.** Styles and text add anti-aliased drawing; a plain code is written as
  a 1-bit PNG.

The repository's `benchmarks/` project measures these numbers with BenchmarkDotNet:

```shell
dotnet run -c Release --project benchmarks/TerraFluent.Barcode.Benchmarks -- --filter '*'
```
