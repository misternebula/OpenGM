using MemoryPack;
using NAudio.Wave;
using NVorbis;
using OpenGM.SerializedFiles;
using OpenGM.VirtualMachine;
using System.Diagnostics;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using EventType = OpenGM.VirtualMachine.EventType;
using OpenGM.IO;
using StbImageSharp;
using System.Numerics;

namespace OpenGM.Loading;

/// <summary>
/// Converts the UTMT data into our custom formats, which are saved into files
/// </summary>
public static class GameConverter
{
    public static bool DecompressOnConvert = true;
    
    public static void ConvertGame(UndertaleData data)
    {
        Console.WriteLine($"Converting game assets...");

        using var stream = File.OpenWrite("data_OpenGM.win");

        // must match order of gameloader
        ExportAssetOrder(stream, data);
        ConvertScripts(stream, data, data.Code.Where(c => c.ParentEntry is null).ToList());
        ExportObjectDefinitions(stream, data);
        ExportRooms(stream, data);
        ConvertSprites(stream, data.Sprites);
        ExportFonts(stream, data);
        ExportPages(stream, data);
        ExportTextureGroups(stream, data);
        ExportTileSets(stream, data);
        ExportSounds(stream, data);
        
        GC.Collect();
    }

    public static void ConvertScripts(FileStream stream, UndertaleData data, List<UndertaleCode> codes)
    {
        Console.Write($"Converting scripts...");

        stream.Write(codes.Count);
        foreach (var code in codes)
        {
            var asmFile = code.Disassemble(data.Variables, data.CodeLocals.For(code));

            var asset = ConvertScript(asmFile);

            asset.AssetId = codes.IndexOf(code);
            asset.IsGlobalInit = data.GlobalInitScripts.Select(x => x.Code).Contains(code);

            if (code.Name.Content.StartsWith("gml_Object_"))
            {
                asset.Name = code.Name.Content.Substring("gml_Object_".Length);
            }
            else if (code.Name.Content.StartsWith("gml_Script_"))
            {
                asset.Name = code.Name.Content.Substring("gml_Script_".Length);
            }
            else if (code.Name.Content.StartsWith("gml_GlobalScript_"))
            {
                asset.Name = code.Name.Content.Substring("gml_GlobalScript_".Length);
            }
            else if (code.Name.Content.StartsWith("gml_RoomCC_"))
            {
                asset.Name = code.Name.Content.Substring("gml_RoomCC_".Length);
            }
            else
            {
                asset.Name = code.Name.Content;
            }

            stream.Write(asset);
        }
        Console.WriteLine($" Done!");
    }

