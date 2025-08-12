using OpenGM.VirtualMachine;

namespace OpenGM.IO;

public static class DebugLog
{
    public static LogType Verbosity = LogType.Info;

    public static void Log(string message, LogType? type = null)
    {
        if (Verbosity < (type ?? LogType.Info))
        {
            return;
        }

        Action<string> logFunc = type switch
        {
            LogType.Error => LogError,
            LogType.Warning => LogWarning,
            LogType.Info => LogInfo,
            LogType.Verbose => LogVerbose,
            _ => Console.WriteLine
        };

        logFunc.Invoke(message);
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

    public static void LogVerbose(string message)
    {
        if (Verbosity < LogType.Verbose)
        {
            return;
        }

        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    public static void PrintCallStack(LogType? type = LogType.Verbose)
    {
        Log($"--Stacktrace--", type);
        foreach (var item in VMExecutor.CallStack)
        {
            Log($" - {item.CodeName}", type);
        }
    }

    public enum LogType
    {
        Error = 0,
        Warning = 1,
        Info = 2,
        Verbose = 3
    }
}
