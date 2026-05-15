# Adapter Validation Status

## Phase 8 Decision

`plans/PLAN.md` makes Avalonia and WinForms adapters validation-gated:

- Add Avalonia adapter only when an Avalonia host can validate behavior.
- Add WinForms adapter only when a WinForms host can validate behavior.

Repository discovery found no Avalonia or WinForms demo/host project. The only matches for Avalonia/WinForms are repository instructions and the integration plan itself.

## Current Outcome

- Avalonia adapter: not added yet because no validating Avalonia host is available.
- WinForms adapter: not added yet because no validating WinForms host is available.
- Shared adapter contract tests are available in `mcp/server/tests/InSharpMcp.AdapterContractTests`.
- The implemented Uno adapter builds against its planned target frameworks.

## Next Required Input

To implement either additional adapter, add or provide a validating host project for that framework. The adapter should then be implemented against the shared contracts and run through the shared adapter contract tests.
