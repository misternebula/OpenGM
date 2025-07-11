using OpenGM.IO;
using OpenGM.Loading;
using OpenGM.Rendering;
using OpenGM.VirtualMachine;
using OpenGM.VirtualMachine.BuiltInFunctions;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using UndertaleModLib;

namespace OpenGM;

internal class Entry
{
    public static float GameSpeed { get; private set; } = 30;

    private static CustomWindow window = null!;

    public static string[] LaunchParameters = [];
    public static string DataWinFolder = "";

    static void Main(string[] args)
    {
        var passedArgs = ProcessArgs(args);

        GraphicFunctions.draw_set_circle_precision(24); // to generate sin/cos cache

        var exeLocation = AppDomain.CurrentDomain.BaseDirectory;
        Directory.CreateDirectory(Path.Combine(exeLocation, "game"));
        var defaultPath = Path.Combine(exeLocation, "game", "data.win");
        LoadGame(defaultPath, passedArgs);
    }

    static string[] ProcessArgs(string[] args)
    {
        var passedArgs = new List<string>();
        var endOfOptions = false;
        foreach (var arg in args)
        {
            if (endOfOptions)
            {
                passedArgs.Add(arg);
                continue;
            }

            switch (arg)
            {
                case "--warnings-only":
                    DebugLog.Verbosity = DebugLog.LogType.Warning;
                    break;
                case "--errors-only":
                    DebugLog.Verbosity = DebugLog.LogType.Error;
                    break;
                case "--verbose":
                case "-v":
                    DebugLog.Verbosity = DebugLog.LogType.Verbose;
                    break;
                case "--log-all-stubs":
                    ScriptResolver.AlwaysLogStubs = true;
                    break;
                case "--compat-collision":
                    CollisionManager.CompatMode = true;
                    break;
                case "--no-compat-collision":
                    CollisionManager.CompatMode = false;
                    break;
                case "--":
                    endOfOptions = true;
                    break;
                default:
                    passedArgs.Add(arg);
                    break;
            }
        }

        return passedArgs.ToArray();
    }

    public static DateTime GameLoadTime;

    public static void LoadGame(string dataWinPath, string[] parameters)
    {
        GameLoadTime = DateTime.Now;

        LaunchParameters = parameters;

        DataWinFolder = new FileInfo(dataWinPath).DirectoryName!;

        //CollisionManager.colliders.Clear();

        AudioManager.Dispose();
        AudioManager.Init();
        GameLoader.LoadGame(dataWinPath);
        VersionManager.Init();
        ScriptResolver.InitGMLFunctions(); // needs version stuff

        VMExecutor.EnvStack.Clear();
        VMExecutor.CallStack.Clear();
        InstanceManager.instances.Clear();
        DrawManager._drawObjects.Clear();

        GameSpeed = GameLoader.GeneralInfo.FPS;
        InstanceManager.NextInstanceID = GameLoader.GeneralInfo.LastObjectID + 1;

        // TODO : is RNG re-initialized after game_change?
        GMRandom.InitialiseRNG(0);

        if (window == null)
        {
            var gameSettings = GameWindowSettings.Default;
            gameSettings.UpdateFrequency = 30;
            var nativeSettings = NativeWindowSettings.Default;
            nativeSettings.WindowBorder = WindowBorder.Fixed;
            nativeSettings.ClientSize = GameLoader.GeneralInfo.DefaultWindowSize;
            // nativeSettings.Profile = ContextProfile.Compatability; // needed for immediate mode gl
            nativeSettings.Flags = ContextFlags.Default;

            window = new CustomWindow(gameSettings, nativeSettings, (uint)GameLoader.GeneralInfo.DefaultWindowSize.X, (uint)GameLoader.GeneralInfo.DefaultWindowSize.Y);
        }
        else
        {
            window.ClientSize = GameLoader.GeneralInfo.DefaultWindowSize;
        }

        DebugLog.LogInfo($"Binding page textures...");
        PageManager.BindTextures();

        DebugLog.LogInfo($"Executing global init scripts...");

        foreach (var item in ScriptResolver.GlobalInit)
        {
            VMExecutor.ExecuteCode(item, null);
        }

        DebugLog.LogInfo($"Changing to first room...");

        RoomManager.FirstRoom = true;
        RoomManager.New_Room = 0;
        RoomManager.ChangeToWaitingRoom();

        DebugLog.LogInfo($"Starting main loop...");
        window.Run();

        AudioManager.Dispose();
    }

    public static void SetGameSpeed(int fps)
    {
        GameSpeed = fps;
        window.UpdateFrequency = fps;
    }
}
