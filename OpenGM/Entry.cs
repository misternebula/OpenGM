﻿using OpenGM.IO;
using OpenGM.Loading;
using OpenGM.Rendering;
using OpenGM.VirtualMachine;
using OpenGM.VirtualMachine.BuiltInFunctions;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.IO.Compression;
using UndertaleModLib;

namespace OpenGM;

internal class Entry
{
    public static float GameSpeed { get; private set; } = 30;

    private static CustomWindow window = null!;

    public static string[] LaunchParameters = [];
    public static string DataWinFolder = "";

    public static string? PathOverride = null;

    static int Main(string[] args)
    {
        var passedArgs = ProcessArgs(args);

        GraphicFunctions.draw_set_circle_precision(24); // to generate sin/cos cache

        var exeLocation = AppDomain.CurrentDomain.BaseDirectory;
        var path = Path.Combine(exeLocation, "game");

        if (PathOverride == null)
        {
            Directory.CreateDirectory(path);
        }
        else
        {
            path = PathOverride;
            if (!Path.Exists(PathOverride))
            {
                DebugLog.LogError($"ERROR - Path {PathOverride} not found.");
                return -1;
            }
        }

        var dataWin = File.Exists(path) ? path : Path.Combine(path, "data.win");
        LoadGame(dataWin, passedArgs);

        return GameFunctions.GameEndReturnCode;
    }

    static string[] ProcessArgs(string[] args)
    {
        var passedArgs = new List<string>();
        var endOfOptions = false;
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (endOfOptions)
            {
                passedArgs.Add(arg);
                continue;
            }

            switch (arg)
            {
                case "--game-path":
                    if (i == args.Length - 1)
                    {
                        DebugLog.LogError("ERROR - Must provide a path to --game-path.");
                        Environment.Exit(1);
                    }

                    PathOverride = args[++i];
                    break;

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

                case "--record-legacy":
                {
                    InputHandler.HandlerState = InputHandler.State.RECORD;

                    var path = args[++i];
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }

                    InputHandler.IOStream = File.OpenWrite(path);
                    break;
                }

                case "--record":
                {
                    InputHandler.HandlerState = InputHandler.State.RECORD;

                    var path = args[++i];
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }

                    var source = File.OpenWrite(path);
                    source.Write(InputHandler.ReplayHeader);

                    var compressed = new GZipStream(source, CompressionMode.Compress, leaveOpen: false);
                    InputHandler.IOStream = compressed;
                    break;
                }

                case "--playback":
                    InputHandler.HandlerState = InputHandler.State.PLAYBACK;

                    var stream = File.OpenRead(args[++i]);
                    var buf = new byte[InputHandler.ReplayHeader.Length];
                    stream.ReadExactly(buf);

                    if (buf.SequenceEqual(InputHandler.ReplayHeader))
                    {
                        // compressed OpenGM replay
                        InputHandler.IOStream = new GZipStream(stream, CompressionMode.Decompress, leaveOpen: false);
                    }
                    else
                    {
                        // legacy GameMaker replay
                        stream.Seek(0, SeekOrigin.Begin);
                        InputHandler.IOStream = stream;
                    }

                    break;

                case "--stack-logs":
                    VMExecutor.VerboseStackLogs = true;
                    VMExecutor.ForceVerboseStackLogs = true;
                    break;
                
                case "--script-name-filter":
                    VMExecutor.ScriptFilter = new(args[++i]);
                    break;
                
                case "--enable-flag":
                    CompatFlags.ForcedValues[args[++i]] = true;
                    break;
                
                case "--disable-flag":
                    CompatFlags.ForcedValues[args[++i]] = false;
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

        if (!File.Exists(Path.Combine(DataWinFolder, "data_OpenGM.win")))
        {
            if (!File.Exists(dataWinPath))
            {
                DebugLog.LogError($"ERROR - data.win not found. Make sure all game files are copied to {DataWinFolder}");
                return;
            }

            Console.WriteLine($"Extracting game assets...");
            using var stream = new FileStream(dataWinPath, FileMode.Open, FileAccess.Read);
            using var data = UndertaleIO.Read(stream);
            GameConverter.ConvertGame(data);
        }

        //CollisionManager.colliders.Clear();

        AudioManager.Dispose();
        AudioManager.Init();
        GameLoader.LoadGame();
        VersionManager.Init();
        ScriptResolver.InitGMLFunctions(); // needs version stuff
        ExtensionManager.Init();

        VMExecutor.EnvStack.Clear();
        VMExecutor.CallStack.Clear();
        InstanceManager.instances.Clear();
        DrawManager._drawObjects.Clear();

        GameSpeed = GameLoader.GeneralInfo.FPS;
        TimingManager.Initialize();
        InstanceManager.NextInstanceID = GameLoader.GeneralInfo.LastObjectID + 1;

        // TODO : is RNG re-initialized after game_change?
        GMRandom.InitRandom(0);

        if (window == null)
        {
            var gameSettings = GameWindowSettings.Default;
            gameSettings.UpdateFrequency = GameSpeed;
            var nativeSettings = NativeWindowSettings.Default;
            nativeSettings.WindowBorder = WindowBorder.Fixed;
            nativeSettings.ClientSize = GameLoader.GeneralInfo.DefaultWindowSize;
            // nativeSettings.Profile = ContextProfile.Compatability; // needed for immediate mode gl
            nativeSettings.Flags = ContextFlags.Default;
            GLFW.WindowHint(WindowHintBool.ScaleFramebuffer, false);
            GLFW.WindowHint(WindowHintBool.ScaleToMonitor, false);

            window = new CustomWindow(gameSettings, nativeSettings);
        }
        else
        {
            window.ClientSize = GameLoader.GeneralInfo.DefaultWindowSize;
        }

        DebugLog.LogInfo($"Binding page textures...");
        PageManager.BindTextures();

        DebugLog.LogInfo("Compiling shaders...");
        ShaderManager.CompileShaders();

        DebugLog.LogInfo($"Executing global init scripts...");

        foreach (var item in ScriptResolver.GlobalInit)
        {
            VMExecutor.ExecuteCode(item, null);
        }

        DebugLog.LogInfo($"Changing to first room...");

        RoomManager.FirstRoom = true;
        RoomManager.New_Room = GameLoader.GeneralInfo.RoomOrder[0];
        RoomManager.ChangeToWaitingRoom();

        DebugLog.LogInfo($"Starting main loop...");
        window.Run();

        AudioManager.Dispose();
        InputHandler.IOStream?.Close();
    }

    public static void SetGameSpeed(int fps)
    {
        GameSpeed = fps;
        window.UpdateFrequency = fps;
    }
}
