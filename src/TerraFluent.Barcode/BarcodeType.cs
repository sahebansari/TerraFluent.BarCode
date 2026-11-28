namespace TerraFluent.Barcode;

/// <summary>The barcode symbologies supported by TerraFluent.Barcode.</summary>
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

    /// <summary>GS1-128 (Code 128 with FNC1): GS1 application identifiers and data for logistics labels.</summary>
    Gs1Code128,

    /// <summary>Data Matrix ECC 200 (ISO/IEC 16022): compact 2D code, square or rectangular.</summary>
    DataMatrix,

    /// <summary>PDF417 (ISO/IEC 15438): stacked 2D code used on ID cards, boarding passes and shipping labels.</summary>
    Pdf417,

    /// <summary>Interleaved 2 of 5 (ISO/IEC 16390): an even number of digits, with an optional modulo 10 check digit.</summary>
    Itf,

    /// <summary>ITF-14 (GS1): Interleaved 2 of 5 for a 14-digit GTIN on cartons and outer packaging.</summary>
    Itf14,

    /// <summary>Codabar (NW-7): digits and <c>- $ : / . +</c> framed by start/stop characters A to D; used by libraries and blood banks.</summary>
    Codabar,

    /// <summary>Aztec Code (ISO/IEC 24778): compact 2D code with a central bullseye that needs no quiet zone; used on transport tickets.</summary>
    Aztec,

    /// <summary>MaxiCode (ISO/IEC 16023): fixed-size 2D code of hexagons around a bullseye, used for parcel sorting.</summary>
    MaxiCode,
}
