using InSharpMcp.Concurrency;
using InSharpMcp.Transports;

namespace InSharpMcp.Broker;

internal enum BrokerTransport
{
    Stdio,
    Http,
}

internal sealed record BrokerCommandLineOptions(
    BrokerTransport Transport,
    int HttpPort,
    string HttpPath,
    int? MaxConcurrentCalls,
    int? MaxQueuedUiOperations,
    bool ShowHelp)
{
    public static BrokerCommandLineOptions Default { get; } = new(
        BrokerTransport.Stdio,
        HttpPort: 52001,
        HttpPath: "/mcp",
        MaxConcurrentCalls: null,
        MaxQueuedUiOperations: null,
        ShowHelp: false);

    public BrokerMcpHostOptions ToHostOptions()
    {
        var options = new BrokerMcpHostOptions
        {
            HttpPort = HttpPort,
            HttpPath = HttpPath,
        };

        if (MaxConcurrentCalls is not null || MaxQueuedUiOperations is not null)
        {
            options.Concurrency = new InSharpMcpConcurrencyOptions
            {
                MaxConcurrentCalls = MaxConcurrentCalls ?? new InSharpMcpConcurrencyOptions().MaxConcurrentCalls,
                MaxQueuedUiOperations = MaxQueuedUiOperations ?? new InSharpMcpConcurrencyOptions().MaxQueuedUiOperations,
            };
        }

        return options;
    }
}
