namespace TerraFluent.BarCode;

/// <summary>QR code error-correction level: how much of the symbol can be damaged and still read.</summary>
public enum QrErrorCorrectionLevel
{
    /// <summary>About 7% of codewords can be restored. Smallest symbol.</summary>
    L,

    /// <summary>About 15% of codewords can be restored. The default.</summary>
    M,

    /// <summary>About 25% of codewords can be restored.</summary>
    Q,

    /// <summary>About 30% of codewords can be restored. Use when printing a logo over the code or on rough surfaces.</summary>
    H,
}
