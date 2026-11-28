using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace TerraFluent.Barcode.AspNetCore.Tests;

public class TagHelperTests
{
    [Fact]
    public void Renders_an_img_with_an_svg_data_uri_by_default()
    {
        var helper = Create();
        helper.Type = BarcodeType.QrCode;
        helper.Value = "hello";

        var output = Run(helper, new TagHelperAttribute("class", "qr"));

        Assert.Equal("img", output.TagName);
        Assert.Equal(TagMode.SelfClosing, output.TagMode);
        Assert.Equal(Barcodes.QrCode("hello").ToDataUri(BarcodeImageFormat.Svg), output.Attributes["src"].Value);
        Assert.Equal("hello", output.Attributes["alt"].Value);
        Assert.Equal("qr", output.Attributes["class"].Value);
    }

    [Fact]
    public void Applies_attributes_and_keeps_an_explicit_alt()
    {
        var helper = Create();
        helper.Type = BarcodeType.Code128;
        helper.Value = "ABC";
        helper.Format = BarcodeImageFormat.Png;
        helper.ModuleSize = 3;
        helper.Height = 40;
        helper.QuietZone = 4;
        helper.HumanReadableText = true;
        helper.Foreground = "#112233";
        helper.Background = "#EEEEEE";

        var output = Run(helper, new TagHelperAttribute("alt", "Order ABC"));

        var expected = Barcodes.Code128("ABC").ModuleSize(3).Height(40).QuietZone(4).HumanReadableText()
            .Foreground("#112233").Background("#EEEEEE").ToDataUri(BarcodeImageFormat.Png);
        Assert.Equal(expected, output.Attributes["src"].Value);
        Assert.Equal("Order ABC", output.Attributes["alt"].Value);
    }

    [Fact]
    public void Inline_svg_carries_the_other_attributes_and_an_accessible_label()
    {
        var helper = Create();
        helper.Type = BarcodeType.QrCode;
        helper.Value = "a\"<b>";
        helper.InlineSvg = true;
        helper.ErrorCorrection = QrErrorCorrectionLevel.H;
        helper.TransparentBackground = true;

        var output = Run(helper, new TagHelperAttribute("class", "big \"x\""));

        Assert.Null(output.TagName);
        Assert.Empty(output.Attributes);
        var html = output.Content.GetContent();
        Assert.StartsWith("<svg role=\"img\" aria-label=\"a&quot;&lt;b&gt;\" class=\"big &quot;x&quot;\" xmlns=", html, StringComparison.Ordinal);
        var expected = Barcodes.QrCode("a\"<b>").ErrorCorrection(QrErrorCorrectionLevel.H).TransparentBackground().ToSvg();
        Assert.EndsWith(expected["<svg".Length..], html, StringComparison.Ordinal);
    }

    [Fact]
    public void Inline_svg_size_from_the_page_replaces_the_intrinsic_size()
    {
        var helper = Create();
        helper.Type = BarcodeType.Code128;
        helper.Value = "ABC";
        helper.InlineSvg = true;

        var html = Run(helper, new TagHelperAttribute("width", "200")).Content.GetContent();
        var root = html[..(html.IndexOf('>', StringComparison.Ordinal) + 1)];

        Assert.Single(System.Text.RegularExpressions.Regex.Matches(root, " width="));
        Assert.Contains(" width=\"200\"", root, StringComparison.Ordinal);
        Assert.DoesNotContain(" height=", root, StringComparison.Ordinal);
        Assert.Contains("viewBox=", root, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Empty_values_render_nothing(string? value)
    {
        var helper = Create();
        helper.Value = value;
        var output = Run(helper);
        Assert.Null(output.TagName);
        Assert.True(output.Content.IsEmptyOrWhiteSpace);
    }

    [Fact]
    public void Uses_the_registered_generator_defaults()
    {
        var services = new ServiceCollection().AddBarcodes(o => o.Defaults = new BarcodeOptions { ModuleSize = 8 }).BuildServiceProvider();
        var helper = Create(services);
        helper.Type = BarcodeType.QrCode;
        helper.Value = "x";

        var output = Run(helper);
        Assert.Equal(Barcodes.QrCode("x").ModuleSize(8).ToDataUri(BarcodeImageFormat.Svg), output.Attributes["src"].Value);
    }

    [Fact]
    public void Values_the_symbology_cannot_encode_throw()
    {
        var helper = Create();
        helper.Type = BarcodeType.Ean13;
        helper.Value = "abc";
        Assert.ThrowsAny<ArgumentException>(() => Run(helper));
    }

    private static BarcodeTagHelper Create(IServiceProvider? services = null)
    {
        var http = new DefaultHttpContext { RequestServices = services ?? new ServiceCollection().BuildServiceProvider() };
        var viewContext = new ViewContext { HttpContext = http, RouteData = new RouteData(), ActionDescriptor = new ActionDescriptor() };
        return new BarcodeTagHelper(HtmlEncoder.Default) { ViewContext = viewContext };
    }

    private static TagHelperOutput Run(BarcodeTagHelper helper, params TagHelperAttribute[] attributes)
    {
        var all = new TagHelperAttributeList(attributes);
        var context = new TagHelperContext("barcode", new TagHelperAttributeList(attributes), new Dictionary<object, object>(), "id");
        var output = new TagHelperOutput("barcode", all, (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
        helper.Process(context, output);
        return output;
    }
}
