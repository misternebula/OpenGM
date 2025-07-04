namespace OpenGM.IO;

public static class DebugLog
{
    public static LogType Verbosity = LogType.Info;

    public static void Log(string message)
    {
        if (Verbosity < LogType.Info)
        {
            return;
        }

        Console.WriteLine(message);
    }

    public static void LogWarning(string message)
    {
        if (Verbosity < LogType.Warning)
        {
            return;
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    public static void LogInfo(string message)
    {
        if (Verbosity < LogType.Info)
        {
            return;
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    public static void LogError(string message)
    {
        if (Verbosity < LogType.Error)
        {
            return;
        }

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    public enum LogType
    {
        Error = 0,
        Warning = 1,
        Info = 2
    }
}
