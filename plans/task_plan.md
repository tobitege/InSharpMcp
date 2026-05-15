# Task Plan: InSharpMcp full implementation

## Goal
Fully implement the InSharpMcp integration plan in `plans/PLAN.md`, with coherent feature commits, verification evidence, and a clean final working tree.

## Current Phase
Phase 11: Demo apps for planned environments

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

### Phase 3: Uno Adapter MVP
- [x] Create `InSharpMcp.Adapters.Uno`
- [x] Implement `UnoUiDispatcher`
- [x] Implement bounded `UnoVisualTreeInspector`
- [x] Add visual tree and metadata tools
- [x] Add tests for limits, truncation, unsupported behavior, and queueing
- [x] Commit tested Uno MVP work
- **Status:** complete

### Phase 4: Screenshot and DataContext Metadata
- [x] Implement supported Windows screenshot capture and MCP image-content shape
- [x] Return explicit unsupported result for Desktop/Skia screenshot until validated
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
- [x] Map every `plans/PLAN.md` requirement to implementation or documented allowed unsupported/TBD behavior
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
- [ ] Create and build `demos/demo.uno`
- [ ] Create and build `demos/demo.avalonia`
- [ ] Create and build `demos/demo.winforms`
- [ ] Include simple menu, buttons, single-line input, editable text area, scrollable lorem ipsum, labels/text blocks, and adapter-specific controls in each demo
- [ ] Document run/build commands and any framework-specific validation notes
- [ ] Add demos to the solution or a documented solution structure
- [ ] Run focused demo builds and full `dotnet test mcp/server/InSharpMcp.sln`
- [ ] Commit coherent demo slices and finish with a clean working tree
- **Status:** in_progress

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

## Notes
- Re-read this file before significant implementation decisions.
- Update `plans/findings.md` after discoveries.
- Update `plans/progress.md` after implementation steps, tests, commits, and errors.
- Follow the repository 3-iteration rule and ask before changing approach after repeated failures.
