﻿using MemoryPack;
using System.Collections;
using System.Text;
using OpenGM.SerializedFiles;
using OpenGM.VirtualMachine;
using Newtonsoft.Json;
using OpenGM.IO;
using StbImageSharp;
using OpenGM.Rendering;

namespace OpenGM.Loading;
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
        LoadTextureGroups();
        LoadTileSets();
        AudioManager.LoadSounds();
    }

    private static void LoadScripts()
    {
        Console.Write($"Loading scripts...");
        var scriptsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Output", "Scripts");
        var files = Directory.GetFiles(scriptsFolder);
        foreach (var file in files)
        {
            var text = File.ReadAllBytes(file);
            var asset = MemoryPackSerializer.Deserialize<VMScript>(text)!;

            if (asset.IsGlobalInit)
            {
                ScriptResolver.GlobalInitScripts.Add(asset);
            }
            else
            {
                ScriptResolver.Scripts.Add(asset.Name, asset);
            }

            foreach (var func in asset.Functions)
            {
                ScriptResolver.ScriptFunctions.Add(func.FunctionName, (asset, func.InstructionIndex));
            }
        }
        Console.WriteLine($" Done!");
    }

    private static void LoadObjects()
    {
        Console.Write($"Loading objects...");

        var objectsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Output", "Objects");
        var files = Directory.GetFiles(objectsFolder);

        // dictionary makes noticeable performance improvement. maybe move to ScriptResolver if the optimization is needed elsewhere
        var id2Script = ScriptResolver.Scripts.Values.ToDictionary(x => x.AssetId, x => (VMScript?)x);
        id2Script[-1] = null;

        foreach (var file in files)
        {
            var text = File.ReadAllBytes(file);
            var asset = MemoryPackSerializer.Deserialize<ObjectDefinition>(text)!;
            var storage = asset.FileStorage;

            asset.CreateScript = id2Script[storage.CreateScriptID];
            asset.DestroyScript = id2Script[storage.DestroyScriptID];

            foreach (var (subtype, codeId) in storage.AlarmScriptIDs)
            {
                asset.AlarmScript[subtype] = id2Script[codeId]!;
            }

            foreach (var (subtype, codeId) in storage.StepScriptIDs)
            {
                asset.StepScript[subtype] = id2Script[codeId]!;
            }

            foreach (var (subtype, codeId) in storage.CollisionScriptIDs)
            {
                asset.CollisionScript[subtype] = id2Script[codeId]!;
            }

            // Keyboard
            // Mouse

            foreach (var (subtype, codeId) in storage.OtherScriptIDs)
            {
                asset.OtherScript[subtype] = id2Script[codeId]!;
            }

            foreach (var (subtype, codeId) in storage.DrawScriptIDs)
            {
                asset.DrawScript[subtype] = id2Script[codeId]!;
            }

            // KeyPress
            // KeyRelease
            // Trigger

            asset.CleanUpScript = id2Script[storage.CleanUpScriptID];

            // Gesture

            asset.PreCreateScript = id2Script[storage.PreCreateScriptID];

            InstanceManager.ObjectDefinitions.Add(asset.AssetId, asset);
        }

        foreach (var item in InstanceManager.ObjectDefinitions.Values)
        {
            if (item.FileStorage.ParentID != -1)
            {
                item.parent = InstanceManager.ObjectDefinitions[item.FileStorage.ParentID];
            }
            
            item.FileStorage = null!; // not used after here, so let it gc
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
            var text = File.ReadAllBytes(file);
            var asset = MemoryPackSerializer.Deserialize<Room>(text)!;

            foreach (var layer in asset.Layers)
            {
                foreach (var element in layer.Elements)
                {
                    if (element is CLayerTilemapElement tilemap)
                    {
                        var uintData = tilemap.Tiles;
                        tilemap.TilesData = new TileBlob[tilemap.Height, tilemap.Width];

                        var cols = tilemap.Height;
                        var rows = tilemap.Width;
						for (var col = 0; col < cols; col++)
						{
							for (var row = 0; row < rows; row++)
							{
								var blobData = uintData[col][row];

								var blob = new TileBlob
								{
									TileIndex = (int)blobData & 0x7FFFF, // bits 0-18
									Mirror = (blobData & 0x8000000) != 0, // bit 28
									Flip = (blobData & 0x10000000) != 0, // bit 29
									Rotate = (blobData & 0x20000000) != 0 // bit 30
								};

								tilemap.TilesData[col, row] = blob;
							}
						}
					}
                }
            }

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
            var text = File.ReadAllBytes(file);
            var asset = MemoryPackSerializer.Deserialize<SpriteData>(text)!;

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
            var text = File.ReadAllBytes(file);
            var asset = MemoryPackSerializer.Deserialize<FontAsset>(text)!;

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

    public static Dictionary<string, TextureGroup> TexGroups = new();

    private static void LoadTextureGroups()
    {
        Console.Write($"Loading Texture Groups...");
        var objectsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Output", "TexGroups");
        var files = Directory.GetFiles(objectsFolder);

        foreach (var file in files)
        {
            var text = File.ReadAllBytes(file);
            var asset = MemoryPackSerializer.Deserialize<TextureGroup>(text)!;

            TexGroups.Add(asset.GroupName, asset);
        }

        Console.WriteLine($" Done!");
    }

    public static Dictionary<int, TileSet> TileSets = new();

	private static void LoadTileSets()
    {
	    Console.Write($"Loading Tile Sets...");
	    var objectsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Output", "TileSets");
	    var files = Directory.GetFiles(objectsFolder);

	    foreach (var file in files)
	    {
            var text = File.ReadAllBytes(file);
            var asset = MemoryPackSerializer.Deserialize<TileSet>(text)!;

		    TileSets.Add(asset.AssetIndex, asset);
	    }

	    Console.WriteLine($" Done!");
	}
}
