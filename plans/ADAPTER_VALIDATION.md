# Adapter Validation Status

## Current Status

All planned adapters are implemented and have validating hosts or build targets:

- Uno adapter: `mcp/server/InSharpMcp.Adapters.Uno`
- Avalonia adapter: `mcp/server/InSharpMcp.Adapters.Avalonia`
- WinForms adapter: `mcp/server/InSharpMcp.Adapters.WinForms`
- Uno demo host: `demos/demo.uno`
- Avalonia demo host: `demos/demo.avalonia`
- WinForms demo host: `demos/demo.winforms`

The Avalonia and WinForms validation gate is resolved. Both demo projects reference and register their adapter services. The Uno adapter builds against its planned target frameworks.

## Implemented Coverage

- Shared adapter contract tests: `mcp/server/tests/InSharpMcp.AdapterContractTests`
- Avalonia focused tests: `mcp/server/tests/InSharpMcp.Adapters.Avalonia.Tests`
- WinForms focused tests: `mcp/server/tests/InSharpMcp.Adapters.WinForms.Tests`
- Demo solution covering Uno, Avalonia, and WinForms: `demos/InSharpMcp.Demos.slnx`

## Current Unsupported Paths

These are the current intentional structured `unsupported` paths:

- Uno Desktop/Skia screenshot remains unsupported until a validated backend-specific screenshot path exists.
- Uno Desktop/Skia pointer click remains unsupported until a validated backend-specific screen-coordinate path exists.
- Uno default action returns `unsupported` for elements that do not expose `ButtonBase.Command`.
- Avalonia default action returns `unsupported` for elements that do not expose `ICommandSource.Command`.
- WinForms default action returns `unsupported` for elements that do not expose `IButtonControl`.

Pointer, key, and text input are no longer broad adapter stubs. The implemented paths use native Windows input APIs where the adapter has a validated platform route.

## Verification

Current verification command:

```powershell
dotnet test mcp/server/InSharpMcp.sln
```

Current result:

- `InSharpMcp.Tests`: 55 passed
- `InSharpMcp.AdapterContractTests`: 9 passed
- `InSharpMcp.Adapters.Avalonia.Tests`: 3 passed
- `InSharpMcp.Adapters.WinForms.Tests`: 5 passed
- Total: 72 passed

Demo build command:

```powershell
dotnet build demos/InSharpMcp.Demos.slnx
```
