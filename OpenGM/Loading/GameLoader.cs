using NAudio.Wave;
using Newtonsoft.Json;
using OpenGM.IO;
using OpenGM.Rendering;
using OpenGM.SerializedFiles;
using OpenGM.VirtualMachine;
using OpenTK.Audio.OpenAL;
using OpenTK.Mathematics;
using StbVorbisSharp;
using System.Text;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using EventType = OpenGM.VirtualMachine.EventType;

namespace OpenGM.Loading;
public static class GameLoader
{
    public static bool DebugDumpFunctions = false;
    public static Dictionary<int, VMCode> Codes = new();
    public static int CurrentElementID = 0;
    public static Dictionary<string, TextureGroup> TexGroups = new();
    public static Dictionary<int, TileSet> TileSets = new();
    public static Dictionary<int, Background> Backgrounds = new();
    public static Dictionary<int, Shader> Shaders = new();
    public static UndertaleGeneralInfo GeneralInfo => _data.GeneralInfo;

	private static List<VMCode> _replacementVMCodes = new();
    private static UndertaleData _data = new();

    public static void LoadGame(string dataWinPath)
    {
        _replacementVMCodes.Clear();

        var replacementFolder = Path.Combine(Entry.DataWinFolder, "replacement_scripts");
        if (Directory.Exists(replacementFolder))
        {
            var replacementScripts = Directory.GetFiles(replacementFolder, "*.json");
            foreach (var replacementScript in replacementScripts)
            {
                _replacementVMCodes.Add(JsonConvert.DeserializeObject<VMCode>(File.ReadAllText(replacementScript))!);
            }
        }

        if (!File.Exists(dataWinPath))
        {
            DebugLog.LogError($"ERROR - data.win not found. Make sure all game files are copied to {Entry.DataWinFolder}");
            return;
        }

        Console.Write($"Loading data.win...");
        using var stream = new FileStream(dataWinPath, FileMode.Open, FileAccess.Read);
        var data = UndertaleIO.Read(stream);
        Console.WriteLine($" DONE");

        AssetOrder(data);
        Scripts(data);
        GlobalInitCode(data);
        ObjectDefinitions(data);
        LoadBackgrounds(data);
        Rooms(data);
        Sprites(data, data.Sprites);
        Fonts(data);
        Pages(data);
        TextureGroups(data);
        LoadTileSets(data);
        Sounds(data);
        LoadPaths(data);
        LoadShaders(data);

        _data = data;
    }

    public static void Scripts(UndertaleData data)
    {
        Console.WriteLine("Scripts");
        ScriptResolver.ScriptsByName.Clear();
        ScriptResolver.ScriptsByIndex.Clear();
        var scripts = data.Scripts;
        foreach (var script in scripts)
        {
            var asset = new VMScript();
            asset.AssetIndex = scripts.IndexOf(script);
            asset.Name = script.Name.Content;

            if (script.Code != null)
            {
                asset.CodeIndex = data.Code.IndexOf(script.Code);
            }

            ScriptResolver.ScriptsByName.Add(asset.Name, asset);
            ScriptResolver.ScriptsByIndex.Add(asset.AssetIndex, asset);
        }
    }

    public static void GlobalInitCode(UndertaleData data)
    {
        Console.WriteLine("GlobalInit");
		ScriptResolver.GlobalInit.Clear();
        foreach (var item in data.GlobalInitScripts)
        {
            var index = data.Code.IndexOf(item.Code);
            ScriptResolver.GlobalInit.Add(Codes[index]);
        }
    }

    public static void Pages(UndertaleData data)
    {
        Console.WriteLine("Pages");
		PageManager.TexturePages.Clear();

        foreach (var page in data.EmbeddedTextures)
        {
            PageManager.TexturePages.Add(page.Name.Content, (page.TextureData.Image.GetMagickImage(), -1));
        }
    }

