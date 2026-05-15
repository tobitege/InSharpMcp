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

## Issues Encountered
| Issue | Resolution |
|-------|------------|
| First test command used `--no-restore` after adding new package references, causing missing package namespaces. | Reran `dotnet test` with restore enabled. |
| Initial HTTP host wrapper used `WebApplication` async overloads unavailable in this target shape. | Switched to configured URLs plus cancellation-triggered `StopAsync()` and `RunAsync()`. |
| Tool catalog initially assumed SDK tool attribute names are non-null. | Added method-name fallback for nullable `Name`. |

## Resources
- `plans/PLAN.md`
- `plans/task_plan.md`
- `plans/progress.md`

## Verification Notes
- `dotnet test mcp/server/InSharpMcp.sln` passed with 25 tests after completing Phase 1 verification coverage.
