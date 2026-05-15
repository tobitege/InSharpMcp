using InSharpMcp.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InSharpMcp.Transports;

public static class StdioBrokerHost
{
    public static async Task RunAsync(
        Action<BrokerMcpHostOptions>? configure = null,
        CancellationToken cancellationToken = default)
    {
        var options = Configure(configure);
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddInSharpMcpCore(options);
        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithTools<InSharpMcpTools>();

        await builder.Build().RunAsync(cancellationToken).ConfigureAwait(false);
    }

    private static BrokerMcpHostOptions Configure(Action<BrokerMcpHostOptions>? configure)
    {
        var options = new BrokerMcpHostOptions();
        configure?.Invoke(options);
        return options;
    }
}
