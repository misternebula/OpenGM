﻿using OpenGM.IO;
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
		if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "data_OpenGM.win")))
		{
			Console.WriteLine($"Extracting game assets...");
			var dataPath = @"data.win";
			using var stream = new FileStream(dataPath, FileMode.Open, FileAccess.Read);
			using var data = UndertaleIO.Read(stream);
			GameConverter.ConvertGame(data);
		}

		AudioManager.Init();
		GameLoader.LoadGame();

		Environment.Exit(0);

		var firstRoom = RoomManager.RoomList[0];

		var gameSettings = GameWindowSettings.Default;
		gameSettings.UpdateFrequency = 30;
		var nativeSettings = NativeWindowSettings.Default;
		nativeSettings.WindowBorder = WindowBorder.Fixed;
		nativeSettings.ClientSize = new Vector2i((int)firstRoom.SizeX, (int)firstRoom.SizeY);
		nativeSettings.Profile = ContextProfile.Compatability; // needed for immediate mode gl

		window = new CustomWindow(gameSettings, nativeSettings, (uint)firstRoom.SizeX, (uint)firstRoom.SizeY);

		PageManager.BindTextures();

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
