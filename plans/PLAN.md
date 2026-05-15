# InSharpMcp Integration Plan

## Overview

Add a framework-independent C# MCP server, tentatively named `InSharpMcp`, that can be embedded into desktop or
app-hosted .NET applications through framework-specific adapters. The project and
package names must remain application-agnostic. The core server must not depend on Uno Platform, WinUI,
Avalonia, WinForms, WPF, or any other UI framework.

The server exposes tools with the `ism_` prefix for runtime information,
bounded UI inspection, screenshot capture, and carefully gated UI interaction.
Each UI framework supplies an adapter package that implements the shared
contracts. Uno/WinUI, Avalonia, and WinForms should fit the same contract shape
without changes to the MCP host.

---

## Design Rules

1. MCP is disabled by default. It starts only when `ISM_ENABLED=1` or an
   equivalent explicit app setting is present.
2. A broker process is the primary MCP entry point. MCP clients launch or connect
   to the broker, and running apps register themselves with it.
3. The broker must support multiple app instances at the same time, including
   multiple instances of one app and multiple different apps using the same
   adapter package.
4. Tool methods use dependency injection. Avoid static service locators for app
   services and adapters.
5. Every tool that touches UI state must run through the target app adapter's UI dispatcher.
6. Every inspection tool must have hard bounds for depth, node count, output size,
   and timeout.
7. Potentially destructive or privacy-sensitive tools require authorization when
   HTTP is enabled: close, click, key press, type text, screenshot, and DataContext
   inspection.
8. The server must support multiple concurrent MCP calls. Non-UI work can run in
   parallel; UI work must be coordinated through the adapter dispatcher without
   blocking unrelated requests.

---

## NuGet Dependencies

| Package | Purpose |
|---------|---------|
| `ModelContextProtocol` | Core SDK for tool definitions, stdio/in-memory testing, and hosting integration |
| `ModelContextProtocol.AspNetCore` | HTTP transport for an in-app endpoint |
| `Microsoft.Extensions.Hosting` | Host builder support for standalone stdio test host or future out-of-process server |

The C# SDK package choice follows the official SDK split:

- `ModelContextProtocol` for stdio or in-memory servers.
- `ModelContextProtocol.AspNetCore` for HTTP-based servers hosted through ASP.NET Core.

If the project uses central package management, package
versions belong in the central package file and project files should add
versionless `PackageReference` entries. If it does not, versions belong in the
individual project files.

---

## Project Structure

```
mcp/server/
  InSharpMcp.Contracts/
    InSharpMcp.Contracts.csproj
    IAppProvider.cs
    IUiDispatcher.cs
    IUiTreeInspector.cs
    IPointerInputSimulator.cs
    IScreenshotProvider.cs
    IAutomationPeerInvoker.cs
    ToolResult.cs
    ToolLimits.cs
    ToolLimitPolicy.cs

  InSharpMcp/
    InSharpMcp.csproj
    Tools/
      InSharpMcpTools.cs
    Transports/
      BrokerMcpHost.cs
      StdioBrokerHost.cs
      HttpBrokerHost.cs
      AppConnectionTransport.cs
    Registry/
      AppInstanceDescriptor.cs
      AppInstanceRegistry.cs
      AppInstanceSelector.cs
    Concurrency/
      InSharpMcpConcurrencyOptions.cs
      IUiOperationQueue.cs
      UiOperationQueue.cs
    Security/
      McpAccessOptions.cs
      McpAuthorization.cs

  InSharpMcp.Adapters.Uno/
    InSharpMcp.Adapters.Uno.csproj
    UnoAppProvider.cs
    UnoUiDispatcher.cs
    UnoVisualTreeInspector.cs
    UnoScreenshotProvider.cs
    UnoPointerInputSimulator.Windows.cs
    UnoPointerInputSimulator.Skia.cs
    UnoAutomationPeerInvoker.cs

  InSharpMcp.Adapters.Avalonia/
    InSharpMcp.Adapters.Avalonia.csproj
    AvaloniaAppProvider.cs
    AvaloniaUiDispatcher.cs
    AvaloniaVisualTreeInspector.cs
    AvaloniaScreenshotProvider.cs
    AvaloniaPointerInputSimulator.cs
    AvaloniaAutomationPeerInvoker.cs

  InSharpMcp.Adapters.WinForms/
    InSharpMcp.Adapters.WinForms.csproj
    WinFormsAppProvider.cs
    WinFormsUiDispatcher.cs
    WinFormsTreeInspector.cs
    WinFormsScreenshotProvider.cs
    WinFormsPointerInputSimulator.cs
    WinFormsAutomationInvoker.cs

  tests/
    InSharpMcp.Tests/
    InSharpMcp.AdapterContractTests/
    InSharpMcp.Adapters.Uno.Tests/

  demos/
    demo.uno/
    demo.avalonia/
    demo.winforms/

  PLAN.md
```

