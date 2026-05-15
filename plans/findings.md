# Findings and Decisions

## Requirements
- Fully implement `plans/PLAN.md`.
- Keep project and package names application-agnostic.
- Core server must remain framework-independent and must not reference UI frameworks.
- MCP must be disabled by default and enabled only by explicit setting.
- Broker is the primary MCP entry point and must support multiple registered app instances.
- Tool names use the `ism_` prefix.
- UI work must flow through adapter dispatchers and bounded UI operation queues.
- Inspection tools must enforce depth, node count, text size, timeout, and cancellation bounds.
- Privacy-sensitive and interaction operations are local-only and should be approval-gated by MCP clients.
- Implement and verify in coherent feature/step commits.
- Use planning-with-files state under `plans`.

## Research Findings
- `plans/PLAN.md` defines eight implementation phases plus a broad test plan.
- The current workspace started clean according to `git status --short`.
- No active Codex goal existed before this session.
- Goalcraft validation passed for the activated objective at 2,915 objective characters.
- `planning-with-files` session catchup produced no unsynced context output for the `plans` folder.
- Repository discovery found no existing `.sln`, `.csproj`, source, or test projects. The repo currently contains planning/docs/license files only.
- NuGet package search found `ModelContextProtocol` and `ModelContextProtocol.AspNetCore` latest stable package version `1.3.0`.
- The .NET SDK default is an 11 preview, but stable .NET 8/9/10 SDKs are installed. Projects target `net8.0` per plan.
- The .NET 11 `dotnet new sln` default is `.slnx`; the solution was explicitly created as standard `.sln`.
- Restored MCP SDK metadata documents `AddMcpServer`, `WithStdioServerTransport`, `WithHttpTransport`, `WithTools<T>`, and `MapMcp`.
- `McpServerToolAttribute.Name` is nullable in the SDK surface, so tool catalog discovery must provide a deterministic method-name fallback.
- Shared adapter contract harness now uses framework-neutral `UiElementNode`, `UiTreeSnapshot`, and `ElementMetadata` records.
- `Uno.Sdk` version `6.5.33` exists on NuGet and restores for the adapter project.
- `InSharpMcp.Adapters.Uno` builds for `net9.0-windows10.0.19041` and `net9.0-desktop` in the current environment.
- DataContext metadata is produced by a shared `DataContextMetadataFactory`, making redaction/cap behavior testable outside UI framework code.
- Structured selectors are represented as JSON-bindable `ElementSelector` records and matched against bounded `UiTreeSnapshot` data in deterministic preorder.
- Event log entries are bounded and redact sensitive data keys before storage.
- Interaction tools validate input before dispatch, run through the UI queue where applicable, and record interaction event-log entries.
- Trace start/stop uses a bounded in-memory trace store and assertion helpers return structured pass/fail `AssertionResult` data without throwing for normal failures.
- Phase 8 recorded the additional-adapter validation gate. Phase 11 and Phase 12 later resolved it by adding demo hosts and implementing the Avalonia and WinForms adapters.
- Final verification passed with 56 tests. `plans/IMPLEMENTATION_SUMMARY.md` maps the implemented scope and validation-gated scope.
- Follow-up review found the prior completion state was premature: most tools bypass target selection, registered endpoints are not mapped to executable app operations, traces are not populated by real tool execution, `ism_close` bypasses the queue, and Uno lookup/text limits are incomplete.
- Tool entrypoints now route through `AppInstanceRouter` and `IAppInstanceClient`, which maps selected registry descriptors to executable app operations and returns `ambiguous_target` or `stale_instance` before dispatch.
- Broker shared-token authorization was removed; HTTP is loopback-only and rejects non-loopback clients.
- Approval-sensitive tools select targets through the same local broker routing path as other tools.
- Trace and event recording now happens around actual selected tool execution; `ism_start_trace`/`ism_stop_trace` are target-scoped.
- Uno visual-tree traversal now uses a shared `NodeVisitBudget` so node limits are consumed globally across sibling branches, and snapshot node metadata uses the caller's text limit instead of default limits.
- New demo-app goal targets the three planned environments in `plans/PLAN.md`: `demos/demo.uno`, `demos/demo.avalonia`, and `demos/demo.winforms`.
- Installed .NET templates include `unoapp`, `avalonia.app`, and `winforms`, so each demo can start from a framework-native template.
- `demos/InSharpMcp.Demos.slnx` builds all three demo projects. The first aggregate build failed because Uno SDK version resolution could not see the nested `demo.uno/global.json`; setting the Uno demo project SDK to `Uno.Sdk/6.5.33` fixed the solution-level build.
- The new adapter goal targets `InSharpMcp.Adapters.Avalonia` and `InSharpMcp.Adapters.WinForms`, which can now be validated because Phase 11 added buildable demo hosts for both frameworks.
- `InSharpMcp.Adapters.Avalonia` now builds against Avalonia 11.3.9 and provides UI dispatcher marshalling, bounded visual-tree inspection, DataContext metadata, screenshot capture for measured controls, app close, accessibility-tree delegation, Windows pointer/key/text input through native input APIs, and default action invocation through public `ICommandSource.Command`.
- `InSharpMcp.Adapters.WinForms` now builds for `net8.0-windows` and provides control-tree inspection, Tag-based DataContext metadata, `DrawToBitmap` screenshot capture, app close, accessibility-tree delegation, Windows pointer/key/text input through native input APIs, and default action invocation through `IButtonControl.PerformClick()`.
- Avalonia and WinForms demos now reference their adapter projects and register adapter services at startup, giving compile-time integration coverage for the two new adapter packages.
- `InSharpMcp.Adapters.Uno` now uses native Windows input APIs for key/text input and Windows-target pointer input when a native window handle is available; Desktop/Skia pointer click remains structured `unsupported` until a validated backend-specific screen-coordinate path exists.
- `InSharpMcp.Adapters.Uno` default action invocation is now wired to public `ButtonBase.Command`; unsupported remains only for elements without that command surface.
- The full server test suite now includes adapter-specific test projects and passes with 69 tests before the Phase 15 additions; focused Phase 15 adapter test projects pass with 5 WinForms tests and 3 Avalonia tests.