    public static void Sprites(UndertaleData data, IList<UndertaleSprite> sprites)
    {
        Console.WriteLine("Sprites");
		SpriteManager._spriteDict.Clear();

        for (var i = 0; i < sprites.Count; i++)
        {
            var sprite = sprites[i];

            var asset = new SpriteData
            {
                AssetIndex = i,
                Name = sprite.Name.Content,
                Width = (int)sprite.Width,
                Height = (int)sprite.Height,
                MarginLeft = sprite.MarginLeft,
                MarginRight = sprite.MarginRight,
                MarginBottom = sprite.MarginBottom,
                MarginTop = sprite.MarginTop,
                BBoxMode = (int)sprite.BBoxMode,
                SepMasks = sprite.SepMasks,
                OriginX = sprite.OriginX,
                OriginY = sprite.OriginY,
                PlaybackSpeed = sprite.GMS2PlaybackSpeed,
                PlaybackSpeedType = sprite.GMS2PlaybackSpeedType,
                Textures = new List<SpritePageItem>()
            };

            foreach (var item in sprite.Textures)
            {
                if (item == null || item.Texture == null)
                {
                    continue;
                }

                var pageItem = new SpritePageItem
                {
                    SourceX = item.Texture.SourceX,
                    SourceY = item.Texture.SourceY,
                    SourceWidth = item.Texture.SourceWidth,
                    SourceHeight = item.Texture.SourceHeight,
                    TargetX = item.Texture.TargetX,
                    TargetY = item.Texture.TargetY,
                    TargetWidth = item.Texture.TargetWidth,
                    TargetHeight = item.Texture.TargetHeight,
                    BoundingWidth = item.Texture.BoundingWidth,
                    BoundingHeight = item.Texture.BoundingHeight,
                    Page = item.Texture.TexturePage.Name.Content
                };

                asset.Textures.Add(pageItem);
            }

            foreach (var item in sprite.CollisionMasks)
            {
                asset.CollisionMasks.Add(item.Data);
            }

            SpriteManager._spriteDict.Add(asset.AssetIndex, asset);
        }
    }

    public static void AssetOrder(UndertaleData data)
    {
        Console.WriteLine("AssetOrder");
		// jank, but it works and its fast to load

		void WriteAssetNames<T>(StringBuilder writer, IList<T> assets) where T : UndertaleNamedResource
        {
            if (assets.Count == 0)
                return;
            foreach (var asset in assets)
            {
                if (asset is not null)
                    writer.AppendLine(asset.Name?.Content ?? assets.IndexOf(asset).ToString());
                else
                    writer.AppendLine("(null)");
            }
        }

        var streamWriter = new StringBuilder();
        {
            // https://github.com/UnderminersTeam/UndertaleModTool/blob/482f5597f18c7833134971790311db8b28ec27c1/UndertaleModTool/Scripts/Technical%20Scripts/ExportAssetOrder.csx

            // Write Sounds.
            streamWriter.AppendLine("@@sounds@@");
            WriteAssetNames(streamWriter, data.Sounds);

            // Write Sprites.
            streamWriter.AppendLine("@@sprites@@");
            WriteAssetNames(streamWriter, data.Sprites);

            // Write Backgrounds.
            streamWriter.AppendLine("@@backgrounds@@");
            WriteAssetNames(streamWriter, data.Backgrounds);

            // Write Paths.
            streamWriter.AppendLine("@@paths@@");
            WriteAssetNames(streamWriter, data.Paths);

            // Write Scripts.
            streamWriter.AppendLine("@@scripts@@");
            WriteAssetNames(streamWriter, data.Scripts);

            // Write Fonts.
            streamWriter.AppendLine("@@fonts@@");
            WriteAssetNames(streamWriter, data.Fonts);

            // Write Objects.
            streamWriter.AppendLine("@@objects@@");
            WriteAssetNames(streamWriter, data.GameObjects);

            // Write Timelines.
            streamWriter.AppendLine("@@timelines@@");
            WriteAssetNames(streamWriter, data.Timelines);

            // Write Rooms.
            streamWriter.AppendLine("@@rooms@@");
            WriteAssetNames(streamWriter, data.Rooms);

            // Write Shaders.
            streamWriter.AppendLine("@@shaders@@");
            WriteAssetNames(streamWriter, data.Shaders);

            // TODO: Perhaps detect GMS2.3, export those asset names as well.
        }

        AssetIndexManager.LoadAssetIndexes(streamWriter.ToString());
    }

