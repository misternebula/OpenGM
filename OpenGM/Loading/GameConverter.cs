using OpenGM.IO;
using OpenGM.SerializedFiles;
using OpenGM.VirtualMachine;
using OpenTK.Mathematics;
using System.Text;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using EventType = OpenGM.VirtualMachine.EventType;

namespace OpenGM.Loading;

/// <summary>
/// Converts the UTMT data into our custom formats, which are saved into files
/// </summary>
public static class GameConverter
{
    public static void ConvertGame(UndertaleData data)
    {
        Console.WriteLine($"Converting game assets...");

        using var stream = File.OpenWrite(Path.Combine(Entry.DataWinFolder, "data_OpenGM.win"));
        using var writer = new BinaryWriter(stream);

        try
        {
            // must match order of gameloader
            ExportGeneralInfo(writer, data);
            ExportAssetOrder(writer, data);
            ConvertScripts(writer, data);
            ConvertCode(writer, data, data.Code);
            ExportExtensions(writer, data);
            ExportGlobalInitCode(writer, data);
            ExportObjectDefinitions(writer, data);
            ExportBackgrounds(writer, data);
            ExportRooms(writer, data);
            ConvertSprites(writer, data, data.Sprites);
            ExportFonts(writer, data);
            ExportPages(writer, data);
            ExportTextureGroups(writer, data);
            ExportTileSets(writer, data);
            ExportSounds(writer, data);
            ExportPaths(writer, data);
            ExportShaders(writer, data);
            ExportAnimCurves(writer, data);
        }
        catch
        {
            writer.Close();
            File.Delete(Path.Combine(Entry.DataWinFolder, "data_OpenGM.win"));
            throw;
        }

        GC.Collect(); // gc after doing a buncha loading
    }

    public static void ExportGeneralInfo(BinaryWriter writer, UndertaleData data)
    {
        var asset = new GameData()
        {
            Filename = data.GeneralInfo.FileName.Content,
            LastObjectID = (int)data.GeneralInfo.LastObj,
            LastTileID = (int)data.GeneralInfo.LastTile,
            Name = data.GeneralInfo.Name.Content,
            BranchType = (BranchType)data.GeneralInfo.Branch,
            Major = data.GeneralInfo.Major,
            Minor = data.GeneralInfo.Minor,
            Release = data.GeneralInfo.Release,
            Build = data.GeneralInfo.Build,
            DefaultWindowSize = new Vector2i((int)data.GeneralInfo.DefaultWindowWidth, (int)data.GeneralInfo.DefaultWindowHeight),
            FPS = data.GeneralInfo.GMS2FPS,
            RoomOrder = data.GeneralInfo.RoomOrder.Select(p => p.CachedId).ToArray(),
            IsYYC = data.IsYYC(),
            Config = data.GeneralInfo.Config.Content
        };
        writer.WriteMemoryPack(asset);
    }

    public static void ConvertScripts(BinaryWriter writer, UndertaleData data)
    {
        var scripts = data.Scripts;

        Console.Write($"Converting scripts...");
        writer.Write(scripts.Count);
        foreach (var script in scripts)
        {
            var asset = new VMScript();
            asset.AssetIndex = scripts.IndexOf(script);
            asset.Name = script.Name.Content;

            if (script.Code != null)
            {
                asset.CodeIndex = data.Code.IndexOf(script.Code);
            }

            //asset.IsGlobalInit = data.GlobalInitScripts.Select(x => x.Code).Contains(script.Code);

            writer.WriteMemoryPack(asset);
        }

        Console.WriteLine($" Done!");
    }