## Technical Decisions
| Decision | Rationale |
|----------|-----------|
| Use the existing repository structure where possible before adding new solution files | The implementation should match repo conventions rather than inventing structure prematurely. |
| Start with Phase 1 foundation slices | Later adapters and tools depend on shared contracts, limits, registry, concurrency, and host structure. |
| Commit planning files as the first step if tests/build discovery does not reveal an immediate blocker | The user requested frequent commits, and planning state is a coherent setup step. |
| Scaffold a new .NET solution using the `mcp/server` structure from `plans/PLAN.md` | No existing project structure is present, and the plan specifies the target layout. |
| Use central package management | The repository is greenfield and central package management keeps project files versionless as requested by `plans/PLAN.md`. |
| Keep broker host classes as thin startup wrappers around SDK hosting APIs | Transport behavior should stay in the official SDK while InSharpMcp owns policy, registry, and adapter routing services. |
| Use reflection over `InSharpMcpTools` for the initial tool catalog test | It verifies SDK attributes and `ism_` tool names without starting a long-lived MCP server process. |
| Put fake adapters in `InSharpMcp.AdapterContractTests` first | The harness validates contract expectations without adding test-only helpers to production packages. |
| Keep only plan-approved Uno unsupported paths as structured unsupported results | Windows screenshot, DataContext, Windows input, and command-backed default action support are implemented; Desktop/Skia screenshot and Desktop/Skia pointer click remain unsupported until validated backend-specific paths exist. |
| Put DataContext reflection in contracts instead of the Uno adapter | The bounding/redaction behavior is framework-independent and can be tested without a live UI. |
| Implement selector matching over framework-neutral snapshots | It keeps the selector grammar independent from any adapter and makes query/wait behavior testable without a live UI. |
| Use event-log entries as the initial trace surface for interaction tools | It provides auditable interaction result entries before the dedicated trace start/stop phase. |
| Keep assertion helpers result-oriented rather than exception-oriented | Normal assertion failures are expected tool outcomes and should return structured data. |
| Document the temporary Avalonia/WinForms validation blocker instead of adding unvalidated adapters during Phase 8 | `plans/PLAN.md` explicitly gates those adapters on available validation hosts; Phase 11 later added validating demo hosts and Phase 12 implemented both adapters. |
| Reopen implementation as Phase 10 instead of rewriting history | The workspace is clean and committed; remediation should be additive, tested, and committed in coherent slices. |
| Add demos as Phase 11 | The prior phases are complete; demo apps are a separate delivery slice requested by the new goal. |
| Add Avalonia/WinForms adapters as Phase 12 | The demo hosts satisfy the plan's validation gate for these framework adapters. |
| Implement Avalonia pointer/key/text input only through native input APIs | The plan requires proven platform APIs; the implementation uses Windows native input from Avalonia screen coordinates and avoids fabricated framework events. |
| Implement public default actions through command/button contracts | Avalonia uses `ICommandSource.Command`, Uno uses `ButtonBase.Command`, and WinForms uses `IButtonControl.PerformClick()`. |
| Use WinForms `Tag` as the inspected DataContext surface | WinForms has no native DataContext equivalent; `Tag` is the conventional object payload surface and can use the shared metadata factory safely. |