    public static void ObjectDefinitions(UndertaleData data)
    {
        Console.WriteLine("ObjectDefinitions");
		InstanceManager.ObjectDefinitions.Clear();

        UndertaleCode? GetCodeFromCodeIndex(int codeIndex)
        {
            if (codeIndex == -1)
            {
                return null;
            }

            return Codes[codeIndex];
        }

        for (var i = 0; i < data.GameObjects.Count; i++)
        {
            var obj = data.GameObjects[i];

            var asset = new ObjectDefinition();
            asset.Name = obj.Name.Content;
            asset.AssetId = i;
            asset.sprite = data.Sprites.IndexOf(obj.Sprite);
            asset.visible = obj.Visible;
            asset.solid = obj.Solid;
            asset.depth = obj.Depth;
            asset.persistent = obj.Persistent;
            asset.textureMaskId = data.Sprites.IndexOf(obj.TextureMaskId);

            var storage = new ObjectDefinitionStorage();
            storage.ParentID = data.GameObjects.IndexOf(obj.ParentId);

            var exportableCode = data.Code.ToList();

            int GetCodeID(EventType type, int eventSubtype)
            {
                if (obj.Events.Count <= (int)type - 1) // -1 because they have a None entry in the enum for some reason
                {
                    return -1; // Most likely UNDERTALE being exported, and type is Gesture/Precreate
                }

                var eventContainer = obj.Events[(int)type - 1];
                if (eventContainer == null || eventContainer.Count <= eventSubtype)
                {
                    return -1;
                }

                var subtypeContainer = eventContainer[eventSubtype];

                var action = subtypeContainer?.Actions[0];

                return action == null ? -1 : exportableCode.IndexOf(action.CodeId);
            }

            storage.CreateCodeID = GetCodeID(EventType.Create, 0);
            storage.DestroyScriptID = GetCodeID(EventType.Destroy, 0);

            storage.AlarmScriptIDs = new();
            foreach (var subtypeContainer in obj.Events[(int)EventType.Alarm - 1])
            {
                storage.AlarmScriptIDs.Add((int)subtypeContainer.EventSubtype, exportableCode.IndexOf(subtypeContainer.Actions[0].CodeId));
            }

            storage.StepScriptIDs = new();
            foreach (var subtypeContainer in obj.Events[(int)EventType.Step - 1])
            {
                storage.StepScriptIDs.Add(subtypeContainer.EventSubtypeStep, exportableCode.IndexOf(subtypeContainer.Actions[0].CodeId));
            }

            storage.CollisionScriptIDs = new();
            foreach (var subtypeContainer in obj.Events[(int)EventType.Collision - 1])
            {
                storage.CollisionScriptIDs.Add((int)subtypeContainer.EventSubtype, exportableCode.IndexOf(subtypeContainer.Actions[0].CodeId));
            }

            storage.KeyboardScriptIDs = new();
            foreach (var subtypeContainer in obj.Events[(int)EventType.Keyboard - 1])
            {
                storage.KeyboardScriptIDs.Add(subtypeContainer.EventSubtypeKey, exportableCode.IndexOf(subtypeContainer.Actions[0].CodeId));
            }

            // mouse

            storage.OtherScriptIDs = new();
            foreach (var subtypeContainer in obj.Events[(int)EventType.Other - 1])
            {
                storage.OtherScriptIDs.Add(subtypeContainer.EventSubtypeOther, exportableCode.IndexOf(subtypeContainer.Actions[0].CodeId));
            }

            storage.DrawScriptIDs = new();
            foreach (var subtypeContainer in obj.Events[(int)EventType.Draw - 1])
            {
                storage.DrawScriptIDs.Add(subtypeContainer.EventSubtypeDraw, exportableCode.IndexOf(subtypeContainer.Actions[0].CodeId));
            }

            storage.KeyPressScriptIDs = new();
            foreach (var subtypeContainer in obj.Events[(int)EventType.KeyPress - 1])
            {
                storage.KeyPressScriptIDs.Add(subtypeContainer.EventSubtypeKey, exportableCode.IndexOf(subtypeContainer.Actions[0].CodeId));
            }

            storage.KeyReleaseScriptIDs = new();
            foreach (var subtypeContainer in obj.Events[(int)EventType.KeyRelease - 1])
            {
                storage.KeyReleaseScriptIDs.Add(subtypeContainer.EventSubtypeKey, exportableCode.IndexOf(subtypeContainer.Actions[0].CodeId));
            }

            // trigger
            storage.CleanUpScriptID = GetCodeID(EventType.CleanUp, 0);
            // gesture
            storage.PreCreateScriptID = GetCodeID(EventType.PreCreate, 0);

            asset.FileStorage = storage;

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

        InstanceManager.InitObjectMap();
    }

    public static void Rooms(UndertaleData data)
    {
        Console.WriteLine("Rooms");
		var codes = data.Code.ToList();
        RoomManager.RoomList.Clear();

        foreach (var room in data.Rooms)
        {
            var asset = new Room
            {
                AssetId = data.Rooms.IndexOf(room),
                Name = room.Name.Content,
                SizeX = (int)room.Width,
                SizeY = (int)room.Height,
                Persistent = room.Persistent,
                CreationCodeId = codes.IndexOf(room.CreationCodeId),
                GravityX = room.GravityX,
                GravityY = room.GravityY,
                CameraWidth = room.Views[0].ViewWidth,
                CameraHeight = room.Views[0].ViewHeight,
                FollowsObject = data.GameObjects.IndexOf(room.Views[0].ObjectId)
            };

            var encounteredGameobjects = new List<UndertaleRoom.GameObject>();

            foreach (var layer in room.Layers)
            {
                var layerasset = new Layer
                {
                    LayerName = layer.LayerName.Content,
                    LayerID = (int)layer.LayerId,
                    LayerDepth = layer.LayerDepth,
                    XOffset = layer.XOffset,
                    YOffset = layer.YOffset,
                    HSpeed = layer.HSpeed,
                    VSpeed = layer.VSpeed,
                    IsVisible = layer.IsVisible
                };

                if (layer.LayerType == UndertaleRoom.LayerType.Tiles)
                {
                    var tilelayer = new CLayerTilemapElement
                    {
                        Type = ElementType.Tilemap,
                        Id = CurrentElementID++,
                        Name = layer.LayerName.Content,
                        Width = (int)layer.TilesData.TilesX,
                        Height = (int)layer.TilesData.TilesY,
                        BackgroundIndex = data.Backgrounds.IndexOf(layer.TilesData.Background),
                        Tiles = layer.TilesData.TileData,
                    };

                    layerasset.Elements.Add(tilelayer);
                }
                else if (layer.LayerType == UndertaleRoom.LayerType.Instances)
                {
                    foreach (var instance in layer.InstancesData.Instances)
                    {
                        var objectAsset = new GameObject
                        {
                            Type = ElementType.Instance,
                            Id = CurrentElementID++,
                            X = instance.X,
                            Y = instance.Y,
                            DefinitionID = data.GameObjects.IndexOf(instance.ObjectDefinition),
                            InstanceID = (int)instance.InstanceID,
                            CreationCodeID = codes.IndexOf(instance.CreationCode),
                            ScaleX = instance.ScaleX,
                            ScaleY = instance.ScaleY,
                            Color = (int)instance.Color,
                            Rotation = instance.Rotation,
                            FrameIndex = instance.ImageIndex,
                            ImageSpeed = instance.ImageSpeed,
                            PreCreateCodeID = codes.IndexOf(instance.PreCreateCode),
                        };

                        encounteredGameobjects.Add(instance);
                        layerasset.Elements.Add(objectAsset);
                    }
                }
                else if (layer.LayerType == UndertaleRoom.LayerType.Background)
                {
                    var backgroundElement = new CLayerBackgroundElement()
                    {
                        Type = ElementType.Background,
                        Id = CurrentElementID++,
                        Name = layer.LayerName.Content,
                        Visible = layer.BackgroundData.Visible,
                        Foreground = layer.BackgroundData.Foreground,
                        Index = data.Sprites.IndexOf(layer.BackgroundData.Sprite),
                        HTiled = layer.BackgroundData.TiledHorizontally,
                        VTiled = layer.BackgroundData.TiledVertically,
                        XScale = layer.BackgroundData.CalcScaleX,
                        YScale = layer.BackgroundData.CalcScaleY,
                        Stretch = layer.BackgroundData.Stretch,
                        Color = layer.BackgroundData.Color,
                        Alpha = layer.BackgroundData.Color.ABGRToCol4().A,
                        FirstFrame = (int)layer.BackgroundData.FirstFrame,
                        AnimationSpeed = layer.BackgroundData.AnimationSpeed,
                        AnimationSpeedType = layer.BackgroundData.AnimationSpeedType
                    };

                    layerasset.Elements.Add(backgroundElement);
                }
                else if (layer.LayerType == UndertaleRoom.LayerType.Assets)
                {
                    var assetsData = layer.AssetsData;

                    if (assetsData.LegacyTiles != null)
                    {
                        foreach (var tile in assetsData.LegacyTiles)
                        {
                            var val = new CLayerTileElement();
                            val.Type = ElementType.Tile;
                            val.Id = CurrentElementID++;
                            val.X = tile.X;
                            val.Y = tile.Y;
                            val.Definition = data.Sprites.IndexOf(tile.SpriteDefinition);
                            val.SourceLeft = (int)tile.SourceX;
                            val.SourceTop = (int)tile.SourceY;
                            val.SourceWidth = (int)tile.Width;
                            val.SourceHeight = (int)tile.Height;
                            val.ScaleX = tile.ScaleX;
                            val.ScaleY = tile.ScaleY;
                            val.Color = tile.Color;
                            val.SpriteMode = tile.spriteMode;

                            layerasset.Elements.Add(val);
                        }
                    }

                    if (assetsData.NineSlices != null)
                    {
                        foreach (var item in assetsData.NineSlices)
                        {
                            DebugLog.LogError($"Don't know how to handle Nine Slice {item.Name.Content}!!!!");
                        }
                    }

                    if (assetsData.ParticleSystems != null)
                    {
                        foreach (var item in assetsData.ParticleSystems)
                        {
                            DebugLog.LogError($"Don't know how to handle Particle System {item.Name.Content}!!!!");
                        }
                    }

                    if (assetsData.Sequences != null)
                    {
                        foreach (var item in assetsData.Sequences)
                        {
                            DebugLog.LogError($"Don't know how to handle Sequence {item.Name.Content}!!!!");
                        }
                    }

                    if (assetsData.TextItems != null)
                    {
                        foreach (var item in assetsData.TextItems)
                        {
                            DebugLog.LogError($"Don't know how to handle TextItems {item.Name.Content}!!!!");
                        }
                    }

                    if (assetsData.Sprites != null)
                    {
                        foreach (var item in assetsData.Sprites)
                        {
                            var val = new CLayerSpriteElement();

                            val.Type = ElementType.Sprite;
                            val.Id = CurrentElementID++;
                            val.Name = item.Name.Content;
                            val.Definition = data.Sprites.IndexOf(item.Sprite);
                            val.X = item.X;
                            val.Y = item.Y;
                            val.ScaleX = item.ScaleX;
                            val.ScaleY = item.ScaleY;
                            val.Color = item.Color;
                            val.AnimationSpeed = item.AnimationSpeed;
                            val.AnimationSpeedType = item.AnimationSpeedType;
                            val.FrameIndex = item.FrameIndex;
                            val.Rotation = item.Rotation;

                            layerasset.Elements.Add(val);
                        }
                    }
                }
                else
                {
                    DebugLog.LogError($"Don't know how to handle layer type {layer.LayerType}");
                }

                asset.Layers.Add(layerasset);
            }

            foreach (var instance in room.GameObjects)
            {
                if (encounteredGameobjects.Contains(instance))
                {
                    continue;
                }

                var objectAsset = new GameObject
                {
                    Type = ElementType.Instance,
                    Id = CurrentElementID++,
                    X = instance.X,
                    Y = instance.Y,
                    DefinitionID = data.GameObjects.IndexOf(instance.ObjectDefinition),
                    InstanceID = (int)instance.InstanceID,
                    CreationCodeID = codes.IndexOf(instance.CreationCode),
                    ScaleX = instance.ScaleX,
                    ScaleY = instance.ScaleY,
                    Color = (int)instance.Color,
                    Rotation = instance.Rotation,
                    FrameIndex = instance.ImageIndex,
                    ImageSpeed = instance.ImageSpeed,
                    PreCreateCodeID = codes.IndexOf(instance.PreCreateCode),
                };

                asset.LooseObjects.Add(objectAsset);

                // Gameobject does not appear in any layers. Probably dealing with UNDERTALE which doesn't have layers.
            }

            foreach (var tile in room.Tiles)
            {
                var definition = tile.spriteMode
                    ? data.Sprites.IndexOf(tile.SpriteDefinition)
                    : data.Backgrounds.IndexOf(tile.BackgroundDefinition);

                var tileAsset = new Tile()
                {
                    X = tile.X,
                    Y = tile.Y,
                    Definition = definition,
                    SpriteMode = tile.spriteMode,
                    SourceLeft = (int)tile.SourceX,
                    SourceTop = (int)tile.SourceY,
                    SourceHeight = (int)tile.Height,
                    SourceWidth = (int)tile.Width,
                    Depth = tile.TileDepth,
                    InstanceID = (int)tile.InstanceID,
                    ScaleX = tile.ScaleX,
                    ScaleY = tile.ScaleY,
                    Color = tile.Color
                };

                asset.Tiles.Add(tileAsset);
            }

            foreach (var background in room.Backgrounds)
            {
                var backgroundAsset = new OldBackground()
                {
                    Enabled = background.Enabled,
                    Foreground = background.Foreground,
                    Definition = data.Backgrounds.IndexOf(background.BackgroundDefinition),
                    Position = new Vector2i(background.X, background.Y),
                    TilingX = background.TiledHorizontally,
                    TilingY = background.TiledVertically,
                    Speed = new Vector2i(background.SpeedX, background.SpeedY),
                    Stretch = background.Stretch
                };

                asset.OldBackgrounds.Add(backgroundAsset);
            }

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
    }

    public static void Fonts(UndertaleData data)
    {
        Console.WriteLine("Fonts");
		TextManager.FontAssets.Clear();

        foreach (var item in data.Fonts)
        {
            var fontAsset = new FontAsset();
            fontAsset.name = item.Name.Content;
            fontAsset.AssetIndex = data.Fonts.IndexOf(item);
            fontAsset.Size = item.EmSize;
            fontAsset.ScaleX = item.ScaleX;
            fontAsset.ScaleY = item.ScaleY;

            var pageItem = new SpritePageItem
            {
                SourceX = item.Texture.SourceX,
                SourceY = item.Texture.SourceY,
                SourceWidth = item.Texture.SourceWidth,
                SourceHeight = item.Texture.SourceHeight,
                TargetX = item.Texture.TargetX,
                TargetY = item.Texture.TargetY,
                TargetWidth = item.Texture.TargetWidth,
                TargetHeight = item.Texture.TargetHeight,
                BoundingWidth = item.Texture.BoundingWidth,
                BoundingHeight = item.Texture.BoundingHeight,
                Page = item.Texture.TexturePage.Name.Content
            };

            fontAsset.texture = pageItem;

            foreach (var glyph in item.Glyphs)
            {
                var glyphAsset = new Glyph
                {
                    characterIndex = glyph.Character,
                    frameIndex = -1,
                    x = glyph.SourceX,
                    y = glyph.SourceY,
                    w = glyph.SourceWidth,
                    h = glyph.SourceHeight,
                    shift = glyph.Shift,
                    xOffset = glyph.Offset
                };

                // i cant remember why theres two of this lol
                fontAsset.entries.Add(glyphAsset);
                fontAsset.entriesDict.Add(glyphAsset.characterIndex, glyphAsset);
            }

            TextManager.FontAssets.Add(fontAsset);
        }
    }

    public static void Sounds(UndertaleData data)
    {
        Console.WriteLine("Sounds");
		foreach (var item in data.Sounds)
        {
            var asset = new SoundAsset();
            asset.AssetID = data.Sounds.IndexOf(item);
            asset.Name = item.Name.Content;
            asset.Volume = item.Volume;
            asset.Pitch = 1; // going by the docs, im fairly certain item.Pitch is not used

            byte[] bytes;
            // https://github.com/UnderminersTeam/UndertaleModTool/blob/master/UndertaleModTool/Scripts/Resource%20Unpackers/ExportAllSounds.csx
            // ignore compressed for now
            {
                var isEmbedded = item.Flags.HasFlag(UndertaleSound.AudioEntryFlags.IsEmbedded);
                var isCompressed = item.Flags.HasFlag(UndertaleSound.AudioEntryFlags.IsCompressed);

                if (item.GroupID != data.GetBuiltinSoundGroupID())
                {
                    // .wav in some audio group file
                    asset.File = $"{asset.Name}.wav";

                    var audioGroupPath = Path.Combine(Entry.DataWinFolder, $"audiogroup{item.GroupID}.dat");
                    using var stream = new FileStream(audioGroupPath, FileMode.Open, FileAccess.Read);
                    using var audioGroupData = UndertaleIO.Read(stream);

                    var embeddedAudio = audioGroupData.EmbeddedAudio;
                    bytes = embeddedAudio[item.AudioID].Data;
                }
                else if (isEmbedded)
                {
                    if (isCompressed)
                    {
                        // embedded .ogg
                        asset.File = $"{asset.Name}.ogg";
                        var embeddedAudio = data.EmbeddedAudio;
                        bytes = embeddedAudio[item.AudioID].Data;
                    }
                    else
                    {
                        // embedded .wav
                        asset.File = $"{asset.Name}.wav";
                        var embeddedAudio = data.EmbeddedAudio;
                        bytes = embeddedAudio[item.AudioID].Data;
                    }
                }
                else
                {
                    if (isCompressed)
                    {
                        // embedded .ogg
                        asset.File = $"{asset.Name}.ogg";
                        var embeddedAudio = data.EmbeddedAudio;
                        bytes = embeddedAudio[item.AudioID].Data;
                    }
                    else
                    {
                        // external .ogg
                        asset.File = $"{asset.Name}.ogg";
                        bytes = File.ReadAllBytes(Path.Combine(Entry.DataWinFolder, asset.File));
                    }
                }
            }

            float[] _data;
            bool stereo;
            int freq;
            if (Path.GetExtension(asset.File) == ".wav")
            {
                // write to the same file to get slightly more perf. need the extension so the reader knows what it is
                // file sucks but is needed to have read that lets us use floats
                File.WriteAllBytes("temp.wav", bytes);

                try
                {
                    using var audioFileReader = new AudioFileReader("temp.wav");
                    _data = new float[audioFileReader.Length * 8 / audioFileReader.WaveFormat.BitsPerSample]; // taken from owml
                    var realLength = audioFileReader.Read(_data, 0, _data.Length);
                    if (realLength != _data.Length)
                    {
                        DebugLog.LogWarning($"{asset.File} length {realLength} != {_data.Length}");
                    }
                    stereo = audioFileReader.WaveFormat.Channels == 2;
                    freq = audioFileReader.WaveFormat.SampleRate;
                }
                catch (Exception e) // i think this is caused by ch1. not sure why
                {
                    DebugLog.LogWarning($"couldnt read wave file {asset.File}: {e}");
                    _data = new float[] { };
                    freq = 1;
                    stereo = false;
                }
            }
            else if (Path.GetExtension(asset.File) == ".ogg")
            {
                using var vorbis = Vorbis.FromMemory(bytes);
                _data = new float[vorbis.StbVorbis.total_samples * vorbis.Channels];
                unsafe
                {
                    fixed (float* ptr = _data)
                    {
                        var realLength = StbVorbis.stb_vorbis_get_samples_float_interleaved(vorbis.StbVorbis, vorbis.Channels, ptr, _data.Length);
                        realLength *= vorbis.Channels;
                        if (realLength != _data.Length)
                        {
                            DebugLog.LogWarning($"{asset.File} length {realLength} != {_data.Length}");
                        }
                    }
                }
                stereo = vorbis.Channels == 2;
                freq = vorbis.SampleRate;
            }
            else
            {
                throw new NotImplementedException($"unknown audio file format {asset.File}");
            }

            var buffer = AL.GenBuffer();
            AudioManager.CheckALError();
            AL.BufferData(buffer, stereo ? ALFormat.StereoFloat32Ext : ALFormat.MonoFloat32Ext, _data, freq);
            AudioManager.CheckALError();

            AudioManager._audioClips[asset.AssetID] = new()
            {
                AssetIndex = asset.AssetID,
                Name = asset.Name,
                Clip = buffer,
                Gain = asset.Volume,
                Pitch = asset.Pitch,
                Offset = 0,
            };
        }
        File.Delete("temp.wav");
    }

    public static void TextureGroups(UndertaleData data)
    {
        Console.WriteLine("TextureGroups");
		TexGroups.Clear();

		if (data.TextureGroupInfo == null)
        {
            //writer.Write(0);
            return;
        }

        foreach (var group in data.TextureGroupInfo)
        {
            var asset = new TextureGroup();

            asset.GroupName = group.Name.Content;
            asset.TexturePages = group.TexturePages.Select(x => x.Resource.Name.Content).ToArray();
            asset.Sprites = group.Sprites.Select(x => data.Sprites.IndexOf(x.Resource)).ToArray();
            asset.Fonts = group.Fonts.Select(x => data.Fonts.IndexOf(x.Resource)).ToArray();

            TexGroups.Add(asset.GroupName, asset);
		}

        Console.WriteLine(" Done!");
    }

    public static void LoadTileSets(UndertaleData data)
    {
        Console.WriteLine("Tilesets");
		TileSets.Clear();

		if (data.Backgrounds == null)
        {
            //writer.Write(0);
            return;
        }

        foreach (var set in data.Backgrounds)
        {
            var asset = new TileSet();

            asset.Name = set.Name.Content;
            asset.AssetIndex = data.Backgrounds.IndexOf(set);
            asset.TileWidth = (int)set.GMS2TileWidth;
            asset.TileHeight = (int)set.GMS2TileHeight;
            asset.OutputBorderX = (int)set.GMS2OutputBorderX;
            asset.OutputBorderY = (int)set.GMS2OutputBorderY;
            asset.TileColumns = (int)set.GMS2TileColumns;
            asset.FramesPerTile = (int)set.GMS2ItemsPerTileCount;
            asset.TileCount = (int)set.GMS2TileCount;
            asset.FrameTime = (int)set.GMS2FrameLength;
            asset.TileIds = set.GMS2TileIds.Select(x => (int)x.ID).ToArray();

            if (set.Texture != null)
            {
                asset.Texture = new SpritePageItem
                {
                    SourceX = set.Texture.SourceX,
                    SourceY = set.Texture.SourceY,
                    SourceWidth = set.Texture.SourceWidth,
                    SourceHeight = set.Texture.SourceHeight,
                    TargetX = set.Texture.TargetX,
                    TargetY = set.Texture.TargetY,
                    TargetWidth = set.Texture.TargetWidth,
                    TargetHeight = set.Texture.TargetHeight,
                    BoundingWidth = set.Texture.BoundingWidth,
                    BoundingHeight = set.Texture.BoundingHeight,
                    Page = set.Texture.TexturePage.Name.Content
                };
            }

            TileSets.Add(asset.AssetIndex, asset);
		}
    }

    public static void LoadPaths(UndertaleData data)
    {
        Console.WriteLine("Paths");
		PathManager.Paths.Clear();

		foreach (var path in data.Paths)
        {
            var p = new CPath(path.Name.Content);
            p.closed = path.IsClosed;
            p.precision = (int)path.Precision;

            foreach (var point in path.Points)
            {
                p.points.Add(point);
            }
            p.count = p.points.Count;

            PathManager.ComputeInternal(p);

            PathManager.Paths.Add(PathManager.HighestPathIndex++, p);
		}
    }

    public static void LoadBackgrounds(UndertaleData data)
    {
        Console.WriteLine("Backgrounds");
		Backgrounds.Clear();

		foreach (var background in data.Backgrounds)
        {
            SpritePageItem? pageItem;

            if (background.Texture == null)
            {
                pageItem = null;
            }
            else
            {
                pageItem = new SpritePageItem
                {
                    SourceX = background.Texture.SourceX,
                    SourceY = background.Texture.SourceY,
                    SourceWidth = background.Texture.SourceWidth,
                    SourceHeight = background.Texture.SourceHeight,
                    TargetX = background.Texture.TargetX,
                    TargetY = background.Texture.TargetY,
                    TargetWidth = background.Texture.TargetWidth,
                    TargetHeight = background.Texture.TargetHeight,
                    BoundingWidth = background.Texture.BoundingWidth,
                    BoundingHeight = background.Texture.BoundingHeight,
                    Page = background.Texture.TexturePage.Name.Content
                };
            }

            var asset = new Background()
            {
                AssetIndex = data.Backgrounds.IndexOf(background),
                Name = background.Name.Content,
                Transparent = background.Transparent,
                Smooth = background.Smooth,
                Preload = background.Preload,
                Texture = pageItem
            };

			Backgrounds.Add(asset.AssetIndex, asset);
		}
    }

    public static void LoadShaders(UndertaleData data)
    {
        Console.WriteLine("Shaders");
		Shaders.Clear();

		foreach (var shader in data.Shaders)
        {
            if (shader == null)
            {
                continue;
            }

            var asset = new Shader();
            asset.AssetIndex = data.Shaders.IndexOf(shader);
            asset.Name = shader.Name.Content;
            asset.ShaderType = (int)shader.Type;

            switch (shader.Type)
            {
                case UndertaleShader.ShaderType.GLSL_ES:
                    asset.VertexSource = shader.GLSL_ES_Vertex.Content;
                    asset.FragmentSource = shader.GLSL_ES_Fragment.Content;
                    break;
                case UndertaleShader.ShaderType.GLSL:
                    asset.VertexSource = shader.GLSL_Vertex.Content;
                    asset.FragmentSource = shader.GLSL_Fragment.Content;
                    break;
                case UndertaleShader.ShaderType.HLSL9:
                    asset.VertexSource = shader.HLSL9_Vertex.Content;
                    asset.FragmentSource = shader.HLSL9_Fragment.Content;
                    break;
                default:
                    // There are other shader types (HLSL11, PSSL, CG_PS3, CG_VITA) but they shouldn't show up
                    throw new NotImplementedException();
            }

            asset.ShaderAttributes = new string[shader.VertexShaderAttributes.Count];
            for (var i = 0; i < shader.VertexShaderAttributes.Count; i++)
            {
                asset.ShaderAttributes[i] = shader.VertexShaderAttributes[i].Name.Content;
            }

			Shaders.Add(asset.AssetIndex, asset);
		}

        Console.WriteLine(" Done!");
    }
}
