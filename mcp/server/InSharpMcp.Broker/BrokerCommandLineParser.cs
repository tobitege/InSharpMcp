namespace InSharpMcp.Broker;

internal static class BrokerCommandLineParser
{
    public static BrokerCommandLineParseResult Parse(IReadOnlyList<string> args)
    {
        var options = BrokerCommandLineOptions.Default;
        for (var index = 0; index < args.Count; index++)
        {
            var arg = args[index];
            switch (arg)
            {
                case "-h":
                case "--help":
                    return BrokerCommandLineParseResult.Ok(options with { ShowHelp = true });
                case "--transport":
                    if (!TryReadValue(args, ref index, arg, out var transport))
                    {
                        return BrokerCommandLineParseResult.Fail($"Missing value for {arg}.");
                    }

                    options = transport.ToLowerInvariant() switch
                    {
                        "stdio" => options with { Transport = BrokerTransport.Stdio },
                        "http" => options with { Transport = BrokerTransport.Http },
                        _ => options,
                    };
                    if (!string.Equals(transport, "stdio", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(transport, "http", StringComparison.OrdinalIgnoreCase))
                    {
                        return BrokerCommandLineParseResult.Fail("Transport must be 'stdio' or 'http'.");
                    }

                    break;
                case "--http-port":
                    if (!TryReadInt(args, ref index, arg, out var port))
                    {
                        return BrokerCommandLineParseResult.Fail("HTTP port must be an integer from 1 to 65535.");
                    }

                    if (port is < 1 or > 65535)
                    {
                        return BrokerCommandLineParseResult.Fail("HTTP port must be an integer from 1 to 65535.");
                    }

                    options = options with { HttpPort = port };
                    break;
                case "--http-path":
                    if (!TryReadValue(args, ref index, arg, out var path))
                    {
                        return BrokerCommandLineParseResult.Fail($"Missing value for {arg}.");
                    }

                    options = options with { HttpPath = path.StartsWith("/", StringComparison.Ordinal) ? path : $"/{path}" };
                    break;
                case "--max-concurrent-calls":
                    if (!TryReadPositiveInt(args, ref index, arg, out var maxConcurrentCalls))
                    {
                        return BrokerCommandLineParseResult.Fail("Max concurrent calls must be a positive integer.");
                    }

                    options = options with { MaxConcurrentCalls = maxConcurrentCalls };
                    break;
                case "--max-queued-ui-operations":
                    if (!TryReadPositiveInt(args, ref index, arg, out var maxQueuedUiOperations))
                    {
                        return BrokerCommandLineParseResult.Fail("Max queued UI operations must be a positive integer.");
                    }

                    options = options with { MaxQueuedUiOperations = maxQueuedUiOperations };
                    break;
                default:
                    return BrokerCommandLineParseResult.Fail($"Unknown argument '{arg}'.");
            }
        }

        return BrokerCommandLineParseResult.Ok(options);
    }

    private static bool TryReadValue(IReadOnlyList<string> args, ref int index, string argumentName, out string value)
    {
        _ = argumentName;
        var valueIndex = index + 1;
        if (valueIndex >= args.Count || args[valueIndex].StartsWith("-", StringComparison.Ordinal))
        {
            value = string.Empty;
            return false;
        }

        value = args[valueIndex];
        index = valueIndex;
        return true;
    }

    private static bool TryReadInt(IReadOnlyList<string> args, ref int index, string argumentName, out int value)
    {
        if (!TryReadValue(args, ref index, argumentName, out var text))
        {
            value = 0;
            return false;
        }

        return int.TryParse(text, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out value);
    }

    private static bool TryReadPositiveInt(IReadOnlyList<string> args, ref int index, string argumentName, out int value)
    {
        if (!TryReadInt(args, ref index, argumentName, out value))
        {
            return false;
        }

        return value > 0;
    }
}

internal sealed record BrokerCommandLineParseResult(
    bool Success,
    BrokerCommandLineOptions? Options,
    string? Error)
{
    public static BrokerCommandLineParseResult Ok(BrokerCommandLineOptions options) => new(true, options, null);

    public static BrokerCommandLineParseResult Fail(string error) => new(false, null, error);
}
