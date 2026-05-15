using InSharpMcp.Concurrency;
using InSharpMcp.Contracts;
using InSharpMcp.Events;
using InSharpMcp.Interaction;
using InSharpMcp.Limits;
using InSharpMcp.Registry;
using InSharpMcp.Routing;
using InSharpMcp.Selectors;
using InSharpMcp.Tracing;
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
        services.AddSingleton<AppInstanceConnectionRegistry>();
        services.AddSingleton<AppInstanceSelector>();
        services.AddSingleton<AppInstanceRouter>();
        services.AddSingleton<AppRegistrationService>();
        services.AddSingleton<ToolLimitPolicy>();
        services.AddSingleton<ToolLimitPolicyEvaluator>();
        services.AddSingleton<ClientLimitConfigurationParser>();
        services.AddSingleton<ElementSelectorMatcher>();
        services.AddSingleton<InteractionInputValidator>();
        services.AddSingleton<IEventLogProvider, BoundedEventLog>();
        services.AddSingleton<ITraceStore, BoundedTraceStore>();
        services.AddSingleton(options.Concurrency);
        services.AddSingleton<ConcurrentCallGate>();
        services.AddSingleton<IUiOperationQueue, UiOperationQueue>();
        services.AddSingleton(options.LocalAppTransport);
        services.AddHostedService<LocalBrokerPipeServer>();

        return services;
    }
}