## Issues Encountered
| Issue | Resolution |
|-------|------------|
| First test command used `--no-restore` after adding new package references, causing missing package namespaces. | Reran `dotnet test` with restore enabled. |
| Initial HTTP host wrapper used `WebApplication` async overloads unavailable in this target shape. | Switched to configured URLs plus cancellation-triggered `StopAsync()` and `RunAsync()`. |
| Tool catalog initially assumed SDK tool attribute names are non-null. | Added method-name fallback for nullable `Name`. |
| xUnit template restore failed under central package management for the new adapter contract test project. | Removed generated package versions and restored through the solution. |
| Uno adapter registration needed a non-null content root for `UnoVisualTreeInspector`. | Added an explicit guard that reports missing window content at DI resolution time. |
| Visual-tree tool test forgot policy minimum clamping for `MaxTextCharacters`. | Corrected the expected value to `1024`. |
| Non-Windows screenshot branch returned the wrong type from an async method. | Returned `ScreenshotResult` directly. |
| Accessibility tool test mixed named and positional arguments. | Changed the cancellation token argument to named form. |
| Review found missing routed app execution. | Added Phase 10 remediation scope before code changes. |
| Parallel adapter builds locked the shared contracts output. | Rebuilt sequentially and kept later verification commands sequential. |
| WinForms `PerformClick()` test did not fire before the form was visible. | Updated the STA test to show the form and pump events before invoking the default action. |

## Resources
- `plans/PLAN.md`
- `plans/task_plan.md`
- `plans/progress.md`

