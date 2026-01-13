namespace EmmyLuaDocxgen;

/// <summary>
/// Configuration for the Lua documentation generator
/// </summary>
public sealed record GeneratorConfig
{
    public required List<AssemblyConfig> Assemblies { get; init; } = [];
    public string OutputDir { get; init; } = "output";
}

/// <summary>
/// Configuration for a single assembly to process
/// </summary>
public sealed record AssemblyConfig
{
    public required string Path { get; init; } = string.Empty;
    public required List<string> Types { get; init; } = [];
}
