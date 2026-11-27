namespace TerraFluent.BarCode.Encoding;

/// <summary>A piece of human-readable text placed at a module position of a linear barcode.</summary>
internal readonly struct TextItem
{
    public TextItem(string text, double centreModule, bool small = false, bool aboveAddOn = false)
    {
        Text = text;
        CentreModule = centreModule;
        Small = small;
        AboveAddOn = aboveAddOn;
    }

    public string Text { get; }

    /// <summary>Horizontal centre, in modules from the start of the symbol (may be negative: inside the left quiet zone).</summary>
    public double CentreModule { get; }

    /// <summary>Drawn at a smaller size (the outer UPC-A digits).</summary>
    public bool Small { get; }

    /// <summary>Drawn above the add-on bars instead of below the main symbol.</summary>
    public bool AboveAddOn { get; }
}

/// <summary>
/// Where a linear barcode's human-readable text goes, and which bars extend
/// down beside it (the EAN/UPC guard bars).
/// </summary>
internal sealed class LinearLayout
{
    /// <summary>Text centred under the whole symbol (Code 128, Code 39, GS1-128).</summary>
    public string? CentredText { get; set; }

    /// <summary>Individually placed digits (EAN/UPC and add-ons).</summary>
    public List<TextItem> Items { get; } = new();

    /// <summary>Module ranges [Start, End) whose bars extend into the text band.</summary>
    public List<(int Start, int End)> ExtendedBars { get; } = new();

    /// <summary>First module of an EAN-2/EAN-5 add-on, or -1.</summary>
    public int AddOnStart { get; set; } = -1;

    public bool IsExtended(int module)
    {
        foreach (var (start, end) in ExtendedBars)
        {
            if (module >= start && module < end) return true;
        }

        return false;
    }
}
