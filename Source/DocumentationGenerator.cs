namespace EmmyLuaDocxgen;

/// <summary>
/// Main documentation generator orchestrator
/// </summary>
internal sealed class DocumentationGenerator
{
    private readonly ILogger _logger;
    private readonly AssemblyLoader _assemblyLoader;
    private readonly TypeMapper _typeMapper;
    private readonly TypeGenerator _typeGenerator;
    private readonly LuaFileWriter _fileWriter;

    public TypeMapper TypeMapper => _typeMapper;
    public TypeGenerator TypeGenerator => _typeGenerator;

    public DocumentationGenerator(ILogger logger)
    {
        _logger = logger;
        _typeMapper = new TypeMapper(this);
        _typeGenerator = new TypeGenerator(this);
        _assemblyLoader = new AssemblyLoader(logger, _typeMapper);
        _fileWriter = new LuaFileWriter(logger);
    }

    public async Task GenerateAsync(GeneratorConfig config)
    {
        Directory.CreateDirectory(config.OutputDir);

        foreach (var assemblyConfig in config.Assemblies)
        {
            var result = _assemblyLoader.LoadAssembly(assemblyConfig);

            if (result is AssemblyLoadResult.Success success)
            {
                await _fileWriter.WriteLuaFilesAsync(success.AssemblyName, success.Types, config.OutputDir, _typeGenerator);
            }
        }

        await _fileWriter.WriteLuaGlobalAsync(config.OutputDir);
    }

    public IReadOnlyCollection<string> GetGeneratedFiles() => _fileWriter.GetGeneratedFiles();
}
