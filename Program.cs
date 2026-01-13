using System.Text.Json;

namespace EmmyLuaDocxgen;

/// <summary>
/// EmmyLua Documentation Generator - Generates Lua type annotations from .NET assemblies
/// </summary>
public static class Program
{
    private const int SuccessExitCode = 0;
    private const int ErrorExitCode = 1;

    public static async Task<int> Main(string[] args)
    {
        if (args.Length < 1)
        {
            PrintUsage();
            return ErrorExitCode;
        }

        try
        {
            var configPath = args[0];
            var config = await LoadConfigAsync(configPath);

            var logger = new ConsoleLogger();
            var generator = new DocumentationGenerator(logger);

            await generator.GenerateAsync(config);

            var fullPath = Path.GetFullPath(config.OutputDir);
            var generatedFiles = generator.GetGeneratedFiles();
            logger.LogInfo($"Documentation generated successfully in: {fullPath}");
            logger.LogInfo($"Total files generated: {generatedFiles.Count}");

            return SuccessExitCode;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return ErrorExitCode;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine(@"EmmyLua Documentation Generator
==============================

Usage:
  EmmyLuaDocxgen <config-file>

Config file format (JSON):
{
  ""assemblies"": [
    {
      ""path"": ""path/to/assembly.dll"",
      ""types"": [""Namespace.Type1"", ""Namespace.Type2"", ""*""]
    }
  ],
  ""outputDir"": ""output""
}

Examples:
  EmmyLuaDocxgen config.json
  EmmyLuaDocxgen ./config.json

Type filters:
  ""*""           - Generate all types
  ""Namespace.*"" - Generate all types in namespace
  ""Full.Type""   - Generate specific type only");
    }

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static async Task<GeneratorConfig> LoadConfigAsync(string configPath)
    {
        var json = await File.ReadAllTextAsync(configPath);

        return JsonSerializer.Deserialize<GeneratorConfig>(json, JsonSerializerOptions)
            ?? throw new InvalidOperationException("Invalid config file: deserialization returned null");
    }
}