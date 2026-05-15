using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls;
using InSharpMcp.Adapters.Avalonia;
using InSharpMcp.Bridge;
using Microsoft.Extensions.DependencyInjection;

namespace InSharpMcp.Demo.Avalonia;

public partial class MainWindow : Window
{
    private readonly ServiceProvider _mcpServices;

    public MainWindow()
    {
        InitializeComponent();

        var services = new ServiceCollection();
        services.AddInSharpMcpAvaloniaAdapter(
            this,
            "InSharpMcp Avalonia Demo",
            typeof(MainWindow).Assembly.GetName().Version?.ToString() ?? "0.0.0",
            "Avalonia");
        services.AddInSharpMcpBridge();
        _mcpServices = services.BuildServiceProvider();
        Opened += (_, _) => _ = RegisterWithBrokerAsync();
        Closed += (_, _) => _mcpServices.Dispose();
    }

    private async Task RegisterWithBrokerAsync()
    {
        var registration = new AppBridgeRegistration(
            AppId: "insharpmcp.demo.avalonia",
            AppName: "InSharpMcp Avalonia Demo",
            AdapterKind: "avalonia",
            PlatformTarget: "Avalonia",
            AppVersion: typeof(MainWindow).Assembly.GetName().Version?.ToString() ?? "0.0.0",
            Capabilities: AppBridgeCapabilities.Standard,
            InstanceId: $"avalonia-demo-{Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture)}");

        try
        {
            await _mcpServices.GetRequiredService<InSharpMcpBridge>()
                .StartAsync(registration)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"InSharpMcp demo registration failed: {exception.Message}");
        }
    }
}