    public static void ConvertCode(BinaryWriter writer, UndertaleData data, IList<UndertaleCode> codes)
    {
        Console.Write($"Converting code...");
        writer.Write(codes.Count);
        foreach (var code in codes)
        {
            UndertaleCodeLocals? codeLocals = null;
            if (data.CodeLocals != null)
            {
                codeLocals = data.CodeLocals.For(code);
            }

            var asmFile = code.Disassemble(data.Variables, codeLocals);

            var asset = ConvertAssembly(asmFile);

            asset.AssetId = codes.IndexOf(code);
            asset.Name = code.Name.Content;

            if (code.ParentEntry != null)
            {
                asset.ParentAssetId = codes.IndexOf(code.ParentEntry);
            }
            else
            {
                asset.ParentAssetId = -1;
            }

            writer.WriteMemoryPack(asset);
        }

        Console.WriteLine($" Done!");
    }

    public static void ExportExtensions(BinaryWriter writer, UndertaleData data)
    {
        var extensions = data.Extensions;
        if (extensions is null)
        {
            writer.Write(0);
            return;
        }

        Console.Write($"Exporting extensions...");
        writer.Write(extensions.Count);
        foreach (var extension in extensions)
        {
            var asset = new Extension();
            asset.Name = extension.Name.Content;
            
            foreach (var file in extension.Files)
            {
                var fileAsset = new ExtensionFile();
                fileAsset.Name = file.Filename.Content;
                fileAsset.Kind = (ExtensionKind)file.Kind;

                foreach (var function in file.Functions)
                {
                    var funcAsset = new ExtensionFunction();
                    funcAsset.Id = function.ID;
                    funcAsset.Name = function.Name.Content;
                    funcAsset.ExternalName = function.ExtName.Content;
                    funcAsset.ReturnType = (ExtensionVarType)function.RetType;

                    foreach (var arg in function.Arguments)
                    {
                        funcAsset.Arguments.Add((ExtensionVarType)arg.Type);
                    }

                    fileAsset.Functions.Add(funcAsset);
                }

                asset.Files.Add(fileAsset);
            }

            writer.WriteMemoryPack(asset);
        }

        Console.WriteLine($" Done!");
    }

    public static void ExportGlobalInitCode(BinaryWriter writer, UndertaleData data)
    {
        if (data.GlobalInitScripts is null)
        {
            writer.Write(0);
            return;
        }

        writer.Write(data.GlobalInitScripts.Count);

        foreach (var item in data.GlobalInitScripts)
        {
            writer.Write(data.Code.IndexOf(item.Code));
        }
    }

