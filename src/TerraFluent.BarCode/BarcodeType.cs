namespace TerraFluent.BarCode;

/// <summary>The barcode symbologies supported by TerraFluent.BarCode.</summary>
public enum BarcodeType
{
    /// <summary>Code 128 (ISO/IEC 15417): full ASCII, with automatic code set A/B/C switching.</summary>
    Code128,

    /// <summary>Code 39 (ISO/IEC 16388): digits, upper-case letters and <c>- . $ / + %</c> and space.</summary>
    Code39,

    /// <summary>EAN-13 (ISO/IEC 15420): 12 digits plus a check digit.</summary>
    Ean13,

    /// <summary>EAN-8 (ISO/IEC 15420): 7 digits plus a check digit.</summary>
    Ean8,

    /// <summary>UPC-A (ISO/IEC 15420): 11 digits plus a check digit.</summary>
    UpcA,

    /// <summary>QR code (ISO/IEC 18004), model 2, versions 1-40.</summary>
    QrCode,
}
