using System.IO;
using System.Text;

namespace EmmyLuaDocxgen;

/// <summary>
/// Represents a namespace tree node
/// </summary>
public sealed class LuaNamespaceTree
{
    public string Name { get; }
    public SortedDictionary<string, LuaNamespaceTree> Children { get; }

    public LuaNamespaceTree(string name)
    {
        Name = name;
        Children = [];
    }

    public LuaNamespaceTree GetOrCreateChild(string name)
    {
        if (!Children.TryGetValue(name, out var child))
        {
            child = new LuaNamespaceTree(name);
            Children[name] = child;
        }
        return child;
    }

    /// <summary>
    /// Generate nested namespace table from type list
    /// </summary>
    public static LuaNamespaceTree ConstructNamespaceTree(List<Type> types, out string luaTable)
    {
        var root = new LuaNamespaceTree("CS");

        foreach (var type in types)
        {
            var tree = root;
            if (string.IsNullOrEmpty(type.Namespace)) { continue; }
            string[] nsSequence = type.Namespace.Split('.');

            foreach (string part in nsSequence)
            {
                tree = tree.GetOrCreateChild(part);
            }
        }

        luaTable = GenerateLuaTable(root, trail: true, deep: 0);

        return root;
    }

    private static string GenerateLuaTable(LuaNamespaceTree tree, bool trail, int deep)
    {
        StringBuilder lines = new();
        int indent = deep * 4;

        lines.Append($"{tree.Name} = {{".Tab(indent));

        if (tree.Children.Count > 0)
        {
            lines.AppendLine();
            int count = 0;
            foreach (var child in tree.Children.Values)
            {
                lines.Append(GenerateLuaTable(child, trail: ++count == tree.Children.Count, deep + 1));
            }
        }

        lines.AppendLine($"}}{(trail ? string.Empty : ",")}".Tab(tree.Children.Count > 0 ? indent : 0));

        return lines.ToString();
    }
}

/// <summary>
/// Represents a Lua namespace
/// </summary>
public sealed record LuaNamespace : LuaNode
{
    public string Name { get; init; } = string.Empty;
    public List<LuaType> LuaTypes { get; init; } = [];

    public override string Generate()
    {
        string header = $"---Namespace: {(string.IsNullOrEmpty(Name) ? "-" : Name)}";
        string content = string.Concat(LuaTypes.Select(t => $"\n{t.Generate()}"));
        StringBuilder lines = new(header.Length + content.Length + 32);

        lines.AppendLine(header);
        lines.Append(content);

        return lines.ToString();
    }
}