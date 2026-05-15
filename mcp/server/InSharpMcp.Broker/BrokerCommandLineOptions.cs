using InSharpMcp.Concurrency;
using InSharpMcp.Security;
using InSharpMcp.Transports;

namespace InSharpMcp.Broker;

internal enum BrokerTransport
{
    Stdio,
    Http,
}

internal sealed record BrokerCommandLineOptions(
    BrokerTransport Transport,
    string? SharedToken,
    bool AllowUnauthenticatedHttp,
    int HttpPort,
    string HttpPath,
    bool BindHttpToLoopbackOnly,
    int? MaxConcurrentCalls,
    int? MaxQueuedUiOperations,
    bool ShowHelp)
{
    public static BrokerCommandLineOptions Default { get; } = new(
        BrokerTransport.Stdio,
        SharedToken: null,
        AllowUnauthenticatedHttp: false,
        HttpPort: 52001,
        HttpPath: "/mcp",
        BindHttpToLoopbackOnly: true,
        MaxConcurrentCalls: null,
        MaxQueuedUiOperations: null,
        ShowHelp: false);

    public BrokerMcpHostOptions ToHostOptions()
    {
        var options = new BrokerMcpHostOptions
        {
            HttpPort = HttpPort,
            HttpPath = HttpPath,
            BindHttpToLoopbackOnly = BindHttpToLoopbackOnly,
            Access = new McpAccessOptions
            {
                SharedToken = SharedToken,
                AllowUnauthenticatedHttp = AllowUnauthenticatedHttp,
            },
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