    public static VMScript ConvertScript(string asmFile)
    {
        var asmFileLines = asmFile.FixCRLF().Split('\n');

        var localVariables = new List<string>();

        int startLine = -1;
        for (var i = 0; i < asmFileLines.Length; i++)
        {
            if (asmFileLines[i] == ":[0]")
            {
                startLine = i;
            }

            if (asmFileLines[i].StartsWith(".localvar"))
            {
                var split = asmFileLines[i].Split(' ');
                localVariables.Add(split[2]);
            }
        }

        var asset = new VMScript();
        asset.LocalVariables = localVariables;

        if (startLine == -1)
        {
            // no code in file???

            asset.Instructions = new();
            asset.Labels = new() { { 0, 0 } };

            return asset;
        }

        asmFileLines = asmFileLines.Skip(startLine).ToArray();
        asmFileLines = asmFileLines.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

        string? functionLabelAtNextLine = null;

        foreach (var line in asmFileLines)
        {
            if (line.StartsWith(":["))
            {
                // Label Declaration

                var id = line.Substring(2, line.Length - 3);

                if (id == "end")
                {
                    break;
                }

                if (functionLabelAtNextLine != null)
                {
                    asset.Functions.Add(new FunctionDefinition() { InstructionIndex = asset.Instructions.Count, FunctionName = functionLabelAtNextLine });
                    functionLabelAtNextLine = null;
                }

                asset.Labels.Add(int.Parse(id), asset.Instructions.Count);
            }
            else if (line.StartsWith("> "))
            {
                // Function Declaration

                // next line should be a label
                var removeChevron = line[2..];
                var split = removeChevron.Split(" (");
                functionLabelAtNextLine = split[0];
            }
            else
            {
                // Instruction
                // instructions are in form OPERATION.TYPE.TYPE DATA

                var opcode = line.Split(" ")[0];
                var operation = opcode.Split('.')[0];
                var types = opcode.Split(".").Skip(1).ToArray();

                var enumOperation = (VMOpcode)Enum.Parse(typeof(VMOpcode), operation.ToUpper());

                var instruction = new VMScriptInstruction
                {
                    Raw = line,
                    Opcode = enumOperation,
                    TypeOne = types.Length >= 1 ? (VMType)Enum.Parse(typeof(VMType), types[0]) : VMType.None,
                    TypeTwo = types.Length == 2 ? (VMType)Enum.Parse(typeof(VMType), types[1]) : VMType.None,
                };

                switch (enumOperation)
                {
                    case VMOpcode.CHKINDEX:
                        // no data
                        break;
                    case VMOpcode.CONV:
                        // no data
                        break;
                    case VMOpcode.MUL:
                        // no data
                        break;
                    case VMOpcode.DIV:
                        // no data
                        break;
                    case VMOpcode.REM:
                        break;
                    case VMOpcode.MOD:
                        break;
                    case VMOpcode.ADD:
                        // no data
                        break;
                    case VMOpcode.SUB:
                        // no data
                        break;
                    case VMOpcode.AND:
                        break;
                    case VMOpcode.OR:
                        break;
                    case VMOpcode.XOR:
                        break;
                    case VMOpcode.NEG:
                        break;
                    case VMOpcode.NOT:
                        break;
                    case VMOpcode.SHL:
                        break;
                    case VMOpcode.SHR:
                        break;
                    case VMOpcode.CMP:
                        var comparison = line.Substring(opcode.Length + 1);
                        var enumComparison = (VMComparison)Enum.Parse(typeof(VMComparison), comparison);
                        instruction.Comparison = enumComparison;
                        break;
                    case VMOpcode.POP:
                        var variableName = line.Substring(opcode.Length + 1);
                        instruction.StringData = variableName;
                        break;
                    case VMOpcode.DUP:
                        var indexBack = line.Substring(opcode.Length + 1);

                        indexBack = indexBack.Split(" ;;; ")[0];

                        var splitBySpace = indexBack.Split(" ");
                        if (splitBySpace.Length == 1)
                        {
                            instruction.IntData = int.Parse(indexBack);
                        }
                        else
                        {
                            // This opcode has TWO PARAMETERS?? Gamemaker you've gone TOO FAR.
                            instruction.IntData = int.Parse(splitBySpace[0]);
                            instruction.SecondIntData = int.Parse(splitBySpace[1]);
                        }


                        break;
                    case VMOpcode.RET:
                        // ???
                        break;
                    case VMOpcode.EXIT:
                        // ???
                        break;
                    case VMOpcode.POPZ:
                        // no data
                        break;
                    case VMOpcode.B:
                    case VMOpcode.BT:
                    case VMOpcode.BF:
                    case VMOpcode.PUSHENV:
                    case VMOpcode.POPENV:
                        var blockId = line.Substring(opcode.Length + 1)[1..^1];
                        if (blockId == "end")
                        {
                            instruction.JumpToEnd = true;
                        }
                        else if (blockId == "drop")
                        {
                            instruction.Drop = true;
                        }
                        else
                        {
                            instruction.IntData = int.Parse(blockId);
                        }
                        break;
                    case VMOpcode.PUSH:
                    case VMOpcode.PUSHLOC:
                    case VMOpcode.PUSHGLB:
                    case VMOpcode.PUSHBLTN:
                    case VMOpcode.PUSHI:
                        var value = line.Substring(opcode.Length + 1);
                        switch (instruction.TypeOne)
                        {
                            case VMType.None:
                                // what
                                break;
                            case VMType.s:
                                var indexOfLast = value.LastIndexOf('@');
                                var removedAddress = value.Substring(0, indexOfLast);
                                var removedQuotes = removedAddress[1..^1];
                                // https://manual.gamemaker.io/monthly/en/GameMaker_Language/GML_Reference/Strings/Strings.htm
                                var stringData = removedQuotes
                                    .Replace(@"\n", "\n")
                                    .Replace(@"\\", @"\")
                                    .Replace("\\\"", "\"");
                                instruction.StringData = stringData;
                                break;
                            case VMType.i:
                                if (int.TryParse(value, out var intResult))
                                {
                                    instruction.IntData = intResult;
                                }
                                else
                                {
                                    // Probably dealing with text.
                                    instruction.StringData = value;
                                }
                                break;
                            case VMType.l:
                                instruction.LongData = long.Parse(value);
                                break;
                            case VMType.v:
                                instruction.StringData = value;
                                break;
                            case VMType.b:
                                // not used i think?
                                instruction.BoolData = bool.Parse(value);
                                break;
                            case VMType.d:
                                instruction.DoubleData = double.Parse(value);
                                break;
                            case VMType.e:
                                instruction.ShortData = short.Parse(value);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        break;
                    case VMOpcode.CALL:
                        var function = line.Substring(opcode.Length + 1);
                        var argcIndex = function.IndexOf("argc=");
                        var argumentCount = int.Parse(function[(argcIndex + 5)..^1]);
                        var functionName = function.Substring(0, function.IndexOf('('));
                        instruction.FunctionArgumentCount = argumentCount;
                        instruction.FunctionName = functionName;
                        break;
                    case VMOpcode.CALLV:
                        instruction.IntData = int.Parse(line.Substring(opcode.Length + 1));
                        break;
                    case VMOpcode.BREAK:
                        // ???
                        break;
                    case VMOpcode.SETOWNER:
                        break;
                    case VMOpcode.PUSHAF:
                        break;
                    case VMOpcode.POPAF:
                        break;
                    case VMOpcode.SAVEAREF:
                        break;
                    case VMOpcode.RESTOREAREF:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                asset.Instructions.Add(instruction);
            }
        }

        return asset;
    }

    public static void ExportPages(FileStream stream, UndertaleData data)
    {
        Console.Write($"Exporting texture pages...");

        if (DecompressOnConvert)
        {
            StbImage.stbi_set_flip_vertically_on_load(0);
        }

        stream.Write(data.EmbeddedTextures.Count);
        foreach (var page in data.EmbeddedTextures)
        {
            var asset = new TexturePage
            {
                Name = page.Name.Content,
                Width = page.TextureData.Width,
                Height = page.TextureData.Height,
                Data = page.TextureData.TextureBlob
            };
            if (DecompressOnConvert)
            {
                asset.Data = ImageResult.FromMemory(asset.Data, ColorComponents.RedGreenBlueAlpha).Data;
            }

            stream.Write(asset);
        }
        Console.WriteLine($" Done!");
    }

    public static void ConvertSprites(FileStream stream, IList<UndertaleSprite> sprites)
    {
        Console.Write($"Converting sprites...");

        stream.Write(sprites.Count);
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
                    SourcePosX = item.Texture.SourceX,
                    SourcePosY = item.Texture.SourceY,
                    SourceSizeX = item.Texture.SourceWidth,
                    SourceSizeY = item.Texture.SourceHeight,
                    TargetPosX = item.Texture.TargetX,
                    TargetPosY = item.Texture.TargetY,
                    TargetSizeX = item.Texture.TargetWidth,
                    TargetSizeY = item.Texture.TargetHeight,
                    BSizeX = item.Texture.BoundingWidth,
                    BSizeY = item.Texture.BoundingHeight,
                    Page = item.Texture.TexturePage.Name.Content
                };

                asset.Textures.Add(pageItem);
            }

            foreach (var item in sprite.CollisionMasks)
            {
                asset.CollisionMasks.Add(item.Data);
            }

            stream.Write(asset);
        }

        Console.WriteLine($" Done!");
    }

