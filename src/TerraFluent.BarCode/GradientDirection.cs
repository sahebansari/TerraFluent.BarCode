namespace TerraFluent.BarCode;

/// <summary>Direction of a foreground colour gradient, measured across the symbol (quiet zone excluded).</summary>
public enum GradientDirection
{
    /// <summary>Start colour on the left, end colour on the right.</summary>
    Horizontal,

    /// <summary>Start colour at the top, end colour at the bottom.</summary>
    Vertical,

    /// <summary>Start colour at the top-left corner, end colour at the bottom-right corner.</summary>
    Diagonal,

    /// <summary>Start colour in the centre, end colour at the corners.</summary>
    Radial,
}
