using InSharpMcp.Concurrency;
using InSharpMcp.Security;

namespace InSharpMcp.Transports;

public sealed class BrokerMcpHostOptions
{
    public McpAccessOptions Access { get; set; } = new();

    public InSharpMcpConcurrencyOptions Concurrency { get; set; } = new();

    public string HttpPath { get; set; } = "/mcp";

    public int HttpPort { get; set; } = 52001;

    public bool BindHttpToLoopbackOnly { get; set; } = true;
}
