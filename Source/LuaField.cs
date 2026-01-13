namespace EmmyLuaDocxgen;

/// <summary>
/// Represents a field in a Lua class
/// </summary>
public sealed record LuaField : LuaNode
{
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public LuaAccessModifier AccessModifier { get; init; } = LuaAccessModifier.Unknown;

    public override string Generate()
    {
        return AccessModifier.IsAnnotationRequired()
            ? $"---@field {AccessModifier.ToName()} {Name} {Type}"
            : $"---@field {Name} {Type}";
    }
}