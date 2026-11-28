namespace TerraFluent.Barcode;

/// <summary>Which Data Matrix symbol shapes may be chosen.</summary>
public enum DataMatrixShape
{
    /// <summary>Square symbols only, 10x10 to 144x144 (the default, and what most scanners expect).</summary>
    Square,

    /// <summary>Rectangular symbols only, 8x18 to 16x48, for narrow labels.</summary>
    Rectangle,

    /// <summary>The smallest symbol of either shape.</summary>
    Any,
}
