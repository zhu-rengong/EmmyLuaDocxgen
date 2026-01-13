namespace EmmyLuaDocxgen;

/// <summary>
/// Represents an overload signature for a Lua function/method
/// </summary>
public sealed record LuaOverloadFunction : LuaNode
{
    public bool IsMethodCallWithImplicitSelf { get; init; }
    public List<LuaParameter> Parameters { get; init; } = [];
    public string ReturnType { get; init; } = string.Empty;

    public override string Generate()
    {
        var paramParts = new List<string>(2);

        if (IsMethodCallWithImplicitSelf)
        {
            paramParts.Add("self: self");
        }

        if (LuaCallable.GenerateParameterList(Parameters, forAnnotation: true) is string { Length: > 0 } paramsList)
        {
            paramParts.Add(paramsList);
        }

        var paramsStr = string.Join(", ", paramParts);
        var returnStr = ReturnType switch
        {
            null or "void" => "",
            "self" => ": self",
            _ => $": {ReturnType}"
        };

        return $"---@overload fun({paramsStr}){returnStr}";
    }
}