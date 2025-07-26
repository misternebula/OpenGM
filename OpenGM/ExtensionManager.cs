using MemoryPack;
using OpenGM.IO;
using OpenGM.VirtualMachine;

namespace OpenGM;

public static class ExtensionManager
{
    public static List<Extension> Extensions = new();

    public static void Init()
    {
        foreach (var extension in Extensions)
        {
            foreach (var file in extension.Files)
            {
                if (file.Kind != ExtensionKind.Dll)
                {
                    continue;
                }

                foreach (var func in file.Functions)
                {
                    ScriptResolver.BuiltInFunctions.Add(func.Name, (object?[] args) => {
                        DebugLog.LogWarning($"Extension function {func.Name} stubbed out.");
                        return null;
                    });
                }
            }
        }
    }
}

[MemoryPackable]
public partial class Extension
{
    public string Name = null!;
    public string Version = null!;
    public List<ExtensionFile> Files = new();
}

[MemoryPackable]
public partial class ExtensionFile
{
    public string Name = null!;
    public int CleanupScript = -1;
    public int InitScript = -1;
    public ExtensionKind Kind;
    public List<ExtensionFunction> Functions = new();
}

[MemoryPackable]
public partial class ExtensionFunction
{
    public uint Id;
    public string Name = null!;
    public ExtensionVarType ReturnType;
    public List<ExtensionVarType> Arguments = new();
}

public enum ExtensionVarType : uint
{
    String = 1,
    Double = 2
}

public enum ExtensionKind : uint
{
    Unknown0 = 0,
    Dll = 1,
    GML = 2,
    ActionLib = 3,
    Generic = 4,
    Js = 5
}