using System.IO.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace TerraFluent.Barcode.Mcp.Tests;

/// <summary>Runs the real server in-process over a pair of pipes and connects an MCP client to it.</summary>
public sealed class McpServerFixture : IAsyncLifetime
{
    private readonly Pipe _clientToServer = new();
    private readonly Pipe _serverToClient = new();
    private IHost? _host;

    public McpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddSingleton(new BarcodeServerSettings(maxImagePixels: 1_000_000));
        builder.Services
            .AddMcpServer(o => o.ServerInfo = BarcodeServerSettings.ServerInfo)
            .WithStreamServerTransport(_clientToServer.Reader.AsStream(), _serverToClient.Writer.AsStream())
            .WithTools<BarcodeTools>();
        _host = builder.Build();
        await _host.StartAsync();

        Client = await McpClient.CreateAsync(
            new StreamClientTransport(_clientToServer.Writer.AsStream(), _serverToClient.Reader.AsStream()));
    }

    public async Task DisposeAsync()
    {
        await Client.DisposeAsync();
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }

    public Task<CallToolResult> CallAsync(string tool, Dictionary<string, object?> arguments) =>
        Client.CallToolAsync(tool, arguments).AsTask();
}
