using System.Text;

namespace EmmyLuaDocxgen;

/// <summary>
/// Represents a Lua class
/// </summary>
public sealed record LuaClass : LuaType
{
    public string? BaseType { get; init; }
    public List<LuaField> Fields { get; init; } = [];
    public List<LuaOperator> Operators { get; init; } = [];
    public List<LuaMethod> Methods { get; init; } = [];
    public List<LuaConstructor> Constructors { get; init; } = [];

    public override string Generate()
    {
        var lines = new StringBuilder();

        // Class annotation
        lines.AppendLine($"---@class {Name}{(BaseType != null ? $": {BaseType}" : "")}");

        // Fields
        foreach (var field in Fields)
        {
            lines.AppendLine(field.Generate());
        }

        // Operators
        foreach (var luaOperator in Operators)
        {
            lines.AppendLine(luaOperator.Generate());
        }

        // Class table declaration
        lines.AppendLine($"CS.{Name} = {{}}");
        lines.AppendLine();

        // Methods
        foreach (var method in Methods)
        {
            lines.AppendLine(method.Generate());
        }

        // Constructors
        foreach (var ctor in Constructors)
        {
            lines.AppendLine(ctor.Generate());
        }

        return lines.ToString();
    }
}