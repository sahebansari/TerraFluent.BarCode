namespace TerraFluent.Barcode.Encoding;

/// <summary>
/// The (n, k) width enumeration shared by the GS1 DataBar family (ISO/IEC 24724
/// Annex B): maps a value to the widths of <c>elements</c> bars (or spaces)
/// that span <c>modules</c> modules, none wider than <c>maxWidth</c>, and,
/// unless <c>noNarrow</c>, at least one of them a single module.
/// </summary>
internal static class DataBarWidths
{
    /// <summary>Binomial coefficient <c>n</c> choose <c>r</c>.</summary>
    private static int Combinations(int n, int r)
    {
        if (r < 0 || n < r) return 0;
        long value = 1;
        r = Math.Min(r, n - r);
        for (var i = 1; i <= r; i++) value = value * (n - r + i) / i;
        return (int)value;
    }

    public static int[] Widths(int value, int modules, int elements, int maxWidth, bool noNarrow)
    {
        var widths = new int[elements];
        var narrowMask = 0;
        var n = modules;
        int bar;
        for (bar = 0; bar < elements - 1; bar++)
        {
            int width;
            var subValue = 0;
            narrowMask |= 1 << bar;
            for (width = 1; ; width++, narrowMask &= ~(1 << bar))
            {
                // Combinations for the remaining elements once this one is `width` wide.
                subValue = Combinations(n - width - 1, elements - bar - 2);

                // Less those where no remaining element is narrow, if one must be.
                if (!noNarrow && narrowMask == 0 && n - width - (elements - bar - 1) >= elements - bar - 1)
                {
                    subValue -= Combinations(n - width - (elements - bar), elements - bar - 2);
                }

                // Less those where a remaining element is wider than maxWidth.
                if (elements - bar - 1 > 1)
                {
                    var lessValue = 0;
                    for (var widest = n - width - (elements - bar - 2); widest > maxWidth; widest--)
                    {
                        lessValue += Combinations(n - width - widest - 1, elements - bar - 3);
                    }

                    subValue -= lessValue * (elements - 1 - bar);
                }
                else if (n - width > maxWidth)
                {
                    subValue--;
                }

                value -= subValue;
                if (value < 0) break;
            }

            value += subValue;
            n -= width;
            widths[bar] = width;
        }

        widths[bar] = n;
        return widths;
    }

    /// <summary>
    /// Widths of a data character: <paramref name="oddValue"/> and
    /// <paramref name="evenValue"/> select the odd and even element widths,
    /// which are interleaved odd first.
    /// </summary>
    public static int[] Character(int oddValue, int evenValue, int oddModules, int evenModules, int elementsEach, int oddWidest, int evenWidest, bool oddNoNarrow)
    {
        var odd = Widths(oddValue, oddModules, elementsEach, oddWidest, oddNoNarrow);
        var even = Widths(evenValue, evenModules, elementsEach, evenWidest, !oddNoNarrow);
        var result = new int[elementsEach * 2];
        for (var i = 0; i < elementsEach; i++)
        {
            result[2 * i] = odd[i];
            result[2 * i + 1] = even[i];
        }

        return result;
    }

    /// <summary>A reversed copy, for characters and finders read right to left.</summary>
    public static int[] Reversed(int[] widths)
    {
        var result = (int[])widths.Clone();
        Array.Reverse(result);
        return result;
    }

    /// <summary>Appends alternating space/bar runs, starting with a space, to a module list.</summary>
    public static bool[] ToModules(IReadOnlyList<int> widths)
    {
        var total = 0;
        foreach (var w in widths) total += w;
        var modules = new bool[total];
        var pos = 0;
        for (var i = 0; i < widths.Count; i++)
        {
            for (var j = 0; j < widths[i]; j++) modules[pos++] = i % 2 == 1;
        }

        return modules;
    }
}
