using System.IO.Pipes;
using InSharpMcp.Contracts.LocalTransport;
using InSharpMcp.Registry;
using Microsoft.Extensions.Hosting;

namespace InSharpMcp.Transports;

internal sealed class LocalBrokerPipeServer : BackgroundService
{
    private readonly AppRegistrationService _registrationService;
    private readonly LocalAppTransportOptions _options;

    public LocalBrokerPipeServer(
        AppRegistrationService registrationService,
        LocalAppTransportOptions options)
    {
        _registrationService = registrationService;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var expirationTask = Task.Run(() => ExpireStaleRegistrationsAsync(stoppingToken), CancellationToken.None);

        while (!stoppingToken.IsCancellationRequested)
        {
            var pipe = new NamedPipeServerStream(
                _options.BrokerPipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

            try
            {
                await pipe.WaitForConnectionAsync(stoppingToken).ConfigureAwait(false);
                _ = Task.Run(() => HandleConnectionSafelyAsync(pipe, stoppingToken), CancellationToken.None);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                await pipe.DisposeAsync().ConfigureAwait(false);
                break;
            }
            catch
            {
                await pipe.DisposeAsync().ConfigureAwait(false);
            }
        }

        try
        {
            await expirationTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task ExpireStaleRegistrationsAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(_options.HeartbeatInterval);
        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            _registrationService.ExpireStale(
                DateTimeOffset.UtcNow,
                new AppRegistrationOptions { StaleInstanceAge = _options.StaleInstanceAge });
        }
    }

    private async Task HandleConnectionSafelyAsync(NamedPipeServerStream pipe, CancellationToken cancellationToken)
    {
        try
        {
            await HandleConnectionAsync(pipe, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await pipe.DisposeAsync().ConfigureAwait(false);
        }
        catch
        {
            await pipe.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task HandleConnectionAsync(NamedPipeServerStream pipe, CancellationToken cancellationToken)
    {
        await using (pipe.ConfigureAwait(false))
        {
            LocalBrokerRequest? request;
            try
            {
                request = await LocalAppPipe.ReadLineAsync<LocalBrokerRequest>(pipe, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                await WriteResponseAsync(pipe, LocalBrokerResponseFailed("Invalid local broker request."), cancellationToken)
                    .ConfigureAwait(false);
                return;
            }

            if (request is null)
            {
                await WriteResponseAsync(pipe, LocalBrokerResponseFailed("Empty local broker request."), cancellationToken)
                    .ConfigureAwait(false);
                return;
            }

            LocalBrokerResponse response;
            try
            {
                response = HandleRequest(request);
            }
            catch (Exception exception)
            {
                response = LocalBrokerResponseFailed($"Local broker request failed: {exception.GetType().Name}.");
            }

            await WriteResponseAsync(pipe, response, cancellationToken).ConfigureAwait(false);
        }
    }

    private LocalBrokerResponse HandleRequest(LocalBrokerRequest request)
    {
        var now = DateTimeOffset.UtcNow;
        switch (request.Kind)
        {
            case LocalBrokerRequestKind.Register:
                if (request.Registration is null)
                {
                    return LocalBrokerResponseFailed("Registration request did not include an app descriptor.");
                }

                var descriptor = LocalAppTransportWire.ToDescriptor(request.Registration, now);
                var client = new RemoteAppInstanceClient(request.Registration.AppPipeName, _options);
                _registrationService.Register(descriptor, client);
                return new LocalBrokerResponse(Success: true);

            case LocalBrokerRequestKind.Heartbeat:
                if (string.IsNullOrWhiteSpace(request.InstanceId))
                {
                    return LocalBrokerResponseFailed("Heartbeat request did not include an instance id.");
                }

                return _registrationService.TryHeartbeat(request.InstanceId, now)
                    ? new LocalBrokerResponse(Success: true)
                    : LocalBrokerResponseFailed("Heartbeat referenced an unknown app instance.");

            case LocalBrokerRequestKind.Unregister:
                if (string.IsNullOrWhiteSpace(request.InstanceId))
                {
                    return LocalBrokerResponseFailed("Unregister request did not include an instance id.");
                }

                _registrationService.Unregister(request.InstanceId);
                return new LocalBrokerResponse(Success: true);

            default:
                return LocalBrokerResponseFailed($"Unknown local broker request kind '{request.Kind}'.");
        }
    }

    private static LocalBrokerResponse LocalBrokerResponseFailed(string error) =>
        new(Success: false, error);

    private static Task WriteResponseAsync(
        NamedPipeServerStream pipe,
        LocalBrokerResponse response,
        CancellationToken cancellationToken) =>
        LocalAppPipe.WriteLineAsync(pipe, response, cancellationToken);
}
