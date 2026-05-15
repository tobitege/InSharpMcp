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
- Protected/privacy-sensitive operations require authorization when HTTP is enabled.
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
- Protected interaction tools now authorize through `McpAuthorization`, validate input before dispatch, run through the UI queue where applicable, and record interaction event-log entries.
- Trace start/stop uses a bounded in-memory trace store and assertion helpers return structured pass/fail `AssertionResult` data without throwing for normal failures.
- Phase 8 found no Avalonia or WinForms validation host in the repository. Per `plans/PLAN.md`, those adapters are not added until a validating host is available.
- Final verification passed with 56 tests. `plans/IMPLEMENTATION_SUMMARY.md` maps the implemented scope and validation-gated scope.
- Follow-up review found the prior completion state was premature: most tools bypass target selection, HTTP authorization is not transport-aware, registered endpoints are not mapped to executable app operations, traces are not populated by real tool execution, `ism_close` bypasses the queue, and Uno lookup/text limits are incomplete.
- Tool entrypoints now route through `AppInstanceRouter` and `IAppInstanceClient`, which maps selected registry descriptors to executable app operations and returns `ambiguous_target` or `stale_instance` before dispatch.
- Protected tools now use `McpRequestAuthorizationResolver`, which derives HTTP vs stdio context and extracts bearer/header/query tokens for HTTP requests.
- Protected tools authorize before target selection, so unauthenticated callers cannot use protected operations to probe registered or ambiguous targets.
- Trace and event recording now happens around actual selected tool execution; `ism_start_trace`/`ism_stop_trace` are target-scoped.
- Uno visual-tree traversal now uses a shared `NodeVisitBudget` so node limits are consumed globally across sibling branches, and snapshot node metadata uses the caller's text limit instead of default limits.
- New demo-app goal targets the three planned environments in `plans/PLAN.md`: `demos/demo.uno`, `demos/demo.avalonia`, and `demos/demo.winforms`.
- Installed .NET templates include `unoapp`, `avalonia.app`, and `winforms`, so each demo can start from a framework-native template.
- `demos/InSharpMcp.Demos.slnx` builds all three demo projects. The first aggregate build failed because Uno SDK version resolution could not see the nested `demo.uno/global.json`; setting the Uno demo project SDK to `Uno.Sdk/6.5.33` fixed the solution-level build.
- The new adapter goal targets `InSharpMcp.Adapters.Avalonia` and `InSharpMcp.Adapters.WinForms`, which can now be validated because Phase 11 added buildable demo hosts for both frameworks.
- `InSharpMcp.Adapters.Avalonia` now builds against Avalonia 11.3.9 and provides UI dispatcher marshalling, bounded visual-tree inspection, DataContext metadata, screenshot capture for measured controls, app close, accessibility-tree delegation, and explicit unsupported results for unsafe input/automation paths.
- `InSharpMcp.Adapters.WinForms` now builds for `net8.0-windows` and provides control-tree inspection, Tag-based DataContext metadata, `DrawToBitmap` screenshot capture, app close, accessibility-tree delegation, explicit unsupported pointer/key/text input, and default action invocation through `IButtonControl.PerformClick()`.
- Avalonia and WinForms demos now reference their adapter projects and register adapter services at startup, giving compile-time integration coverage for the two new adapter packages.
- The full server test suite now includes adapter-specific test projects and passes with 69 tests.

