namespace TerraFluent.Barcode.AspNetCore;

/// <summary>Lenient parsing of type, format and enum names from URLs: case, dashes, underscores and spaces are ignored.</summary>
internal static class BarcodeParsing
{
    private static readonly Dictionary<string, BarcodeType> Aliases = new(StringComparer.Ordinal)
    {
        ["qr"] = BarcodeType.QrCode,
        ["gs1128"] = BarcodeType.Gs1Code128,
        ["ean128"] = BarcodeType.Gs1Code128,
        ["upc"] = BarcodeType.UpcA,
        ["dm"] = BarcodeType.DataMatrix,
        ["interleaved2of5"] = BarcodeType.Itf,
        ["i2of5"] = BarcodeType.Itf,
        ["nw7"] = BarcodeType.Codabar,
        ["azteccode"] = BarcodeType.Aztec,
        ["microqr"] = BarcodeType.MicroQrCode,
        ["rmqr"] = BarcodeType.RectangularMicroQrCode,
        ["rectangularmicroqr"] = BarcodeType.RectangularMicroQrCode,
        ["databar"] = BarcodeType.Gs1DataBar,
        ["databaromnidirectional"] = BarcodeType.Gs1DataBar,
        ["rss14"] = BarcodeType.Gs1DataBar,
        ["databarlimited"] = BarcodeType.Gs1DataBarLimited,
        ["rsslimited"] = BarcodeType.Gs1DataBarLimited,
        ["databarexpanded"] = BarcodeType.Gs1DataBarExpanded,
        ["rssexpanded"] = BarcodeType.Gs1DataBarExpanded,
        ["imb"] = BarcodeType.IntelligentMail,
        ["uspsimb"] = BarcodeType.IntelligentMail,
    };

    public static bool TryParseType(string? value, out BarcodeType type)
    {
        var key = Normalize(value);
        if (Aliases.TryGetValue(key, out type)) return true;
        return TryParseEnum(key, out type);
    }

    public static bool TryParseFormat(string? value, out BarcodeImageFormat format)
    {
        switch (Normalize(value))
        {
            case "png":
                format = BarcodeImageFormat.Png;
                return true;
            case "jpg" or "jpeg" or "jpe":
                format = BarcodeImageFormat.Jpeg;
                return true;
            case "svg":
                format = BarcodeImageFormat.Svg;
                return true;
            default:
                format = default;
                return false;
        }
    }

    public static bool TryParseEnum<T>(string? value, out T result) where T : struct, Enum
    {
        var key = Normalize(value);
        foreach (var candidate in Enum.GetValues<T>())
        {
            if (Normalize(candidate.ToString()) == key)
            {
                result = candidate;
                return true;
            }
        }

        result = default;
        return false;
    }

    public static bool IsLinear(BarcodeType type) =>
        type is not (BarcodeType.QrCode or BarcodeType.DataMatrix or BarcodeType.Pdf417 or BarcodeType.Aztec or BarcodeType.MaxiCode
            or BarcodeType.MicroQrCode or BarcodeType.RectangularMicroQrCode or BarcodeType.IntelligentMail);

    public static string Normalize(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var chars = new char[value.Length];
        var n = 0;
        foreach (var c in value)
        {
            if (c is '-' or '_' or ' ') continue;
            chars[n++] = char.ToLowerInvariant(c);
        }

        return new string(chars, 0, n);
    }
}
