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

## Technical Decisions
| Decision | Rationale |
|----------|-----------|
| Use the existing repository structure where possible before adding new solution files | The implementation should match repo conventions rather than inventing structure prematurely. |
| Start with Phase 1 safe-foundation slices | Later adapters and tools depend on shared contracts, limits, registry, auth, concurrency, and host structure. |
| Commit planning files as the first step if tests/build discovery does not reveal an immediate blocker | The user requested frequent commits, and planning state is a coherent setup step. |

## Issues Encountered
| Issue | Resolution |
|-------|------------|

## Resources
- `plans/PLAN.md`
- `plans/task_plan.md`
- `plans/progress.md`

## Verification Notes
- Pending repository discovery and initial build/test command identification.
