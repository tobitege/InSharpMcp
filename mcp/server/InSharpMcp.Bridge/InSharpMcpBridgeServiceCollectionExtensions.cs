using Microsoft.Extensions.DependencyInjection;

namespace InSharpMcp.Bridge;

public static class InSharpMcpBridgeServiceCollectionExtensions
{
    public static IServiceCollection AddInSharpMcpBridge(this IServiceCollection services)
    {
        services.AddSingleton<LocalBridgeOptions>();
        services.AddSingleton<InSharpMcpBridge>();
        return services;
    }

    public static IServiceCollection AddInSharpMcpBridge(
        this IServiceCollection services,
        Action<LocalBridgeOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var options = new LocalBridgeOptions();
        configure(options);
        services.AddSingleton(options);
        services.AddSingleton<InSharpMcpBridge>();
        return services;
    }
}
