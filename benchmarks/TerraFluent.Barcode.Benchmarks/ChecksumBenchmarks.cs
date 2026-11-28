using BenchmarkDotNet.Attributes;

namespace TerraFluent.Barcode.Benchmarks;

/// <summary>
/// Symbols where check-word work is a large part of the encode: Reed-Solomon
/// error correction for the 2D symbologies at high levels, and the zlib
/// Adler-32 and PNG CRC-32 over a large image.
/// </summary>
[MemoryDiagnoser]
public class ChecksumBenchmarks
{
    private static readonly string Text600 = Samples.Text(600);
    private static readonly string Text1000 = Samples.Text(1000);
    private static readonly string Text1500 = Samples.Text(1500);
    private static readonly string Text1700 = Samples.Text(1700);

    /// <summary>Version 40-H: 81 blocks of 30 error-correction bytes each.</summary>
    [Benchmark]
    public BarcodeMatrix QrHigh() => Barcodes.QrCode(Text1000).ErrorCorrection(QrErrorCorrectionLevel.H).Encode();

    /// <summary>144 x 144 symbol: 10 interleaved blocks with 620 error-correction codewords in all.</summary>
    [Benchmark]
    public BarcodeMatrix DataMatrixLarge() => Barcodes.DataMatrix(Text1500).Encode();

    /// <summary>Level 8: 512 error-correction codewords in GF(929).</summary>
    [Benchmark]
    public BarcodeMatrix Pdf417Level8() => Barcodes.Pdf417(Text600).Pdf417ErrorCorrection(8).Encode();

    /// <summary>A large Aztec symbol with half of it error correction.</summary>
    [Benchmark]
    public BarcodeMatrix AztecHalf() => Barcodes.Aztec(Text1000).AztecErrorCorrection(50).Encode();

    [Benchmark]
    public BarcodeMatrix MaxiCode() => Samples.Create("MaxiCode").Encode();

    /// <summary>A 6,440 x 6,440 pixel PNG: Adler-32 over about 5 MB of filtered rows.</summary>
    [Benchmark]
    public byte[] LargePng() => Barcodes.QrCode(Text1700).ModuleSize(40).ToPng();
}
