using DELTARUNITYStandalone.VirtualMachine;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using UndertaleModLib;

namespace DELTARUNITYStandalone;

internal class Entry
{
	public static int GameSpeed { get; private set; } = 30;

	private static CustomWindow window;

	static void Main(string[] args)
	{
		if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Output")))
		{
			Console.WriteLine($"Extracting game assets...");
			var dataPath = @"data.win";
			using var stream = new FileStream(dataPath, FileMode.Open, FileAccess.Read);
			using var data = UndertaleIO.Read(stream);
			GameConverter.ConvertGame(data);
		}

		GameLoader.LoadGame();

		var firstRoom = RoomManager.RoomList[0];

		var gameSettings = GameWindowSettings.Default;
		gameSettings.UpdateFrequency = 30;
		var nativeSettings = NativeWindowSettings.Default;
		nativeSettings.WindowBorder = WindowBorder.Fixed;
		nativeSettings.ClientSize = new Vector2i((int)firstRoom.SizeX, (int)firstRoom.SizeY);
		nativeSettings.Profile = ContextProfile.Compatability; // needed for immediate mode gl

		window = new CustomWindow(gameSettings, nativeSettings, firstRoom.SizeX, firstRoom.SizeY);

		PageManager.BindTextures();
		AudioManager.Init();

		Console.WriteLine($"Executing global scripts...");

		foreach (var item in ScriptResolver.GlobalInitScripts)
		{
			VMExecutor.ExecuteScript(item, null, null);
		}

		RoomManager.ChangeRoomAfterEvent(0);
		RoomManager.ChangeToWaitingRoom();

		window.Run();

		AudioManager.Dispose();
	}

	public static void SetGameSpeed(int fps)
	{
		GameSpeed = fps;
		window.UpdateFrequency = fps;
	}
}
