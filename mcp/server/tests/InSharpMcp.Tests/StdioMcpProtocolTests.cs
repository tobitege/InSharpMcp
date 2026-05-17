using System.Diagnostics;
using System.Text.Json;

namespace InSharpMcp.Tests;

public sealed class StdioMcpProtocolTests
{
#if DEBUG
    private const string BuildConfiguration = "Debug";
#else
    private const string BuildConfiguration = "Release";
#endif

    [Fact]
    public async Task BrokerStdio_InitializesAndListsTools_WithProtocolOnlyStdout()
    {
        var brokerPath = ResolveBrokerExecutablePath();
        using var process = StartBroker(brokerPath);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        try
        {
            await SendAsync(
                process,
                new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "initialize",
                    @params = new
                    {
                        protocolVersion = "2025-06-18",
                        capabilities = new { },
                        clientInfo = new { name = "insharpmcp-test", version = "1.0.0" },
                    },
                },
                timeout.Token);

            using var initializeResponse = await ReadJsonRpcResponseAsync(process, timeout.Token);
            Assert.Equal(1, initializeResponse.RootElement.GetProperty("id").GetInt32());
            Assert.Equal("2025-06-18", initializeResponse.RootElement.GetProperty("result").GetProperty("protocolVersion").GetString());

            await SendAsync(
                process,
                new
                {
                    jsonrpc = "2.0",
                    method = "notifications/initialized",
                    @params = new { },
                },
                timeout.Token);

            await SendAsync(
                process,
                new
                {
                    jsonrpc = "2.0",
                    id = 2,
                    method = "tools/list",
                    @params = new { },
                },
                timeout.Token);

            using var toolsResponse = await ReadJsonRpcResponseAsync(process, timeout.Token);
            var tools = toolsResponse.RootElement
                .GetProperty("result")
                .GetProperty("tools")
                .EnumerateArray()
                .ToArray();
            var toolNames = tools
                .Select(tool => tool.GetProperty("name").GetString())
                .ToArray();
            var pointerClick = tools.Single(tool => tool.GetProperty("name").GetString() == "ism_pointer_click");
            var screenshot = tools.Single(tool => tool.GetProperty("name").GetString() == "ism_get_screenshot");

            Assert.Equal(2, toolsResponse.RootElement.GetProperty("id").GetInt32());
            Assert.Contains("ism_list_instances", toolNames);
            Assert.Contains("ism_visualtree_snapshot", toolNames);
            Assert.Contains("ism_pointer_click", toolNames);
            Assert.Contains("ism_element_click", toolNames);
            Assert.Contains("ism_set_element_property", toolNames);
            Assert.Equal(22, toolNames.Length);
            Assert.False(ToolHasInputProperty(pointerClick, "authorizationToken"));
            AssertToolAnnotation(pointerClick, "readOnlyHint", expected: false);
            AssertToolAnnotation(pointerClick, "destructiveHint", expected: true);
            AssertToolAnnotation(screenshot, "readOnlyHint", expected: true);
            AssertToolAnnotation(screenshot, "destructiveHint", expected: false);
        }
        finally
        {
            StopBroker(process);
        }
    }

    private static Process StartBroker(string brokerPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = brokerPath,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        return Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start broker process.");
    }

    private static async Task SendAsync(Process process, object message, CancellationToken cancellationToken)
    {
        await process.StandardInput
            .WriteLineAsync(JsonSerializer.Serialize(message).AsMemory(), cancellationToken)
            .ConfigureAwait(false);
        await process.StandardInput.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<JsonDocument> ReadJsonRpcResponseAsync(Process process, CancellationToken cancellationToken)
    {
        var line = await process.StandardOutput.ReadLineAsync(cancellationToken).ConfigureAwait(false);
        Assert.False(string.IsNullOrWhiteSpace(line), "Broker closed stdout before returning a JSON-RPC response.");

        try
        {
            return JsonDocument.Parse(line);
        }
        catch (JsonException exception)
        {
            var stderr = await process.StandardError.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            throw new Xunit.Sdk.XunitException(
                $"Broker wrote non-JSON data to stdout during MCP stdio exchange: '{line}'. Stderr: '{stderr}'. {exception.Message}");
        }
    }

    private static void StopBroker(Process process)
    {
        if (process.HasExited)
        {
            return;
        }

        process.Kill(entireProcessTree: true);
        process.WaitForExit(5_000);
    }

    private static bool ToolHasInputProperty(JsonElement tool, string propertyName)
    {
        if (!tool.TryGetProperty("inputSchema", out var schema)
            || !schema.TryGetProperty("properties", out var properties))
        {
            return false;
        }

        return properties.TryGetProperty(propertyName, out _);
    }

    private static void AssertToolAnnotation(JsonElement tool, string propertyName, bool expected)
    {
        Assert.True(tool.TryGetProperty("annotations", out var annotations), "Tool annotations were not emitted.");
        Assert.True(annotations.TryGetProperty(propertyName, out var property), $"Tool annotation '{propertyName}' was not emitted.");
        Assert.Equal(expected, property.GetBoolean());
    }

    private static string ResolveBrokerExecutablePath()
    {
        var root = FindRepositoryRoot();
        var fileName = OperatingSystem.IsWindows() ? "InSharpMcp.Broker.exe" : "InSharpMcp.Broker";
        var brokerPath = Path.Combine(
            root,
            "mcp",
            "server",
            "InSharpMcp.Broker",
            "bin",
            BuildConfiguration,
            "net8.0",
            fileName);

        Assert.True(File.Exists(brokerPath), $"Broker executable was not built at '{brokerPath}'.");
        return brokerPath;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var projectPath = Path.Combine(
                directory.FullName,
                "mcp",
                "server",
                "InSharpMcp.Broker",
                "InSharpMcp.Broker.csproj");
            if (File.Exists(projectPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root from the test output directory.");
    }
}
