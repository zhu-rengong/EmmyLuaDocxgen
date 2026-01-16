using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace EmmyLuaDocxgen;

/// <summary>
/// Handles loading and processing assemblies
/// </summary>
internal sealed class AssemblyLoader
{
    private readonly ILogger _logger;
    private readonly TypeMapper _typeMapper;

    public AssemblyLoader(ILogger logger, TypeMapper typeMapper)
    {
        _logger = logger;
        _typeMapper = typeMapper;
    }

    public AssemblyLoadResult LoadAssembly(AssemblyConfig config)
    {
        var assemblyPath = Path.GetFullPath(config.Path);
        Console.WriteLine();
        _logger.LogInfo($"Loading assembly: {assemblyPath}");

        if (!File.Exists(assemblyPath))
        {
            _logger.LogWarning($"Assembly file not found: {assemblyPath}");
            return AssemblyLoadResult.NotFound;
        }

        Assembly? assembly;
        try
        {
            assembly = Assembly.LoadFrom(assemblyPath);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Loading assembly: {ex.Message}");
            LogLoaderExceptions(ex);
            return AssemblyLoadResult.Failed;
        }

        if (assembly == null)
        {
            return AssemblyLoadResult.Failed;
        }

        try
        {
            if (assembly.IsDefined(typeof(AssemblyMetadataAttribute)))
            {
                _logger.LogWarning($"This is a Assembly Metadata: {assembly}");

                if (assembly.GetName().Name == "System.Runtime")
                {
                    assembly = typeof(object).Assembly;
                    _logger.LogWarning($"Forward to: {assembly.Location}");
                }

            }

            var types = CollectTypes(assembly, config.Types);
            _logger.LogInfo($"Found {types.Count} types to generate");

            var assemblyName = assembly.GetName();
            return new AssemblyLoadResult.Success(assembly, types, assemblyName.Name ?? assemblyName.FullName);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Collecting types: {ex.Message}");
            return AssemblyLoadResult.Failed;
        }
    }

    private List<Type> CollectTypes(Assembly assembly, List<string> typeFilters)
    {

        var duplicates = typeFilters.GroupBy(tf => tf)
                       .Where(g => g.Count() > 1)
                       .Select(g => g.Key);

        if (duplicates.Any())
        {
            _logger.LogWarning($"Detected duplicate type filters:");
            foreach (string filter in duplicates)
            {
                _logger.LogWarning($"\"{filter}\"".Tab(4));
            }

            typeFilters = new HashSet<string>(typeFilters).ToList();
            _logger.LogWarning($"Removed duplicates!");
        }

        var types = assembly.GetTypes()
            .Where(t => !t.IsGenericType && !t.IsSpecialName
                && !TypeHelper.IsCompilerGenerated(t)
                && !t.IsDefined(typeof(GeneratedCodeAttribute), inherit: false));

        if (!typeFilters.Contains("*"))
        {
            types = types.Where(t =>
            {
                string typeQualifiedName = _typeMapper.GetQualifiedTypeName(t);
                foreach (string filter in typeFilters)
                {
                    if (filter.Contains('*')
                        ? typeQualifiedName.MatchesWildcard(filter)
                        : t == assembly.GetType(filter))
                    {
                        return true;
                    }
                }
                return false;
            });
        }

        return types.ToList();
    }

    private void LogLoaderExceptions(Exception ex)
    {
        if (ex is ReflectionTypeLoadException rtle && rtle.LoaderExceptions.Length > 0)
        {
            _logger.LogError("Loader exceptions:");
            foreach (var loaderEx in rtle.LoaderExceptions)
            {
                _logger.LogError($"{loaderEx?.Message ?? "Unknown error"}");
            }
        }
    }
}

/// <summary>
/// Result of assembly loading operation
/// </summary>
internal abstract record AssemblyLoadResult
{
    public static readonly AssemblyLoadResult NotFound = new NotFoundResult();
    public static readonly AssemblyLoadResult Failed = new FailedResult();

    public sealed record Success(Assembly Assembly, List<Type> Types, string AssemblyName) : AssemblyLoadResult;
    private sealed record NotFoundResult : AssemblyLoadResult;
    private sealed record FailedResult : AssemblyLoadResult;
}
