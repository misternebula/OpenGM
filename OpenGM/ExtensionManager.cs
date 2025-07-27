using MemoryPack;
using OpenGM.IO;
using OpenGM.VirtualMachine;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;

namespace OpenGM;

public static class ExtensionManager
{
    public static List<Extension> Extensions = new();
    public static Dictionary<string, IntPtr> _nativeLibs = new();

    // https://stackoverflow.com/a/26700515
    // this is what lets us make delegates from a Type[] array
    // final element is return type, all elements before are argument types
    private static readonly Func<Type[],Type> MakeNewCustomDelegate = (Func<Type[],Type>)Delegate.CreateDelegate(
        typeof(Func<Type[], Type>),
        typeof(Expression)
            .Assembly
            .GetType("System.Linq.Expressions.Compiler.DelegateHelpers")!
            .GetMethod("MakeNewCustomDelegate", BindingFlags.NonPublic | BindingFlags.Static)!
    );

    public static void Init()
    {
        ScriptResolver.GMLFunctionType MakeStubFunction(string functionName) 
        {
            return (object?[] args) => {
                var alwaysLog = ScriptResolver.AlwaysLogStubs;
                var logged = ScriptResolver.LoggedStubs;
                
                if (alwaysLog || !logged.Contains(functionName)) {
                    if (!alwaysLog)
                    {
                        logged.Add(functionName);
                    }

                    DebugLog.LogWarning($"Extension function {functionName} stubbed out.");
                }
                
                return null;
            };
        }

        void StubAllFuncs(ExtensionFile file)
        {
            foreach (var func in file.Functions)
            {
                ScriptResolver.BuiltInFunctions.Add(func.Name, MakeStubFunction(func.Name));
            }
        }

        foreach (var extension in Extensions)
        {
            foreach (var file in extension.Files)
            {
                if (file.Kind != ExtensionKind.Dll)
                {
                    continue;
                }

                if (!OperatingSystem.IsWindows())
                {
                    StubAllFuncs(file);
                    continue;
                }

                if (!_nativeLibs.TryGetValue(file.Name, out var handle))
                {
                    var path = Path.Join(Entry.DataWinFolder, file.Name);
                    var file64bit = Is64BitPe(path) ?? false;
                    var system64bit = Environment.Is64BitProcess;

                    if (file64bit != system64bit)
                    {
                        DebugLog.LogError($"Can't load {file.Name} (DLL is {(file64bit ? "64" : "32")}-bit, process is {(system64bit ? "64" : "32")}-bit)");
                        StubAllFuncs(file);
                        continue;
                    }

                    // cd to the dll's folder so it can find any other dlls it may need
                    var lastDir = Directory.GetCurrentDirectory();
                    Directory.SetCurrentDirectory(Entry.DataWinFolder);

                    // load the library, if it fails then get the win32 error and throw it
                    handle = WinLoadLibrary(path);
                    if (handle == 0)
                    {
                        var err = Marshal.GetLastWin32Error();
                        throw new Exception($"Failed to load extension DLL at {path} (error code {err})");
                    }

                    // cd back to last dir and add library handle to _nativeLibs
                    Directory.SetCurrentDirectory(lastDir);
                    _nativeLibs[file.Name] = handle;
                }

                foreach (var func in file.Functions)
                {
                    var types = new List<Type>();

                    // add types of every argument
                    foreach (var arg in func.Arguments)
                    {
                        types.Add(arg switch
                        {
                            ExtensionVarType.String => typeof(string),
                            ExtensionVarType.Double => typeof(double),
                            _ => throw new UnreachableException()
                        });
                    }

                    // add return type as last element
                    types.Add(func.ReturnType switch
                    {
                        ExtensionVarType.String => typeof(string),
                        ExtensionVarType.Double => typeof(double),
                        _ => throw new UnreachableException()
                    });

                    // get the address of the function based on its external name
                    var funcPtr = WinGetProcAddress(handle, func.ExternalName);
                    if (funcPtr == 0)
                    {
                        var err = Marshal.GetLastWin32Error();
                        throw new Exception($"Failed to load extension function {func.ExternalName} (error code {err})");
                    }

                    DebugLog.LogVerbose($"{func.ExternalName}({func.Arguments.Count} args): {funcPtr}");

                    // create delegate type
                    var deleType = MakeNewCustomDelegate([.. types]);

                    // convert the address of our external function to said delegate type
                    var dele = Marshal.GetDelegateForFunctionPointer(funcPtr, deleType);

                    // shove the delegate's DynamicInvoke into BuiltInFunctions
                    // (which conveniently has the same type semantics as GMLFunctionType)
                    ScriptResolver.BuiltInFunctions.Add(func.Name, dele.DynamicInvoke);
                }
            }
        }
    }

    public static bool? Is64BitPe(string path)
    {
        var stream = File.OpenRead(path);

        // find DOS header (MZ)
        var header = new byte[2];
        stream.ReadExactly(header);
        Console.WriteLine(header);
        if (!Enumerable.SequenceEqual(header, "MZ"u8.ToArray()))
        {
            return null;
        }

        // find PE address
        stream.Seek(0x3c, SeekOrigin.Begin);

        var peAddr = new byte[4];
        stream.ReadExactly(peAddr);
        var peAddrInt = BitConverter.ToInt32(peAddr);

        // find PE header (PE\0\0)
        stream.Seek(peAddrInt, SeekOrigin.Begin);
        
        var peHeader = new byte[4];
        stream.ReadExactly(peHeader);
        if (!Enumerable.SequenceEqual(peHeader, (byte[])[0x50, 0x45, 0x00, 0x00]))
        {
            return null;
        }

        // find arch header (0x8664 for 64-bit)
        var archHeader = new byte[2];
        stream.ReadExactly(archHeader);
        stream.Close();

        return Enumerable.SequenceEqual(archHeader, (byte[])[0x64, 0x86]);
    }

    [DllImport("kernel32.dll", EntryPoint = "LoadLibrary", SetLastError = true)]
    static extern IntPtr WinLoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpLibFileName);

    [DllImport("kernel32.dll", EntryPoint = "GetProcAddress", SetLastError = true)]
    static extern IntPtr WinGetProcAddress(IntPtr hModule, [MarshalAs(UnmanagedType.LPStr)] string lpProcName);

    [DllImport("kernel32.dll", EntryPoint = "FreeLibrary", SetLastError = true)]
    static extern bool WinFreeLibrary(IntPtr hModule);
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
    public string ExternalName = null!;
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