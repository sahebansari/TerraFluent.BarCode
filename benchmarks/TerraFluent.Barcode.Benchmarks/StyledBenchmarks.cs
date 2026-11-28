using BenchmarkDotNet.Attributes;

namespace TerraFluent.Barcode.Benchmarks;

/// <summary>Styled output, which is rasterised with anti-aliasing.</summary>
[MemoryDiagnoser]
public class StyledBenchmarks
{
    private readonly byte[] _logo = Barcodes.QrCode("logo").ModuleStyle(QrModuleStyle.Dots).ToPng();

    [Benchmark]
    public byte[] Code128TextPng() => Barcodes.Code128("ABC-12345-XYZ").HumanReadableText().ToPng();

    [Benchmark]
    public byte[] Ean13TextPng() => Barcodes.Ean13("590123412345").HumanReadableText().ToPng();

    [Benchmark]
    public byte[] QrRoundedGradientPng() => RoundedGradient().ToPng();

    [Benchmark]
    public byte[] QrRoundedGradientJpeg() => RoundedGradient().ToJpeg();

    [Benchmark]
    public string QrRoundedGradientSvg() => RoundedGradient().ToSvg();

    [Benchmark]
    public byte[] QrLogoPng() => Barcodes.QrCode(Samples.Url).Logo(_logo).ToPng();

    private static BarcodeBuilder RoundedGradient() =>
        Barcodes.QrCode(Samples.Url).ModuleStyle(QrModuleStyle.Rounded).Gradient("#123456", "#000000");
}
