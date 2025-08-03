using OpenGM.IO;
using OpenGM.Loading;
using OpenGM.VirtualMachine;

namespace OpenGM.Tests;

public static class TestUtils
{
    public static object? ExecuteScript(string name, string asmFile)
    {
        var code = GameLoader.ConvertAssembly(asmFile);
        code.Name = name;

        // TODO: add functions properly

        VMExecutor.VerboseStackLogs = true;
        var result = VMExecutor.ExecuteCode(code, null);
        
        DebugLog.Log($"result = {result ?? "null value"} ({result?.GetType().ToString() ?? "null type"})");
        
        return result;
    }
}
