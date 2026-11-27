namespace TerraFluent.BarCode.Encoding;

/// <summary>Appends alternating bar/space runs to a linear module list.</summary>
internal sealed class ModuleWriter
{
    private readonly List<bool> _modules = new(128);

    public int Count => _modules.Count;

    public IReadOnlyList<bool> Modules => _modules;

    /// <summary>Appends a run of <paramref name="width"/> modules.</summary>
    public void Append(bool dark, int width)
    {
        for (var i = 0; i < width; i++) _modules.Add(dark);
    }

    /// <summary>Appends runs from a digit string of widths that alternates bar, space, bar..., starting with a bar.</summary>
    public void AppendWidths(string widths)
    {
        var dark = true;
        foreach (var w in widths)
        {
            Append(dark, w - '0');
            dark = !dark;
        }
    }

    /// <summary>Appends a binary pattern such as <c>"0001101"</c> where <c>1</c> is a dark module.</summary>
    public void AppendBits(string bits)
    {
        foreach (var b in bits) _modules.Add(b == '1');
    }
}