## Verification Notes
- `dotnet test mcp/server/InSharpMcp.sln` passed with 25 tests after completing Phase 1 verification coverage.
- `dotnet test mcp/server/InSharpMcp.sln` passed with 34 tests after adding Phase 2 adapter contract harness.
- `dotnet test mcp/server/InSharpMcp.sln` passed with 36 tests after adding the Uno adapter and visual-tree/metadata tools.
- `dotnet test mcp/server/InSharpMcp.sln` passed with 42 tests after adding screenshot tool shape and DataContext metadata redaction/cap tests.
- `dotnet test mcp/server/InSharpMcp.sln` passed with 48 tests after adding selectors, wait, accessibility, and event-log tooling.
- `dotnet test mcp/server/InSharpMcp.sln` passed with 53 tests after adding interaction tools.
- `dotnet test mcp/server/InSharpMcp.sln` passed with 56 tests after adding trace start/stop and assertion helpers.
- Final `dotnet test mcp/server/InSharpMcp.sln` passed with 56 tests.
- `dotnet test mcp/server/InSharpMcp.sln` passed with 62 tests after routed tool dispatch, trace recording, and close queue regression coverage.
- `dotnet test mcp/server/InSharpMcp.sln` passed with 63 tests after fixing Uno traversal/text-limit handling and adding `NodeVisitBudget` coverage.
- `dotnet test mcp/server/InSharpMcp.sln` passed with 64 tests after target-routing regression coverage.
- Final Phase 10 `dotnet test mcp/server/InSharpMcp.sln` passed with 64 tests.
- `dotnet build demos/demo.avalonia/InSharpMcp.Demo.Avalonia.csproj` passed.
- `dotnet build demos/demo.winforms/InSharpMcp.Demo.WinForms.csproj` passed.
- `dotnet build demos/demo.uno/InSharpMcp.Demo.Uno/InSharpMcp.Demo.Uno.csproj -f net9.0-desktop` passed.
- `dotnet build demos/InSharpMcp.Demos.slnx` passed with 0 warnings and 0 errors.
- Final Phase 11 `dotnet test mcp/server/InSharpMcp.sln` passed with 64 tests after adding demo package versions and projects.
- Phase 12 `dotnet build mcp/server/InSharpMcp.Adapters.WinForms/InSharpMcp.Adapters.WinForms.csproj` passed with 0 warnings and 0 errors.
- Phase 12 `dotnet build mcp/server/InSharpMcp.Adapters.Avalonia/InSharpMcp.Adapters.Avalonia.csproj` passed with 0 warnings and 0 errors.
- Phase 12 `dotnet build mcp/server/InSharpMcp.sln` passed with 0 warnings and 0 errors.
- Phase 12 `dotnet build demos/InSharpMcp.Demos.slnx` passed with 0 warnings and 0 errors.
- Phase 12 `dotnet test mcp/server/InSharpMcp.sln` passed with 69 tests.
- Phase 15 `dotnet build mcp/server/InSharpMcp.Adapters.WinForms/InSharpMcp.Adapters.WinForms.csproj` passed with 0 warnings and 0 errors.
- Phase 15 `dotnet build mcp/server/InSharpMcp.Adapters.Avalonia/InSharpMcp.Adapters.Avalonia.csproj` passed with 0 warnings and 0 errors.
- Phase 15 `dotnet build mcp/server/InSharpMcp.Adapters.Uno/InSharpMcp.Adapters.Uno.csproj` passed with 0 warnings and 0 errors.
- Phase 15 `dotnet test mcp/server/tests/InSharpMcp.Adapters.WinForms.Tests/InSharpMcp.Adapters.WinForms.Tests.csproj` passed with 5 tests.
- Phase 15 `dotnet test mcp/server/tests/InSharpMcp.Adapters.Avalonia.Tests/InSharpMcp.Adapters.Avalonia.Tests.csproj` passed with 3 tests.
- Phase 15 final `dotnet test mcp/server/InSharpMcp.sln` passed with 72 tests.

## README Source Notes
- There is no root `README.md` yet.
- The public tool set currently contains 20 `ism_` tools.
- The broker exposes stdio and HTTP host wrappers. HTTP binds to `127.0.0.1:52001` and `/mcp`.
- MCP startup is explicitly gated by `ISM_ENABLED=1`.
- Approval-sensitive tools are `ism_get_screenshot`, `ism_get_element_datacontext`, `ism_pointer_click`, `ism_key_press`, `ism_type_text`, `ism_element_peer_default_action`, and `ism_close`.
- Client-configurable inspection limit keys are `ISM_MAX_DEPTH`, `ISM_MAX_NODES`, `ISM_MAX_TEXT_CHARACTERS`, `X-InSharpMcp-Max-Depth`, `X-InSharpMcp-Max-Nodes`, and `X-InSharpMcp-Max-Text-Characters`.
- Current adapter packages provide in-process adapter building blocks. An app instance is represented by an `AppInstanceDescriptor` and an active `IAppInstanceClient`; external app-to-broker discovery/transport is still a host integration concern.