### Target Frameworks

| Project | Target Frameworks | Notes |
|---------|-------------------|-------|
| `InSharpMcp.Contracts` | `net8.0` | No UI framework references; shared tool contracts and result models |
| `InSharpMcp` | `net8.0` | MCP broker, app registry, tool routing, and transport host code |
| `InSharpMcp.Adapters.Uno` | `net9.0-windows10.0.19041;net9.0-desktop` | Uno/WinUI adapter implementation; target frameworks may vary by supported Uno version |
| `InSharpMcp.Adapters.Avalonia` | adapter-specific .NET TFMs | Future Avalonia adapter implementation |
| `InSharpMcp.Adapters.WinForms` | Windows desktop TFM | Future WinForms adapter implementation |
| `InSharpMcp.Tests` | supported test TFM | In-memory MCP/tool contract tests |
| `InSharpMcp.AdapterContractTests` | supported test TFM | Shared adapter behavior test harness |

Do not place UI framework adapter code in the `net8.0` server project.

---

## Key Abstractions

### ToolResult

Tools should return structured, bounded results instead of ad hoc success/error
strings.

```csharp
namespace InSharpMcp.Contracts;

public sealed record ToolResult(
    bool Success,
    string Message,
    object? Data = null,
    string? ErrorCode = null);
```

### ToolLimits

```csharp
public sealed record ToolLimits
{
    public int MaxDepth { get; init; } = 20;
    public int MaxNodes { get; init; } = 500;
    public int MaxTextCharacters { get; init; } = 64_000;
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(5);
    public TimeSpan QueueTimeout { get; init; } = TimeSpan.FromSeconds(2);
}
```

`MaxDepth`, `MaxNodes`, and `MaxTextCharacters` are defaults, not hidden
constants. They should be overridable from the MCP client configuration when the
client supports passing optional settings through `mcp.json`. Depending on the
transport and client, those settings may arrive as launch arguments, environment
variables, HTTP headers, or query parameters. The server must accept only the
three documented limit keys and clamp the requested values before using them.

Example `mcp.json` shape:

```json
{
  "mcpServers": {
    "insharp-mcp": {
      "url": "http://127.0.0.1:52001/mcp",
      "headers": {
        "X-InSharpMcp-Max-Depth": "20",
        "X-InSharpMcp-Max-Nodes": "500",
        "X-InSharpMcp-Max-Text-Characters": "64000",
        "X-InSharpMcp-Max-Concurrent-Calls": "1"
      }
    }
  }
}
```

For a client that launches a local command instead of connecting to HTTP, the
same values can be supplied as arguments or environment variables. Keep the
canonical setting names documented as:

- `ISM_MAX_DEPTH`
- `ISM_MAX_NODES`
- `ISM_MAX_TEXT_CHARACTERS`
- `ISM_MAX_CONCURRENT_CALLS`

### ToolLimitPolicy

```csharp
public sealed record ToolLimitPolicy
{
    public ToolLimits Defaults { get; init; } = new();
    public int MinDepth { get; init; } = 1;
    public int MaxDepth { get; init; } = 50;
    public int MinNodes { get; init; } = 1;
    public int MaxNodes { get; init; } = 2_000;
    public int MinTextCharacters { get; init; } = 1_024;
    public int MaxTextCharacters { get; init; } = 256_000;
}
```

Limit policy requirements:

- parse client-provided values as integers only
- accept only `MaxDepth`, `MaxNodes`, and `MaxTextCharacters` from client config
- reject non-numeric, negative, zero, `NaN`, infinity, and overflow values
- clamp valid values to the server's configured min/max range
- log when requested values are clamped
- ignore or reject unknown client-provided limit/security keys according to the
  configured startup policy
- invalid limit values fall back to defaults and log a warning
- treat absent values as defaults
- never allow client config to change timeout, queue timeout, auth, host binding,
  CORS, or transport security
- keep effective limits per call or per client session, not in static mutable state

### IUiDispatcher

```csharp
public interface IUiDispatcher
{
    Task<T> RunAsync<T>(Func<CancellationToken, T> action, CancellationToken cancellationToken);
    Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken);
}
```

### IUiOperationQueue

Adapters decide whether UI operations can overlap. Most UI frameworks require UI
tree reads and writes to execute on a single UI thread, so the shared host
provides a queue that preserves concurrent request handling while serializing the
actual UI-thread critical section.

```csharp
public interface IUiOperationQueue
{
    Task<ToolResult> RunAsync(
        string operationName,
        Func<CancellationToken, Task<ToolResult>> operation,
        ToolLimits limits,
        CancellationToken cancellationToken);
}
```

