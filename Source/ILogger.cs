namespace EmmyLuaDocxgen;

/// <summary>
/// Simple logger interface
/// </summary>
internal interface ILogger
{
    void LogInfo(string message);
    void LogWarning(string message);
    void LogError(string message);
}

/// <summary>
/// Console-based logger implementation
/// </summary>
internal sealed class ConsoleLogger : ILogger
{
    private void WriteLineColored(string message, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    public void LogInfo(string message) => WriteLineColored(message, ConsoleColor.Green);
    public void LogWarning(string message) => WriteLineColored($"[Warning] {message}", ConsoleColor.Yellow);
    public void LogError(string message) => WriteLineColored($"[Error] {message}", ConsoleColor.Red);
}
