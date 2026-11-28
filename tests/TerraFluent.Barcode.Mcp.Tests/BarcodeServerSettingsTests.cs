namespace TerraFluent.Barcode.Mcp.Tests;

public sealed class BarcodeServerSettingsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Unset_variable_uses_the_default(string? value) =>
        Assert.Equal(BarcodeServerSettings.DefaultMaxImagePixels, BarcodeServerSettings.FromValue(value).MaxImagePixels);

    [Fact]
    public void Variable_sets_the_limit() =>
        Assert.Equal(250_000, BarcodeServerSettings.FromValue("250000").MaxImagePixels);

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("1e6")]
    [InlineData("lots")]
    [InlineData("67108865")]
    public void Invalid_variable_fails_with_its_name(string value)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => BarcodeServerSettings.FromValue(value));
        Assert.Contains(BarcodeServerSettings.MaxImagePixelsVariable, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_rejects_limits_above_the_library_cap() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new BarcodeServerSettings(BarcodeBuilder.MaxImagePixels + 1));
}
