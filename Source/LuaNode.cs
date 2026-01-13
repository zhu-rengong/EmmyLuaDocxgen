namespace EmmyLuaDocxgen;

/// <summary>
/// Base interface for all Lua AST nodes
/// </summary>
public interface ILuaNode
{
    /// <summary>
    /// Generates the Lua code representation of this node
    /// </summary>
    string Generate();
}

/// <summary>
/// Abstract base class for Lua nodes providing common functionality
/// </summary>
public abstract record LuaNode : ILuaNode
{
    public abstract string Generate();
}