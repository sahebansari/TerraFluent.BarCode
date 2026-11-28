namespace TerraFluent.Barcode;

/// <summary>How the data modules (the small squares) of a QR code are drawn.</summary>
public enum QrModuleStyle
{
    /// <summary>Plain squares (the default).</summary>
    Square,

    /// <summary>Squares with softly rounded corners.</summary>
    Rounded,

    /// <summary>Circles.</summary>
    Dots,

    /// <summary>
    /// Squares that join up with their neighbours and round only their outer
    /// corners, giving smooth, flowing shapes.
    /// </summary>
    Connected,
}
