using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OpenTK.Graphics.OpenGL;
using StbImageSharp;

namespace DELTARUNITYStandalone;
public static class GameLoader
{
	public static void LoadGame()
	{
		Console.WriteLine($"Loading game files...");
		AssetIndexManager.LoadAssetIndexes();
		LoadScripts();
		LoadObjects();
		LoadRooms();
		LoadSprites();
		LoadFonts();
		LoadTexturePages();
	}

	private static void LoadScripts()
	{
		Console.Write($"Loading scripts...");
		var scriptsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Output", "Scripts");
		var files = Directory.GetFiles(scriptsFolder);
		foreach (var file in files)
		{
			var text = File.ReadAllText(file);
			var asset = JsonConvert.DeserializeObject<VMScript>(text);

			if (asset.IsGlobalInit)
			{
				ScriptResolver.GlobalInitScripts.Add(asset);
			}
			else
			{
				ScriptResolver.Scripts.Add(asset.Name, asset);
			}
		}
		Console.WriteLine($" Done!");
	}

	private static void LoadObjects()
	{
		Console.Write($"Loading objects...");

		var objectsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Output", "Objects");
		var files = Directory.GetFiles(objectsFolder);

		foreach (var file in files)
		{
			var text = File.ReadAllText(file);
			var asset = JsonConvert.DeserializeObject<ObjectDefinition>(text);
			var storage = asset.FileStorage;

			VMScript GetVMScriptFromCodeIndex(int codeIndex)
			{
				if (codeIndex == -1)
				{
					return null;
				}

				return ScriptResolver.Scripts.Values.Single(x => x.AssetId == codeIndex);
			}

			asset.CreateScript = GetVMScriptFromCodeIndex(storage.CreateScriptID);
			asset.DestroyScript = GetVMScriptFromCodeIndex(storage.DestroyScriptID);

			foreach (var (subtype, codeId) in storage.AlarmScriptIDs)
			{
				asset.AlarmScript[subtype] = GetVMScriptFromCodeIndex(codeId);
			}

			foreach (var (subtype, codeId) in storage.StepScriptIDs)
			{
				asset.StepScript[subtype] = GetVMScriptFromCodeIndex(codeId);
			}

			foreach (var (subtype, codeId) in storage.CollisionScriptIDs)
			{
				asset.CollisionScript[subtype] = GetVMScriptFromCodeIndex(codeId);
			}

			// Keyboard
			// Mouse

			foreach (var (subtype, codeId) in storage.OtherScriptIDs)
			{
				asset.OtherScript[subtype] = GetVMScriptFromCodeIndex(codeId);
			}

			foreach (var (subtype, codeId) in storage.DrawScriptIDs)
			{
				asset.DrawScript[subtype] = GetVMScriptFromCodeIndex(codeId);
			}

			// KeyPress
			// KeyRelease
			// Trigger

			asset.CleanUpScript = GetVMScriptFromCodeIndex(storage.CleanUpScriptID);

			// Gesture

			asset.PreCreateScript = GetVMScriptFromCodeIndex(storage.PreCreateScriptID);

			InstanceManager.ObjectDefinitions.Add(asset.AssetId, asset);
		}
		Console.WriteLine($" Done!");
	}

	private static void LoadRooms()
	{
		Console.Write($"Loading rooms...");

		var objectsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Output", "Rooms");
		var files = Directory.GetFiles(objectsFolder);

		foreach (var file in files)
		{
			var text = File.ReadAllText(file);
			var asset = JsonConvert.DeserializeObject<Room>(text);

			RoomManager.RoomList.Add(asset.AssetId, asset);
		}

		Console.WriteLine($" Done!");
	}

	private static void LoadSprites()
	{
		Console.Write($"Loading sprites...");
		var objectsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Output", "Sprites");
		var files = Directory.GetFiles(objectsFolder);

		foreach (var file in files)
		{
			var text = File.ReadAllText(file);
			var asset = JsonConvert.DeserializeObject<SpriteData>(text);

			SpriteManager._spriteDict.Add(asset.AssetIndex, asset);
		}
		Console.WriteLine($" Done!");
	}

	private static void LoadFonts()
	{
		Console.Write($"Loading Fonts...");
		var objectsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Output", "Fonts");
		var files = Directory.GetFiles(objectsFolder);

		foreach (var file in files)
		{
			var text = File.ReadAllText(file);
			var asset = JsonConvert.DeserializeObject<FontAsset>(text);

			TextManager.FontAssets.Add(asset);
		}
		Console.WriteLine($" Done!");
	}

	private static void LoadTexturePages()
	{
		Console.Write($"Loading Texture Pages...");
		var objectsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Output", "Pages");
		var files = Directory.GetFiles(objectsFolder);

		//StbImage.stbi_set_flip_vertically_on_load(1);

		foreach (var image in files)
		{
			var imageResult = ImageResult.FromStream(File.OpenRead(image), ColorComponents.RedGreenBlueAlpha);
			PageManager.TexturePages.Add(Path.GetFileNameWithoutExtension(image), (imageResult, -1));
		}

		Console.WriteLine($" Done!");
	}
}
