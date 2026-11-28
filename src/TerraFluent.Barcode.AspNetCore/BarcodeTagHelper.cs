using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace TerraFluent.Barcode.AspNetCore;

/// <summary>
/// Renders a barcode in a Razor view or page, as an <c>img</c> with a <c>data:</c> URI or as
/// inline SVG. Register it in <c>_ViewImports.cshtml</c> with
/// <c>@addTagHelper *, TerraFluent.Barcode.AspNetCore</c>, then write:
/// <code>
/// &lt;barcode type="QrCode" value="@Model.Url" module-size="6" class="qr" /&gt;
/// &lt;barcode type="Code128" value="@Model.Sku" human-readable="true" inline-svg="true" /&gt;
/// </code>
/// </summary>
/// <remarks>
/// The barcode uses the <see cref="IBarcodeGenerator"/> from dependency injection (or a default
/// one), so <see cref="BarcodeEndpointOptions.Defaults"/> apply; the attributes override them.
/// Other attributes such as <c>class</c>, <c>width</c> and <c>style</c> are copied to the
/// <c>img</c> or <c>svg</c> element. <c>alt</c> defaults to the value; for inline SVG it becomes
/// <c>aria-label</c> with <c>role="img"</c>. A null or empty value renders nothing. A value the
/// symbology cannot encode throws, like the rest of the library.
/// </remarks>
[HtmlTargetElement("barcode", TagStructure = TagStructure.WithoutEndTag)]
public sealed class BarcodeTagHelper : TagHelper
{
    private readonly HtmlEncoder _encoder;

    /// <summary>Creates the tag helper.</summary>
    /// <param name="encoder">Encodes attribute values copied into inline SVG.</param>
    public BarcodeTagHelper(HtmlEncoder encoder) => _encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));

    /// <summary>The view context, set by Razor.</summary>
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext? ViewContext { get; set; }

    /// <summary>The symbology. Default <see cref="BarcodeType.Code128"/>.</summary>
    [HtmlAttributeName("type")]
    public BarcodeType Type { get; set; } = BarcodeType.Code128;

    /// <summary>The data to encode.</summary>
    [HtmlAttributeName("value")]
    public string? Value { get; set; }

    /// <summary>Image format of the <c>data:</c> URI. Default SVG, which stays sharp at any size.</summary>
    [HtmlAttributeName("format")]
    public BarcodeImageFormat Format { get; set; } = BarcodeImageFormat.Svg;

    /// <summary>Writes the SVG markup into the page instead of an <c>img</c> element.</summary>
    [HtmlAttributeName("inline-svg")]
    public bool InlineSvg { get; set; }

    /// <summary>Module size in pixels.</summary>
    [HtmlAttributeName("module-size")]
    public int? ModuleSize { get; set; }

    /// <summary>Bar height in pixels (linear barcodes).</summary>
    [HtmlAttributeName("height")]
    public int? Height { get; set; }

    /// <summary>Quiet zone in modules.</summary>
    [HtmlAttributeName("quiet-zone")]
    public int? QuietZone { get; set; }

    /// <summary>Prints the text under a linear barcode.</summary>
    [HtmlAttributeName("human-readable")]
    public bool? HumanReadableText { get; set; }

    /// <summary>Bar colour as <c>#RRGGBB</c>.</summary>
    [HtmlAttributeName("foreground")]
    public string? Foreground { get; set; }

    /// <summary>Background colour as <c>#RRGGBB</c>.</summary>
    [HtmlAttributeName("background")]
    public string? Background { get; set; }

    /// <summary>Leaves the background transparent (PNG and SVG).</summary>
    [HtmlAttributeName("transparent")]
    public bool TransparentBackground { get; set; }

    /// <summary>QR error-correction level.</summary>
    [HtmlAttributeName("error-correction")]
    public QrErrorCorrectionLevel? ErrorCorrection { get; set; }

    /// <inheritdoc/>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(output);

        if (string.IsNullOrEmpty(Value))
        {
            output.SuppressOutput();
            return;
        }

        var generator = ViewContext?.HttpContext is { } http ? BarcodeHttpResult.ResolveGenerator(http) : new BarcodeGenerator();
        var builder = generator.Create(Type, Value);
        if (ModuleSize is { } moduleSize) builder.ModuleSize(moduleSize);
        if (Height is { } height) builder.Height(height);
        if (QuietZone is { } quietZone) builder.QuietZone(quietZone);
        if (HumanReadableText is { } text) builder.HumanReadableText(text);
        if (!string.IsNullOrEmpty(Foreground)) builder.Foreground(Foreground);
        if (!string.IsNullOrEmpty(Background)) builder.Background(Background);
        if (TransparentBackground) builder.TransparentBackground();
        if (ErrorCorrection is { } level) builder.ErrorCorrection(level);

        var alt = output.Attributes.TryGetAttribute("alt", out var altAttribute) ? altAttribute.Value?.ToString() : Value;

        if (InlineSvg)
        {
            output.TagName = null;
            output.Content.SetHtmlContent(WithAttributes(builder.ToSvg(), output.Attributes, alt));
            output.Attributes.Clear();
            return;
        }

        output.TagName = "img";
        output.TagMode = TagMode.SelfClosing;
        output.Attributes.SetAttribute("src", builder.ToDataUri(Format));
        if (altAttribute is null) output.Attributes.SetAttribute("alt", alt);
    }

    // Copies the element's other attributes onto the root <svg> element. An attribute the root already
    // has (width, height) is replaced; a width or height of the page's own drops both intrinsic sizes so
    // the viewBox keeps the aspect ratio.
    private string WithAttributes(string svg, TagHelperAttributeList attributes, string? label)
    {
        var start = svg.IndexOf("<svg", StringComparison.Ordinal);
        var end = start < 0 ? -1 : svg.IndexOf('>', start);
        if (end < 0) return svg;

        var root = svg[(start + "<svg".Length)..end];
        var resized = attributes.ContainsName("width") || attributes.ContainsName("height");
        if (resized)
        {
            root = RemoveAttribute(RemoveAttribute(root, "width"), "height");
        }

        using var writer = new StringWriter();
        writer.Write("<svg role=\"img\"");
        if (!string.IsNullOrEmpty(label))
        {
            writer.Write(" aria-label=\"");
            _encoder.Encode(writer, label);
            writer.Write('"');
        }

        foreach (var attribute in attributes)
        {
            if (string.Equals(attribute.Name, "alt", StringComparison.OrdinalIgnoreCase)) continue;
            root = RemoveAttribute(root, attribute.Name);
            writer.Write(' ');
            attribute.WriteTo(writer, _encoder);
        }

        writer.Write(root);
        return string.Concat(svg.AsSpan(0, start), writer.ToString(), svg.AsSpan(end));
    }

    // The renderer writes plain name="value" pairs with encoded values, so a simple scan is enough.
    private static string RemoveAttribute(string rootAttributes, string name)
    {
        var needle = " " + name + "=\"";
        var index = rootAttributes.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return rootAttributes;
        var close = rootAttributes.IndexOf('"', index + needle.Length);
        return close < 0 ? rootAttributes : rootAttributes.Remove(index, close + 1 - index);
    }
}
