using System.Text;

namespace EmmyLuaDocxgen;

/// <summary>
/// Represents a member of a Lua enum
/// </summary>
public sealed record LuaEnumMember(string Name, string Value);

/// <summary>
/// Represents a Lua enum type
/// </summary>
public sealed record LuaEnum : LuaType
{
    public List<LuaEnumMember> Members { get; init; } = [];

    public override string Generate()
    {
        var members = string.Join(",\n", Members.Select(m => $"{m.Name} = {m.Value}".Tab(4)));
        StringBuilder lines = new();
        lines.AppendLine($"---@enum {Name}");
        lines.AppendLine($"CS.{Name} = {{");
        lines.AppendLine(members);
        lines.AppendLine($"}}");
        return lines.ToString();
    }
}