Queue requirements:

- no global lock around the entire MCP request
- bounded queue wait using `ToolLimits.QueueTimeout`
- per-call cancellation token honored before and during dispatch
- clear `busy` or `timeout` error when the queue limit is exceeded
- independent non-UI tools do not wait behind UI work

### IAppProvider

```csharp
public interface IAppProvider
{
    int ProcessId { get; }
    string OperatingSystem { get; }
    string PlatformTarget { get; }
    string AppName { get; }
    string AppVersion { get; }
    Task<ToolResult> CloseAsync(CancellationToken cancellationToken);
}
```

### IUiTreeInspector

```csharp
public interface IUiTreeInspector
{
    Task<ToolResult> GetVisualTreeSnapshotAsync(ToolLimits limits, CancellationToken cancellationToken);
    Task<ToolResult> GetElementMetadataAsync(string elementIdentifier, ToolLimits limits, CancellationToken cancellationToken);
    Task<ToolResult> GetElementDataContextAsync(string elementIdentifier, ToolLimits limits, CancellationToken cancellationToken);
}
```

`GetElementDataContextAsync` must return bounded metadata by default:

- DataContext type name
- public primitive/string properties only
- max property count
- max string length
- no recursive object walking unless an explicit allowlist is added later

### IPointerInputSimulator

```csharp
public interface IPointerInputSimulator
{
    Task<ToolResult> PointerClickAsync(double x, double y, CancellationToken cancellationToken);
    Task<ToolResult> KeyPressAsync(string key, IReadOnlyList<string> modifiers, CancellationToken cancellationToken);
    Task<ToolResult> TypeTextAsync(string text, CancellationToken cancellationToken);
}
```

### IScreenshotProvider

```csharp
public interface IScreenshotProvider
{
    Task<ScreenshotResult> CaptureScreenshotAsync(CancellationToken cancellationToken);
}

public sealed record ScreenshotResult(
    bool Success,
    byte[]? PngBytes,
    string? Message,
    string? ErrorCode = null);
```

The MCP tool should return MCP image content, not a base64 string, when the SDK
shape allows it.

### IAutomationPeerInvoker

```csharp
public interface IAutomationPeerInvoker
{
    Task<ToolResult> InvokeDefaultActionAsync(string elementIdentifier, CancellationToken cancellationToken);
}
```

Automation peer invocation must use public automation APIs. Do not call protected
members such as `FrameworkElement.OnCreateAutomationPeer()` from outside the
control.

---

## MCP Tool Definitions

Tools live in an `[McpServerToolType]` class and receive services through method
parameters registered in DI. The SDK supports service injection into tool
methods, so the tool class should not resolve adapters from static global state.

| Tool Name | Method Shape | Description |
|-----------|--------------|-------------|
| `ism_list_instances` | `Task<ToolResult> ListInstances(AppInstanceRegistry registry)` | Registered app instances and capabilities |
| `ism_get_runtime_info` | `Task<ToolResult> GetRuntimeInfo(AppTargetSelector? target)` | PID, OS, platform target, app name/version for a selected instance |
| `ism_get_screenshot` | image content or `ToolResult` error | PNG screenshot for a selected instance |
| `ism_query_elements` | `Task<ToolResult> QueryElements(AppTargetSelector? target, ElementSelector selector)` | Framework-neutral selector query |
| `ism_wait_for_element` | `Task<ToolResult> WaitForElement(AppTargetSelector? target, ElementSelector selector, int? timeoutMs)` | Wait/retry until an element matches |
| `ism_get_accessibility_tree` | `Task<ToolResult> GetAccessibilityTree(AppTargetSelector? target)` | Bounded accessibility/automation tree |
| `ism_get_event_log` | `Task<ToolResult> GetEventLog(AppTargetSelector? target, string[]? categories)` | Recent adapter/app events |
| `ism_start_trace` | `Task<ToolResult> StartTrace(AppTargetSelector? target)` | Start bounded action/diagnostic trace |
| `ism_stop_trace` | `Task<ToolResult> StopTrace(AppTargetSelector? target, string traceId)` | Stop trace and return summary/artifact references |
| `ism_pointer_click` | `Task<ToolResult> PointerClick(AppTargetSelector? target, double x, double y)` | Authorized click at client/window coordinates |
| `ism_key_press` | `Task<ToolResult> KeyPress(AppTargetSelector? target, string key, string[]? modifiers)` | Authorized key press |
| `ism_type_text` | `Task<ToolResult> TypeText(AppTargetSelector? target, string text)` | Authorized text input with length limit |
| `ism_visualtree_snapshot` | `Task<ToolResult> VisualTreeSnapshot(AppTargetSelector? target, int? maxDepth, int? maxNodes)` | Bounded visual-tree dump |
| `ism_get_element_metadata` | `Task<ToolResult> GetElementMetadata(AppTargetSelector? target, string elementIdentifier)` | Safe element metadata |
| `ism_get_element_datacontext` | `Task<ToolResult> GetElementDataContext(AppTargetSelector? target, string elementIdentifier)` | Bounded, non-recursive DataContext metadata |
| `ism_element_peer_default_action` | `Task<ToolResult> ElementPeerDefaultAction(AppTargetSelector? target, string elementIdentifier)` | Authorized invoke through public peer pattern APIs |
| `ism_close` | `Task<ToolResult> Close(AppTargetSelector? target)` | Authorized graceful close |

