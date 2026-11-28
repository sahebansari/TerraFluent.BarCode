namespace TerraFluent.Barcode.Benchmarks;

internal static class Samples
{
    public const string Url = "https://terrafluent.dev/products/12345?ref=label";

    /// <summary>1,500 alphanumeric characters then 200 lower-case letters: a version 30-something QR code with two segments.</summary>
    public static readonly string QrLarge = new string('A', 1500) + Text(200);

    public static readonly string Text200 = Text(200);

    public static string Text(int length) => string.Concat(Enumerable.Range(0, length).Select(i => (char)('a' + i % 26)));

    public static BarcodeBuilder Create(string kind) => kind switch
    {
        "Code128" => Barcodes.Code128("ABC-12345-XYZ"),
        "Ean13" => Barcodes.Ean13("590123412345"),
        "QrSmall" => Barcodes.QrCode(Url),
        "QrLarge" => Barcodes.QrCode(QrLarge),
        "DataMatrix" => Barcodes.DataMatrix(Text200),
        "Pdf417" => Barcodes.Pdf417(Text200),
        "Itf14" => Barcodes.Itf14("1540014128876"),
        "Codabar" => Barcodes.Codabar("A40156B"),
        "Aztec" => Barcodes.Aztec(Text200),
        "MaxiCode" => Barcodes.MaxiCode("[)>\u001e01\u001d961Z00004951\u001dUPSN\u001d06X610\u001d159\u001d1234567\u001d1/1"),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown sample."),
    };
}
