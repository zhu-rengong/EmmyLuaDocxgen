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

        lines.AppendLine($"do");
        
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
        lines.AppendLine($"local __ctor = function({paramsStr}) end");
        lines.AppendLine($"CS.{ClassName} = __ctor");
        lines.AppendLine($"CS.{ClassName}.__new = __ctor");
        lines.Append($"end");

        return lines.ToString();
    }
}
