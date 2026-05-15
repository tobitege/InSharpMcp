using InSharpMcp.Concurrency;
using InSharpMcp.Contracts;
using InSharpMcp.Limits;
using InSharpMcp.Registry;
using Microsoft.Extensions.DependencyInjection;

namespace InSharpMcp;

public static class InSharpMcpServiceCollectionExtensions
{
    public static IServiceCollection AddInSharpMcpCore(this IServiceCollection services)
    {
        services.AddSingleton<AppInstanceRegistry>();
        services.AddSingleton<AppInstanceSelector>();
        services.AddSingleton<ToolLimitPolicy>();
        services.AddSingleton<ToolLimitPolicyEvaluator>();
        services.AddSingleton<InSharpMcpConcurrencyOptions>();
        services.AddSingleton<IUiOperationQueue, UiOperationQueue>();

        return services;
    }
}
