using OpenGM.VirtualMachine;

namespace OpenGM.Tests;

public static class TestUtils
{
	public static object? ExecuteScript(string name, string asmFile)
	{
		var script = GameConverter.ConvertScript(asmFile.Replace("\t", ""));
		script.Name = name;

		var verboseStackLogs = VMExecutor.VerboseStackLogs;
		VMExecutor.VerboseStackLogs = true;
		var result = VMExecutor.ExecuteScript(script, null);
		VMExecutor.VerboseStackLogs = verboseStackLogs;
		
		DebugLog.Log($"result = {result ?? "null value"} ({result?.GetType().ToString() ?? "null type"})");
		
		return result;
	}
}
