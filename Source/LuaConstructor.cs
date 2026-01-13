using System.Text;

namespace EmmyLuaDocxgen;

/// <summary>
/// Represents a Lua constructor
/// </summary>
public sealed record LuaConstructor : LuaCallable
{
    public string ClassName { get; init; } = string.Empty;
    public LuaAccessModifier AccessModifier { get; init; } = LuaAccessModifier.Unknown;
    public List<LuaParameter> Parameters { get; init; } = [];
    public List<LuaOverloadFunction> Overloads { get; init; } = [];

    public override string Generate()
    {
        StringBuilder lines = new();

        // Access modifier annotation
        if (AccessModifier.IsAnnotationRequired())
        {
            lines.AppendLine($"---@{AccessModifier.ToName()}");
        }

        // Overload signatures
        foreach (var overload in Overloads)
        {
            lines.AppendLine(overload.Generate());
        }

        // Parameter annotations
        if (GenerateParamAnnotations(Parameters) is string { Length: > 0 } paramAnnotations)
        {
            lines.AppendLine(paramAnnotations);
        }

        // Return type annotation
        lines.AppendLine($"---@return {ClassName}");

        // Constructor declaration
        var paramsStr = GenerateParameterList(Parameters, forAnnotation: false);
        lines.Append($"function CS.{ClassName}({paramsStr}) end");

        return lines.ToString();
    }
}
