using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TerraFluent.Barcode.AspNetCore.Tests;

/// <summary>Hosts a minimal API on the in-memory test server.</summary>
internal sealed class TestApp : IAsyncDisposable
{
    private readonly WebApplication _app;

    private TestApp(WebApplication app)
    {
        _app = app;
        Client = app.GetTestClient();
    }

    public HttpClient Client { get; }

    public IEnumerable<Microsoft.AspNetCore.Http.Endpoint> Endpoints =>
        _app.Services.GetRequiredService<Microsoft.AspNetCore.Routing.EndpointDataSource>().Endpoints;

    public static async Task<TestApp> StartAsync(Action<WebApplication> map, Action<IServiceCollection>? services = null)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        services?.Invoke(builder.Services);
        var app = builder.Build();
        map(app);
        await app.StartAsync();
        return new TestApp(app);
    }

    public static Task<TestApp> StartEndpointAsync(Action<BarcodeEndpointOptions>? configure = null, string pattern = "/barcodes") =>
        StartAsync(app => app.MapBarcodes(pattern), s => s.AddBarcodes(configure));

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.DisposeAsync();
    }
}
