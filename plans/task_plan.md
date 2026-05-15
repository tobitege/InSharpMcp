# Task Plan: InSharpMcp full implementation

## Goal
Fully implement the InSharpMcp integration plan in `plans/PLAN.md`, with coherent feature commits, verification evidence, and a clean final working tree.

## Current Phase
Phase 17: Rename core library for broker clarity

## Phases

### Phase 0: Setup and Orientation
- [x] Read `plans/PLAN.md`
- [x] Activate a validated Codex goal for the work
- [x] Initialize planning-with-files state under `plans`
- **Status:** complete

### Phase 1: Safe Foundation
- [x] Add package versions according to the selected package-management style
- [x] Create `InSharpMcp.Contracts`
- [x] Create `InSharpMcp`
- [x] Define shared result, limits, policy, and adapter contracts
- [x] Implement client limit parsing, validation, and clamping
- [x] Implement concurrency options and UI operation queue
- [x] Implement app registry, selector, and lifecycle primitives
- [x] Implement DI-based tools for instance listing and runtime info
- [x] Implement broker stdio/HTTP transport, authorization, and bounded concurrency
- [x] Implement app-side registration/unregistration and stale expiration
- [x] Add tests for discovery, registration, routing, authorization, limits, and runtime-info concurrency
- [x] Commit coherent safe-foundation slices as they pass verification
- **Status:** complete

### Phase 2: Adapter Contract Harness
- [x] Create shared adapter contract tests
- [x] Define framework-neutral expectations for lookup, tree output, screenshots, and input
- [x] Add fake/in-memory adapters for core MCP tests
- [x] Commit tested harness work
- **Status:** complete

### Phase 3: Uno Adapter
- [x] Create `InSharpMcp.Adapters.Uno`
- [x] Implement `UnoUiDispatcher`
- [x] Implement bounded `UnoVisualTreeInspector`
- [x] Add visual tree and metadata tools
- [x] Add tests for limits, truncation, structured unsupported behavior, and queueing
- [x] Commit tested Uno adapter work
- **Status:** complete

### Phase 4: Screenshot and DataContext Metadata
- [x] Implement supported Windows screenshot capture and MCP image-content shape
- [x] Return explicit unsupported result for Desktop/Skia screenshot until a validated backend path exists
- [x] Implement bounded non-recursive DataContext metadata
- [x] Add tests for redaction, caps, image/error shapes, and concurrency
- [x] Commit tested screenshot/DataContext work
- **Status:** complete

### Phase 5: Selectors, Waits, Accessibility, and Events
- [x] Implement structured JSON selectors and validation
- [x] Add `ism_query_elements`
- [x] Add bounded wait/retry support
- [x] Add bounded accessibility tree support where available
- [x] Add bounded redacted event log capture
- [x] Add tests for selector parsing, ordering, waits, bounds, and redaction
- [x] Commit tested automation-platform work
- **Status:** complete

### Phase 6: Interaction Tools
- [x] Implement Windows pointer/key/text input only through proven platform APIs
- [x] Return explicit unsupported result for unvalidated Desktop/Skia input
- [x] Implement automation peer default action through public patterns only
- [x] Add optional protected before/after screenshot trace capture
- [x] Add tests for auth, validation, unsupported paths, cancellation, serialization, and trace entries
- [x] Commit tested interaction work
- **Status:** complete

### Phase 7: Tracing and Assertions
- [x] Add bounded trace start/stop tools
- [x] Record selector resolution, waits, timings, errors, and optional screenshot references
- [x] Add assertion helpers for existence, visibility, enabled state, text, and value
- [x] Add tests for trace limits, redaction, assertion results, and cleanup
- [x] Commit tested tracing/assertion work
- **Status:** complete

### Phase 8: Additional Framework Adapters
- [x] Add Avalonia adapter only when a validating host is available
- [x] Add WinForms adapter only when a validating host is available
- [x] Run shared adapter contract tests against each implemented adapter
- [x] Document any validation-host blocker instead of adding unverified implementation
- [x] Commit tested adapter work or documented blocker state
- **Status:** complete

### Phase 9: Final Verification and Handoff
- [x] Map every `plans/PLAN.md` requirement to implementation or documented allowed structured unsupported behavior
- [x] Run final build/test suite
- [x] Confirm no assistant-started processes are left running
- [x] Confirm final git working tree is clean after final commit
- [x] Mark planning files complete
- **Status:** complete

