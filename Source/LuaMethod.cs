using System.Text;

namespace EmmyLuaDocxgen;

/// <summary>
/// Represents a Lua method
/// </summary>
public record LuaMethod : LuaCallable
{
    public string ClassName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public bool IsMethodCallWithImplicitSelf { get; init; }
    public bool IsAsync { get; init; }
    public LuaAccessModifier AccessModifier { get; init; } = LuaAccessModifier.Unknown;
    public List<LuaParameter> Parameters { get; init; } = [];
    public List<LuaOverloadFunction> Overloads { get; init; } = [];
    public string ReturnType { get; init; } = string.Empty;

    public override string Generate()
    {
        StringBuilder lines = new();

        // Access modifier annotation
        if (AccessModifier.IsAnnotationRequired())
        {
            lines.AppendLine($"---@{AccessModifier.ToName()}");
        }

        // Async annotation
        if (IsAsync)
        {
            lines.AppendLine("---@async");
        }

        // Overload methods\functions
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
        if (!string.IsNullOrEmpty(ReturnType) && ReturnType != "void")
        {
            lines.AppendLine($"---@return {ReturnType}");
        }

        // Function declaration
        var callNotation = IsMethodCallWithImplicitSelf ? ":" : ".";
        var paramsStr = GenerateParameterList(Parameters, forAnnotation: false);
        lines.AppendLine($"function CS.{ClassName}{callNotation}{Name}({paramsStr}) end");

        return lines.ToString();
    }
}