## Technical Decisions
| Decision | Rationale |
|----------|-----------|
| Use the existing repository structure where possible before adding new solution files | The implementation should match repo conventions rather than inventing structure prematurely. |
| Start with Phase 1 safe-foundation slices | Later adapters and tools depend on shared contracts, limits, registry, auth, concurrency, and host structure. |
| Commit planning files as the first step if tests/build discovery does not reveal an immediate blocker | The user requested frequent commits, and planning state is a coherent setup step. |
| Scaffold a new .NET solution using the `mcp/server` structure from `plans/PLAN.md` | No existing project structure is present, and the plan specifies the target layout. |
| Use central package management | The repository is greenfield and central package management keeps project files versionless as requested by `plans/PLAN.md`. |
| Keep broker host classes as thin startup wrappers around SDK hosting APIs | Transport behavior should stay in the official SDK while InSharpMcp owns policy, registry, and adapter routing services. |
| Use reflection over `InSharpMcpTools` for the initial tool catalog test | It verifies SDK attributes and `ism_` tool names without starting a long-lived MCP server process. |
| Put fake adapters in `InSharpMcp.AdapterContractTests` first | The harness validates contract expectations without adding test-only helpers to production packages. |
| Leave Uno screenshot, input, and automation peer invocation as explicit unsupported results until their later plan phases | Phase 3 only requires dispatcher, visual-tree inspector, metadata, and unsupported-path behavior; later phases add screenshot/DataContext/input details. |
| Put DataContext reflection in contracts instead of the Uno adapter | The bounding/redaction behavior is framework-independent and can be tested without a live UI. |
| Implement selector matching over framework-neutral snapshots | It keeps the selector grammar independent from any adapter and makes query/wait behavior testable without a live UI. |
| Use event-log entries as the initial trace surface for interaction tools | It provides auditable interaction result entries before the dedicated trace start/stop phase. |
| Keep assertion helpers result-oriented rather than exception-oriented | Normal assertion failures are expected tool outcomes and should return structured data. |
| Document the Avalonia/WinForms validation blocker instead of adding unvalidated adapters | `plans/PLAN.md` explicitly gates those adapters on available validation hosts. |
| Reopen implementation as Phase 10 instead of rewriting history | The workspace is clean and committed; remediation should be additive, tested, and committed in coherent slices. |
| Add demos as Phase 11 | The prior phases are complete; demo apps are a separate delivery slice requested by the new goal. |
| Add Avalonia/WinForms adapters as Phase 12 | The demo hosts satisfy the plan's validation gate for these framework adapters. |
| Keep Avalonia pointer/key/text input and automation invocation unsupported for now | The plan requires proven platform APIs; the current implementation avoids fabricated input events and returns explicit `unsupported` results. |
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
| Review found missing routed app execution and auth integration. | Added Phase 10 remediation scope before code changes. |
| Parallel adapter builds locked the shared contracts output. | Rebuilt sequentially and kept later verification commands sequential. |
| WinForms `PerformClick()` test did not fire before the form was visible. | Updated the STA test to show the form and pump events before invoking the default action. |

## Resources
- `plans/PLAN.md`
- `plans/task_plan.md`
- `plans/progress.md`

## Verification Notes
- `dotnet test mcp/server/InSharpMcp.sln` passed with 25 tests after completing Phase 1 verification coverage.
- `dotnet test mcp/server/InSharpMcp.sln` passed with 34 tests after adding Phase 2 adapter contract harness.
- `dotnet test mcp/server/InSharpMcp.sln` passed with 36 tests after adding the Uno adapter MVP and visual-tree/metadata tools.
- `dotnet test mcp/server/InSharpMcp.sln` passed with 42 tests after adding screenshot tool shape and DataContext metadata redaction/cap tests.
- `dotnet test mcp/server/InSharpMcp.sln` passed with 48 tests after adding selectors, wait, accessibility, and event-log tooling.
- `dotnet test mcp/server/InSharpMcp.sln` passed with 53 tests after adding interaction tools.
- `dotnet test mcp/server/InSharpMcp.sln` passed with 56 tests after adding trace start/stop and assertion helpers.
- Final `dotnet test mcp/server/InSharpMcp.sln` passed with 56 tests.
- `dotnet test mcp/server/InSharpMcp.sln` passed with 62 tests after routed tool dispatch, transport-aware auth, trace recording, and close queue regression coverage.
- `dotnet test mcp/server/InSharpMcp.sln` passed with 63 tests after fixing Uno traversal/text-limit handling and adding `NodeVisitBudget` coverage.
- `dotnet test mcp/server/InSharpMcp.sln` passed with 64 tests after proving protected tools authorize before target selection.
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
