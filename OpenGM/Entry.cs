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
	public static int GameSpeed { get; private set; } = 30; // TODO : load this from data.win

	private static CustomWindow window = null!;

	static void Main(string[] args)
	{
		Directory.CreateDirectory("game");
		if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "game", "data_OpenGM.win")))
		{
			Console.WriteLine($"Extracting game assets...");
			var dataPath = Path.Combine("game", "data.win");
			using var stream = new FileStream(dataPath, FileMode.Open, FileAccess.Read);
			using var data = UndertaleIO.Read(stream);
			GameConverter.ConvertGame(data);
		}

		AudioManager.Init();
		GameLoader.LoadGame();

		GMRandom.InitialiseRNG(0);

		var firstRoom = RoomManager.RoomList[0];

		var gameSettings = GameWindowSettings.Default;
		gameSettings.UpdateFrequency = 30;
		var nativeSettings = NativeWindowSettings.Default;
		nativeSettings.WindowBorder = WindowBorder.Fixed;
		nativeSettings.ClientSize = new Vector2i((int)firstRoom.SizeX, (int)firstRoom.SizeY);
		nativeSettings.Profile = ContextProfile.Compatability; // needed for immediate mode gl

		window = new CustomWindow(gameSettings, nativeSettings, (uint)firstRoom.SizeX, (uint)firstRoom.SizeY);

		DebugLog.LogInfo($"Binding page textures...");
		PageManager.BindTextures();

		DebugLog.LogInfo($"Executing global init scripts...");

		foreach (var item in ScriptResolver.GlobalInit)
		{
			if (item == null)
			{
				continue;
			}

			VMExecutor.ExecuteCode(item, null);
		}

		DebugLog.LogInfo($"Changing to first room...");

		RoomManager.FirstRoom = true;
		RoomManager.ChangeRoomAfterEvent(0);
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
