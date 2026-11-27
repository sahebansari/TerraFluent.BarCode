namespace TerraFluent.BarCode;

/// <summary>Shape of each dark QR data module.</summary>
public enum QrModuleShape
{
    /// <summary>Classic square modules that join into solid areas.</summary>
    Square,

    /// <summary>Squares with rounded corners, drawn individually.</summary>
    RoundedSquare,

    /// <summary>Round dots.</summary>
    Circle,

    /// <summary>Squares rotated 45 degrees.</summary>
    Diamond,
}

/// <summary>Shape of the three large corner squares (finder patterns) of a QR code.</summary>
public enum QrFinderShape
{
    /// <summary>Classic square ring and centre.</summary>
    Square,

    /// <summary>Square ring and centre with rounded corners.</summary>
    Rounded,

    /// <summary>Circular ring and centre.</summary>
    Circle,
}

/// <summary>Shape of the bars of a linear barcode.</summary>
public enum BarShape
{
    /// <summary>Classic rectangular bars.</summary>
    Square,

    /// <summary>Bars with fully rounded ends.</summary>
    Rounded,
}

/// <summary>How a gradient runs across the symbol.</summary>
public enum GradientDirection
{
    /// <summary>Left to right.</summary>
    Horizontal,

    /// <summary>Top to bottom.</summary>
    Vertical,

    /// <summary>Top-left to bottom-right.</summary>
    Diagonal,

    /// <summary>From the centre outwards.</summary>
    Radial,
}
