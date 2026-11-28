using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TerraFluent.Barcode.Mcp;

var builder = Host.CreateApplicationBuilder(args);

// stdout carries the MCP protocol, so every log line goes to stderr.
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Logging.SetMinimumLevel(LogLevel.Warning);

builder.Services.AddSingleton(BarcodeServerSettings.FromEnvironment());
builder.Services
    .AddMcpServer(o => o.ServerInfo = BarcodeServerSettings.ServerInfo)
    .WithStdioServerTransport()
    .WithTools<BarcodeTools>();

await builder.Build().RunAsync().ConfigureAwait(false);
