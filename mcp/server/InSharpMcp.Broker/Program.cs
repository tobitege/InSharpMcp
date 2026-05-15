using InSharpMcp.Broker;
using InSharpMcp.Transports;

var parsed = BrokerCommandLineParser.Parse(args);
if (!parsed.Success)
{
    Console.Error.WriteLine(parsed.Error);
    Console.Error.WriteLine();
    Console.Error.WriteLine(HelpText.Text);
    return 2;
}

var options = parsed.Options!;
if (options.ShowHelp)
{
    Console.WriteLine(HelpText.Text);
    return 0;
}

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

try
{
    var hostOptions = options.ToHostOptions();
    switch (options.Transport)
    {
        case BrokerTransport.Stdio:
            await StdioBrokerHost.RunAsync(configure: target => CopyOptions(hostOptions, target), cancellation.Token)
                .ConfigureAwait(false);
            break;
        case BrokerTransport.Http:
            await HttpBrokerHost.RunAsync(configure: target => CopyOptions(hostOptions, target), cancellation.Token)
                .ConfigureAwait(false);
            break;
    }

    return 0;
}
catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
{
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}

static void CopyOptions(BrokerMcpHostOptions source, BrokerMcpHostOptions target)
{
    target.Access = source.Access;
    target.Concurrency = source.Concurrency;
    target.HttpPath = source.HttpPath;
    target.HttpPort = source.HttpPort;
    target.BindHttpToLoopbackOnly = source.BindHttpToLoopbackOnly;
}

internal static class HelpText
{
    public const string Text = """
InSharpMcp broker

Usage:
    insharp-mcp [options]

Options:
    --transport stdio|http          MCP transport to run. Defaults to stdio.
    --token <token>                 Shared token for protected tools.
    --allow-unauthenticated-http    Allow HTTP protected tools without a token.
    --http-port <port>              HTTP port. Defaults to 52001.
    --http-path <path>              HTTP MCP path. Defaults to /mcp.
    --http-any-host                 Bind HTTP to 0.0.0.0 instead of loopback.
    --max-concurrent-calls <count>  Maximum concurrent broker calls.
    --max-queued-ui-operations <count>
                                    Maximum queued UI operations per app client.
    -h, --help                      Show help.

IDE MCP clients normally use stdio:
    insharp-mcp --transport stdio --token <token>
""";
}
