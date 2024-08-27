using OpenGM.IO;
using OpenGM.Loading;
using OpenGM.VirtualMachine;

namespace OpenGM.Tests;

public static class TestUtils
{
	public static object? ExecuteScript(string name, string asmFile)
	{
		var script = GameConverter.ConvertScript(asmFile);
		script.Name = name;

		if (script.IsGlobalInit)
		{
			ScriptResolver.GlobalInitScripts.Add(script);
		}
		else
		{
			ScriptResolver.Scripts.Add(script.Name, script);
		}

		foreach (var func in script.Functions)
		{
			ScriptResolver.ScriptFunctions.Add(func.FunctionName, (script, func.InstructionIndex));
		}

		VMExecutor.VerboseStackLogs = true;
		var result = VMExecutor.ExecuteScript(script, null);
		
		DebugLog.Log($"result = {result ?? "null value"} ({result?.GetType().ToString() ?? "null type"})");
		
		return result;
	}
}
