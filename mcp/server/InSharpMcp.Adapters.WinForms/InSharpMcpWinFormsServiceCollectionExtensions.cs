using InSharpMcp.Contracts;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Forms;

namespace InSharpMcp.Adapters.WinForms;

public static class InSharpMcpWinFormsServiceCollectionExtensions
{
    public static IServiceCollection AddInSharpMcpWinFormsAdapter(
        this IServiceCollection services,
        Form form,
        string appName,
        string appVersion,
        string platformTarget)
    {
        services.AddSingleton<IUiDispatcher>(_ => new WinFormsUiDispatcher(form));
        services.AddSingleton<IUiTreeInspector>(provider =>
            new WinFormsVisualTreeInspector(form, provider.GetRequiredService<IUiDispatcher>()));
        services.AddSingleton<IAppProvider>(provider =>
            new WinFormsAppProvider(form, provider.GetRequiredService<IUiDispatcher>(), appName, appVersion, platformTarget));
        services.AddSingleton<IScreenshotProvider>(provider =>
            new WinFormsScreenshotProvider(form, provider.GetRequiredService<IUiDispatcher>()));
        services.AddSingleton<WinFormsPointerInputSimulator>(provider =>
            new WinFormsPointerInputSimulator(form, provider.GetRequiredService<IUiDispatcher>()));
        services.AddSingleton<IPointerInputSimulator>(provider =>
            provider.GetRequiredService<WinFormsPointerInputSimulator>());
        services.AddSingleton<IElementClickSimulator>(provider =>
            provider.GetRequiredService<WinFormsPointerInputSimulator>());
        services.AddSingleton<IAutomationPeerInvoker>(provider =>
            new WinFormsAutomationPeerInvoker(form, provider.GetRequiredService<IUiDispatcher>()));
        services.AddSingleton<IElementPropertyEditor>(provider =>
            new WinFormsElementPropertyEditor(form, provider.GetRequiredService<IUiDispatcher>()));
        services.AddSingleton<IAccessibilityTreeProvider, WinFormsAccessibilityTreeProvider>();
        return services;
    }
}