### Phase 10: Review-critical Remediation
- [x] Add routed target dispatch for UI, screenshot, event-log, trace, assertion, and interaction tools
- [x] Add transport-aware authorization for protected tools, including HTTP request token extraction
- [x] Add an app-instance client/connection layer so selected registry instances map to executable adapter operations
- [x] Record trace entries from actual tool execution instead of requiring manual trace-store writes
- [x] Ensure `ism_close` runs through the selected app UI operation path
- [x] Fix Uno visual-tree limit enforcement for lookup and snapshot text caps
- [x] Add tests proving routing, ambiguity/stale errors, HTTP/stdio auth behavior, trace recording, close queueing, and Uno bounds
- [x] Run full verification and commit coherent remediation slices
- **Status:** complete

### Phase 11: Demo Apps for Planned Environments
- [x] Create and build `demos/demo.uno`
- [x] Create and build `demos/demo.avalonia`
- [x] Create and build `demos/demo.winforms`
- [x] Include simple menu, buttons, single-line input, editable text area, scrollable lorem ipsum, labels/text blocks, and adapter-specific controls in each demo
- [x] Document run/build commands and any framework-specific validation notes
- [x] Add demos to the solution or a documented solution structure
- [x] Run focused demo builds and full `dotnet test mcp/server/InSharpMcp.sln`
- [x] Commit coherent demo slices and finish with a clean working tree
- **Status:** complete

### Phase 12: Avalonia and WinForms Adapters
- [x] Create `InSharpMcp.Adapters.Avalonia`
- [x] Implement Avalonia app provider, UI dispatcher, visual tree inspector, screenshot provider, input/automation paths or explicit unsupported results, and DI extension
- [x] Create `InSharpMcp.Adapters.WinForms`
- [x] Implement WinForms app provider, UI dispatcher, tree inspector, screenshot provider, input/automation paths or explicit unsupported results, and DI extension
- [x] Add focused tests or contract checks for adapter behavior and limitations
- [x] Wire adapter projects into `mcp/server/InSharpMcp.sln` and central package management
- [x] Build each adapter, build demo solution, run full server tests
- [x] Update docs/planning summary and commit coherent slices
- **Status:** complete

### Phase 13: GitHub README and User Manual
- [x] Inventory available documentation and source surfaces
- [x] Extract the public integration, adapter, tool, security, and verification details
- [x] Create root `README.md` with clear GitHub structure and plain-language user guidance
- [x] Verify Markdown/source consistency and commit the documentation slice
- **Status:** complete

### Phase 14: Adapter Completeness Wording and Release Packaging
- [x] Treat Uno, Avalonia, and WinForms as first-class adapter packages in planning and docs
- [x] Remove misleading foundation-only wording from current plan/progress surfaces
- [x] Keep structured unsupported paths documented only where `plans/PLAN.md` requires a proven public platform path
- [x] Ensure NuGet package versions are owned by each packable adapter/core project
- [x] Ensure demo release packaging includes Uno, Avalonia, and WinForms demo builds
- **Status:** complete

### Phase 15: Public Input and Default-Action Wiring
- [x] Replace WinForms pointer/key/text unsupported stubs with native Windows input injection
- [x] Replace Avalonia pointer/key/text unsupported stubs with native Windows input injection from Avalonia screen coordinates
- [x] Replace Uno key/text unsupported stubs with native Windows input injection
- [x] Add Uno Windows pointer input through native HWND client-to-screen translation where available
- [x] Wire Avalonia default action invocation through public `ICommandSource.Command`
- [x] Wire Uno default action invocation through public `ButtonBase.Command`
- [x] Keep only backend/platform paths without a proven public implementation as structured `unsupported`
- [x] Add focused tests for non-destructive input forwarding and command invocation
- **Status:** complete

#### Remaining Intentional Structured Unsupported Paths
- Uno Desktop/Skia screenshot remains structured `unsupported` until a validated backend-specific screenshot path exists.
- Uno Desktop/Skia pointer click remains structured `unsupported` until a validated backend-specific screen-coordinate path exists.
- Uno default action invocation returns structured `unsupported` for elements that do not expose `ButtonBase.Command`.
- Avalonia default action invocation returns structured `unsupported` for elements that do not expose `ICommandSource.Command`.
- WinForms default action invocation returns structured `unsupported` for elements that do not expose `IButtonControl`.

### Phase 16: Callable MCP Broker Executable
- [x] Add an executable broker project that can be referenced from IDE MCP config
- [x] Default the executable to stdio MCP transport
- [x] Add CLI options for HTTP mode, token, loopback binding, and concurrency basics
- [x] Add the executable project to `mcp/server/InSharpMcp.sln`
- [x] Document Codex/Cursor-style MCP config examples in `README.md`
- [x] Build the broker executable and server solution
- **Status:** complete

