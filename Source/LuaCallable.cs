using System.Text;

namespace EmmyLuaDocxgen;

/// <summary>
/// Represents a generic parameter in a Lua function/method
/// </summary>
public sealed record LuaGenericParameter(string Name, string? Constraint);

/// <summary>
/// Represents a parameter in a Lua function/method
/// </summary>
public sealed record LuaParameter(string Name, string Type, bool IsOptional = false, bool IsVariadic = false);

/// <summary>
/// Base record for callable Lua constructs (methods, constructors)
/// </summary>
public abstract record LuaCallable : LuaNode
{
    /// <summary>
    /// Generates parameter list string for Lua function declarations
    /// </summary>
    public static string GenerateParameterList(IReadOnlyList<LuaParameter> parameters, bool forAnnotation = false)
    {
        return string.Join(", ", parameters.Select(p =>
        {
            if (forAnnotation)
            {
                return p switch
                {
                    { IsVariadic: true } => $"...: {p.Type}",
                    { IsOptional: true } => $"{p.Name}?: {p.Type}",
                    _ => $"{p.Name}: {p.Type}"
                };
            }
            return p.IsVariadic ? "..." : p.Name;
        }));
    }

    /// <summary>
    /// Generates parameter annotations for Lua function declarations
    /// </summary>
    public static string GenerateParamAnnotations(IReadOnlyList<LuaParameter> parameters)
    {
        StringBuilder lines = new();

        for (int i = 0; i < parameters.Count; i++)
        {
            LuaParameter param = parameters[i];
            string annotation = param.IsOptional
                ? $"---@param {param.Name}? {param.Type}"
                : (
                    param.IsVariadic
                        ? $"---@param ... {param.Type}"
                        : $"---@param {param.Name} {param.Type}"
                );

            if (i == parameters.Count - 1)
            {
                lines.Append(annotation);
            }
            else
            {
                lines.AppendLine(annotation);
            }
        }

        return lines.ToString();
    }

    /// <summary>
    /// Generates generic annotations for Lua function declarations
    /// </summary>
    public static string GenerateGenericAnnotations(IReadOnlyList<LuaGenericParameter> genericParameters)
    {
        StringBuilder lines = new();

        for (int i = 0; i < genericParameters.Count; i++)
        {
            LuaGenericParameter param = genericParameters[i];
            string annotation = param.Constraint is not null
                ? $"---@generic {param.Name} : {param.Constraint}"
                : $"---@generic {param.Name}";

            if (i == genericParameters.Count - 1)
            {
                lines.Append(annotation);
            }
            else
            {
                lines.AppendLine(annotation);
            }
        }

        return lines.ToString();
    }
}
