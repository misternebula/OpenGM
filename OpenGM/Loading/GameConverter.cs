using MemoryPack;
using OpenGM.SerializedFiles;
using OpenGM.VirtualMachine;
using System.Diagnostics;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using EventType = OpenGM.VirtualMachine.EventType;
using OpenGM.IO;
using System.Numerics;
using System.Text;
using System.Linq;
using OpenGM.Rendering;
using OpenTK.Mathematics;

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

		// must match order of gameloader
		ExportGeneralInfo(writer, data);
		ExportAssetOrder(writer, data);
		ConvertScripts(writer, data);
		ConvertCode(writer, data, data.Code);
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
			FPS = data.GeneralInfo.GMS2FPS

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
			var asmFile = code.Disassemble(data.Variables, data.CodeLocals.For(code));

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

	public static void ExportGlobalInitCode(BinaryWriter writer, UndertaleData data)
	{
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

				var enumOperation = (VMOpcode)Enum.Parse(typeof(VMOpcode), operation.ToUpper());

				var instruction = new VMCodeInstruction
				{
					Raw = line,
					Opcode = enumOperation,
					TypeOne = types.Length >= 1 ? (VMType)Enum.Parse(typeof(VMType), types[0]) : VMType.None,
					TypeTwo = types.Length == 2 ? (VMType)Enum.Parse(typeof(VMType), types[1]) : VMType.None,
				};

				var shouldGetVariableInfo = false;

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
					default:
						throw new ArgumentOutOfRangeException();
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
			variableType = VariableType.None;
			variableName = split[0];
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

			writer.WriteMemoryPack(asset);
		}

		Console.WriteLine($" Done!");
	}

	public static void ExportAssetOrder(BinaryWriter writer, UndertaleData data)
	{
		Console.Write($"Exporting asset order...");

		// jank, but it works and its fast to load
		var streamWriter = new StringBuilder();
		{
			// https://github.com/UnderminersTeam/UndertaleModTool/blob/4e98560d3eea85dbeac89285c9bebe1385c3207c/UndertaleModTool/Scripts/Resource%20Unpackers/ExportAssetOrder.csx

			// Write Sounds.
			streamWriter.AppendLine("@@sounds@@");
			if (data.Sounds.Count > 0)
			{
				foreach (UndertaleSound sound in data.Sounds)
					streamWriter.AppendLine(sound.Name.Content);
			}

			// Write Sprites.
			streamWriter.AppendLine("@@sprites@@");
			if (data.Sprites.Count > 0)
			{
				foreach (var sprite in data.Sprites)
					streamWriter.AppendLine(sprite.Name.Content);
			}

			// Write Backgrounds.
			streamWriter.AppendLine("@@backgrounds@@");
			if (data.Backgrounds.Count > 0)
			{
				foreach (var background in data.Backgrounds)
					streamWriter.AppendLine(background.Name.Content);
			}

			// Write Paths.
			streamWriter.AppendLine("@@paths@@");
			if (data.Paths.Count > 0)
			{
				foreach (UndertalePath path in data.Paths)
					streamWriter.AppendLine(path.Name.Content);
			}

			// Write Scripts.
			streamWriter.AppendLine("@@scripts@@");
			if (data.Scripts.Count > 0)
			{
				foreach (UndertaleScript script in data.Scripts)
					streamWriter.AppendLine(script.Name.Content);
			}

			// Write Fonts.
			streamWriter.AppendLine("@@fonts@@");
			if (data.Fonts.Count > 0)
			{
				foreach (UndertaleFont font in data.Fonts)
					streamWriter.AppendLine(font.Name.Content);
			}

			// Write Objects.
			streamWriter.AppendLine("@@objects@@");
			if (data.GameObjects.Count > 0)
			{
				foreach (UndertaleGameObject gameObject in data.GameObjects)
					streamWriter.AppendLine(gameObject.Name.Content);
			}

			// Write Timelines.
			streamWriter.AppendLine("@@timelines@@");
			if (data.Timelines.Count > 0)
			{
				foreach (UndertaleTimeline timeline in data.Timelines)
					streamWriter.AppendLine(timeline.Name.Content);
			}

			// Write Rooms.
			streamWriter.AppendLine("@@rooms@@");
			if (data.Rooms.Count > 0)
			{
				foreach (UndertaleRoom room in data.Rooms)
					streamWriter.AppendLine(room.Name.Content);
			}

			// Write Shaders.
			streamWriter.AppendLine("@@shaders@@");
			if (data.Shaders.Count > 0)
			{
				foreach (UndertaleShader shader in data.Shaders)
					streamWriter.AppendLine(shader.Name.Content);
			}

			// Write Extensions.
			streamWriter.AppendLine("@@extensions@@");
			if (data.Extensions.Count > 0)
			{
				foreach (UndertaleExtension extension in data.Extensions)
					streamWriter.AppendLine(extension.Name.Content);
			}

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
					SourcePosX = background.Texture.SourceX,
					SourcePosY = background.Texture.SourceY,
					SourceSizeX = background.Texture.SourceWidth,
					SourceSizeY = background.Texture.SourceHeight,
					TargetPosX = background.Texture.TargetX,
					TargetPosY = background.Texture.TargetY,
					TargetSizeX = background.Texture.TargetWidth,
					TargetSizeY = background.Texture.TargetHeight,
					BSizeX = background.Texture.BoundingWidth,
					BSizeY = background.Texture.BoundingHeight,
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

		writer.Write(data.Shaders.Count);
		foreach (var shader in data.Shaders)
		{
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

			writer.WriteMemoryPack(asset);
		}

		Console.WriteLine(" Done!");
	}
}
