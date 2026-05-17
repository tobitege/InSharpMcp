using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text.Json;
using InSharpMcp.Contracts;
using InSharpMcp.Contracts.LocalTransport;

namespace InSharpMcp.Bridge;

public sealed class InSharpMcpBridge : IAsyncDisposable, IDisposable
{
    private readonly IUiTreeInspector _treeInspector;
    private readonly IScreenshotProvider _screenshotProvider;
    private readonly IAccessibilityTreeProvider _accessibilityTreeProvider;
    private readonly IPointerInputSimulator _inputSimulator;
    private readonly IElementClickSimulator _elementClickSimulator;
    private readonly IAutomationPeerInvoker _automationPeerInvoker;
    private readonly IElementPropertyEditor _propertyEditor;
    private readonly IAppProvider _appProvider;
    private readonly LocalBridgeOptions _options;
    private readonly SemaphoreSlim _operationGate = new(1, 1);
    private readonly CancellationTokenSource _stop = new();
    private Task? _serverTask;
    private Task? _heartbeatTask;
    private string? _instanceId;

    public InSharpMcpBridge(
        IUiTreeInspector treeInspector,
        IScreenshotProvider screenshotProvider,
        IAccessibilityTreeProvider accessibilityTreeProvider,
        IPointerInputSimulator inputSimulator,
        IAutomationPeerInvoker automationPeerInvoker,
        IAppProvider appProvider,
        LocalBridgeOptions? options = null,
        IElementPropertyEditor? propertyEditor = null,
        IElementClickSimulator? elementClickSimulator = null)
    {
        _treeInspector = treeInspector;
        _screenshotProvider = screenshotProvider;
        _accessibilityTreeProvider = accessibilityTreeProvider;
        _inputSimulator = inputSimulator;
        _elementClickSimulator = elementClickSimulator ?? UnsupportedElementClickSimulator.Instance;
        _automationPeerInvoker = automationPeerInvoker;
        _propertyEditor = propertyEditor ?? UnsupportedElementPropertyEditor.Instance;
        _appProvider = appProvider;
        _options = options ?? new LocalBridgeOptions();
    }

    public async Task StartAsync(AppBridgeRegistration registration, CancellationToken cancellationToken = default)
    {
        if (_serverTask is not null)
        {
            throw new InvalidOperationException("The InSharpMcp bridge has already started.");
        }

        _instanceId = registration.InstanceId ?? $"{registration.AppId}-{Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        var appPipeName = $"{_options.AppPipeNamePrefix}.{Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture)}.{Guid.NewGuid():N}";
        _serverTask = Task.Run(() => RunAppPipeServerAsync(appPipeName, _stop.Token), CancellationToken.None);

        var message = new LocalAppRegistrationMessage(
            _instanceId,
            registration.AppId,
            registration.AppName,
            registration.ProcessId ?? Environment.ProcessId,
            registration.AdapterKind,
            registration.PlatformTarget,
            registration.OperatingSystem ?? RuntimeInformation.OSDescription,
            registration.AppVersion,
            registration.Capabilities.ToArray(),
            appPipeName);