    // TODO: put in datawin
    public static void ExportAssetOrder(FileStream stream, UndertaleData data)
    {
        Console.Write($"Exporting asset order...");

        var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "Output", "asset_names.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        using (StreamWriter writer = new StreamWriter(outputPath))
        {
            // Write Sounds.
            writer.WriteLine("@@sounds@@");
            if (data.Sounds.Count > 0)
            {
                foreach (UndertaleSound sound in data.Sounds)
                    writer.WriteLine(sound.Name.Content);
            }
            // Write Sprites.
            writer.WriteLine("@@sprites@@");
            if (data.Sprites.Count > 0)
            {
                foreach (var sprite in data.Sprites)
                    writer.WriteLine(sprite.Name.Content);
            }

            // Write Backgrounds.
            writer.WriteLine("@@backgrounds@@");
            if (data.Backgrounds.Count > 0)
            {
                foreach (var background in data.Backgrounds)
                    writer.WriteLine(background.Name.Content);
            }

            // Write Paths.
            writer.WriteLine("@@paths@@");
            if (data.Paths.Count > 0)
            {
                foreach (UndertalePath path in data.Paths)
                    writer.WriteLine(path.Name.Content);
            }

            // Write Code.
            writer.WriteLine("@@code@@");
            var codes = data.Code.Where(c => c.ParentEntry is null).ToList();
            if (codes.Count > 0)
            {
                foreach (UndertaleCode code in codes)
                    writer.WriteLine(code.Name.Content);
            }

            // Write Fonts.
            writer.WriteLine("@@fonts@@");
            if (data.Fonts.Count > 0)
            {
                foreach (UndertaleFont font in data.Fonts)
                    writer.WriteLine(font.Name.Content);
            }

            // Write Objects.
            writer.WriteLine("@@objects@@");
            if (data.GameObjects.Count > 0)
            {
                foreach (UndertaleGameObject gameObject in data.GameObjects)
                    writer.WriteLine(gameObject.Name.Content);
            }

            // Write Timelines.
            writer.WriteLine("@@timelines@@");
            if (data.Timelines.Count > 0)
            {
                foreach (UndertaleTimeline timeline in data.Timelines)
                    writer.WriteLine(timeline.Name.Content);
            }

            // Write Rooms.
            writer.WriteLine("@@rooms@@");
            if (data.Rooms.Count > 0)
            {
                foreach (UndertaleRoom room in data.Rooms)
                    writer.WriteLine(room.Name.Content);
            }

            // Write Shaders.
            writer.WriteLine("@@shaders@@");
            if (data.Shaders.Count > 0)
            {
                foreach (UndertaleShader shader in data.Shaders)
                    writer.WriteLine(shader.Name.Content);
            }

            // Write Extensions.
            writer.WriteLine("@@extensions@@");
            if (data.Extensions.Count > 0)
            {
                foreach (UndertaleExtension extension in data.Extensions)
                    writer.WriteLine(extension.Name.Content);
            }

            // TODO: Perhaps detect GMS2.3, export those asset names as well.
        }
        Console.WriteLine($" Done!");
    }

