namespace EmmyLuaDocxgen;

/// <summary>
/// Parser for delegate types (Action, Func, etc.)
/// </summary>
internal static class DelegateParser
{
    public static (List<LuaParameter> Parameters, string ReturnType) Parse(Type delegateType, TypeGenerator typeGenerator, TypeMapper typeMapper)
    {
        string returnType = "void";

        var invokeMethod = delegateType.GetMethod("Invoke");
        if (invokeMethod == null)
        {
            return ([], returnType);
        }

        var parameters = typeGenerator.GenerateParameters(invokeMethod);

        var returnLuaType = typeMapper.MapMethodReturnType(invokeMethod.ReturnType);
        returnType = returnLuaType;

        return (parameters, returnType);
    }
}
