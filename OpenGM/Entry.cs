using OpenGM.IO;
using OpenGM.Loading;
using OpenGM.Rendering;
using OpenGM.VirtualMachine;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using UndertaleModLib;

namespace OpenGM;

internal class Entry
{
	public static float GameSpeed { get; private set; } = 30;

	private static CustomWindow window = null!;

	public static string[] LaunchParameters = new string[0];
	public static string DataWinFolder = "";

	static void Main(string[] args)
	{
		var exeLocation = AppDomain.CurrentDomain.BaseDirectory;
		Directory.CreateDirectory(Path.Combine(exeLocation, "game"));
		var defaultPath = Path.Combine(exeLocation, "game", "data.win");
		LoadGame(defaultPath, args);
	}

	public static void LoadGame(string dataWinPath, string[] parameters)
	{
		LaunchParameters = parameters;

		DataWinFolder = new FileInfo(dataWinPath).DirectoryName!;

		if (!File.Exists(Path.Combine(DataWinFolder, "data_OpenGM.win")))
		{
			Console.WriteLine($"Extracting game assets...");
			using var stream = new FileStream(dataWinPath, FileMode.Open, FileAccess.Read);
			using var data = UndertaleIO.Read(stream);
			GameConverter.ConvertGame(data);
		}

		//CollisionManager.colliders.Clear();

		AudioManager.Init();
		GameLoader.LoadGame();

		VMExecutor.EnvironmentStack.Clear();
		VMExecutor.CallStack.Clear();
		InstanceManager.instances.Clear();
		DrawManager._drawObjects.Clear();

		GameSpeed = GameLoader.GeneralInfo.FPS;
		InstanceManager.NextInstanceID = GameLoader.GeneralInfo.LastObjectID + 1;

		GMRandom.InitialiseRNG(0);

		var firstRoom = RoomManager.RoomList[0];

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
			// i think this is wrong because it sets the matrix stuff not the actual window size? idk
			window.SetResolution(firstRoom.SizeX, firstRoom.SizeY);
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
