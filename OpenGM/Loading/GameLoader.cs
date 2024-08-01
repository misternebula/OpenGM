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

        using var stream = File.OpenRead("data_OpenGM.win");

        // must match order of gameconverter
        AssetIndexManager.LoadAssetIndexes(stream);
        LoadScripts(stream);
        LoadObjects(stream);
        LoadRooms(stream);
        LoadSprites(stream);
        LoadFonts(stream);
        LoadTexturePages(stream);
        LoadTextureGroups(stream);
        LoadTileSets(stream);
        AudioManager.LoadSounds(stream);
    }

    private static void LoadScripts(FileStream stream)
    {
        Console.Write($"Loading scripts...");
        var length = stream.Read<int>();
        for (var i = 0; i < length; i++)
        {
            var asset = stream.Read<VMScript>();
            
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

    private static void LoadObjects(FileStream stream)
    {
        Console.Write($"Loading objects...");

        // dictionary makes noticeable performance improvement. maybe move to ScriptResolver if the optimization is needed elsewhere
        var id2Script = ScriptResolver.Scripts.Values.ToDictionary(x => x.AssetId, x => (VMScript?)x);
        id2Script[-1] = null;

        var length = stream.Read<int>();
        for (var i = 0; i < length; i++)
        {
            var asset = stream.Read<ObjectDefinition>();
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

    private static void LoadRooms(FileStream stream)
    {
        Console.Write($"Loading rooms...");

        var length = stream.Read<int>();
        for (var i = 0; i < length; i++)
        {
            var asset = stream.Read<Room>();
            
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

    private static void LoadSprites(FileStream stream)
    {
        Console.Write($"Loading sprites...");
        var length = stream.Read<int>();
        for (var i = 0; i < length; i++)
        {
            var asset = stream.Read<SpriteData>();
            
            SpriteManager._spriteDict.Add(asset.AssetIndex, asset);
        }
        Console.WriteLine($" Done!");
    }

    private static void LoadFonts(FileStream stream)
    {
        Console.Write($"Loading Fonts...");
        var length = stream.Read<int>();
        for (var i = 0; i < length; i++)
        {
            var asset = stream.Read<FontAsset>();
            
            TextManager.FontAssets.Add(asset);
        }
        Console.WriteLine($" Done!");
    }

    private static void LoadTexturePages(FileStream stream)
    {
        Console.Write($"Loading Texture Pages...");

        if (GameConverter.DecompressOnConvert)
        {
            StbImage.stbi_set_flip_vertically_on_load(0);
        }
        
        var length = stream.Read<int>();
        for (var i = 0; i < length; i++)
        {
            var asset = stream.Read<TexturePage>();

            if (!GameConverter.DecompressOnConvert)
            {
                asset.Data = ImageResult.FromMemory(asset.Data, ColorComponents.RedGreenBlueAlpha).Data;
            }

            PageManager.TexturePages.Add(asset.Name, (asset, -1));
        }

        Console.WriteLine($" Done!");
    }

    public static Dictionary<string, TextureGroup> TexGroups = new();

    private static void LoadTextureGroups(FileStream stream)
    {
        Console.Write($"Loading Texture Groups...");
        var length = stream.Read<int>();
        for (var i = 0; i < length; i++)
        {
            var asset = stream.Read<TextureGroup>();
            
            TexGroups.Add(asset.GroupName, asset);
        }

        Console.WriteLine($" Done!");
    }

    public static Dictionary<int, TileSet> TileSets = new();

	private static void LoadTileSets(FileStream stream)
    {
	    Console.Write($"Loading Tile Sets...");
        var length = stream.Read<int>();
        for (var i = 0; i < length; i++)
        {
            var asset = stream.Read<TileSet>();
            
		    TileSets.Add(asset.AssetIndex, asset);
	    }

	    Console.WriteLine($" Done!");
	}
}
