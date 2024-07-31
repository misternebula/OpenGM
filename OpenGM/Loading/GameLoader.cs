using MemoryPack;
using System.Collections;
using System.Text;
using OpenGM.SerializedFiles;
using OpenGM.VirtualMachine;
using Newtonsoft.Json;
using OpenGM.IO;
using OpenGM.Rendering;
using StbImageSharp;

namespace OpenGM.Loading;
public static class GameLoader
{
    public static void LoadGame()
    {
        Console.WriteLine($"Loading game files...");
        
        Console.Write("Loading datawin...");
        var dataWin = MemoryPackSerializer.Deserialize<DataWin>(File.ReadAllBytes("data_OpenGM.win"))!;
        Console.WriteLine(" Done!");

        AssetIndexManager.LoadAssetIndexes(dataWin);
        LoadScripts(dataWin);
        LoadObjects(dataWin);
        LoadRooms(dataWin);
        LoadSprites(dataWin);
        LoadFonts(dataWin);
        LoadTexturePages(dataWin);
        LoadTextureGroups(dataWin);
        LoadTileSets(dataWin);
        AudioManager.LoadSounds(dataWin);
    }

    private static void LoadScripts(DataWin dataWin)
    {
        Console.Write($"Loading scripts...");
        foreach (var asset in dataWin.Scripts)
        {
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

    private static void LoadObjects(DataWin dataWin)
    {
        Console.Write($"Loading objects...");

        // dictionary makes noticeable performance improvement. maybe move to ScriptResolver if the optimization is needed elsewhere
        var id2Script = ScriptResolver.Scripts.Values.ToDictionary(x => x.AssetId, x => (VMScript?)x);
        id2Script[-1] = null;

        foreach (var asset in dataWin.Objects)
        {
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

    private static void LoadRooms(DataWin dataWin)
    {
        Console.Write($"Loading rooms...");

        foreach (var asset in dataWin.Rooms)
        {
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

    private static void LoadSprites(DataWin dataWin)
    {
        Console.Write($"Loading sprites...");
        foreach (var asset in dataWin.Sprites)
        {
            SpriteManager._spriteDict.Add(asset.AssetIndex, asset);
        }
        Console.WriteLine($" Done!");
    }

    private static void LoadFonts(DataWin dataWin)
    {
        Console.Write($"Loading Fonts...");
        foreach (var asset in dataWin.Fonts)
        {
            TextManager.FontAssets.Add(asset);
        }
        Console.WriteLine($" Done!");
    }

    private static void LoadTexturePages(DataWin dataWin)
    {
        Console.Write($"Loading Texture Pages...");

        if (GameConverter.DecompressOnConvert)
        {
            StbImage.stbi_set_flip_vertically_on_load(0);
        }
        
        foreach (var asset in dataWin.TexturePages)
        {
            if (!GameConverter.DecompressOnConvert)
            {
                asset.Data = ImageResult.FromMemory(asset.Data, ColorComponents.RedGreenBlueAlpha).Data;
            }

            PageManager.TexturePages.Add(asset.Name, (asset, -1));
        }

        Console.WriteLine($" Done!");
    }

    public static Dictionary<string, TextureGroup> TexGroups = new();

    private static void LoadTextureGroups(DataWin dataWin)
    {
        Console.Write($"Loading Texture Groups...");
        foreach (var asset in dataWin.TextureGroups)
        {
            TexGroups.Add(asset.GroupName, asset);
        }

        Console.WriteLine($" Done!");
    }

    public static Dictionary<int, TileSet> TileSets = new();

	private static void LoadTileSets(DataWin dataWin)
    {
	    Console.Write($"Loading Tile Sets...");
	    foreach (var asset in dataWin.TileSets)
	    {
		    TileSets.Add(asset.AssetIndex, asset);
	    }

	    Console.WriteLine($" Done!");
	}
}
