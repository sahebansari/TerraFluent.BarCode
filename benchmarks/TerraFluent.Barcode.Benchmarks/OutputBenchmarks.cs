using BenchmarkDotNet.Attributes;

namespace TerraFluent.Barcode.Benchmarks;

/// <summary>
/// Every output format for each symbology. Each call starts from a new builder,
/// so the time includes encoding, as in a typical one-shot use.
/// </summary>
[MemoryDiagnoser]
public class OutputBenchmarks
{
    [Params("Code128", "Ean13", "QrSmall", "QrLarge", "DataMatrix", "Pdf417")]
    public string Kind { get; set; } = "Code128";

    [Benchmark]
    public BarcodeMatrix Encode() => Samples.Create(Kind).Encode();

    [Benchmark]
    public byte[] Png() => Samples.Create(Kind).ToPng();

    [Benchmark]
    public byte[] Jpeg() => Samples.Create(Kind).ToJpeg();

    [Benchmark]
    public string Svg() => Samples.Create(Kind).ToSvg();

    [Benchmark]
    public string Base64Png() => Samples.Create(Kind).ToBase64();
}
