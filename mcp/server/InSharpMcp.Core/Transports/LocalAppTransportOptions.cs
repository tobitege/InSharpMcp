namespace InSharpMcp.Transports;

public sealed class LocalAppTransportOptions
{
    public string BrokerPipeName { get; set; } = "InSharpMcp.Broker";

    public string AppPipeNamePrefix { get; set; } = "InSharpMcp.App";

    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(20);

    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(5);

    public TimeSpan StaleInstanceAge { get; set; } = TimeSpan.FromSeconds(15);
}
