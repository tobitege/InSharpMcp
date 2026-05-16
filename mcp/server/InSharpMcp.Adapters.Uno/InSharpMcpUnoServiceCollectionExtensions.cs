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
        services.AddSingleton<IAppProvider>(provider =>
            new UnoAppProvider(window, provider.GetRequiredService<IUiDispatcher>(), appName, appVersion, platformTarget));
        services.AddSingleton<IScreenshotProvider>(provider =>
        {
            var root = window.Content ?? throw new InvalidOperationException("The Uno window has no content root.");
            return new UnoScreenshotProvider(root, provider.GetRequiredService<IUiDispatcher>());
        });
        services.AddSingleton<IPointerInputSimulator>(provider =>
            new UnoPointerInputSimulator(window, provider.GetRequiredService<IUiDispatcher>()));
        services.AddSingleton<IAutomationPeerInvoker>(provider =>
        {
            var root = window.Content ?? throw new InvalidOperationException("The Uno window has no content root.");
            return new UnoAutomationPeerInvoker(root, provider.GetRequiredService<IUiDispatcher>());
        });
        services.AddSingleton<IElementPropertyEditor>(provider =>
        {
            var root = window.Content ?? throw new InvalidOperationException("The Uno window has no content root.");
            return new UnoElementPropertyEditor(root, provider.GetRequiredService<IUiDispatcher>());
        });
        services.AddSingleton<IAccessibilityTreeProvider, UnoAccessibilityTreeProvider>();
        return services;
    }
}
