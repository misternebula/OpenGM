using MemoryPack;
using System.Collections;
using System.Text;
using OpenGM.SerializedFiles;
using OpenGM.VirtualMachine;
using Newtonsoft.Json;
using OpenGM.IO;
using StbImageSharp;
using OpenGM.Rendering;
using System.Diagnostics;

namespace OpenGM.Loading;
public static class GameLoader
{
    public static bool DebugDumpFunctions = false;

    private static List<VMCode> _replacementVMCodes = new();

    public static GameData GeneralInfo = null!;
    
    public static void LoadGame()
    {
        Console.WriteLine($"Loading game files...");

        _replacementVMCodes.Clear();

		var replacementFolder = Path.Combine(Directory.GetCurrentDirectory(), "game", "replacement_scripts");
        if (Directory.Exists(replacementFolder))
        {
	        var replacementScripts = Directory.GetFiles(replacementFolder, "*.json");
	        foreach (var replacementScript in replacementScripts)
	        {
		        _replacementVMCodes.Add(JsonConvert.DeserializeObject<VMCode>(File.ReadAllText(replacementScript))!);
	        }
		}

        using var stream = File.OpenRead(Path.Combine(Entry.DataWinFolder, "data_OpenGM.win"));
        using var reader = new BinaryReader(stream);

        // must match order of gameconverter
        GeneralInfo = reader.ReadMemoryPack<GameData>();
		AssetIndexManager.LoadAssetIndexes(reader);
        LoadScripts(reader);
        LoadCode(reader);
        LoadGlobalInitCode(reader);
        LoadObjects(reader);
		LoadBackgrounds(reader);
		LoadRooms(reader);
        LoadSprites(reader);
        LoadFonts(reader);
        LoadTexturePages(reader);
        LoadTextureGroups(reader);
        LoadTileSets(reader);
        AudioManager.LoadSounds(reader);
        LoadPaths(reader);
        LoadShaders(reader);
        
        GC.Collect(); // gc after doing a buncha loading
    }

    private static void LoadScripts(BinaryReader reader)
    {
	    Console.Write($"Loading scripts...");

	    ScriptResolver.ScriptsByName.Clear();
	    ScriptResolver.ScriptsByIndex.Clear();

		var length = reader.ReadInt32();
	    for (var i = 0; i < length; i++)
	    {
		    var asset = reader.ReadMemoryPack<VMScript>();
		    ScriptResolver.ScriptsByName.Add(asset.Name, asset);
		    ScriptResolver.ScriptsByIndex.Add(asset.AssetIndex, asset);
		}
	    Console.WriteLine($" Done!");
	}

    public static Dictionary<int, VMCode> Codes = new();

