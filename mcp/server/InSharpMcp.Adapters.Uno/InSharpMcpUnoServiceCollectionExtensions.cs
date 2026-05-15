using InSharpMcp.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace InSharpMcp.Adapters.Uno;

public static class InSharpMcpUnoServiceCollectionExtensions
{
    public static IServiceCollection AddInSharpMcpUnoAdapter(
        this IServiceCollection services,
        Window window,
        string appName,
        string appVersion,
        string platformTarget)
    {
        services.AddSingleton<IUiDispatcher>(_ => new UnoUiDispatcher(window.DispatcherQueue));
        services.AddSingleton<IUiTreeInspector>(provider =>
        {
            var root = window.Content ?? throw new InvalidOperationException("The Uno window has no content root.");
            return new UnoVisualTreeInspector(root, provider.GetRequiredService<IUiDispatcher>());
        });
        services.AddSingleton<IAppProvider>(_ => new UnoAppProvider(window, appName, appVersion, platformTarget));
        services.AddSingleton<IScreenshotProvider, UnoScreenshotProvider>();
        services.AddSingleton<IPointerInputSimulator, UnoPointerInputSimulator>();
        services.AddSingleton<IAutomationPeerInvoker, UnoAutomationPeerInvoker>();
        return services;
    }
}
