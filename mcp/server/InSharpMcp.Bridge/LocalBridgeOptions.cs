namespace InSharpMcp.Bridge;

public sealed class LocalBridgeOptions
{
    public string BrokerPipeName { get; set; } = "InSharpMcp.Broker";

    public string AppPipeNamePrefix { get; set; } = "InSharpMcp.App";

    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(20);

    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(5);
}