Validation requirements:

- reject negative coordinates
- reject unsupported keys/modifiers
- cap typed text length
- cap visual tree depth/node count/output size
- clamp client-configured `MaxDepth`, `MaxNodes`, and `MaxTextCharacters` before use
- return explicit unsupported results where a platform adapter cannot implement a tool
- use cancellation tokens and dispatcher timeouts for UI operations
- never store per-call state in static mutable fields
- allow concurrent calls with isolated limits, authorization context, and cancellation
- resolve target app instances through the broker registry before dispatch
- reject ambiguous target selection instead of guessing

---

## Automation Platform Features

The broker should grow beyond raw tool calls toward a small, framework-neutral app
automation platform. These features close the main gap with browser automation
systems while staying independent of any one UI framework.

### Selectors

Define selectors as structured JSON objects instead of a custom string grammar.
This avoids inventing escaping rules and keeps special characters handled by JSON.

Example:

```json
{
  "role": "button",
  "name": "Save"
}
```

Minimum selector fields:

- `name`
- `automationId`
- `type`
- `text`
- `role`
- `index`
- `path`

Selector requirements:

- adapter-independent JSON shape
- bounded result count
- deterministic ordering
- clear errors for invalid selectors
- no arbitrary code execution in selectors
- no custom escaping rules beyond normal JSON escaping
- adapter-specific escape hatch only through an explicit `adapter` object

### Waits and Retries

Add explicit wait tools for app automation:

- wait for element exists
- wait for element visible/enabled
- wait for element text/value
- wait for app idle, where the adapter can define a safe idle signal

Waits must use bounded polling, cancellation tokens, and maximum timeout clamps.
Wait behavior should be controlled by static global defaults. The defaults should
avoid overly conservative short waits while still preventing very long waits.

### Accessibility Tree

Expose a bounded accessibility/automation tree where the framework supports it.
This gives clients more stable roles and labels than raw visual trees.

### Event and Log Capture

Adapters should publish recent structured events:

- app instance registered/unregistered
- focus changes
- navigation/view changes, when available
- errors and unhandled exceptions, if the host opts in
- tool calls and adapter failures

Event logs must be bounded and redact sensitive values.

### Tracing

Tracing records a bounded timeline around tool calls:

- selected target instance
- selector resolution attempts
- waits and retries
- screenshots before/after selected actions, when enabled
- errors and timing information

Traces must have size/time limits and must not capture screenshots or private
data unless the protected-tool policy allows it.

Trace and event storage:

- Store trace and event artifacts under the app settings folder.
- Use instance-id driven subfolders so multiple instances of the same app do not
  overwrite each other's artifacts.
- Default layout:

```text
<user-settings>/InSharpMcp/
  tokens/
  instances/<appId>/<instanceId>/
  traces/<appId>/<instanceId>/<traceId>/
  events/<appId>/<instanceId>/
```

Screenshot trace policy:

- session-level opt-in defines the default
- each protected tool call can override the session default
- screenshots are captured only when auth policy and screenshot policy both allow it
- traces must record whether screenshots were disabled by policy

### Assertions

The MCP server should expose simple assertion-style tools or result helpers later,
but core behavior should remain useful without adopting a full test framework.
Initial assertions can cover element existence, visibility, enabled state, text,
value, and screenshot availability.

---

## Transport, Broker, and Lifecycle

The MCP client should talk to a broker process, not directly to a single app
instance. The broker is the stable MCP endpoint. Apps that embed an adapter
register with the broker when they start and unregister when they exit.

```text
MCP client -> InSharpMcp broker -> registered app instance -> framework adapter
```

This is required because there may be:

- multiple instances of the same app
- multiple different apps using the same adapter package
- multiple adapters loaded across different running apps
- app restarts while the MCP client stays open

### Broker Host

The broker can expose stdio for MCP clients that launch a server command, and it
can optionally expose HTTP for clients that support remote/local HTTP MCP
connections. Both transports route into the same app-instance registry and tool
dispatcher.

Broker requirements:

