# InSharpMcp Demo Apps

The demo apps provide small, stable UI surfaces for manual adapter validation across the implemented Uno, Avalonia, and WinForms adapters.

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

The demos enable the InSharpMcp Bridge by default. Start `InSharpMcp.Broker` first, then start a demo. The demo registers itself with the broker over the local Bridge pipe, so `ism_list_instances` should show the running demo and inspection tools can target it.

The Bridge is app-side hosting code. The demos reference their framework adapter and `InSharpMcp.Bridge`; they do not reference broker internals from `InSharpMcp.Core`.

All demos register with `AppBridgeCapabilities.Standard`, which includes runtime info, visual tree inspection, metadata, DataContext metadata, screenshots where supported, inspectable accessibility metadata, input, default actions, property editing, and close support.

The demos are useful targets for `ism_set_element_property`: agents can set public element properties or direct DataContext properties on discovered element identifiers to validate debugging and test workflows. Property editing is a protected developer action, so validate changes with metadata, screenshots, events, or assertions after setting values.
