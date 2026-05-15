namespace InSharpMcp.Security;

public sealed record McpAccessOptions
{
    public bool RequireTokenForProtectedTools { get; init; } = true;

    public bool AllowUnauthenticatedHttp { get; init; }

    public string? SharedToken { get; init; }

    public IReadOnlySet<string> ProtectedTools { get; init; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "ism_get_screenshot",
        "ism_get_element_datacontext",
        "ism_pointer_click",
        "ism_key_press",
        "ism_type_text",
        "ism_element_peer_default_action",
        "ism_close",
    };
}