## Broker Executable Gap
- The core library contains `StdioBrokerHost.RunAsync` and `HttpBrokerHost.RunAsync`, but there is no executable project with `Program.Main`.
- Without an executable broker, users cannot configure the project directly as a command-based MCP server in Codex, Cursor, or similar IDE/client MCP lists.
- A proper broker executable should default to stdio, because command-launched MCP clients normally communicate over stdio.
- `InSharpMcp.Broker` now provides the missing callable process. It defaults to stdio, supports HTTP mode, can be packed as a .NET tool with command name `insharp-mcp`, and has source-checkout MCP config examples in `README.md`.

## Core/Broker Naming
- The old `mcp/server/InSharpMcp` project name was confusing after adding `InSharpMcp.Broker`, because the runnable MCP server is the broker executable.
- The reusable library project is now `mcp/server/InSharpMcp.Core/InSharpMcp.Core.csproj`.
- The source namespace remains `InSharpMcp` for API continuity; the package/project identity is `InSharpMcp.Core`.

## MCP Stdio Protocol Regression
- The MCP server executable must keep stdout reserved for JSON-RPC protocol messages when running stdio transport.
- `StdioBrokerHost` previously allowed default .NET logging providers, which wrote startup/request logs to stdout and corrupted the MCP stdio stream.
- Reflection-based tool catalog tests were insufficient because they proved attributes existed but did not prove an IDE/client could complete `initialize` and `tools/list`.
- `StdioMcpProtocolTests` now launches the built `InSharpMcp.Broker` executable, performs MCP `initialize`, sends `notifications/initialized`, calls `tools/list`, asserts 20 tools are returned, and fails if any stdout line is not JSON.

## Demo Bridge Registration and Transport
- `InSharpMcp.Bridge` is the app-side package. Demos reference their framework adapter plus `InSharpMcp.Bridge`; the installable MCP server remains `InSharpMcp.Broker`.
- `InSharpMcp.Core` owns the broker-local named pipe listener and routes registered live app instances through `RemoteAppInstanceClient`.
- WinForms, Avalonia, and Uno demos start the Bridge by default during normal window startup and do not require `ISM_ENABLED`.
- Live MCP verification through the installed broker listed all three demos: `winforms-demo-32636`, `avalonia-demo-14124`, and `uno-demo-20012`.
- Live `ism_get_runtime_info` and `ism_visualtree_snapshot` succeeded for WinForms, Avalonia, and Uno through the broker-to-Bridge path.
- Live selector/wait/assertion checks succeeded: WinForms `ism_query_elements` matched `PrimaryActionButton`; Avalonia `ism_wait_for_element` matched `DemoMenu`; Uno `ism_assert_element_exists` matched `MainPage`.
- Screenshot remains routed through the selected local app instance.
- Demo cleanup killed the launched processes, then stale registrations were removed through the local broker pipe. A Windows PowerShell cleanup attempt using `System.Text.Json` failed because that type is not available in Windows PowerShell 5.1; `ConvertTo-Json` succeeded.

## Bridge Complexity Cleanup
- The local broker/app pipe DTOs and operation names now live once in `InSharpMcp.Contracts.LocalTransport`; Bridge and Core use the same types instead of duplicate record definitions.
- The Bridge now sends periodic heartbeat requests after registration. The broker updates `LastHeartbeatAt` and periodically expires stale app instances.
- Stale expiration now removes both the registry descriptor and the active `AppInstanceConnectionRegistry` entry, so expired instances stop routing as well as listing.
- The broker pipe now returns a structured failed response for malformed JSON instead of letting the handler task fault silently.
- `AddInSharpMcpBridge` now supports configuring `LocalBridgeOptions` through DI.
- Demos now share `AppBridgeCapabilities.Standard` instead of repeating the same capability list three times.
- Focused tests now cover heartbeat updates, unregister-on-dispose, stale connection removal, non-visual metadata routing through the Bridge, and malformed broker-pipe JSON.
- Release broker build is currently blocked by an active installed `InSharpMcp.Broker` process locking the Release output DLLs. The full server test suite and demo solution build pass.
