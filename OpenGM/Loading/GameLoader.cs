using MemoryPack;
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
    public static bool DebugDumpFunctions = false;
    
    public static void LoadGame()
    {
        Console.WriteLine($"Loading game files...");

        using var stream = File.OpenRead("data_OpenGM.win");
        using var reader = new BinaryReader(stream);

        // must match order of gameconverter
        AssetIndexManager.LoadAssetIndexes(reader);
        LoadScripts(reader);
        LoadObjects(reader);
        LoadRooms(reader);
        LoadSprites(reader);
        LoadFonts(reader);
        LoadTexturePages(reader);
        LoadTextureGroups(reader);
        LoadTileSets(reader);
        AudioManager.LoadSounds(reader);
        
        GC.Collect(); // gc after doing a buncha loading
    }

    private static void LoadScripts(BinaryReader reader)
    {
        Console.Write($"Loading scripts...");

        var allUsedFunctions = new HashSet<string>();

        var length = reader.ReadInt32();
        for (var i = 0; i < length; i++)
        {
            var asset = reader.Read<VMScript>();

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

            if (DebugDumpFunctions)
            {
                var usedFunctions = asset.Instructions.Where(x => x.Opcode == VMOpcode.CALL).Select(x => x.FunctionName);
                usedFunctions = usedFunctions.Where(x =>
                    !x.StartsWith("gml_Object_") &&
                    !x.StartsWith("gml_Script_") &&
                    !x.StartsWith("gml_GlobalScript_") &&
                    !x.StartsWith("gml_RoomCC_")
                );
                foreach (var usedFunction in usedFunctions)
                {
                    allUsedFunctions.Add(usedFunction);
                }
            }
        }

        if (DebugDumpFunctions)
        {
            var builtInfunctions = ScriptResolver.BuiltInFunctions.Select(x => x.Key).ToHashSet();
            IEnumerable<string> allUsedFunctions2 = allUsedFunctions.Order();
            allUsedFunctions2 = allUsedFunctions2.Select(x => builtInfunctions.Contains(x) ? $"[IMPLEMENTED] {x}" : x);
            File.WriteAllText("used functions.txt", string.Join('\n', allUsedFunctions2));
        }

        Console.WriteLine($" Done!");
    }

    private static void LoadObjects(BinaryReader reader)
    {
        Console.Write($"Loading objects...");

        // dictionary makes noticeable performance improvement. maybe move to ScriptResolver if the optimization is needed elsewhere
        var id2Script = ScriptResolver.Scripts.Values.ToDictionary(x => x.AssetId, x => (VMScript?)x);
        id2Script[-1] = null;

        var length = reader.ReadInt32();
        for (var i = 0; i < length; i++)
        {
            var asset = reader.Read<ObjectDefinition>();
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

    private static void LoadRooms(BinaryReader reader)
    {
        Console.Write($"Loading rooms...");

        var length = reader.ReadInt32();
        for (var i = 0; i < length; i++)
        {
            var asset = reader.Read<Room>();

            foreach (var layer in asset.Layers)
            {
                foreach (var element in layer.Elements)
                {
                    if (element is CLayerTilemapElement tilemap)
                    {
                        var uintData = tilemap.Tiles;
                        tilemap.Tiles = null!; // not used after here, so let it gc 
                        
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

    private static void LoadSprites(BinaryReader reader)
    {
        Console.Write($"Loading sprites...");

        var length = reader.ReadInt32();
        for (var i = 0; i < length; i++)
        {
            var asset = reader.Read<SpriteData>();

            SpriteManager._spriteDict.Add(asset.AssetIndex, asset);
        }
        Console.WriteLine($" Done!");
    }

    private static void LoadFonts(BinaryReader reader)
    {
        Console.Write($"Loading Fonts...");

        var length = reader.ReadInt32();
        for (var i = 0; i < length; i++)
        {
            var asset = reader.Read<FontAsset>();

            TextManager.FontAssets.Add(asset);
        }
        Console.WriteLine($" Done!");
    }

    private static void LoadTexturePages(BinaryReader reader)
    {
        Console.Write($"Loading Texture Pages...");
        var objectsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Output", "Pages");
        var files = Directory.EnumerateFiles(objectsFolder);

        //StbImage.stbi_set_flip_vertically_on_load(1);

        foreach (var image in files)
        {
            var imageResult = ImageResult.FromStream(File.OpenRead(image), ColorComponents.RedGreenBlueAlpha);
            PageManager.TexturePages.Add(Path.GetFileNameWithoutExtension(image), (imageResult, -1));
        }

        Console.WriteLine($" Done!");
    }

    public static Dictionary<string, TextureGroup> TexGroups = new();

    private static void LoadTextureGroups(BinaryReader reader)
    {
        Console.Write($"Loading Texture Groups...");

        var length = reader.ReadInt32();
        for (var i = 0; i < length; i++)
        {
            var asset = reader.Read<TextureGroup>();

            TexGroups.Add(asset.GroupName, asset);
        }

        Console.WriteLine($" Done!");
    }

    public static Dictionary<int, TileSet> TileSets = new();

	private static void LoadTileSets(BinaryReader reader)
    {
	    Console.Write($"Loading Tile Sets...");

        var length = reader.ReadInt32();
        for (var i = 0; i < length; i++)
        {
            var asset = reader.Read<TileSet>();

		    TileSets.Add(asset.AssetIndex, asset);
	    }

	    Console.WriteLine($" Done!");
	}
}
