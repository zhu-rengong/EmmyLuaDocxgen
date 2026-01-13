using System.Reflection;

namespace EmmyLuaDocxgen;

/// <summary>
/// Handles loading and processing assemblies
/// </summary>
internal sealed class AssemblyLoader
{
    private readonly ILogger _logger;

    public AssemblyLoader(ILogger logger)
    {
        _logger = logger;
    }

    public AssemblyLoadResult LoadAssembly(AssemblyConfig config)
    {
        var assemblyPath = Path.GetFullPath(config.Path);
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
        return typeFilters
            .Select(filter => {
                if (assembly.GetType(filter) is Type type) { return type; }
                _logger.LogWarning($"Not found {filter}");
                return null;
            })
            .OfType<Type>()
            .ToList();
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