1. The broker starts only when explicitly launched or enabled.
2. The broker maintains an `AppInstanceRegistry` keyed by stable instance IDs.
3. Each app registration includes app name, process ID, adapter kind, capabilities,
   connection endpoint, and display metadata.
4. Tool calls must select a target app instance explicitly or use a deterministic
   default only when exactly one compatible instance is registered.
5. If multiple compatible instances exist and no target is supplied, return a
   structured ambiguity error with the registered instance list.
6. If the selected app disconnects, return a structured stale-instance error.
7. The broker enforces auth, concurrency, and limit policy before forwarding a
   request to an app instance.
8. The broker must not hold UI framework references.

### App Registration

Each host app embeds a framework adapter and a small app-side connection endpoint.
On startup, the app registers with the broker.

Registration data:

```csharp
public sealed record AppInstanceDescriptor(
    string InstanceId,
    string AppId,
    string AppName,
    int ProcessId,
    string AdapterKind,
    IReadOnlySet<string> Capabilities,
    Uri Endpoint,
    DateTimeOffset RegisteredAt);
```

`AppId` identifies the product or executable. `InstanceId` identifies one running
instance. Two running instances of the same app have the same `AppId` but
different `InstanceId` values.

Identity defaults:

- `AppId`: stable UUID stored in the app settings folder.
- `InstanceId`: composed from `AppId` and process ID.
- Multiple instances of the same app share the same `AppId`, app settings folder,
  and token policy, but receive different `InstanceId` values.

### App-Side Endpoint

The app-side endpoint is not an MCP endpoint. It uses named pipes as the default
private broker-to-app IPC so the broker can call the adapter inside the app
process.

Requirements:

- use named pipes by default
- bind locally only if a fallback loopback transport is added later
- require broker authentication
- expose only the internal adapter protocol, not public MCP
- unregister on shutdown when possible
- expire stale registrations through heartbeat or connection loss
- support concurrent broker calls subject to the app adapter's UI queue rules

### Target Selection

MCP tools should accept an optional target selector for calls that require a
specific app instance.

Selector fields:

- `instanceId`
- `appId`
- `adapterKind`

Selection rules:

- `instanceId` wins when provided
- `appId` filters registered instances
- `adapterKind` filters by adapter package
- zero matches returns `not_found`
- more than one match returns `ambiguous_target`
- one match routes the call

### Broker Transport Security

For stdio, the MCP client has already launched the broker process, but protected
tools still require the configured policy because the broker can control running
apps. For HTTP, bind to loopback by default and require a token unless explicitly
configured otherwise for local development.

Token policy:

- Store the app token in the user's app settings folder.
- Multiple instances of the same app share the same token and app settings.
- The broker may issue or validate tokens, but token persistence remains
  app-settings based.

---

## Concurrency Model

The server must handle multiple simultaneous MCP calls from one or more clients.
Concurrency is part of the contract, not an implementation detail.

### Request Handling

- The MCP host must not use static mutable per-call state.
- Tool services should be registered with lifetimes that are safe for concurrent
  calls.
- Per-request data such as authorization context, limits, and cancellation tokens
  must stay local to the call.
- Long-running calls must not block unrelated runtime-info or metadata calls.
- Shared adapter state must be protected with explicit synchronization.
- Per-instance concurrency limits apply independently so one busy app instance
  does not block unrelated instances.

### UI Operations

UI frameworks usually require all UI access on one UI thread. The server should
therefore accept concurrent MCP calls, then serialize only the actual UI
operation through `IUiOperationQueue` and `IUiDispatcher`.

Examples:

- two runtime-info calls can run concurrently
- runtime-info can run while a visual-tree snapshot is queued
- two visual-tree snapshots may queue behind each other if the adapter requires a
  single UI-thread critical section
- visual-tree snapshots for two different app instances can run independently
  unless they share an adapter-side resource that requires serialization
- input operations should be serialized with other UI-mutating operations
- screenshot capture should be serialized if the adapter cannot prove concurrent
  capture is safe

### Limits

Recommended defaults:

| Limit | Default | Purpose |
|-------|---------|---------|
| max concurrent MCP calls | 1 default, 5 maximum | Avoid unbounded request fan-out while allowing opt-in parallelism |
| max queued UI operations | 8 | Avoid request pileups on the UI thread |
| UI queue wait | 2 seconds | Fail fast when the app is busy |
| per-tool timeout | 5 seconds | Prevent stuck tools |

These values should be configurable through `InSharpMcpConcurrencyOptions`.
The max concurrent call value is configurable as an MCP client parameter and must
be clamped to the server maximum of 5.

---

## Security

Security is Phase 1, not an open question.

### Default Policy

