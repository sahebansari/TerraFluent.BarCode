namespace TerraFluent.BarCode.Rendering;

/// <summary>Where a centre logo sits, in module units, and which QR modules it hides.</summary>
internal static class LogoLayout
{
    /// <summary>Margin, in modules, kept clear around the logo.</summary>
    public const double Padding = 0.5;

    /// <summary>Logo box (left, top, size) in module coordinates of a <paramref name="size"/> x <paramref name="size"/> symbol.</summary>
    public static (double Left, double Top, double Size) Box(int size, double ratio)
    {
        var logo = size * ratio;
        var offset = (size - logo) / 2;
        return (offset, offset, logo);
    }

    /// <summary>Modules (row-major) that touch the logo box grown by <see cref="Padding"/>.</summary>
    public static bool[] HiddenModules(int size, double ratio)
    {
        var (left, top, logo) = Box(size, ratio);
        var hidden = new bool[size * size];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                hidden[y * size + x] = x + 1 > left - Padding && x < left + logo + Padding &&
                                       y + 1 > top - Padding && y < top + logo + Padding;
            }
        }

        return hidden;
    }
}
