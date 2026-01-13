using System.Reflection;

namespace EmmyLuaDocxgen;

/// <summary>
/// Lua access modifiers
/// </summary>
public enum LuaAccessModifier
{
    Unknown,
    Public,
    Private,
    Protected,
    Package
}

/// <summary>
/// Extension methods for Lua access modifiers
/// </summary>
public static class LuaAccessModifierExtensions
{
    private static readonly Dictionary<LuaAccessModifier, string> AccessModifierNames = new()
    {
        [LuaAccessModifier.Public] = "public",
        [LuaAccessModifier.Private] = "private",
        [LuaAccessModifier.Protected] = "protected",
        [LuaAccessModifier.Package] = "package",
        [LuaAccessModifier.Unknown] = string.Empty
    };

    public static string ToName(this LuaAccessModifier accessModifier) =>
        AccessModifierNames[accessModifier];

    public static bool IsAnnotationRequired(this LuaAccessModifier accessModifier) =>
        accessModifier is LuaAccessModifier.Private or LuaAccessModifier.Protected or LuaAccessModifier.Package;

    public static LuaAccessModifier AccessModifierMappingToLua(this MemberInfo member) =>
        member switch
        {
            PropertyInfo { GetMethod: not null } property => property.GetMethod!.AccessModifierMappingToLua(),
            PropertyInfo { SetMethod: not null } property => property.SetMethod!.AccessModifierMappingToLua(),
            FieldInfo field => MapFieldAccessModifier(field),
            MethodBase method => MapMethodAccessModifier(method),
            _ => LuaAccessModifier.Unknown
        };

    private static LuaAccessModifier MapFieldAccessModifier(FieldInfo field) =>
        field switch
        {
            _ when field.IsPublic => LuaAccessModifier.Public,
            _ when field.IsPrivate => LuaAccessModifier.Private,
            _ when field.IsFamily => LuaAccessModifier.Protected,
            _ when field.IsAssembly => LuaAccessModifier.Package,
            _ => LuaAccessModifier.Unknown
        };

    private static LuaAccessModifier MapMethodAccessModifier(MethodBase method) =>
        method switch
        {
            _ when method.IsPublic => LuaAccessModifier.Public,
            _ when method.IsPrivate => LuaAccessModifier.Private,
            _ when method.IsFamily => LuaAccessModifier.Protected,
            _ when method.IsAssembly => LuaAccessModifier.Package,
            _ => LuaAccessModifier.Unknown
        };
}