    private static void LoadCode(BinaryReader reader)
    {
        Console.Write($"Loading code...");

        var allUsedFunctions = new HashSet<string>();
        Codes.Clear();

		var length = reader.ReadInt32();
        for (var i = 0; i < length; i++)
        {
            var asset = reader.ReadMemoryPack<VMCode>();

            if (_replacementVMCodes.Any(x => x.Name == asset.Name))
            {
                DebugLog.Log($"Replacing {asset.Name} with custom script...");
	            var assetID = asset.AssetId;
	            var parentAssetID = asset.ParentAssetId;
	            asset = _replacementVMCodes.First(x => x.Name == asset.Name);
                asset.AssetId = assetID;
                asset.ParentAssetId = parentAssetID;
            }

            Codes.Add(asset.AssetId, asset);

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

    private static void LoadGlobalInitCode(BinaryReader reader)
    {
	    ScriptResolver.GlobalInit.Clear();

		var count = reader.ReadInt32();

        for (int i = 0; i < count; i++)
        {
            ScriptResolver.GlobalInit.Add(Codes[reader.ReadInt32()]);
        }
    }

	private static void LoadObjects(BinaryReader reader)
    {
        Console.Write($"Loading objects...");

        InstanceManager.ObjectDefinitions.Clear();

        VMCode? GetCodeFromCodeIndex(int codeIndex)
        {
	        if (codeIndex == -1)
	        {
		        return null;
	        }

	        return GameLoader.Codes[codeIndex];
        }
        
        var length = reader.ReadInt32();
        for (var i = 0; i < length; i++)
        {
            var asset = reader.ReadMemoryPack<ObjectDefinition>();
            var storage = asset.FileStorage;

            asset.CreateCode = GetCodeFromCodeIndex(storage.CreateCodeID);
            asset.DestroyScript = GetCodeFromCodeIndex(storage.DestroyScriptID);

            foreach (var (subtype, codeId) in storage.AlarmScriptIDs)
            {
                asset.AlarmScript[subtype] = Codes[codeId];
            }

            foreach (var (subtype, codeId) in storage.StepScriptIDs)
            {
                asset.StepScript[subtype] = Codes[codeId];
            }

            foreach (var (subtype, codeId) in storage.CollisionScriptIDs)
            {
                asset.CollisionScript[subtype] = Codes[codeId];
            }

			foreach (var (subtype, codeId) in storage.KeyboardScriptIDs)
			{
				asset.KeyboardScripts[subtype] = Codes[codeId];
			}

			// Mouse

			foreach (var (subtype, codeId) in storage.OtherScriptIDs)
            {
                asset.OtherScript[subtype] = Codes[codeId];
            }

            foreach (var (subtype, codeId) in storage.DrawScriptIDs)
            {
                asset.DrawScript[subtype] = Codes[codeId];
            }

			foreach (var (subtype, codeId) in storage.KeyPressScriptIDs)
			{
				asset.KeyPressScripts[subtype] = Codes[codeId];
			}

			foreach (var (subtype, codeId) in storage.KeyReleaseScriptIDs)
			{
				asset.KeyReleaseScripts[subtype] = Codes[codeId];
			}

			// Trigger

			asset.CleanUpScript = GetCodeFromCodeIndex(storage.CleanUpScriptID);

            // Gesture

            asset.PreCreateScript = GetCodeFromCodeIndex(storage.PreCreateScriptID);

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

        RoomManager.RoomList.Clear();

		var length = reader.ReadInt32();
        for (var i = 0; i < length; i++)
        {
            var asset = reader.ReadMemoryPack<Room>();

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

        SpriteManager._spriteDict.Clear();

		var length = reader.ReadInt32();
        for (var i = 0; i < length; i++)
        {
            var asset = reader.ReadMemoryPack<SpriteData>();

            SpriteManager._spriteDict.Add(asset.AssetIndex, asset);
        }
        Console.WriteLine($" Done!");
    }

    private static void LoadFonts(BinaryReader reader)
    {
        Console.Write($"Loading Fonts...");

        TextManager.FontAssets.Clear();

		var length = reader.ReadInt32();
        for (var i = 0; i < length; i++)
        {
            var asset = reader.ReadMemoryPack<FontAsset>();

            TextManager.FontAssets.Add(asset);
        }
        Console.WriteLine($" Done!");
    }

    private static void LoadTexturePages(BinaryReader reader)
    {
        Console.Write($"Loading Texture Pages...");

        PageManager.UnbindTextures();

		//StbImage.stbi_set_flip_vertically_on_load(1);

		var length = reader.ReadInt32();
        for (var i = 0; i < length; i++)
        {
            var pageName = reader.ReadString();
            var blobLength = reader.ReadInt32();
            var blob = reader.ReadBytes(blobLength);
            
            var imageResult = ImageResult.FromMemory(blob, ColorComponents.RedGreenBlueAlpha);
            PageManager.TexturePages.Add(pageName, (imageResult, -1));
        }

        Console.WriteLine($" Done!");
    }

    public static Dictionary<string, TextureGroup> TexGroups = new();

    private static void LoadTextureGroups(BinaryReader reader)
    {
        Console.Write($"Loading Texture Groups...");

        TexGroups.Clear();

        var length = reader.ReadInt32();
        for (var i = 0; i < length; i++)
        {
            var asset = reader.ReadMemoryPack<TextureGroup>();

            TexGroups.Add(asset.GroupName, asset);
        }

        Console.WriteLine($" Done!");
    }

    public static Dictionary<int, TileSet> TileSets = new();

	private static void LoadTileSets(BinaryReader reader)
    {
	    Console.Write($"Loading Tile Sets...");

	    TileSets.Clear();

		var length = reader.ReadInt32();
        for (var i = 0; i < length; i++)
        {
            var asset = reader.ReadMemoryPack<TileSet>();

		    TileSets.Add(asset.AssetIndex, asset);
	    }

	    Console.WriteLine($" Done!");
	}

	private static void LoadPaths(BinaryReader reader)
	{
		Console.Write($"Loading paths...");
		PathManager.Paths.Clear();

		var length = reader.ReadInt32();
		for (var i = 0; i < length; i++)
		{
			var asset = reader.ReadMemoryPack<GMPath>();

			var path = new CPath(asset.Name);
            // TODO : smooth????
			path.closed = asset.IsClosed;
			path.precision = asset.Precision;

			foreach (var point in asset.Points)
			{
                path.points.Add(point);
			}
            path.count = path.points.Count;

            PathManager.ComputeInternal(path);

            PathManager.Paths.Add(PathManager.HighestPathIndex++, path);
		}
		Console.WriteLine($" Done!");
	}

	public static Dictionary<int, Background> Backgrounds = new();

	private static void LoadBackgrounds(BinaryReader reader)
	{
		Console.Write($"Loading backgrounds...");
		Backgrounds.Clear();

		var length = reader.ReadInt32();
		for (var i = 0; i < length; i++)
		{
			var asset = reader.ReadMemoryPack<Background>();
			Backgrounds.Add(asset.AssetIndex, asset);
		}
		Console.WriteLine($" Done!");
	}

	public static Dictionary<int, Shader> Shaders = new();

	private static void LoadShaders(BinaryReader reader)
    {
		Console.Write($"Loading shaders...");
        Shaders.Clear();

		var length = reader.ReadInt32();
		for (var i = 0; i < length; i++)
        {
			var asset = reader.ReadMemoryPack<Shader>();
			Shaders.Add(asset.AssetIndex, asset);
		}

		Console.WriteLine($" Done!");
	}
}