### Phase 17: Rename Core Library for Broker Clarity
- [x] Rename folder `mcp/server/InSharpMcp` to `mcp/server/InSharpMcp.Core`
- [x] Rename project file `InSharpMcp.csproj` to `InSharpMcp.Core.csproj`
- [x] Update project references and solution membership
- [x] Update README and implementation summary wording to distinguish `InSharpMcp.Core` from `InSharpMcp.Broker`
- [x] Build/test after rename
- **Status:** complete

## Key Questions
1. Which package-management style does this repository currently use?
2. Which test framework is already present or most consistent with the repository?
3. Which parts of `ModelContextProtocol` can be exercised with in-memory or stdio tests in this repo?
4. Are Avalonia and WinForms validation hosts available, or must Phase 8 stop at documented blockers?

## Decisions Made
| Decision | Rationale |
|----------|-----------|
| Planning files live under `plans` | User explicitly requested planning-with-files within the `plans` folder. |
| Commit after coherent tested slices | User requested frequent commits for features/steps. |
| Treat Avalonia/WinForms as validation-gated | `plans/PLAN.md` says these adapters are added only when validation hosts are available. |
| Build real framework demos from installed templates | `dotnet new list` shows installed Uno, Avalonia, and WinForms templates, so demo scaffolding can be validated locally. |
| Implement Avalonia/WinForms now that demos exist | The previous validation gate is satisfied by the Phase 11 demo hosts. |
| Implement input only through native platform input APIs | Pointer/key/text paths now use Windows native input injection instead of fabricated framework events. |
| Implement default actions only through public command/button contracts | Avalonia uses `ICommandSource.Command`, Uno uses `ButtonBase.Command`, and WinForms uses `IButtonControl.PerformClick()`. |

## Errors Encountered
| Error | Attempt | Resolution |
|-------|---------|------------|
| `dotnet test --no-restore` failed after adding package references | 1 | Reran `dotnet test` with restore enabled; build and tests passed. |
| HTTP host wrapper used unavailable `RunAsync(CancellationToken)`/`WaitForShutdownAsync` shapes for `WebApplication` | 1 | Registered cancellation to call `StopAsync()` and awaited `RunAsync()` after configuring URLs. |
| xUnit template restore failed because versioned package references conflicted with central package management | 1 | Removed package versions from the new adapter contract test project before restore. |
| Uno adapter DI registration treated nullable `window.Content` as non-null | 1 | Added explicit content-root guard before constructing `UnoVisualTreeInspector`. |
| Visual-tree metadata test expected unclamped `MaxTextCharacters` | 1 | Updated the assertion to expect the configured policy minimum. |
| Desktop/Skia screenshot branch returned `Task<ScreenshotResult>` inside an async method | 1 | Returned `ScreenshotResult` directly from the non-Windows branch. |
| Accessibility tool test used a positional cancellation argument after a named argument | 1 | Made the cancellation argument named. |
| Final review found core plan gaps despite passing tests | 1 | Reopened the implementation with Phase 10 remediation focused on routed broker dispatch, auth, tracing, close queueing, and Uno bounds. |
| New goal could not be registered through the goal tool because the thread still reports an existing goal | 1 | Validated the new goal text at 2,196 characters and proceeded under the user request while recording Phase 11 in planning files. |
| New adapter goal could not be registered through the goal tool because the thread still reports an existing goal | 1 | Validated the new goal text at 1,976 characters and proceeded under the user request while recording Phase 12 in planning files. |
| Parallel adapter builds contended for `InSharpMcp.Contracts.dll` | 1 | Rebuilt adapters sequentially; both adapter projects passed with 0 warnings and 0 errors. |
| WinForms invoker initially targeted `ButtonBase.PerformClick()` | 1 | Switched to the public `IButtonControl.PerformClick()` contract. |
| Avalonia dispatcher async overload returned `Task<T>` directly | 1 | Removed the extra `GetTask()` from the async dispatcher overload. |
| New test code used a stale `UiTreeSnapshot` named parameter and an unshown WinForms button | 1 | Updated the parameter name and showed the form in the STA test before invoking the default action. |
| Native input injector method-group overload was ambiguous for `Enumerable.Select` | 1 | Replaced the method-group calls with explicit static lambdas. |
| Avalonia automation invoker missed the `Avalonia.Visual` namespace import | 1 | Added the required `Avalonia` namespace import and rebuilt successfully. |
| Broker command-line parser result used `Success` for both a record property and static factory | 1 | Renamed the static factory to `Ok`. |

## Notes
- Re-read this file before significant implementation decisions.
- Update `plans/findings.md` after discoveries.
- Update `plans/progress.md` after implementation steps, tests, commits, and errors.
- Follow the repository 3-iteration rule and ask before changing approach after repeated failures.
