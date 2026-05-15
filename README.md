# InSharpMcp

<p align="center">
  <img src="architecture.png" alt="InSharpMcp architecture diagram" width="800">
</p>

<p align="center">
  <strong>Framework-independent MCP automation for .NET desktop apps.</strong><br>
  Inspect UI, query elements, capture supported screenshots, trace tool calls, and route actions through framework adapters.
</p>

<table align="center">
  <tr>
    <td align="center"><strong>Core</strong><br><code>InSharpMcp</code> broker</td>
    <td align="center"><strong>Adapters</strong><br>Uno, Avalonia, WinForms</td>
    <td align="center"><strong>Transports</strong><br>stdio and HTTP</td>
    <td align="center"><strong>Tests</strong><br>xUnit projects</td>
  </tr>
</table>

InSharpMcp is a framework-independent MCP automation layer for .NET desktop applications. It gives an MCP client a bounded, structured way to inspect a running app, query UI elements, capture screenshots, read safe metadata, collect recent events, start traces, run simple assertions, and invoke carefully gated interaction tools.

The core package does not reference Uno, Avalonia, WinForms, WPF, WinUI, or any other UI framework. UI frameworks plug in through small adapter packages that implement shared contracts from `InSharpMcp.Contracts`.

This repository currently contains the core broker, shared contracts, Uno, Avalonia, and WinForms adapters, adapter tests, and demo apps for the three planned environments.

## Table of Contents

