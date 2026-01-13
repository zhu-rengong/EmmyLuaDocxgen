using System.Text;

namespace EmmyLuaDocxgen;

/// <summary>
/// Represents a complete Lua source file
/// </summary>
public sealed record LuaFile : LuaNode
{
    public string AssemblyName { get; init; } = string.Empty;
    public LuaNamespace Namespace { get; init; } = default!;

    public override string Generate()
    {
        StringBuilder lines = new();
        lines.AppendLine("---@meta");
        lines.AppendLine($"---Auto-generated from {AssemblyName}");
        lines.AppendLine(Namespace.Generate());

        return lines.ToString();
    }
}

public sealed record LuaGlobal : LuaNode
{
    public List<Type> Types { get; init; } = [];

    public override string Generate()
    {
        StringBuilder lines = new();

        lines.AppendLine("---@meta");

        LuaNamespaceTree.ConstructNamespaceTree(Types, out string luaTable);
        lines.Append(luaTable);

        return lines.ToString();
    }
}