        try
        {
            await RegisterWithBrokerAsync(message, cancellationToken).ConfigureAwait(false);
            _heartbeatTask = Task.Run(() => RunHeartbeatAsync(_instanceId, _stop.Token), CancellationToken.None);
        }
        catch
        {
            await StopAsync().ConfigureAwait(false);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        _operationGate.Dispose();
        _stop.Dispose();
    }

    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
        _operationGate.Dispose();
        _stop.Dispose();
    }

    private async Task StopAsync()
    {
        if (_instanceId is { } instanceId)
        {
            try
            {
                await LocalBridgePipe.SendAsync<LocalBrokerRequest, LocalBrokerResponse>(
                    _options.BrokerPipeName,
                    new LocalBrokerRequest(LocalBrokerRequestKind.Unregister, InstanceId: instanceId),
                    _options.RequestTimeout,
                    CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
            }
        }

        _stop.Cancel();
        if (_serverTask is not null)
        {
            try
            {
                await _serverTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        if (_heartbeatTask is not null)
        {
            try
            {
                await _heartbeatTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    private async Task RegisterWithBrokerAsync(
        LocalAppRegistrationMessage message,
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        Exception? lastError = null;

        while (Stopwatch.GetElapsedTime(started) < _options.RequestTimeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var response = await LocalBridgePipe.SendAsync<LocalBrokerRequest, LocalBrokerResponse>(
                    _options.BrokerPipeName,
                    new LocalBrokerRequest(LocalBrokerRequestKind.Register, Registration: message),
                    TimeSpan.FromSeconds(2),
                    cancellationToken).ConfigureAwait(false);

                if (!response.Success)
                {
                    throw new InvalidOperationException(response.Error ?? "The broker rejected the app bridge registration.");
                }

                return;
            }
            catch (Exception exception) when (exception is IOException or TimeoutException or InvalidOperationException)
            {
                lastError = exception;
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }
        }

        throw new InvalidOperationException("The bridge could not connect to the local InSharpMcp broker.", lastError);
    }

    private async Task RunAppPipeServerAsync(string appPipeName, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var pipe = new NamedPipeServerStream(
                appPipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

            try
            {
                await pipe.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
                _ = Task.Run(() => HandleAppRequestAsync(pipe, cancellationToken), CancellationToken.None);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                await pipe.DisposeAsync().ConfigureAwait(false);
                break;
            }
            catch
            {
                await pipe.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private async Task RunHeartbeatAsync(string instanceId, CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(_options.HeartbeatInterval);
        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            try
            {
                await LocalBridgePipe.SendAsync<LocalBrokerRequest, LocalBrokerResponse>(
                    _options.BrokerPipeName,
                    new LocalBrokerRequest(LocalBrokerRequestKind.Heartbeat, InstanceId: instanceId),
                    _options.RequestTimeout,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is IOException or TimeoutException or InvalidOperationException)
            {
                // Heartbeat is best-effort; registration retries already proved initial connectivity.
            }
        }
    }

    private async Task HandleAppRequestAsync(NamedPipeServerStream pipe, CancellationToken cancellationToken)
    {
        await using (pipe.ConfigureAwait(false))
        {
            LocalAppRequest? request;
            try
            {
                request = await LocalBridgePipe.ReadLineAsync<LocalAppRequest>(pipe, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                await LocalBridgePipe.WriteLineAsync(
                        pipe,
                        LocalAppToolResponse.FromToolResult(ToolResult.Fail("Invalid local app request.", "bad_request")),
                        cancellationToken)
                    .ConfigureAwait(false);
                return;
            }

            var result = await ExecuteSafelyAsync(request, cancellationToken).ConfigureAwait(false);
            await LocalBridgePipe.WriteLineAsync(pipe, LocalAppToolResponse.FromToolResult(result), cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task<ToolResult> ExecuteSafelyAsync(LocalAppRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return ToolResult.Fail("Empty local app request.", "bad_request");
        }

        try
        {
            await _operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _operationGate.Release();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return ToolResult.Fail("The bridge operation was canceled.", "canceled");
        }
        catch (Exception exception)
        {
            return ToolResult.Fail(
                "The bridge operation failed.",
                "bridge_operation_failed",
                new { ExceptionType = exception.GetType().Name });
        }
    }

    private Task<ToolResult> ExecuteAsync(LocalAppRequest request, CancellationToken cancellationToken)
    {
        var limits = request.Limits ?? new ToolLimits();
        return request.Operation switch
        {
            LocalAppOperation.VisualTreeSnapshot => _treeInspector.GetVisualTreeSnapshotAsync(limits, cancellationToken),
            LocalAppOperation.GetElementMetadata => _treeInspector.GetElementMetadataAsync(Required(request.ElementIdentifier), limits, cancellationToken),
            LocalAppOperation.GetElementDataContext => _treeInspector.GetElementDataContextAsync(Required(request.ElementIdentifier), limits, cancellationToken),
            LocalAppOperation.GetScreenshot => CaptureScreenshotAsync(cancellationToken),
            LocalAppOperation.GetAccessibilityTree => _accessibilityTreeProvider.GetAccessibilityTreeAsync(limits, cancellationToken),
            LocalAppOperation.PointerClick => _inputSimulator.PointerClickAsync(request.X ?? 0, request.Y ?? 0, cancellationToken),
            LocalAppOperation.ElementClick => _elementClickSimulator.ElementClickAsync(Required(request.ElementIdentifier), cancellationToken),
            LocalAppOperation.KeyPress => _inputSimulator.KeyPressAsync(Required(request.Key), request.Modifiers ?? Array.Empty<string>(), cancellationToken),
            LocalAppOperation.TypeText => _inputSimulator.TypeTextAsync(Required(request.Text), cancellationToken),
            LocalAppOperation.ElementPeerDefaultAction => _automationPeerInvoker.InvokeDefaultActionAsync(Required(request.ElementIdentifier), cancellationToken),
            LocalAppOperation.SetElementProperty => _propertyEditor.SetElementPropertyAsync(
                Required(request.ElementIdentifier),
                request.TargetObject ?? ElementPropertyTarget.Element,
                Required(request.PropertyName),
                Required(request.PropertyValue),
                cancellationToken),
            LocalAppOperation.Close => _appProvider.CloseAsync(cancellationToken),
            _ => Task.FromResult(ToolResult.Fail($"Unknown bridge operation '{request.Operation}'.", "bad_request")),
        };
    }

    private async Task<ToolResult> CaptureScreenshotAsync(CancellationToken cancellationToken)
    {
        var result = await _screenshotProvider.CaptureScreenshotAsync(cancellationToken).ConfigureAwait(false);
        return result.Success
            ? ToolResult.Ok(result.Message ?? "Screenshot captured.", result)
            : ToolResult.Fail(result.Message ?? "Screenshot capture failed.", result.ErrorCode ?? "screenshot_failed", result);
    }

    private static string Required(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException("The bridge request was missing a required value.")
            : value;

    private static JsonElement Required(JsonElement? value) =>
        value is { ValueKind: not JsonValueKind.Undefined }
            ? value.Value
            : throw new InvalidOperationException("The bridge request was missing a required value.");

    private sealed class UnsupportedElementPropertyEditor : IElementPropertyEditor
    {
        public static readonly UnsupportedElementPropertyEditor Instance = new();

        public Task<ToolResult> SetElementPropertyAsync(
            string elementIdentifier,
            string targetObject,
            string propertyName,
            JsonElement value,
            CancellationToken cancellationToken)
        {
            _ = elementIdentifier;
            _ = targetObject;
            _ = propertyName;
            _ = value;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(ToolResult.Fail("Element property editing is unsupported by this adapter.", "unsupported"));
        }
    }

    private sealed class UnsupportedElementClickSimulator : IElementClickSimulator
    {
        public static readonly UnsupportedElementClickSimulator Instance = new();

        public Task<ToolResult> ElementClickAsync(string elementIdentifier, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = elementIdentifier;
            return Task.FromResult(ToolResult.Fail("Element click is unsupported by this adapter.", "unsupported"));
        }
    }
}