- disabled by default
- loopback-only HTTP binding
- token required for HTTP unless `ISM_ALLOW_UNAUTHENTICATED=1`
- deny remote hosts by default
- no CORS unless configured
- store the shared app token in the user's app settings folder
- multiple instances of the same app share the same app settings token
- redact sensitive values in DataContext output
- log tool name, caller transport, success/failure, and error code
- log target `appId` and `instanceId` for broker-routed calls
- client-configured inspection limits can only affect bounded output size and
  must be clamped by server policy

### Tool Classes

| Tool | Default Access |
|------|----------------|
| runtime info | allowed when MCP enabled |
| visual tree snapshot | allowed when MCP enabled, bounded |
| element metadata | allowed when MCP enabled, bounded |
| screenshot | token required |
| DataContext inspection | token required |
| pointer click | token required |
| key press/type text | token required |
| automation peer default action | token required |
| close | token required |

---

## Adapter Model

An adapter is a framework-specific package that implements the core contracts for
one UI stack. The MCP host should not know whether the target app uses Uno,
Avalonia, WinForms, WPF, or another framework.

Adapter packages should expose one registration method, for example:

```csharp
services.AddInSharpMcpUnoAdapter(window);
services.AddInSharpMcpAvaloniaAdapter(applicationLifetime);
services.AddInSharpMcpWinFormsAdapter(mainForm);
```

Every adapter must define:

- how UI work is dispatched onto the framework UI thread
- how the root window/control is found
- which tools are supported
- how framework elements map to framework-neutral selector fields
- how framework accessibility/automation data maps to roles, names, states, and values
- what app-idle means, if wait-for-idle is supported
- which event categories can be captured
- how unsupported tools return structured unsupported results
- how coordinates are interpreted
- how screenshots and input are implemented, if supported
- whether UI operations must be serialized and which operations can safely overlap

---

## Uno/WinUI Adapter Plan

The Uno/WinUI adapter must remain an adapter package, not the core architecture.
It uses WinUI/Uno-specific APIs and compiles only in the Uno adapter project.

| Adapter | Implementation Notes |
|---------|----------------------|
| `UnoAppProvider` | Store the active `Window`; use app/package metadata where available |
| `UnoUiDispatcher` | Marshal every UI read/write onto the window dispatcher |
| `UnoVisualTreeInspector` | Use `VisualTreeHelper.GetChild`; bound depth, node count, string length, and timeout |
| `UnoScreenshotProvider` | Windows: `RenderTargetBitmap.RenderAsync()` plus PNG encoding; Desktop/Skia: TBD future enhancement |
| `UnoPointerInputSimulator.Windows` | Use supported Windows input injection only when available and permission-compatible; otherwise return unsupported |
| `UnoPointerInputSimulator.Skia` | Do not fake `PointerRoutedEventArgs`; use a proven platform path or return unsupported |
| `UnoAutomationPeerInvoker` | Find an existing peer through public APIs/patterns; if no invokable pattern is available, return unsupported |

### Visual Tree Snapshot Strategy

Walk `VisualTreeHelper.GetChild` recursively, building a bounded representation:

```text
Window [Window]
  Grid [LayoutRoot]
    TextBlock [TitleText] "Hello World"
    Button [OkButton] (AutomationId: okButton)
```

Each node may include:

- type
- `Name`
- `AutomationId`
- DataContext type name
- selected leaf text/value, capped per node

The walker must stop when any limit is reached and report truncation in the
result.

### Screenshot Strategy

- Windows: use `RenderTargetBitmap.RenderAsync()` and encode PNG bytes.
- Desktop/Skia: TBD future enhancement.
- Android/WASM: return unsupported in the first version.

### Input Strategy

Input simulation is platform-specific and must not be represented as
framework-agnostic magic.

- Windows: evaluate `InputInjector` or another supported Windows API.
- Desktop/Skia: TBD future enhancement; return unsupported until a tested
  backend-specific input path exists.
- Do not construct or raise framework routed event args manually as a substitute
  for real input.

---

## Avalonia Adapter Plan

The Avalonia adapter should implement the same contracts without changing the MCP
host or tool methods.

Expected implementation areas:

- dispatch through Avalonia's UI thread dispatcher
- inspect the logical/visual tree using Avalonia APIs
- identify elements through `Name`, automation properties, or adapter-defined IDs
- capture screenshots only through a supported Avalonia/platform path
- return unsupported for input simulation until a tested backend path exists

This adapter is not part of the first implementation unless an Avalonia host is
available for validation.

---

## WinForms Adapter Plan

The WinForms adapter should implement the same contracts for classic desktop
apps.

Expected implementation areas:

- dispatch through `Control.InvokeAsync` or the closest supported equivalent
- inspect the `Control.Controls` tree
- identify elements through `Name`, accessibility names, or adapter-defined IDs
- capture screenshots through supported control/window capture APIs
- implement input only through a proven Windows input path, or return unsupported

