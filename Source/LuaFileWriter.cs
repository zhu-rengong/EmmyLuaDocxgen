using System.Collections.Concurrent;
using System.Reflection;

namespace EmmyLuaDocxgen;

/// <summary>
/// Handles writing Lua files to disk
/// </summary>
internal sealed class LuaFileWriter
{
    private readonly ILogger _logger;
    private readonly ConcurrentBag<string> _generatedFiles = new();
    private readonly List<Type> _clrTypes = new(1024);

    public LuaFileWriter(ILogger logger)
    {
        _logger = logger;
    }

    public async Task WriteLuaFilesAsync(
        string assemblyName,
        List<Type> types,
        string outputDir,
        TypeGenerator typeGenerator)
    {
        var byNamespace = types.GroupBy(t => t.Namespace ?? string.Empty);

        var writeTasks = new List<Task>();

        foreach (var group in byNamespace)
        {
            var ns = group.Key;
            var typeList = group.ToList();
            _clrTypes.AddRange(typeList);

            writeTasks.Add(WriteLuaFileAsync(assemblyName, ns, typeList, outputDir, typeGenerator));
        }

        await Task.WhenAll(writeTasks);
    }

    private async Task WriteLuaFileAsync(
        string assemblyName,
        string ns,
        List<Type> types,
        string outputDir,
        TypeGenerator typeGenerator)
    {
        var luaFile = new LuaFile
        {
            AssemblyName = assemblyName,
            Namespace = new LuaNamespace
            {
                Name = ns,
                LuaTypes = types.Select(typeGenerator.GenerateFromReflection).ToList()
            }
        };

        var relativePath = string.IsNullOrEmpty(ns) ? "-" : ns.Replace('.', '/');
        var outputPath = Path.Combine(outputDir, assemblyName, $"{relativePath}.lua");
        var outputDirectory = Path.GetDirectoryName(outputPath);

        if (!string.IsNullOrEmpty(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        _logger.LogInfo($"Generating: {outputPath}");
        await File.WriteAllTextAsync(outputPath, luaFile.Generate());
        _generatedFiles.Add(outputPath);
    }

    public async Task WriteLuaGlobalAsync(string outputDir)
    {
        var luaGlobal = new LuaGlobal
        {
            Types = _clrTypes
        };

        var outputPath = Path.Combine(outputDir, $"global.lua");
        var outputDirectory = Path.GetDirectoryName(outputPath);

        if (!string.IsNullOrEmpty(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        _logger.LogInfo($"Generating: {outputPath}");
        await File.WriteAllTextAsync(outputPath, luaGlobal.Generate());
        _generatedFiles.Add(outputPath);
    }

    public IReadOnlyCollection<string> GetGeneratedFiles() => _generatedFiles;
}