    public static void ExportObjectDefinitions(FileStream stream, UndertaleData data)
    {
        Console.Write($"Exporting object definitions...");

        stream.Write(data.GameObjects.Count);
        for (var i = 0; i < data.GameObjects.Count; i++)
        {
            var obj = data.GameObjects[i];

            var asset = new ObjectDefinition();
            asset.Name = obj.Name.Content;
            asset.AssetId = i;
            asset.sprite = data.Sprites.IndexOf(obj.Sprite);
            asset.visible = obj.Visible;
            asset.solid = obj.Solid;
            asset.persistent = obj.Persistent;
            asset.textureMaskId = data.Sprites.IndexOf(obj.TextureMaskId);

            var storage = new ObjectDefinitionStorage();
            storage.ParentID = data.GameObjects.IndexOf(obj.ParentId);

            var exportableCode = data.Code.Where(c => c.ParentEntry is null).ToList();

            int GetCodeID(EventType type, int eventSubtype)
            {
                var eventContainer = obj.Events[(int)type - 1]; // -1 because they have a None entry in the enum for some reason

                if (eventContainer == null || eventContainer.Count <= eventSubtype)
                {
                    return -1;
                }

                var subtypeContainer = eventContainer[eventSubtype];

                var action = subtypeContainer?.Actions[0];

                return action == null ? -1 : exportableCode.IndexOf(action.CodeId);
            }

            storage.CreateScriptID = GetCodeID(EventType.Create, 0);
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

            // keyboard
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

            // keypress
            // keyrelease
            // trigger
            storage.CleanUpScriptID = GetCodeID(EventType.CleanUp, 0);
            // gesture
            storage.PreCreateScriptID = GetCodeID(EventType.PreCreate, 0);

            asset.FileStorage = storage;

            stream.Write(asset);
        }
        Console.WriteLine(" Done!");
    }

