using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DELTARUNITYStandalone;

public static class DebugLog
{
	public static void Log(string message)
	{
		Console.WriteLine(message);
	}

	public static void Log(object item)
	{
		Log(item.ToString());
	}

	public static void LogWarning(string message)
	{
		Console.ForegroundColor = ConsoleColor.Yellow;
		Console.WriteLine(message);
		Console.ResetColor();
	}

	public static void LogError(string message)
	{
		Console.ForegroundColor = ConsoleColor.Red;
		Console.WriteLine(message);
		Console.ResetColor();
	}
}