- [Current Status](#current-status)
- [Repository Layout](#repository-layout)
- [Requirements](#requirements)
- [Build and Test](#build-and-test)
- [How InSharpMcp Works](#how-insharpmcp-works)
- [Starting the Broker](#starting-the-broker)
- [Registering an App Instance](#registering-an-app-instance)
- [Adapter Capabilities](#adapter-capabilities)
- [Interaction Behavior](#interaction-behavior)
- [MCP Client Configuration](#mcp-client-configuration)
- [Limits and Safety](#limits-and-safety)
- [Security Model](#security-model)
- [Selecting a Target App](#selecting-a-target-app)
- [Selectors](#selectors)
- [Tool Manual](#tool-manual)
- [Data Returned by Inspection Tools](#data-returned-by-inspection-tools)
- [Development Workflow](#development-workflow)
- [Known Limitations](#known-limitations)
- [License](#license)

## Current Status

The project is source-ready but not published as NuGet packages from this repository. Use project references while developing against it.

The verified implementation includes the callable `InSharpMcp.Broker` executable, the reusable `InSharpMcp.Core` broker library, the app-side `InSharpMcp.Bridge`, `InSharpMcp.Contracts`, `InSharpMcp.Adapters.Uno`, `InSharpMcp.Adapters.Avalonia`, and `InSharpMcp.Adapters.WinForms`. The repository currently pins Uno Platform through `Uno.Sdk/6.5.33`. It pins Avalonia to `11.3.9`; the Avalonia adapter has also been compile-checked against Avalonia `12.0.3`, so the current implementation does not require separate v11 and v12 adapter source versions.

Pointer, keyboard, text input, and default actions are implemented where the adapters have validated public platform paths. Remaining `unsupported` results are specific backend or element-shape limits.

The repository includes xUnit test projects for the broker, shared adapter contracts, and framework adapters.

## Repository Layout

The server code lives under `mcp/server`.

| Path | Purpose |
|------|---------|
| `mcp/server/InSharpMcp.Contracts` | Shared result models, limit models, selectors, screenshots, traces, assertions, and adapter interfaces. |
| `mcp/server/InSharpMcp.Core` | Reusable MCP core library: routing, registry, security, limits, event log, trace store, selectors, assertions, transports, and `ism_` tools. |
| `mcp/server/InSharpMcp.Broker` | Command-line MCP broker executable for IDE/client MCP configuration. |
| `mcp/server/InSharpMcp.Bridge` | App-side local bridge that registers a running app instance with the broker and carries UI operations back to the live adapter. |
| `mcp/server/InSharpMcp.Adapters.Uno` | Uno/WinUI adapter for dispatcher, visual tree, metadata, DataContext, screenshots where supported, accessibility delegation, Windows input, and command-backed default action invocation. |
| `mcp/server/InSharpMcp.Adapters.Avalonia` | Avalonia adapter for dispatcher, visual tree, metadata, DataContext, screenshots for measured controls, accessibility delegation, Windows input, and command-backed default action invocation. |
| `mcp/server/InSharpMcp.Adapters.WinForms` | WinForms adapter for dispatcher, control tree, metadata, Tag-based DataContext, screenshots, accessibility delegation, Windows input, and button default action invocation. |
| `mcp/server/tests` | Core tests, shared adapter contract tests, and framework adapter tests. |
| `demos` | Uno, Avalonia, and WinForms demo apps for manual adapter validation. |
| `plans` | Design plan, implementation notes, and verification record. |

## Requirements

Use a current .NET SDK that can build `net8.0`, `net8.0-windows`, and the Uno adapter target frameworks. This repository was verified on Windows. The machine used for verification has a .NET 11 preview SDK as the default SDK, so build output may print `NETSDK1057`; that message is informational for this repo because the projects target the configured TFMs.

The WinForms adapter and WinForms demo require Windows. The Uno adapter targets `net9.0-windows10.0.19041` and `net9.0-desktop`. The Avalonia adapter targets `net8.0`.

## Build and Test

From the repository root, build and test the server solution:

```powershell
dotnet build mcp/server/InSharpMcp.sln
dotnet test mcp/server/InSharpMcp.sln
```

Build all demo apps together:

```powershell
dotnet build demos/InSharpMcp.Demos.slnx
```

Run the demo apps individually:

```powershell
dotnet run --project demos/demo.uno/InSharpMcp.Demo.Uno/InSharpMcp.Demo.Uno.csproj -f net9.0-desktop
dotnet run --project demos/demo.avalonia/InSharpMcp.Demo.Avalonia.csproj
dotnet run --project demos/demo.winforms/InSharpMcp.Demo.WinForms.csproj
```

The demos are intentionally small and stable. They expose menus, buttons, inputs, editable text, scrollable text, labels, and framework-specific controls so selectors, screenshots, accessibility, and metadata can be validated manually.

## How InSharpMcp Works

An MCP client talks to an InSharpMcp broker. The broker owns the MCP tool surface and routes each tool call to a selected app instance. The app instance is represented by an `AppInstanceDescriptor` and an active `IAppInstanceClient`.

The core broker remains UI-framework-neutral. It knows how to select an app instance, enforce limits, authorize protected tools, serialize UI work through a bounded queue, and record tool events. The adapter knows how to run work on the UI thread and inspect the framework-specific UI tree.

When exactly one compatible app instance is registered, tools can run without an explicit target. When more than one instance matches, InSharpMcp returns `ambiguous_target` instead of guessing. When a selected instance has no active client connection, it returns `stale_instance`.

The broker process runs the MCP protocol interface. Apps do not expose MCP directly. A running app references `InSharpMcp.Bridge` plus its framework adapter, and the Bridge registers that live app instance with the broker over a local named pipe. The broker then routes MCP tool calls to that app through the Bridge.

## Starting the Broker

MCP is disabled by default. Host code should only start or register MCP when explicit configuration enables it. The built-in environment switch is:

```powershell
$env:ISM_ENABLED = "1"
```

For IDE MCP clients such as Codex or Cursor, use the broker executable. The default transport is stdio, which is what command-launched MCP clients normally expect.

From a source checkout, the command form is:

```powershell
dotnet run --project mcp/server/InSharpMcp.Broker/InSharpMcp.Broker.csproj -- --transport stdio
```

`InSharpMcp.Broker` is also configured as a .NET tool project with the command name `insharp-mcp`. After packaging and installing it as a tool, the command becomes:

```powershell
insharp-mcp --transport stdio
```

For local HTTP clients, run the same executable in HTTP mode:

```powershell
dotnet run --project mcp/server/InSharpMcp.Broker/InSharpMcp.Broker.csproj -- --transport http --http-port 52001 --http-path /mcp
```

HTTP always binds to `127.0.0.1` and maps the MCP endpoint at `/mcp` by default. Remote HTTP clients are rejected.

## Registering an App Instance

App registration is normal dependency injection. Add the adapter for your framework, add `InSharpMcp.Bridge`, and start the bridge with an `AppBridgeRegistration`. The demos do this by default so they are visible to an already-running broker without setting `ISM_ENABLED`.

The exact window/control type depends on the adapter. A WinForms host can wire itself like this:

```csharp
using InSharpMcp;
using InSharpMcp.Adapters.WinForms;
using InSharpMcp.Bridge;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddInSharpMcpWinFormsAdapter(
    form,
    appName: "Sample WinForms App",
    appVersion: "1.0.0",
    platformTarget: "WinForms");
services.AddInSharpMcpBridge();

var provider = services.BuildServiceProvider();
var registration = new AppBridgeRegistration(
    AppId: "sample-winforms-app",
    AppName: "Sample WinForms App",
    AdapterKind: "winforms",
    PlatformTarget: "WinForms",
    AppVersion: "1.0.0",
    Capabilities: new HashSet<string>(StringComparer.Ordinal)
    {
        "runtime",
        "visualtree",
        "metadata",
        "screenshot",
        "accessibility"
    });

await provider.GetRequiredService<InSharpMcpBridge>().StartAsync(registration);
```

Dispose the service provider when the host closes. That stops the Bridge and unregisters the app instance from the broker when possible.

Avalonia hosts use `AddInSharpMcpAvaloniaAdapter(window, ...)`. Uno hosts use `AddInSharpMcpUnoAdapter(window, ...)`. WinForms hosts use `AddInSharpMcpWinFormsAdapter(form, ...)`.

## Adapter Capabilities

The adapters share the same contract shape, but platform support is intentionally honest.

| Adapter | Implemented |
|---------|-------------|
| Uno | UI dispatch, app info/close, bounded visual tree, element metadata, DataContext metadata, Windows screenshot capture, accessibility tree delegation, Windows pointer/key/text input through native input APIs, command-backed `ButtonBase` default action invocation. |
| Avalonia | UI dispatch, app info/close, bounded visual tree, element metadata, DataContext metadata, screenshot capture for measured controls, accessibility tree delegation, Windows pointer/key/text input through native input APIs, command-backed default action invocation through `ICommandSource`. |
| WinForms | UI dispatch, app info/close, bounded control tree, element metadata, Tag-based DataContext metadata, `DrawToBitmap` screenshot capture, accessibility tree delegation, Windows pointer/key/text input through native input APIs, default action invocation for `IButtonControl`. |

Unsupported results are normal tool outcomes. They let MCP clients distinguish "this platform path has no validated adapter implementation" from a failure in the app.

## Interaction Behavior

Pointer, keyboard, text, and default-action tools are protected operations. They are routed through the selected app instance, run on the adapter dispatcher where UI state is needed, and use structured `ToolResult` outcomes.

Pointer coordinates are adapter-root-relative, not raw desktop/screenshot coordinates. WinForms translates them with `Control.PointToScreen`. Avalonia translates them with Avalonia `PointToScreen`. Uno translates them through the Windows target's native HWND when available; Uno Desktop/Skia pointer clicks remain `unsupported` until a validated backend-specific screen-coordinate path exists.

When a caller starts from visual evidence such as a screenshot or UI Automation `BoundingRectangle`, first convert the target screen point into the adapter root's client coordinate space. For WinForms, use the actual form handle with Win32 `ScreenToClient`; do not pass UIA screen coordinates directly, and do not rely on manual DPI division. A successful `ism_pointer_click` only means the input injector sent a click. Validate the intended effect with state inspection, a screenshot, an event, or a control-specific assertion.

Key and text input use native Windows input APIs instead of fabricated framework events. Key presses accept a single alphanumeric character, `F1` through `F12`, or one of `enter`, `escape`, `tab`, `backspace`, `delete`, `space`, `arrowup`, `arrowdown`, `arrowleft`, `arrowright`, `home`, `end`, `pageup`, and `pagedown`. Modifiers are `alt`, `control`/`ctrl`, `shift`, and `meta`/`win`. Text input is capped by the interaction validator.

Default actions use public control contracts only. Uno invokes `ButtonBase.Command`, Avalonia invokes `ICommandSource.Command`, and WinForms invokes `IButtonControl.PerformClick()`. Elements without those public action surfaces return structured `unsupported`.

## MCP Client Configuration

Command-launched MCP clients should point at the broker executable. From a source checkout, a Codex/Cursor-style configuration looks like this:

```json
{
  "mcpServers": {
    "insharp-mcp": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "mcp/server/InSharpMcp.Broker/InSharpMcp.Broker.csproj",
        "--",
        "--transport",
        "stdio"
      ],
      "env": {
        "ISM_MAX_DEPTH": "20",
        "ISM_MAX_NODES": "500",
        "ISM_MAX_TEXT_CHARACTERS": "64000"
      }
    }
  }
}
```

After the broker is packaged and installed as the `insharp-mcp` .NET tool, the same config can use the tool command directly:

```json
{
  "mcpServers": {
    "insharp-mcp": {
      "command": "insharp-mcp",
      "args": [
        "--transport",
        "stdio"
      ]
    }
  }
}
```

An HTTP MCP client can connect to the default local endpoint with a configuration shaped like this:

```json
{
  "mcpServers": {
    "insharp-mcp": {
      "url": "http://127.0.0.1:52001/mcp",
      "headers": {
        "X-InSharpMcp-Max-Depth": "20",
        "X-InSharpMcp-Max-Nodes": "500",
        "X-InSharpMcp-Max-Text-Characters": "64000"
      }
    }
  }
}
```

For command-launched or stdio flows, pass the same limit values through environment variables:

```powershell
$env:ISM_MAX_DEPTH = "20"
$env:ISM_MAX_NODES = "500"
$env:ISM_MAX_TEXT_CHARACTERS = "64000"
```

Only inspection limits are client-configurable. Clients cannot change timeout, queue timeout, auth, CORS, host binding, or transport security through those keys.

## Limits and Safety

Inspection tools limit how much UI data they read and return. Defaults are `MaxDepth = 20`, `MaxNodes = 500`, `MaxTextCharacters = 64000`, `Timeout = 5 seconds`, and `QueueTimeout = 2 seconds`.

The server clamps requested values to the configured policy. Depth is clamped between 1 and 50, node count between 1 and 2000, and text characters between 1024 and 256000. Invalid values fall back to defaults.

UI operations run through `IUiOperationQueue`. This keeps unrelated non-UI work from blocking behind UI work while still serializing UI-thread critical sections. If the queue is full or wait time is exceeded, the tool returns a structured busy or timeout result.

## Security Model

Some tools expose private app state or modify the running app. MCP clients should treat these as approval-gated actions:

```text
ism_get_screenshot
ism_get_element_datacontext
ism_pointer_click
ism_key_press
ism_type_text
ism_element_peer_default_action
ism_close
```

The broker does not use a shared token. Stdio is intended for IDE-launched local MCP clients, and HTTP is local-only: the broker binds to `127.0.0.1` and rejects non-loopback clients.

## Selecting a Target App

Most tools accept an optional target selector. A target can name an `instanceId`, an `appId`, or an `adapterKind`.

Use `instanceId` when more than one app window or process can be registered. Use `appId` when you know there is only one app instance with that ID. Use `adapterKind` for broad diagnostics only when one compatible instance is registered.

Example selector:

```json
{
  "instanceId": "sample-instance"
}
```

If no target is supplied and exactly one instance is registered, the broker uses that instance. If multiple instances match, the broker returns a candidate list with `ambiguous_target`.

## Selectors

Element selectors are structured JSON objects. They are not CSS, XPath, or a custom string grammar.

```json
{
  "role": "Button",
  "name": "Primary action"
}
```

The supported selector fields are `name`, `automationId`, `type`, `text`, `role`, `index`, `path`, and an adapter-specific `adapter` object. Results are deterministic, bounded, and returned in tree order.

Element identifiers use adapter-generated tree paths such as `0`, `0/0`, or `0/1/2`. Use `ism_visualtree_snapshot` or `ism_query_elements` to discover identifiers, then pass the identifier to metadata, DataContext, or default-action tools.

## Tool Manual

The tool surface uses the `ism_` prefix.

| Tool | Use it for |
|------|------------|
| `ism_list_instances` | List registered app instances and their capabilities. |
| `ism_get_runtime_info` | Read PID, OS, platform target, app name, and app version for the selected instance. |
| `ism_visualtree_snapshot` | Get a bounded visual or control tree snapshot. |
| `ism_get_element_metadata` | Read safe metadata for one element identifier. |
| `ism_get_element_datacontext` | Read bounded, non-recursive DataContext metadata. This is protected. |
| `ism_get_screenshot` | Capture a PNG screenshot where the adapter supports it. This is protected. |
| `ism_query_elements` | Find elements with a structured selector. |
| `ism_wait_for_element` | Poll for a matching element until a bounded timeout. |
| `ism_get_accessibility_tree` | Return the adapter accessibility tree where available. |
| `ism_get_event_log` | Read recent bounded, redacted tool and adapter events. |
| `ism_pointer_click` | Request a pointer click using adapter-root/client coordinates, not raw screen pixels. Convert UIA/screenshot points first and validate the resulting state. This may return `unsupported`. |
| `ism_key_press` | Request a key press. This is protected and may return `unsupported`. |
| `ism_type_text` | Request text input. This is protected and may return `unsupported`. |
| `ism_element_peer_default_action` | Invoke a public default action where the adapter supports it. This is protected. |
| `ism_close` | Request a graceful close. This is protected. |
| `ism_start_trace` | Start recording bounded events for a selected instance. |
| `ism_stop_trace` | Stop a trace and return a summary. |
| `ism_assert_element_exists` | Return a structured pass/fail result for element existence. |
| `ism_assert_element_text` | Return a structured pass/fail result for matching element text. |
| `ism_assert_element_enabled` | Return a structured pass/fail result for enabled state. |

Normal assertion failures return successful tool calls with an `AssertionResult` whose `Passed` value is `false`. They do not throw as tool errors.

## Data Returned by Inspection Tools

Visual-tree snapshots are built from `UiElementNode`. Nodes include an element identifier, type, optional name, optional automation ID, optional text, optional role, optional visible/enabled states, and children.

Element metadata uses the same safe fields without child nodes. DataContext metadata includes the DataContext type name and public primitive/string-like properties only. It does not recursively walk arbitrary object graphs, and sensitive property names such as password, secret, token, or key are redacted.

Trace summaries contain a trace ID, timestamps, bounded event entries, and a truncation flag.

## Development Workflow

This repository uses central package management in `Directory.Packages.props`. Project files should normally use versionless `PackageReference` entries.

For normal development, run:

```powershell
dotnet build mcp/server/InSharpMcp.sln
dotnet test mcp/server/InSharpMcp.sln
dotnet build demos/InSharpMcp.Demos.slnx
```

After repeated builds, you can shut down .NET build servers with:

```powershell
dotnet build-server shutdown
```

The existing tests cover routing, target ambiguity, stale instances, local broker transport, limit clamping, UI queue behavior, selector matching, waits, event redaction, tracing, assertions, adapter contracts, and framework adapter smoke behavior.

## Known Limitations

InSharpMcp currently provides source projects, NuGet packages are planned to come shortly.

The included Bridge provides a local named-pipe app-to-broker transport for desktop apps on the same machine. Production hosts can use it directly or replace it with a custom transport if they need a different discovery, authentication, or deployment model.

Uno Desktop/Skia screenshot and pointer-click paths are intentionally unsupported until validated backend-specific implementations exist. Keyboard/text input uses native Windows input where available. Default action invocation is limited to public command/button patterns: Uno `ButtonBase.Command`, Avalonia `ICommandSource.Command`, and WinForms `IButtonControl.PerformClick()`.

The file `plans/ADAPTER_VALIDATION.md` records the current adapter validation status and remaining structured unsupported paths.

## License

See `LICENSE`.
