# InSharpMcp Demo Apps

The demo apps provide small, stable UI surfaces for manual adapter validation across the three planned environments from `plans/PLAN.md`.

## Projects

| Environment | Project | Primary build command |
|-------------|---------|-----------------------|
| Uno | `demos/demo.uno/InSharpMcp.Demo.Uno/InSharpMcp.Demo.Uno.csproj` | `dotnet build demos/demo.uno/InSharpMcp.Demo.Uno/InSharpMcp.Demo.Uno.csproj -f net9.0-desktop` |
| Avalonia | `demos/demo.avalonia/InSharpMcp.Demo.Avalonia.csproj` | `dotnet build demos/demo.avalonia/InSharpMcp.Demo.Avalonia.csproj` |
| WinForms | `demos/demo.winforms/InSharpMcp.Demo.WinForms.csproj` | `dotnet build demos/demo.winforms/InSharpMcp.Demo.WinForms.csproj` |

Build all demos together:

```powershell
dotnet build demos/InSharpMcp.Demos.slnx
```

Run individual demos:

```powershell
dotnet run --project demos/demo.uno/InSharpMcp.Demo.Uno/InSharpMcp.Demo.Uno.csproj -f net9.0-desktop
dotnet run --project demos/demo.avalonia/InSharpMcp.Demo.Avalonia.csproj
dotnet run --project demos/demo.winforms/InSharpMcp.Demo.WinForms.csproj
```

## Common Control Surface

Each demo includes:

- simple menu
- primary and secondary buttons
- single-line text input
- editable multiline text area
- scrollable lorem ipsum text area
- labels/text blocks
- framework-specific selection/progress controls
- stable control names or automation IDs for selector and accessibility validation

## MCP Notes

MCP remains disabled by default. These apps are demo hosts for validating adapter behavior manually; enable MCP integration only in explicit host wiring so normal app startup remains unchanged.
