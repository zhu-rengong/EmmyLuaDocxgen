using System.Collections;
using System.Collections.Immutable;
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
    /// see: https://github.com/Tencent/xLua/blob/master/Assets/XLua/Doc/XLua_API.md#%E7%B1%BB%E5%9E%8B%E6%98%A0%E5%B0%84
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
        [typeof(nint)] = "integer",
        [typeof(nuint)] = "integer",
        [typeof(IntPtr)] = "integer",
        [typeof(UIntPtr)] = "integer"
    };

    public static ImmutableDictionary<Type, string> LuaCompatibleTypes => _luaCompatibleTypes.ToImmutableDictionary();

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

        CompositeType compositeType = new(8);
        compositeType.AddPart(qualifiedName, false);
        compositeType.AddPartsByCollectingFrom(type, this);

        return compositeType.GetMapResult();
    }
}

internal sealed class CompositeType
{
    public sealed record Part(string Name, bool EnhanceOpPrecIfNecessary);
    private bool isFun = false;

    private List<Part> _parts { get; init; }

    public CompositeType(int capacity)
    {
        _parts = new(capacity);
    }

    public void AddPart(string name, bool enhanceOpPrecIfNecessary)
    {
        _parts.Add(new(name, enhanceOpPrecIfNecessary));
    }

    public string StringRepresentation => _parts.Count > 1
        ? string.Join(" | ", _parts.Select(t => t.EnhanceOpPrecIfNecessary ? $"({t.Name})" : t.Name))
        : (_parts.Any() ? _parts[0].Name : string.Empty);

    public int Count => _parts.Count;

    public TypeMapper.MapResult GetMapResult() => new(StringRepresentation, isFun || Count > 1);

    public void AddPartsByCollectingFrom(Type type, TypeMapper typeMapper)
    {
        if (TypeMapper.LuaCompatibleTypes.TryGetValue(type, out var compatibleTypeName))
        {
            AddPart(compatibleTypeName, false);
        }

        // Handle indexers
        foreach (var iterT in type.GetInheritanceHierarchy())
        {
            foreach (var propertyInfo in iterT.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (propertyInfo.GetIndexParameters() is { Length: 1 } indexParams)
                {
                    string key = typeMapper.MapToLuaType(indexParams[0].ParameterType);
                    string value = typeMapper.MapToLuaType(propertyInfo.PropertyType);
                    AddPart($"{{ [{key}]: {value} }}", false);
                }
            }
        }

        // IEnumerable<T> -> { [nil]: T }
        if (type.ImplementsGenericInterface(typeof(IEnumerable<>), out var implIEnumerable))
        {
            AddPart($"{{ [nil]: {typeMapper.MapToLuaType(implIEnumerable.GetGenericArguments()[0])} }}", false);
            isFun = false;
        }

        // IEnumerator<T> -> { [nil]: T }
        if (type.ImplementsGenericInterface(typeof(IEnumerator<>), out var implIEnumerator))
        {
            AddPart($"{{ [nil]: {typeMapper.MapToLuaType(implIEnumerator.GetGenericArguments()[0])} }}", false);
            isFun = false;
        }
    }
}
