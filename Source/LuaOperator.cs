namespace EmmyLuaDocxgen;

/// <summary>
/// Provides declarations for operator
/// </summary>
public sealed record LuaOperator : LuaNode
{
    public string Name { get; init; } = string.Empty;
    public bool Binary { get; init; }
    public string InputType { get; init; } = string.Empty;
    public string ResultingType { get; init; } = string.Empty;

    public override string Generate()
    {
        return Binary
            ? $"---@operator {Name}({InputType}): {ResultingType}"
            : $"---@operator {Name}: {ResultingType}";
    }
}