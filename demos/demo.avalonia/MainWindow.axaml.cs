using Avalonia.Controls;
using InSharpMcp.Adapters.Avalonia;
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
        _mcpServices = services.BuildServiceProvider();
        Closed += (_, _) => _mcpServices.Dispose();
    }
}
