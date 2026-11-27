namespace TerraFluent.BarCode;

/// <summary>How the three large corner squares (finder patterns, or "eyes") of a QR code are drawn.</summary>
public enum QrEyeStyle
{
    /// <summary>Square frame and square centre (the default).</summary>
    Square,

    /// <summary>Rounded frame and rounded centre.</summary>
    Rounded,

    /// <summary>Circular frame and circular centre.</summary>
    Circle,
}
