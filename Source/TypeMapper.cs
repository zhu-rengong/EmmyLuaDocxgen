using System.Reflection;
using System.Text;

namespace EmmyLuaDocxgen;

/// <summary>
/// Provides type mapping services for CLR to Lua type conversion
/// </summary>
internal sealed class TypeMapper
{
    public static readonly Dictionary<Type, string> LuaCompatibleTypes = new()
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

    private readonly Dictionary<Type, string> _typeCache = new();
    private readonly Dictionary<Type, string> _qualifiedNameCache = new();
    private readonly DocumentationGenerator _docxgen;

    public TypeMapper(DocumentationGenerator docxgen)
    {
        _docxgen = docxgen;
    }

    public string MapToLuaType(Type type)
    {
        if (_typeCache.TryGetValue(type, out var cachedType))
        {
            return cachedType;
        }

        var result = MapTypeInternal(type);
        _typeCache[type] = result;
        return result;
    }

    public string MapToLuaTypeForVariadic(Type type)
    {
        if (type.IsArray)
        {
            return MapToLuaType(type.GetElementType()!);
        }
        return MapToLuaType(type);
    }

    public string MapMethodReturnType(Type type)
    {
        if (type == typeof(void))
        {
            return "void";
        }

        return type.IsGenericType ? "userdata" : MapTypeInternal(type);
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

    private string MapTypeInternal(Type type)
    {
        // Handle generic method params
        if (type.IsGenericMethodParameter)
        {
            return type.Name;
        }

        // Handle generic types
        if (type.IsGenericType)
        {
            return MapGenericType(type);
        }

        // Handle arrays
        if (type.IsArray)
        {
            return $"{MapTypeInternal(type.GetElementType()!)}[]";
        }

        // Handle ref types
        if (type.IsByRef)
        {
            return MapTypeInternal(type.GetElementType()!);
        }

        // Handle enums
        if (type.IsEnum)
        {
            return GetQualifiedTypeName(type);
        }

        // Handle delegates
        if (TypeHelper.IsDelegateType(type))
        {
            var (parameters, returnType) = DelegateParser.Parse(type, _docxgen.TypeGenerator, this);
            return GenerateFunctionType(parameters, returnType);
        }

        return GetQualifiedTypeName(type);
    }

    private string MapGenericType(Type type)
    {
        var genericDef = type.GetGenericTypeDefinition();
        var genericArgs = type.GetGenericArguments();

        // List<T> -> T[]
        if (genericDef == typeof(List<>))
        {
            return $"{MapToLuaType(genericArgs[0])}[]";
        }

        // Dictionary<K, V> -> {[K]: V}
        if (genericDef == typeof(Dictionary<,>))
        {
            var keyType = MapToLuaType(genericArgs[0]);
            var valueType = MapToLuaType(genericArgs[1]);
            return $"{{ [{keyType}]: {valueType} }}";
        }

        if (genericDef == typeof(Nullable<>))
        {
            return $"{MapToLuaType(genericArgs[0])}|nil";
        }

        // All other generic types map to userdata
        return "userdata";
    }

    private static string GenerateFunctionType(List<LuaParameter> parameters, string returnType)
    {
        var paramStr = string.Join(", ", parameters.Select(p => $"{p.Name}: {p.Type}"));
        var returnStr = returnType == "void" ? "" : $": {returnType}";
        return $"fun({paramStr}){returnStr}";
    }
}
