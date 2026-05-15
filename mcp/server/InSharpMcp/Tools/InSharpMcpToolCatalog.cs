using System.Reflection;
using ModelContextProtocol.Server;

namespace InSharpMcp.Tools;

public sealed class InSharpMcpToolCatalog
{
    public IReadOnlyCollection<string> ListToolNames() =>
        typeof(InSharpMcpTools)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Select(method => new
            {
                Method = method,
                Attribute = method.GetCustomAttribute<McpServerToolAttribute>(),
            })
            .Where(item => item.Attribute is not null)
            .Select(item => item.Attribute!.Name ?? item.Method.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
}
