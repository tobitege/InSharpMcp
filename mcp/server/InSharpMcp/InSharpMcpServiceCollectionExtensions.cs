using InSharpMcp.Concurrency;
using InSharpMcp.Contracts;
using InSharpMcp.Events;
using InSharpMcp.Limits;
using InSharpMcp.Registry;
using InSharpMcp.Security;
using InSharpMcp.Selectors;
using InSharpMcp.Transports;
using Microsoft.Extensions.DependencyInjection;

namespace InSharpMcp;

public static class InSharpMcpServiceCollectionExtensions
{
    public static IServiceCollection AddInSharpMcpCore(this IServiceCollection services)
    {
        return services.AddInSharpMcpCore(new BrokerMcpHostOptions());
    }

    public static IServiceCollection AddInSharpMcpCore(this IServiceCollection services, BrokerMcpHostOptions options)
    {
        services.AddSingleton<AppInstanceRegistry>();
        services.AddSingleton<AppInstanceSelector>();
        services.AddSingleton<AppRegistrationService>();
        services.AddSingleton<ToolLimitPolicy>();
        services.AddSingleton<ToolLimitPolicyEvaluator>();
        services.AddSingleton<ClientLimitConfigurationParser>();
        services.AddSingleton<ElementSelectorMatcher>();
        services.AddSingleton<IEventLogProvider, BoundedEventLog>();
        services.AddSingleton(options.Concurrency);
        services.AddSingleton(options.Access);
        services.AddSingleton<McpAuthorization>();
        services.AddSingleton<ConcurrentCallGate>();
        services.AddSingleton<IUiOperationQueue, UiOperationQueue>();

        return services;
    }
}