    public static VMCode ConvertAssembly(string asmFile)
    {
        var asmFileLines = asmFile.SplitLines();

        var localVariables = new List<string>();

        var startLine = -1;
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

        var asset = new VMCode();
        asset.LocalVariables = localVariables;

        if (startLine == -1)
        {
            // this is the case for script functions. they are empty and have parent as script asset

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

                //var enumOperation = (VMOpcode)Enum.Parse(typeof(VMOpcode), operation.ToUpper());
                if (!Enum.TryParse(typeof(VMOpcode), operation.ToUpper(), true, out var enumOperation))
                {
                    throw new NotImplementedException($"Unknown opcode! Trying to parse {opcode} from line {line}");
                }

                var instruction = new VMCodeInstruction
                {
                    Raw = line,
                    Opcode = (VMOpcode)enumOperation,
                    TypeOne = types.Length >= 1 ? (VMType)Enum.Parse(typeof(VMType), types[0]) : VMType.None,
                    TypeTwo = types.Length == 2 ? (VMType)Enum.Parse(typeof(VMType), types[1]) : VMType.None,
                };

                var shouldGetVariableInfo = false;

                switch (enumOperation)
                {
                    case VMOpcode.CHKINDEX:
                        // no data
                        break;
                    case VMOpcode.CHKNULLISH:
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
                        if (instruction.TypeOne != VMType.e)
                        {
                            shouldGetVariableInfo = true;
                        }
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

                                    if (value.StartsWith("[function]"))
                                    {
                                        instruction.PushFunction = true;
                                        instruction.StringData = value[10..];
                                    }
                                    
                                }

                                break;
                            case VMType.l:
                                instruction.LongData = long.Parse(value);
                                break;
                            case VMType.v:
                                instruction.StringData = value;
                                shouldGetVariableInfo = true;
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
                    case VMOpcode.SETSTATIC:
                        break;
                    case VMOpcode.ISSTATICOK:
                        break;
                    case VMOpcode.PUSHAC:
                        break;
                    case VMOpcode.PUSHREF:
                    {
                        var parameterString = line.Substring(opcode.Length + 1);
                        var parameters = parameterString.Split(' ');
                        if (parameters.Length == 1)
                        {
                            instruction.IntData = int.Parse(parameterString);
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }

                        break;
                    }
                    default:
                        throw new NotImplementedException($"Opcode {enumOperation} not implemented");
                }

                if (shouldGetVariableInfo)
                {
                    GetVariableInfo(instruction, out var variableName, out var variableType, out var variablePrefix, out var assetId);
                    instruction.variableName = variableName;
                    instruction.variableType = variableType;
                    instruction.variablePrefix = variablePrefix;
                    instruction.assetId = assetId;
                }

                asset.Instructions.Add(instruction);
            }
        }

        return asset;
    }

    public static void GetVariableInfo(VMCodeInstruction instruction, out string variableName, out VariableType variableType, out VariablePrefix prefix, out int assetIndex)
    {
        variableName = instruction.StringData;
        instruction.StringData = null!; // stuff moved to above, save space here
        prefix = VariablePrefix.None;

        var indexingArray = variableName.StartsWith("[array]");
        if (indexingArray)
        {
            prefix = VariablePrefix.Array;
            variableName = variableName[7..]; // skip [array]
        }

        var stackTop = variableName.StartsWith("[stacktop]");
        if (stackTop)
        {
            prefix = VariablePrefix.Stacktop;
            variableName = variableName[10..]; // skip [stacktop]
        }

        var arraypopaf = variableName.StartsWith("[arraypopaf]");
        if (arraypopaf)
        {
            prefix = VariablePrefix.ArrayPopAF;
            variableName = variableName[12..]; // skip [arraypopaf]
        }

        var arraypushaf = variableName.StartsWith("[arraypushaf]");
        if (arraypushaf)
        {
            prefix = VariablePrefix.ArrayPushAF;
            variableName = variableName[13..]; // skip [arraypushaf]
        }

        variableType = VariableType.None;

        assetIndex = -1;
        var split = variableName.Split('.');

        if (split.Length == 3)
        {
            // weird thing
            var instanceId = GMConstants.FIRST_INSTANCE_ID + int.Parse(split[0]);
            variableName = split[2];
            if (split[1] != "[instance]self")
            {
                throw new NotImplementedException();
            }

            assetIndex = instanceId;
            variableType = VariableType.Index;
            return;
        }

        if (split.Length == 1)
        {
            variableType = VariableType.Index;
            assetIndex = 0;
            return;
        }

        var context = split[0];
        variableName = split[1];

        if (context == "global")
        {
            variableType = VariableType.Global;
        }
        else if (context == "local")
        {
            variableType = VariableType.Local;
        }
        else if (context == "self")
        {
            variableType = VariableType.Self;
        }
        else if (context == "other")
        {
            variableType = VariableType.Other;
        }
        else if (context == "builtin")
        {
            variableType = VariableType.BuiltIn;
        }
        else if (context == "arg")
        {
            variableType = VariableType.Argument;
        }
        else if (context == "stacktop")
        {
            variableType = VariableType.Stacktop;
        }
        else if (context == "static")
        {
            variableType = VariableType.Static;
        }
        else if (int.TryParse(context, out var index))
        {
            variableType = VariableType.Index;
            assetIndex = index;
        }
        else
        {
            throw new NotImplementedException($"Unknown variable type : {context} - {instruction.Raw}");
        }
    }

    public static void ExportPages(BinaryWriter writer, UndertaleData data)
    {
        Console.Write($"Exporting texture pages...");

        writer.Write(data.EmbeddedTextures.Count);
        foreach (var page in data.EmbeddedTextures)
        {
            // dont even need a class for this
            var pageName = page.Name.Content;
            var blob = page.TextureData.Image.ConvertToPng().ToSpan();
            writer.Write(pageName);
            writer.Write(blob.Length);
            writer.Write(blob);
        }

        Console.WriteLine($" Done!");
    }

    public static void ConvertSprites(BinaryWriter writer, UndertaleData data, IList<UndertaleSprite> sprites)
    {
        Console.Write($"Converting sprites...");

        writer.Write(sprites.Count);
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

            writer.WriteMemoryPack(asset);
        }

        Console.WriteLine($" Done!");
    }

    public static void ExportAssetOrder(BinaryWriter writer, UndertaleData data)
    {
        Console.Write($"Exporting asset order...");

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
        writer.Write(streamWriter.ToString());
        Console.WriteLine($" Done!");
    }

    public static void ExportObjectDefinitions(BinaryWriter writer, UndertaleData data)
    {
        Console.Write($"Exporting object definitions...");

        writer.Write(data.GameObjects.Count);
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

            storage.MouseScriptIDs = new();
            foreach (var subtypeContainer in obj.Events[(int)EventType.Mouse - 1])
            {
                storage.MouseScriptIDs.Add(subtypeContainer.EventSubtypeMouse, exportableCode.IndexOf(subtypeContainer.Actions[0].CodeId));
            }

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

            writer.WriteMemoryPack(asset);
        }

        Console.WriteLine(" Done!");
    }

    public static int CurrentElementID = 0;

    public static void ExportRooms(BinaryWriter writer, UndertaleData data)
    {
        Console.Write($"Exporting rooms...");

        var codes = data.Code.ToList();

        writer.Write(data.Rooms.Count);
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
                EnableViews = room.Flags.HasFlag(UndertaleRoom.RoomEntryFlags.EnableViews)
            };

            for (var i = 0; i < 8; i++)
            {
                var view = room.Views[i];

                asset.Views[i] = new View()
                {
                    Enabled = view.Enabled,
                    PositionX = view.ViewX,
                    PositionY = view.ViewY,
                    SizeX = view.ViewWidth,
                    SizeY = view.ViewHeight,
                    PortPositionX = view.PortX,
                    PortPositionY = view.PortY,
                    PortSizeX = view.PortWidth,
                    PortSizeY = view.PortHeight,
                    BorderX = (int)view.BorderX,
                    BorderY = (int)view.BorderY,
                    SpeedX = view.SpeedX,
                    SpeedY = view.SpeedY,
                    FollowsObject = data.GameObjects.IndexOf(view.ObjectId)
                };
            }

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
                            Id = (int)instance.InstanceID,
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
                var objectAsset = new GameObject
                {
                    Type = ElementType.Instance,
                    Id = (int)instance.InstanceID,
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

                asset.GameObjects.Add(objectAsset);
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

            writer.WriteMemoryPack(asset);
        }

        Console.WriteLine(" Done!");
    }

    public static void ExportFonts(BinaryWriter writer, UndertaleData data)
    {
        Console.Write($"Exporting fonts...");
        writer.Write(data.Fonts.Count);
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
                fontAsset.entriesDict[glyphAsset.characterIndex] = glyphAsset;
            }

            writer.WriteMemoryPack(fontAsset);
        }

        Console.WriteLine(" Done!");
    }

    public static void ExportSounds(BinaryWriter writer, UndertaleData data)
    {
        Console.Write($"Exporting sounds...");

        writer.Write(data.Sounds.Count);
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

            writer.WriteMemoryPack(asset);

            writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        Console.WriteLine(" Done!");
    }

    public static void ExportTextureGroups(BinaryWriter writer, UndertaleData data)
    {
        Console.Write($"Exporting texture groups...");

        if (data.TextureGroupInfo == null)
        {
            writer.Write(0);
            return;
        }

        writer.Write(data.TextureGroupInfo.Count);
        foreach (var group in data.TextureGroupInfo)
        {
            var asset = new TextureGroup();

            asset.GroupName = group.Name.Content;
            asset.TexturePages = group.TexturePages.Select(x => x.Resource.Name.Content).ToArray();
            asset.Sprites = group.Sprites.Select(x => data.Sprites.IndexOf(x.Resource)).ToArray();
            asset.Fonts = group.Fonts.Select(x => data.Fonts.IndexOf(x.Resource)).ToArray();

            writer.WriteMemoryPack(asset);
        }

        Console.WriteLine(" Done!");
    }

    public static void ExportTileSets(BinaryWriter writer, UndertaleData data)
    {
        Console.Write($"Exporting tile sets...");

        if (data.Backgrounds == null)
        {
            writer.Write(0);
            return;
        }

        writer.Write(data.Backgrounds.Count);
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

            writer.WriteMemoryPack(asset);
        }

        Console.WriteLine(" Done!");
    }

    public static void ExportPaths(BinaryWriter writer, UndertaleData data)
    {
        Console.Write($"Exporting paths...");

        writer.Write(data.Paths.Count);
        foreach (var path in data.Paths)
        {
            var asset = new GMPath
            {
                Name = path.Name.Content,
                IsSmooth = path.IsSmooth,
                IsClosed = path.IsClosed,
                Precision = (int)path.Precision
            };

            foreach (var point in path.Points)
            {
                asset.Points.Add(new PathPoint() { x = point.X, y = point.Y, speed = point.Speed});
            }

            writer.WriteMemoryPack(asset);
        }

        Console.WriteLine(" Done!");
    }

    public static void ExportBackgrounds(BinaryWriter writer, UndertaleData data)
    {
        Console.Write($"Exporting backgrounds...");

        writer.Write(data.Backgrounds.Count);
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

            writer.WriteMemoryPack(asset);
        }
        Console.WriteLine(" Done!");
    }

    public static void ExportShaders(BinaryWriter writer, UndertaleData data)
    {
        Console.Write($"Exporting shaders...");

        writer.Write(data.Shaders.Count(x => x != null));
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

            /*switch (shader.Type)
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
            }*/

            // just always use glsl because we use opengl
            asset.VertexSource = shader.GLSL_Vertex.Content;
            asset.FragmentSource = shader.GLSL_Fragment.Content;

            asset.ShaderAttributes = new string[shader.VertexShaderAttributes.Count];
            for (var i = 0; i < shader.VertexShaderAttributes.Count; i++)
            {
                asset.ShaderAttributes[i] = shader.VertexShaderAttributes[i].Name.Content;
            }

            writer.WriteMemoryPack(asset);
        }

        Console.WriteLine(" Done!");
    }

    public static void ExportAnimCurves(BinaryWriter writer, UndertaleData data)
    {
        if (data.AnimationCurves is null)
        {
            writer.Write(0);
            return;
        }

        Console.Write($"Exporting animation curves...");
        
        writer.Write(data.AnimationCurves.Count);
        foreach (var animcurve in data.AnimationCurves)
        {
            var asset = new AnimCurve();
            asset.AssetIndex = data.AnimationCurves.IndexOf(animcurve);
            asset.Name = animcurve.Name.Content;

            foreach (var channel in animcurve.Channels)
            {
                var channelAsset = new AnimCurveChannel
                {
                    Name = channel.Name.Content,
                    CurveType = (CurveType)channel.Curve,
                    Iterations = (int)channel.Iterations
                };

                foreach (var point in channel.Points)
                {
                    var pointAsset = new AnimCurvePoint { 
                        X = point.X, 
                        Y = point.Value, 
                        BezierX0 = point.BezierX0, 
                        BezierY0 = point.BezierY0,
                        BezierX1 = point.BezierX1,
                        BezierY1 = point.BezierY1
                    };

                    channelAsset.Points.Add(pointAsset);
                }

                asset.Channels.Add(channelAsset);
            }

            writer.WriteMemoryPack(asset);
        }
    }
}