This adapter is not part of the first implementation unless a WinForms host is
available for validation.

---

## Project File Sketches

### Contracts

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>true</IsPackable>
  </PropertyGroup>
</Project>
```

### MCP Host

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>true</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\InSharpMcp.Contracts\InSharpMcp.Contracts.csproj" />
    <PackageReference Include="ModelContextProtocol" />
    <PackageReference Include="ModelContextProtocol.AspNetCore" />
    <PackageReference Include="Microsoft.Extensions.Hosting" />
  </ItemGroup>
</Project>
```

### Uno Adapter

```xml
<Project Sdk="Uno.Sdk/6.5.33">
  <PropertyGroup>
    <TargetFrameworks>net9.0-windows10.0.19041;net9.0-desktop</TargetFrameworks>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>true</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\InSharpMcp.Contracts\InSharpMcp.Contracts.csproj" />
  </ItemGroup>
</Project>
```

Add package versions according to the package-management style of the standalone
implementation project.

---

## Broker and Host App Integration

The broker is the MCP server process. The host app does not expose MCP directly;
it registers an app-side adapter endpoint with the broker after the window or
main control exists and only when explicitly enabled.

Broker startup sketch:

```csharp
await InSharpMcpBrokerHost.RunStdioAsync(
    configure: options =>
    {
        options.RequireTokenForProtectedTools = true;
        options.MaxRegisteredInstances = 64;
    },
    cancellationToken);
```

Host app registration sketch:

```csharp
#if WINDOWS || __SKIA__
if (InSharpMcpStartupOptions.IsEnabled(configuration))
{
    _mcpRegistration = await InSharpMcpAppEndpoint.RegisterAsync(
        window: _window,
        brokerEndpoint: InSharpMcpBrokerDiscovery.FindLocalBroker(),
        descriptor: new AppInstanceDescriptor(
            InstanceId: InSharpMcpInstanceId.Create(),
            AppId: "sample-app",
            AppName: "Sample App",
            ProcessId: Environment.ProcessId,
            AdapterKind: "uno",
            Capabilities: InSharpMcpUnoCapabilities.Default,
            Endpoint: InSharpMcpAppEndpoint.LocalEndpoint,
            RegisteredAt: DateTimeOffset.UtcNow),
        configureEndpoint: options =>
        {
            options.RequireBrokerToken = true;
            options.BindToLoopbackOnly = true;
        },
        cancellationToken);
}
#endif
```

The host app must unregister during shutdown when possible. The broker must also
expire stale instances that stop heartbeating.

---

## Implementation Phases

### Phase 1 - Safe Foundation

1. Add package versions according to the selected package-management style.
2. Create `InSharpMcp.Contracts`.
3. Create `InSharpMcp`.
4. Define `ToolResult`, `ToolLimits`, `ToolLimitPolicy`, and all interfaces.
5. Implement client configuration parsing for optional inspection limits.
6. Implement server-side limit clamping and validation.
7. Implement `InSharpMcpConcurrencyOptions` and `IUiOperationQueue`.
8. Implement `AppInstanceDescriptor`, `AppInstanceRegistry`, and `AppInstanceSelector`.
9. Implement DI-based `InSharpMcpTools` for listing instances and runtime info only.
10. Implement broker stdio transport, optional broker HTTP transport, token authorization, and bounded concurrency.
11. Implement app-side registration/unregistration and stale-instance expiration.
12. Add in-memory, stdio, or HTTP tests that prove tool registration, instance registration, target routing, authorization behavior, limit clamping, and concurrent runtime-info calls.

### Phase 2 - Adapter Contract Harness

13. Create shared adapter contract tests for dispatcher behavior, unsupported results, bounds, cancellation, and concurrent UI operation queuing.
14. Define framework-neutral expectations for element lookup, visual-tree output, screenshots, and input.
15. Add fake/in-memory adapters for core MCP tests where a real UI framework is unnecessary.

### Phase 3 - Uno Adapter MVP

16. Create `InSharpMcp.Adapters.Uno` for Windows/Desktop adapter code.
17. Implement `UnoUiDispatcher`.
18. Implement bounded `UnoVisualTreeInspector`.
19. Add `ism_visualtree_snapshot` and `ism_get_element_metadata`.
20. Add tests for depth limit, node limit, truncation, unsupported platform behavior, and concurrent snapshot queueing.

### Phase 4 - Screenshot and DataContext Metadata

21. Implement Windows screenshot capture and MCP image-content return in the Uno adapter.
22. Mark Desktop/Skia screenshot as a TBD future enhancement and return unsupported.
23. Implement bounded, non-recursive DataContext metadata.
24. Add tests for redaction, output caps, image/error result shape, and concurrent screenshot/inspection behavior.

