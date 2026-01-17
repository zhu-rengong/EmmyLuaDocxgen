using System.Collections;
using System.Reflection;
using System.Text;

namespace EmmyLuaDocxgen;

/// <summary>
/// Provides type mapping services for CLR to Lua type conversion
/// </summary>
internal sealed class TypeMapper
{
    public const string Void = "";

    /// <summary>
    /// see: https://www.moonsharp.org/mapping.html#auto-conversion-of-clr-types-to-moonsharp-types
    /// </summary>
    private static readonly Dictionary<Type, string> _luaCompatibleTypes = new()
    {
        [typeof(object)] = "userdata",
        [typeof(bool)] = "boolean",
        [typeof(sbyte)] = "integer",
        [typeof(short)] = "integer",
        [typeof(int)] = "integer",
        [typeof(long)] = "integer",
        [typeof(byte)] = "integer",
        [typeof(ushort)] = "integer",
        [typeof(uint)] = "integer",
        [typeof(ulong)] = "integer",
        [typeof(float)] = "number",
        [typeof(double)] = "number",
        [typeof(decimal)] = "number",
        [typeof(char)] = "string",
        [typeof(string)] = "string",
        [typeof(System.Text.StringBuilder)] = "string",
        [typeof(nint)] = "integer",
        [typeof(nuint)] = "integer",
        [typeof(IntPtr)] = "integer",
        [typeof(UIntPtr)] = "integer"
    };

    public sealed record MapResult(string Name, bool EnhanceOpPrecIfNecessary);

    private readonly Dictionary<Type, MapResult> _mapCache = new();
    private readonly Dictionary<Type, string> _qualifiedNameCache = new();
    private readonly DocumentationGenerator _docxgen;

    public TypeMapper(DocumentationGenerator docxgen)
    {
        _docxgen = docxgen;
    }

    public string MapToLuaType(Type type, bool considerOpPrec = false)
    {
        if (!_mapCache.TryGetValue(type, out var result))
        {
            result = MapTypeInternal(type);
        }

        _mapCache[type] = result;

        return considerOpPrec && result.EnhanceOpPrecIfNecessary ? $"({result.Name})" : result.Name;
    }

    public string GetQualifiedTypeName(Type type)
    {
        if (_qualifiedNameCache.TryGetValue(type, out var cachedName))
        {
            return cachedName;
        }

        return _qualifiedNameCache[type] = type.IsGenericType
            ? "userdata"
            : GetQualifiedTypeNameInternal(type);
    }

    private static string GetQualifiedTypeNameInternal(Type type)
    {
        var typeInfo = type.GetTypeInfo();
        List<string> names = new(8);

        if (typeInfo.Namespace is string ns)
        {
            names.AddRange(ns.Split('.'));
        }

        Type? decl = typeInfo.DeclaringType;
        Stack<Type> types = [];
        while (decl != null)
        {
            types.Push(decl);
            decl = decl.DeclaringType;
        }
        while (types.Count > 0)
        {
            decl = types.Pop();
            names.Add(decl.Name);
        }

        names.Add(typeInfo.Name);

        return string.Join('.', names);
    }

    private MapResult MapTypeInternal(Type type)
    {
        if (type == typeof(void))
        {
            return new(Void, false);
        }

        // Handle generic method params
        if (type.IsGenericMethodParameter)
        {
            return new(type.Name, false);
        }

        // Handle enums
        if (type.IsEnum)
        {
            return new(GetQualifiedTypeName(type), false);
        }

        // Handle arrays
        if (type.IsArray)
        {
            return new($"{MapToLuaType(type.GetElementType()!, considerOpPrec: true)}[]", false);
        }

        // Handle ref types
        if (type.IsByRef)
        {
            return new(MapToLuaType(type.GetElementType()!), false);
        }

        // Handle delegates
        if (TypeHelper.IsDelegateType(type))
        {
            var invokeMethod = type.GetMethod("Invoke");
            List<LuaParameter>? parameters;
            string? returnType;
            if (invokeMethod != null)
            {
                parameters = _docxgen.TypeGenerator.GenerateParameters(invokeMethod, considerOpPrec: true);
                returnType = MapToLuaType(invokeMethod.ReturnType);
            }
            else
            {
                parameters = [];
                returnType = MapToLuaType(typeof(void));
            }

            var paramStr = LuaCallable.GenerateParameterList(parameters, forAnnotation: true);
            var returnStr = returnType == Void ? "" : $": {returnType}";
            return new($"fun({paramStr}){returnStr}", true);
        }

        // Handle nullable types
        if (Nullable.GetUnderlyingType(type) is Type underlyingType)
        {
            return new($"{MapToLuaType(underlyingType, considerOpPrec: true)}|nil", true);
        }

        string qualifiedName = GetQualifiedTypeName(type);
        if (qualifiedName != "userdata")
        {
            return new(qualifiedName, false);
        }

        List<string> compositeName = new(8) { qualifiedName };

        AddCompositeTypes(type, compositeName, out bool isNecessaryToEnhanceOpPrec);

        return new(string.Join(" | ", compositeName), isNecessaryToEnhanceOpPrec || compositeName.Count > 1);
    }

    public void AddCompositeTypes(Type type, List<string> compositeName, out bool isNecessaryToEnhanceOpPrec)
    {
        isNecessaryToEnhanceOpPrec = false;

        if (_luaCompatibleTypes.TryGetValue(type, out var compatibleType))
        {
            compositeName.Add(compatibleType);
        }

        // Handle indexers
        foreach (var iterT in type.GetInheritanceHierarchy())
        {
            foreach (var propertyInfo in iterT.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (propertyInfo.GetIndexParameters() is { Length: 1 } indexParams)
                {
                    string key = _docxgen.TypeMapper.GetQualifiedTypeName(indexParams[0].ParameterType);
                    string value = _docxgen.TypeMapper.GetQualifiedTypeName(propertyInfo.PropertyType);
                    compositeName.Add($"{{ [{key}]: {value} }}");
                }
            }
        }

        // IEnumerable<T> -> fun(): T
        if (type.ImplementsGenericInterface(typeof(IEnumerable<>), out var implIEnumerable))
        {
            compositeName.Add($"fun(): {MapToLuaType(implIEnumerable.GetGenericArguments()[0])}");
            isNecessaryToEnhanceOpPrec = true;
        }

        // IEnumerator<T> -> fun(): T
        if (type.ImplementsGenericInterface(typeof(IEnumerator<>), out var implIEnumerator))
        {
            compositeName.Add($"fun(): {MapToLuaType(implIEnumerator.GetGenericArguments()[0])}");
            isNecessaryToEnhanceOpPrec = true;
        }
    }
}
