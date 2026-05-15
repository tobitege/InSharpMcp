using Avalonia.Controls;
using InSharpMcp.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace InSharpMcp.Adapters.Avalonia;

public static class InSharpMcpAvaloniaServiceCollectionExtensions
{
    public static IServiceCollection AddInSharpMcpAvaloniaAdapter(
        this IServiceCollection services,
        Window window,
        string appName,
        string appVersion,
        string platformTarget)
    {
        services.AddSingleton<IUiDispatcher, AvaloniaUiDispatcher>();
        services.AddSingleton<IUiTreeInspector>(provider =>
        {
            var root = window.Content as Control
                ?? throw new InvalidOperationException("The Avalonia window content root must be a Control.");
            return new AvaloniaVisualTreeInspector(root, provider.GetRequiredService<IUiDispatcher>());
        });
        services.AddSingleton<IAppProvider>(provider =>
            new AvaloniaAppProvider(window, provider.GetRequiredService<IUiDispatcher>(), appName, appVersion, platformTarget));
        services.AddSingleton<IScreenshotProvider>(provider =>
        {
            var root = window.Content as Control
                ?? throw new InvalidOperationException("The Avalonia window content root must be a Control.");
            return new AvaloniaScreenshotProvider(root, provider.GetRequiredService<IUiDispatcher>());
        });
        services.AddSingleton<IPointerInputSimulator, AvaloniaPointerInputSimulator>();
        services.AddSingleton<IAutomationPeerInvoker, AvaloniaAutomationPeerInvoker>();
        services.AddSingleton<IAccessibilityTreeProvider, AvaloniaAccessibilityTreeProvider>();
        return services;
    }
}