### Phase 5 - Selectors, Waits, Accessibility, and Events

25. Implement framework-neutral structured JSON selector parsing and validation.
26. Add `ism_query_elements`.
27. Add bounded wait/retry support through `ism_wait_for_element`.
28. Add bounded accessibility-tree support where the adapter can provide it.
29. Add bounded event-log capture.
30. Add tests for selector JSON parsing, deterministic ordering, invalid selectors, JSON escaping, wait timeout, accessibility bounds, and event redaction.

### Phase 6 - Interaction Tools

31. Implement Windows pointer/key/text input only through proven platform APIs.
32. Mark Desktop/Skia input as a TBD future enhancement and return unsupported.
33. Implement automation peer default action only through public invokable patterns.
34. Add optional before/after screenshot capture for protected interaction tools.
35. Add tests for authorization, validation, unsupported paths, cancellation, serialized input operations, and trace entries around interactions.

### Phase 7 - Tracing and Assertions

36. Add bounded trace start/stop tools.
37. Record selector resolution, waits, tool timings, errors, and optional screenshot references.
38. Add simple assertion helpers for element existence, visibility, enabled state, text, and value.
39. Add tests for trace limits, trace redaction, assertion pass/fail results, and trace cleanup.

### Phase 8 - Additional Framework Adapters

40. Add Avalonia adapter only when an Avalonia host can validate behavior.
41. Add WinForms adapter only when a WinForms host can validate behavior.
42. Run the shared adapter contract tests against each adapter.

---

## Test Plan

Required tests:

- tool discovery exposes the expected `ism_` names
- unauthorized HTTP calls are rejected for protected tools
- app instances can register and unregister with the broker
- multiple instances with the same `appId` receive different `instanceId` values
- multiple apps using the same adapter kind can be registered simultaneously
- target selection by `instanceId`, `appId`, and `adapterKind` follows the documented rules
- ambiguous target selection returns `ambiguous_target` with candidate instances
- stale app instances expire after heartbeat loss or failed connection
- runtime info works when MCP is enabled
- broker or app-side registration does not start when MCP is disabled
- absent client limit config uses server defaults
- valid client limit config changes effective `MaxDepth`, `MaxNodes`, and `MaxTextCharacters`
- excessive client limit config is clamped to server maximums
- invalid client limit config falls back to defaults and logs a warning
- client limit config cannot change timeout, queue timeout, auth, binding, CORS, or transport settings
- multiple runtime-info calls execute concurrently
- concurrent UI calls queue with bounded wait and isolated cancellation
- a timed-out UI call does not cancel unrelated calls
- non-UI calls are not blocked behind queued UI operations
- visual tree snapshot respects max depth, max nodes, output size, and timeout
- selector queries reject invalid structured JSON selectors and return deterministic bounded results
- wait tools honor polling limits, timeout clamps, and cancellation
- accessibility-tree output is bounded and maps framework roles/states consistently
- event logs are bounded and redact sensitive values
- tracing records timings, selector attempts, errors, and optional screenshot references
- trace output respects size/time limits and cleanup rules
- assertion helpers return structured pass/fail results without throwing for normal failures
- DataContext metadata does not recursively serialize arbitrary objects
- screenshot returns image content on supported platforms and explicit errors elsewhere
- input tools reject invalid keys, invalid modifiers, negative coordinates, and excessive text
- unsupported platform adapters return structured unsupported results
- cancellation stops long-running UI operations
- shared adapter contract tests pass for each implemented framework adapter

Manual verification:

- host app launches normally with MCP disabled.
- host app registers with the broker only when explicitly enabled.
- MCP client can list instances and call runtime/visual-tree tools on a selected target.
- Protected tools require the configured token.
- two instances of the same app can be selected separately.
- two different apps using the same adapter can be selected separately.
- Desktop/Skia returns correct support/unsupported results per implemented adapter.
- Future Avalonia/WinForms hosts can register their adapters without changing the MCP host.

---

## Demo Apps

Each adapter should have a small demo app under `demos/` for manual testing and
adapter conformance work:

```text
demos/
  demo.uno/
  demo.avalonia/
  demo.winforms/
```

Every demo should include:

- simple menu
- several buttons
- single-line text input
- editable text area
- scrollable lorem ipsum text area
- basic labels/text blocks
- enough adapter-specific controls to validate selectors, waits, screenshots,
  input, accessibility, and event capture

---

## Remaining Decisions

1. **Broker discovery:** how host apps find the local broker.
2. **Wait semantics:** use static global defaults with caps that avoid both overly conservative waits and very long waits; exact values TBD.
3. **Accessibility mapping:** common role/state/value model across frameworks. TBD.
4. **Event categories:** which logs/events are safe enough to expose by default. TBD.
