# Task Plan: InSharpMcp full implementation

## Goal
Fully implement the InSharpMcp integration plan in `plans/PLAN.md`, with coherent feature commits, verification evidence, and a clean final working tree.

## Current Phase
Phase 5 - Selectors, Waits, Accessibility, and Events

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
- [ ] Implement structured JSON selectors and validation
- [ ] Add `ism_query_elements`
- [ ] Add bounded wait/retry support
- [ ] Add bounded accessibility tree support where available
- [ ] Add bounded redacted event log capture
- [ ] Add tests for selector parsing, ordering, waits, bounds, and redaction
- [ ] Commit tested automation-platform work
- **Status:** in_progress

### Phase 6: Interaction Tools
- [ ] Implement Windows pointer/key/text input only through proven platform APIs
- [ ] Return explicit unsupported result for unvalidated Desktop/Skia input
- [ ] Implement automation peer default action through public patterns only
- [ ] Add optional protected before/after screenshot trace capture
- [ ] Add tests for auth, validation, unsupported paths, cancellation, serialization, and trace entries
- [ ] Commit tested interaction work
- **Status:** pending

### Phase 7: Tracing and Assertions
- [ ] Add bounded trace start/stop tools
- [ ] Record selector resolution, waits, timings, errors, and optional screenshot references
- [ ] Add assertion helpers for existence, visibility, enabled state, text, and value
- [ ] Add tests for trace limits, redaction, assertion results, and cleanup
- [ ] Commit tested tracing/assertion work
- **Status:** pending

### Phase 8: Additional Framework Adapters
- [ ] Add Avalonia adapter only when a validating host is available
- [ ] Add WinForms adapter only when a validating host is available
- [ ] Run shared adapter contract tests against each implemented adapter
- [ ] Document any validation-host blocker instead of adding unverified implementation
- [ ] Commit tested adapter work or documented blocker state
- **Status:** pending

### Phase 9: Final Verification and Handoff
- [ ] Map every `plans/PLAN.md` requirement to implementation or documented allowed unsupported/TBD behavior
- [ ] Run final build/test suite
- [ ] Confirm no assistant-started processes are left running
- [ ] Confirm final git working tree is clean after final commit
- [ ] Mark planning files complete
- **Status:** pending

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

## Errors Encountered
| Error | Attempt | Resolution |
|-------|---------|------------|
| `dotnet test --no-restore` failed after adding package references | 1 | Reran `dotnet test` with restore enabled; build and tests passed. |
| HTTP host wrapper used unavailable `RunAsync(CancellationToken)`/`WaitForShutdownAsync` shapes for `WebApplication` | 1 | Registered cancellation to call `StopAsync()` and awaited `RunAsync()` after configuring URLs. |
| xUnit template restore failed because versioned package references conflicted with central package management | 1 | Removed package versions from the new adapter contract test project before restore. |
| Uno adapter DI registration treated nullable `window.Content` as non-null | 1 | Added explicit content-root guard before constructing `UnoVisualTreeInspector`. |
| Visual-tree metadata test expected unclamped `MaxTextCharacters` | 1 | Updated the assertion to expect the configured policy minimum. |
| Desktop/Skia screenshot branch returned `Task<ScreenshotResult>` inside an async method | 1 | Returned `ScreenshotResult` directly from the non-Windows branch. |

## Notes
- Re-read this file before significant implementation decisions.
- Update `plans/findings.md` after discoveries.
- Update `plans/progress.md` after implementation steps, tests, commits, and errors.
- Follow the repository 3-iteration rule and ask before changing approach after repeated failures.
