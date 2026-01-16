using System.Text;

namespace EmmyLuaDocxgen;

/// <summary>
/// Represents a Lua method
/// </summary>
public sealed record LuaGenericMethod : LuaMethod
{
    public List<LuaGenericParameter> GenericParameters { get; init; } = [];

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

        // Generic annotations
        if (GenerateGenericAnnotations(GenericParameters) is string { Length: > 0 } genericAnnotations)
        {
            lines.AppendLine(genericAnnotations);
        }

        // Parameter annotations
        if (GenerateParamAnnotations(Parameters) is string { Length: > 0 } paramAnnotations)
        {
            lines.AppendLine(paramAnnotations);
        }

        // Return type annotation
        if (!string.IsNullOrEmpty(ReturnType) && ReturnType != TypeMapper.Void)
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