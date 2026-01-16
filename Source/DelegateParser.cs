namespace EmmyLuaDocxgen;

/// <summary>
/// Parser for delegate types (Action, Func, etc.)
/// </summary>
internal static class DelegateParser
{
    public static (List<LuaParameter> Parameters, string ReturnType) Parse(Type delegateType, TypeGenerator typeGenerator, TypeMapper typeMapper)
    {
        var invokeMethod = delegateType.GetMethod("Invoke");
        if (invokeMethod == null)
        {
            return ([], typeMapper.MapToLuaType(typeof(void)));
        }

        var parameters = typeGenerator.GenerateParameters(invokeMethod, takeOperatorPrecedenceIntoAccount: true);
        string returnType = typeMapper.MapToLuaType(invokeMethod.ReturnType);

        return (parameters, returnType);
    }
}
