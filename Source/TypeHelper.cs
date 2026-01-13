using System.Data;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace EmmyLuaDocxgen;

/// <summary>
/// Helper methods for type introspection and reflection
/// </summary>
internal static class TypeHelper
{
    public static bool IsCompilerGenerated(MemberInfo member) =>
        member.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false);

    public static bool IsCompilerGenerated(Type? type)
    {
        if (type == null) { return false; }
        return type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false) || IsCompilerGenerated(type.DeclaringType);
    }

    public static bool IsGenericMethod(MethodInfo method) =>
        method.IsGenericMethodDefinition || method.ContainsGenericParameters;

    public static bool IsSupportedGenericMethod(MethodInfo method)
    {
        if (!IsGenericMethod(method)) { return false; }

        var genericArgs = method.GetGenericArguments();
        var methodParameters = method.GetParameters();
        var parameterTypes = methodParameters.Select(p => p.ParameterType).ToList();

        // LuaLS doesn't support type parameter very well
        if (parameterTypes.Any(p => p.IsGenericTypeParameter)) { return false; }

        // Check all generic arguments:
        // 1. Must have class constraint
        // 2. Must appear in method parameter list
        foreach (Type genericArg in genericArgs)
        {
            if (genericArg.IsGenericTypeParameter)
            {
                return false;
            }

            if (!genericArg.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint)
                && (
                    genericArg.GetGenericParameterConstraints() is { Length: 0 } constraints)
                    || genericArg.GetGenericParameterConstraints().Any(t => !t.IsClass)
                )
            {
                return false;
            }

            if (!parameterTypes.Any(t => t.IsGenericMethodParameter && t.Name == genericArg.Name))
            {
                return false;
            }
        }

        return true;
    }

    public static bool IsAsyncMethod(MethodInfo method) =>
        typeof(Task).IsAssignableFrom(method.ReturnType) ||
        method.CustomAttributes.Any(a => a.AttributeType == typeof(AsyncStateMachineAttribute));

    public static bool IsDelegateType(Type type)
    {
        if (type == typeof(Delegate) || type == typeof(MulticastDelegate))
        {
            return false;
        }

        return type.IsSubclassOf(typeof(Delegate));
    }

    public static IEnumerable<Type> GetInheritanceHierarchy(Type type)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            yield return current;
        }
    }

    public static string GetSimpleMemberName(MemberInfo member)
    {
        var name = member.Name;
        var lastDot = name.LastIndexOf('.');
        return lastDot > 0 ? name.Substring(lastDot + 1) : name;
    }

    public static string GetComplexMemberNameIfNecessary(MemberInfo member)
    {
        var name = member.Name;
        if ("<.>".Any(name.Contains))
        {
            return @$"[""{member.Name}""]";
        }

        return member.Name;
    }
}
