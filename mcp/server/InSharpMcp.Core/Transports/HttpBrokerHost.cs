using InSharpMcp.Tools;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace InSharpMcp.Transports;

public static class HttpBrokerHost
{
    public static async Task RunAsync(
        Action<BrokerMcpHostOptions>? configure = null,
        CancellationToken cancellationToken = default)
    {
        var options = Configure(configure);
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls(CreateBinding(options));
        builder.Services.AddInSharpMcpCore(options);
        builder.Services.AddHttpContextAccessor();
        builder.Services
            .AddMcpServer()
            .WithHttpTransport()
            .WithTools<InSharpMcpTools>();

        var app = builder.Build();
        app.Use(RejectRemoteHosts);
        app.MapMcp(options.HttpPath);

        using var stopRegistration = cancellationToken.Register(
            static state => _ = ((WebApplication)state!).StopAsync(),
            app);
        await app.RunAsync().ConfigureAwait(false);
    }

    private static BrokerMcpHostOptions Configure(Action<BrokerMcpHostOptions>? configure)
    {
        var options = new BrokerMcpHostOptions();
        configure?.Invoke(options);
        return options;
    }

    private static string CreateBinding(BrokerMcpHostOptions options)
    {
        return $"http://127.0.0.1:{options.HttpPort.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
    }

    private static async Task RejectRemoteHosts(HttpContext context, RequestDelegate next)
    {
        if (!IPAddressIsLoopback(context.Connection.RemoteIpAddress))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Remote HTTP clients are not allowed.").ConfigureAwait(false);
            return;
        }

        await next(context).ConfigureAwait(false);
    }

    private static bool IPAddressIsLoopback(System.Net.IPAddress? address) =>
        address is null || System.Net.IPAddress.IsLoopback(address);
}
