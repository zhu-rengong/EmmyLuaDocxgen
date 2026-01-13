using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Xml.Linq;

namespace EmmyLuaDocxgen;

/// <summary>
/// Generates Lua type definitions from CLR types
/// </summary>
internal sealed class TypeGenerator
{
    private readonly DocumentationGenerator _docxgen;

    private static readonly BindingFlags AllBindingFlags =
        BindingFlags.Public | BindingFlags.NonPublic |
        BindingFlags.Instance | BindingFlags.Static |
        BindingFlags.DeclaredOnly;

    /// <summary>
    /// Lua reserved keywords that cannot be used as parameter names
    /// </summary>
    private static readonly HashSet<string> LuaKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "and", "break", "do", "else", "elseif", "end", "false", "for", "function", "if",
        "in", "local", "nil", "not", "or", "repeat", "return", "then", "true", "until", "while"
    };

    public TypeGenerator(DocumentationGenerator docxgen)
    {
        _docxgen = docxgen;
    }

    public LuaType GenerateFromReflection(Type type)
    {
        return type.IsEnum
            ? GenerateEnum(type)
            : GenerateClass(type);
    }

    private LuaEnum GenerateEnum(Type enumType)
    {
        var luaEnum = new LuaEnum
        {
            Name = _docxgen.TypeMapper.GetQualifiedTypeName(enumType)
        };

        var values = Enum.GetValues(enumType);
        var names = Enum.GetNames(enumType);

        for (int i = 0; i < names.Length; i++)
        {
            var value = Convert.ToInt32(values.GetValue(i));
            luaEnum.Members.Add(new LuaEnumMember(names[i], value.ToString()));
        }

        return luaEnum;
    }

    private LuaClass GenerateClass(Type clrType)
    {
        var baseTypes = CollectBaseTypes(clrType, out bool isPrimaryBaseUserdata);

        var luaClass = new LuaClass
        {
            Name = _docxgen.TypeMapper.GetQualifiedTypeName(clrType),
            BaseType = baseTypes.Count > 0 ? string.Join(", ", baseTypes) : null
        };

        GenerateFields(clrType, luaClass, findAllMembers: isPrimaryBaseUserdata);

        GenerateOperators(clrType, luaClass);

        GenerateMethods(clrType, luaClass, findAllMembers: isPrimaryBaseUserdata);

        GenerateGenericMethods(clrType, luaClass, findAllMembers: isPrimaryBaseUserdata);

        GenerateConstructors(clrType, luaClass);

        return luaClass;
    }

    private List<string> CollectBaseTypes(Type clrType, out bool isPrimaryBaseUserdata)
    {
        Type[] interfaces = clrType.GetInterfaces();
        var baseTypes = new List<string>(3 + interfaces.Length);

        var baseType = clrType.BaseType;
        isPrimaryBaseUserdata = false;

        if (baseType != null && baseType != typeof(object))
        {
            string name = _docxgen.TypeMapper.GetQualifiedTypeName(baseType);
            isPrimaryBaseUserdata = name == "userdata";
            baseTypes.Add(name);
        }
        else if (!clrType.IsInterface && clrType != typeof(object))
        {
            baseTypes.Add(_docxgen.TypeMapper.GetQualifiedTypeName(typeof(object)));
        }

        foreach (Type i in interfaces)
        {
            if (i.IsGenericType) { continue; }
            baseTypes.Add(_docxgen.TypeMapper.GetQualifiedTypeName(i));
        }

        if (TypeMapper.LuaCompatibleTypes.TryGetValue(clrType, out var compatibleType))
        {
            baseTypes.Add(compatibleType);
        }

        foreach (var propertyInfo in clrType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (propertyInfo.GetIndexParameters() is { Length: 1 } indexParams)
            {
                string key = _docxgen.TypeMapper.GetQualifiedTypeName(indexParams[0].ParameterType);
                string value = _docxgen.TypeMapper.GetQualifiedTypeName(propertyInfo.PropertyType);
                baseTypes.Add($"{{ [{key}]: {value} }}");
            }
        }

        return baseTypes;
    }

    private void GenerateFields(Type clrType, LuaClass luaClass, bool findAllMembers = false)
    {
        var types = findAllMembers ? TypeHelper.GetInheritanceHierarchy(clrType) : [clrType];

        HashSet<string> hidingProperties = [];

        foreach (PropertyInfo property in types.SelectMany(t => t.GetProperties(AllBindingFlags))
                                            .Where(p => p.GetIndexParameters().Length == 0))
        {
            if (TypeHelper.IsCompilerGenerated(property) || hidingProperties.Contains(property.Name)) continue;

            var field = GenerateField(TypeHelper.GetComplexMemberNameIfNecessary(property), property.PropertyType, property.AccessModifierMappingToLua());
            luaClass.Fields.Add(field);

            if ((property.GetMethod != null && TypeHelper.IsBaseDefMethod(property.GetMethod))
                || (property.SetMethod != null && TypeHelper.IsBaseDefMethod(property.SetMethod)))
            {
                hidingProperties.Add(property.Name);
            }
        }

        foreach (FieldInfo field in types.SelectMany(t => t.GetFields(AllBindingFlags)))
        {
            if (TypeHelper.IsCompilerGenerated(field)) continue;

            var luaField = GenerateField(TypeHelper.GetComplexMemberNameIfNecessary(field), field.FieldType, field.AccessModifierMappingToLua());
            luaClass.Fields.Add(luaField);
        }
    }

    private LuaField GenerateField(string name, Type fieldType, LuaAccessModifier accessModifier)
    {
        var field = new LuaField
        {
            Name = name,
            Type = _docxgen.TypeMapper.MapToLuaType(fieldType),
            AccessModifier = accessModifier
        };

        if (TypeHelper.IsDelegateType(fieldType))
        {
            var (delegateParams, returnType) = DelegateParser.Parse(fieldType, this, _docxgen.TypeMapper);
            field = field with
            {
                Type = GenerateFunctionType(delegateParams, returnType)
            };
        }

        return field;
    }

    private void GenerateOperators(Type clrType, LuaClass luaClass)
    {
        var methods = clrType.GetMethods(AllBindingFlags)
            .Where(method => method.IsSpecialName
                && !TypeHelper.IsGenericMethod(method)
                && !TypeHelper.IsCompilerGenerated(method));

        foreach (var method in methods)
        {
            Type[] parameterTypes = method.GetParameters()
                .Select(p => p.ParameterType)
                .ToArray();

            // As for EmmyLua for LuaLS, the first parameter of the class operator annotation defaults to "self"
            if (parameterTypes.Length < 1 || parameterTypes[0] != clrType)
            {
                continue;
            }

            LuaOperator? luaOperator = new()
            {
                ResultingType = _docxgen.TypeMapper.MapMethodReturnType(method.ReturnType)
            };

            luaOperator = method switch
            {
                { Name: "op_Addition" } => luaOperator with { Name = "add", Binary = true },
                { Name: "op_Subtraction" } => luaOperator with { Name = "sub", Binary = true },
                { Name: "op_Multiply" } => luaOperator with { Name = "mul", Binary = true },
                { Name: "op_Division" } => luaOperator with { Name = "div", Binary = true },
                { Name: "op_UnaryNegation" } => luaOperator with { Name = "unm", Binary = false },
                _ => null
            };

            if (luaOperator != null)
            {
                if (luaOperator.Binary)
                {
                    luaOperator = luaOperator with
                    {
                        InputType = _docxgen.TypeMapper.MapMethodReturnType(parameterTypes[1])
                    };
                }
                luaClass.Operators.Add(luaOperator);
            }
        }
    }

    private void GenerateMethods(Type clrType, LuaClass luaClass, bool findAllMembers = false)
    {
        var className = _docxgen.TypeMapper.GetQualifiedTypeName(clrType);

        var methodGroups = (findAllMembers ? TypeHelper.GetInheritanceHierarchy(clrType) : [clrType])
            .SelectMany(t => t.GetMethods(AllBindingFlags))
            .Where(method => !TypeHelper.IsGenericMethod(method) && !TypeHelper.IsCompilerGenerated(method))
            .GroupBy(method => new
            {
                Name = TypeHelper.GetSimpleMemberName(method),
                AccessModifier = method.AccessModifierMappingToLua(),
                IsMethodCallWithImplicitSelf = !method.IsStatic,
                IsAsync = TypeHelper.IsAsyncMethod(method)
            })
            .Select(group => new
            {
                group.Key.Name,
                group.Key.AccessModifier,
                group.Key.IsMethodCallWithImplicitSelf,
                group.Key.IsAsync,
                Overloads = group.ToList()
            });

        foreach (var methodGroup in methodGroups)
        {
            var primaryMethod = methodGroup.Overloads[0];
            string luaReturnType = _docxgen.TypeMapper.MapMethodReturnType(primaryMethod.ReturnType);
            var parameters = GenerateParameters(primaryMethod);

            var luaMethod = new LuaMethod
            {
                ClassName = className,
                Name = methodGroup.Name,
                IsMethodCallWithImplicitSelf = methodGroup.IsMethodCallWithImplicitSelf,
                IsAsync = methodGroup.IsAsync,
                AccessModifier = methodGroup.AccessModifier,
                Parameters = parameters,
                ReturnType = luaReturnType
            };

            luaMethod.Overloads.AddRange(methodGroup.Overloads.Skip(1).Select(overloadMethod =>
            {
                return new LuaOverloadFunction
                {
                    IsMethodCallWithImplicitSelf = !overloadMethod.IsStatic,
                    Parameters = GenerateParameters(overloadMethod),
                    ReturnType = _docxgen.TypeMapper.MapMethodReturnType(overloadMethod.ReturnType)
                };
            }));

            luaClass.Methods.Add(luaMethod);
        }
    }

    private void GenerateGenericMethods(Type clrType, LuaClass luaClass, bool findAllMembers = false)
    {
        var className = _docxgen.TypeMapper.GetQualifiedTypeName(clrType);
        
        var genericMethods = (findAllMembers ? TypeHelper.GetInheritanceHierarchy(clrType) : [clrType])
            .SelectMany(t => t.GetMethods(AllBindingFlags))
            .Where(method => TypeHelper.IsSupportedGenericMethod(method) && !TypeHelper.IsCompilerGenerated(method));

        foreach (var method in genericMethods)
        {
            string luaReturnType = _docxgen.TypeMapper.MapMethodReturnType(method.ReturnType);
            var genericParameters = GenerateGenericParameters(method);

            var luaMethod = new LuaGenericMethod
            {
                ClassName = className,
                Name = TypeHelper.GetSimpleMemberName(method),
                IsMethodCallWithImplicitSelf = !method.IsStatic,
                IsAsync = TypeHelper.IsAsyncMethod(method),
                AccessModifier = method.AccessModifierMappingToLua(),
                GenericParameters = genericParameters,
                Parameters = GenerateParameters(method),
                ReturnType = luaReturnType
            };

            luaClass.Methods.Add(luaMethod);
        }
    }

    private void GenerateConstructors(Type clrType, LuaClass luaClass)
    {
        string className = _docxgen.TypeMapper.GetQualifiedTypeName(clrType);
        var ctors = clrType.GetConstructors(AllBindingFlags).ToList();

        if (ctors.Count == 0) return;

        var primaryCtor = ctors[0];
        var luaCtor = new LuaConstructor
        {
            ClassName = className,
            AccessModifier = primaryCtor.AccessModifierMappingToLua(),
            Parameters = GenerateParameters(primaryCtor)
        };

        // Add overload signatures
        luaCtor.Overloads.AddRange(ctors.Skip(1).Select(overloadCtor => new LuaOverloadFunction
        {
            IsMethodCallWithImplicitSelf = false,
            Parameters = GenerateParameters(overloadCtor),
            ReturnType = className
        }));

        luaClass.Constructors.Add(luaCtor);
    }

    public List<LuaParameter> GenerateParameters(MethodBase method)
    {
        return method.GetParameters()
            .Select(p =>
            {
                var isVariadic = p.IsDefined(typeof(ParamArrayAttribute), inherit: false);
                var paramType = isVariadic
                    ? _docxgen.TypeMapper.MapToLuaTypeForVariadic(p.ParameterType)
                    : _docxgen.TypeMapper.MapToLuaType(p.ParameterType);

                return new LuaParameter(
                    EscapeLuaKeyword(p.Name ?? "arg"),
                    paramType,
                    p.IsOptional,
                    isVariadic
                );
            })
            .ToList();
    }

    /// <summary>
    /// Escapes Lua keywords if the name is a reserved keyword
    /// </summary>
    private static string EscapeLuaKeyword(string name)
    {
        if (LuaKeywords.Contains(name))
        {
            return $"__{name}__";
        }
        return name;
    }

    public List<LuaGenericParameter> GenerateGenericParameters(MethodInfo method)
    {
        if (!TypeHelper.IsSupportedGenericMethod(method)) { return []; }

        return method.GetGenericArguments()
            .Select(genericArg =>
            {
                Type[] constraints = genericArg.GetGenericParameterConstraints();

                string? constraint = null;
                if (constraints.Length > 0)
                {
                    constraint = string.Join(", ", constraints.Select(_docxgen.TypeMapper.GetQualifiedTypeName));
                }
                else if (genericArg.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint))
                {
                    constraint = _docxgen.TypeMapper.GetQualifiedTypeName(typeof(object));
                }

                return new LuaGenericParameter(genericArg.Name, constraint);
            })
            .ToList();
    }

    private static string GenerateFunctionType(List<LuaParameter> parameters, string returnType)
    {
        var paramStr = LuaCallable.GenerateParameterList(parameters, forAnnotation: true);
        var returnStr = returnType == "void" ? "" : $": {returnType}";
        return $"fun({paramStr}){returnStr}";
    }
}