    public static int CurrentElementID = 0;

    public static void ExportRooms(FileStream stream, UndertaleData data)
    {
        Console.Write($"Exporting rooms...");

        var codes = data.Code.Where(c => c.ParentEntry is null).ToList();

        stream.Write(data.Rooms.Count);
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
                            PreCreateCodeID = codes.IndexOf(instance.PreCreateCode),
                        };

                        layerasset.Elements.Add(objectAsset);
                    }
                }
                else if (layer.LayerType == UndertaleRoom.LayerType.Background)
                {
                    var col4 = ((int)layer.BackgroundData.Color).ABGRToCol4();

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
                        Stretch = layer.BackgroundData.Stretch,
                        Color = (int)layer.BackgroundData.Color,
                        Alpha = col4.A,
                        FirstFrame = (int)layer.BackgroundData.FirstFrame,
                        AnimationSpeed = layer.BackgroundData.AnimationSpeed,
                        AnimationSpeedType = layer.BackgroundData.AnimationSpeedType
                    };

                    layerasset.Elements.Add(backgroundElement);
                }
                else
                {
                    DebugLog.LogError($"Don't know how to handle layer type {layer.LayerType}");
                }

                asset.Layers.Add(layerasset);
            }

            stream.Write(asset);
        }
        Console.WriteLine(" Done!");
    }

    public static void ExportFonts(FileStream stream, UndertaleData data)
    {
        Console.Write($"Exporting fonts...");

        stream.Write(data.Fonts.Count);
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
                SourcePosX = item.Texture.SourceX,
                SourcePosY = item.Texture.SourceY,
                SourceSizeX = item.Texture.SourceWidth,
                SourceSizeY = item.Texture.SourceHeight,
                TargetPosX = item.Texture.TargetX,
                TargetPosY = item.Texture.TargetY,
                TargetSizeX = item.Texture.TargetWidth,
                TargetSizeY = item.Texture.TargetHeight,
                BSizeX = item.Texture.BoundingWidth,
                BSizeY = item.Texture.BoundingHeight,
                Page = item.Texture.TexturePage.Name.Content
            };

            fontAsset.texture = pageItem;

            foreach (var glyph in item.Glyphs)
            {
                var glyphAsset = new Glyph
                {
                    characterIndex = glyph.Character,
                    x = glyph.SourceX,
                    y = glyph.SourceY,
                    w = glyph.SourceWidth,
                    h = glyph.SourceHeight,
                    shift = glyph.Shift,
                    offset = glyph.Offset
                };

                // i cant remember why theres two of this lol
                fontAsset.entries.Add(glyphAsset);
                fontAsset.entriesDict.Add(glyphAsset.characterIndex, glyphAsset);
            }

            stream.Write(fontAsset);
        }
        Console.WriteLine(" Done!");
    }

    // TODO: get byte loading work in LoadSounds
    public static void ExportSounds(FileStream stream, UndertaleData data)
    {
        Console.Write($"Exporting sounds...");

        stream.Write(data.Sounds.Count);
        foreach (var item in data.Sounds)
        {
            var asset = new SoundAsset();
            asset.AssetID = data.Sounds.IndexOf(item);
            asset.Name = item.Name.Content;
            asset.Volume = item.Volume;
            asset.Pitch = 1; // going by the docs, im fairly certain item.Pitch is not used

            // https://github.com/UnderminersTeam/UndertaleModTool/blob/master/UndertaleModTool/Scripts/Resource%20Unpackers/ExportAllSounds.csx
            // ignore compressed for now
            {
                if (item.AudioID == -1)
                {
                    // external .ogg
                    asset.IsWav = false;
                    asset.Data = File.ReadAllBytes($"{asset.Name}.ogg");
                }
                else if (item.GroupID == data.GetBuiltinSoundGroupID())
                {
                    // embedded .wav
                    asset.IsWav = true;
                    var embeddedAudio = data.EmbeddedAudio;
                    asset.Data = embeddedAudio[item.AudioID].Data;
                }
                else
                {
                    // .wav in some audio group file
                    asset.IsWav = true;

                    var audioGroupPath = $"audiogroup{item.GroupID}.dat";
                    using var audioGroupStream = new FileStream(audioGroupPath, FileMode.Open, FileAccess.Read);
                    using var audioGroupData = UndertaleIO.Read(audioGroupStream);

                    var embeddedAudio = audioGroupData.EmbeddedAudio;
                    asset.Data = embeddedAudio[item.AudioID].Data;
                }
            }
            
            if (DecompressOnConvert)
            {
                if (asset.IsWav)
                {
                    try
                    {
                        // WaveFileReader doesnt work so have to write to file and then read :(
                        File.WriteAllBytes("TEMP_LOAD_SOUNDS_FILE", asset.Data);
                        
                        using var reader = new AudioFileReader("TEMP_LOAD_SOUNDS_FILE");
                        asset.Data = new byte[reader.Length];
                        reader.ReadExactly(asset.Data);
                        asset.Stereo = reader.WaveFormat.Channels == 2;
                        asset.Freq = reader.WaveFormat.SampleRate;
                    }
                    catch (Exception e)
                    {
                        // ch2 has some empty audio for some reason
                        DebugLog.LogWarning($"error loading wav {asset.Name}: {e.Message}");
                        asset.Data = new byte[] { };
                        asset.Stereo = false;
                        asset.Freq = 1;
                    }
                }
                else
                {
                    // VorbisWaveReader doesnt like me so we have to copy :(
                    using var vorbisStream = new MemoryStream(asset.Data);
                    using var reader = new VorbisReader(vorbisStream);
                    var floatData = new float[reader.TotalSamples * reader.Channels];
                    reader.ReadSamples(floatData);
                    asset.Data = new byte[System.Buffer.ByteLength(floatData)];
                    System.Buffer.BlockCopy(floatData, 0, asset.Data, 0, asset.Data.Length);
                    asset.Stereo = reader.Channels == 2;
                    asset.Freq = reader.SampleRate;
                }
            }

            stream.Write(asset);
        }
        
        File.Delete("TEMP_LOAD_SOUNDS_FILE");
        
        Console.WriteLine(" Done!");
    }

    public static void ExportTextureGroups(FileStream stream, UndertaleData data)
    {
        Console.Write($"Exporting texture groups...");

        stream.Write(data.TextureGroupInfo.Count);
        foreach (var group in data.TextureGroupInfo)
        {
            var asset = new TextureGroup();

            asset.GroupName = group.Name.Content;
            asset.TexturePages = group.TexturePages.Select(x => x.Resource.Name.Content).ToArray();
            asset.Sprites = group.Sprites.Select(x => data.Sprites.IndexOf(x.Resource)).ToArray();
            asset.Fonts = group.Fonts.Select(x => data.Fonts.IndexOf(x.Resource)).ToArray();

            stream.Write(asset);
        }

        Console.WriteLine(" Done!");
    }

    public static void ExportTileSets(FileStream stream, UndertaleData data)
    {
        Console.Write($"Exporting tile sets...");

        stream.Write(data.Backgrounds.Count);
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

            asset.Texture = new SpritePageItem
            {
                SourcePosX = set.Texture.SourceX,
                SourcePosY = set.Texture.SourceY,
                SourceSizeX = set.Texture.SourceWidth,
                SourceSizeY = set.Texture.SourceHeight,
                TargetPosX = set.Texture.TargetX,
                TargetPosY = set.Texture.TargetY,
                TargetSizeX = set.Texture.TargetWidth,
                TargetSizeY = set.Texture.TargetHeight,
                BSizeX = set.Texture.BoundingWidth,
                BSizeY = set.Texture.BoundingHeight,
                Page = set.Texture.TexturePage.Name.Content
            };
            
            stream.Write(asset);
        }

        Console.WriteLine(" Done!");
    }
}
