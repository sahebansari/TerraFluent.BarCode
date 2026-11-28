using BenchmarkDotNet.Attributes;

namespace TerraFluent.Barcode.Benchmarks;

/// <summary>
/// A 6,440 x 6,440 pixel QR code (41 megapixels), to watch peak memory: styled
/// output is rendered in bands, so allocations should stay a few MB.
/// </summary>
[MemoryDiagnoser]
public class LargeImageBenchmarks
{
    private static readonly string Value = Samples.Text(1700);

    [Benchmark]
    public byte[] PlainPng() => Barcodes.QrCode(Value).ModuleSize(40).ToPng();

    [Benchmark]
    public byte[] PlainJpeg() => Barcodes.QrCode(Value).ModuleSize(40).ToJpeg();

    [Benchmark]
    public byte[] RoundedPng() => Barcodes.QrCode(Value).ModuleSize(40).ModuleStyle(QrModuleStyle.Rounded).ToPng();
}
