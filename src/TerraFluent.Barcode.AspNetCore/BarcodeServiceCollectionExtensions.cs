using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace TerraFluent.Barcode.AspNetCore;

/// <summary>Registers TerraFluent.Barcode with an <see cref="IServiceCollection"/>.</summary>
public static class BarcodeServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="BarcodeEndpointOptions"/> and, unless one is registered already,
    /// a singleton <see cref="IBarcodeGenerator"/> that uses <see cref="BarcodeEndpointOptions.Defaults"/>.
    /// The endpoint, <see cref="BarcodeResultExtensions"/> and <see cref="BarcodeTagHelper"/> use that generator.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional settings; may be called more than once.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    public static IServiceCollection AddBarcodes(this IServiceCollection services, Action<BarcodeEndpointOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        var options = services.AddOptions<BarcodeEndpointOptions>();
        if (configure is not null) options.Configure(configure);
        services.TryAddSingleton<IBarcodeGenerator>(sp => new BarcodeGenerator(sp.GetRequiredService<IOptions<BarcodeEndpointOptions>>().Value.Defaults));
        return services;
    }
}
