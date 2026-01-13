namespace EmmyLuaDocxgen;

/// <summary>
/// Base record for all Lua type definitions
/// </summary>
public abstract record LuaType : LuaNode
{
    public string Name { get; init; } = string.Empty;
}
