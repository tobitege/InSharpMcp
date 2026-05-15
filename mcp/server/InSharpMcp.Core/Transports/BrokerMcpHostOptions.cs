using InSharpMcp.Concurrency;

namespace InSharpMcp.Transports;

public sealed class BrokerMcpHostOptions
{
    public InSharpMcpConcurrencyOptions Concurrency { get; set; } = new();

    public string HttpPath { get; set; } = "/mcp";

    public int HttpPort { get; set; } = 52001;

    public LocalAppTransportOptions LocalAppTransport { get; set; } = new();
}
