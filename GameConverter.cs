using DELTARUNITYStandalone.SerializedFiles;
using DELTARUNITYStandalone.VirtualMachine;
using Newtonsoft.Json;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using EventType = DELTARUNITYStandalone.VirtualMachine.EventType;

namespace DELTARUNITYStandalone;

/// <summary>
/// Converts the UTMT data into our custom formats, which are saved into files
/// </summary>
public static class GameConverter
{
	public static void ConvertGame(UndertaleData data)
	{
		Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Output"));

		Console.WriteLine($"Converting game assets...");
		ConvertScripts(data, data.Code.Where(c => c.ParentEntry is null).ToList());

		ExportPages(data);

		ConvertSprites(data.Sprites);

		ExportAssetOrder(data);

		ExportObjectDefinitions(data);

		ExportRooms(data);

		ExportFonts(data);

		ExportSounds(data);

		ExportTextureGroups(data);
	}

	public static void ConvertScripts(UndertaleData data, List<UndertaleCode> codes)
	{
		Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Output", "Scripts"));

		Console.Write($"Converting scripts...");
		foreach (var code in codes)
		{
			var saveDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Output", "Scripts", $"{code.Name.Content}.json");

			var asmFile = code.Disassemble(data.Variables, data.CodeLocals.For(code));
			var asmFileLines = asmFile.Split(Environment.NewLine);

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
			asset.AssetId = codes.IndexOf(code);
			asset.LocalVariables = localVariables;
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

			if (startLine == -1)
			{
				// no code in file???

				asset.Instructions = new();
				asset.Labels = new() { { 0, new Label() { InstructionIndex = 0 } } };
				File.WriteAllText(saveDirectory, JsonConvert.SerializeObject(asset, Formatting.Indented));

				continue;
			}

			asmFileLines = asmFileLines.Skip(startLine).ToArray();
			asmFileLines = asmFileLines.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

			string functionLabelAtNextLine = null;

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

					var label = new Label() { InstructionIndex = asset.Instructions.Count };

					if (functionLabelAtNextLine != null)
					{
						label.FunctionName = functionLabelAtNextLine;
						functionLabelAtNextLine = null;
					}

					asset.Labels.Add(int.Parse(id), label);
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
									var stringData = removedQuotes.Replace(@"\\", @"\");
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

			File.WriteAllText(saveDirectory, JsonConvert.SerializeObject(asset, Formatting.Indented));
		}
		Console.WriteLine($" Done!");
	}

	public static void ExportPages(UndertaleData data)
	{
		Console.Write($"Exporting texture pages...");
		var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "Output", "Pages");
		Directory.CreateDirectory(outputPath);

		foreach (var page in data.EmbeddedTextures)
		{
			var pageName = page.Name.Content;
			var blob = page.TextureData.TextureBlob;
			var texPath = Path.Combine(outputPath, $"{pageName}.png");
			File.WriteAllBytes(texPath, blob);
		}
		Console.WriteLine($" Done!");
	}

	public static void ConvertSprites(IList<UndertaleSprite> sprites)
	{
		Console.Write($"Converting sprites...");

		var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "Output", "Sprites");
		Directory.CreateDirectory(outputPath);

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

			var saveDirectory = Path.Combine(outputPath, $"{sprite.Name.Content}.json");
			File.WriteAllText(saveDirectory, JsonConvert.SerializeObject(asset, Formatting.Indented));
		}

		Console.WriteLine($" Done!");
	}

	public static void ExportAssetOrder(UndertaleData data)
	{
		Console.Write($"Exporting asset order...");

		var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "Output", "asset_names.txt");

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

	public static void ExportObjectDefinitions(UndertaleData data)
	{
		Console.Write($"Exporting object definitions...");
		var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "Output", "Objects");
		Directory.CreateDirectory(outputPath);

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

			var saveDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Output", "Objects", $"{asset.Name}.json");
			File.WriteAllText(saveDirectory, JsonConvert.SerializeObject(asset, Formatting.Indented));
		}
		Console.WriteLine(" Done!");
	}

	public static void ExportRooms(UndertaleData data)
	{
		Console.Write($"Exporting rooms...");
		var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "Output", "Rooms");
		Directory.CreateDirectory(outputPath);

		var codes = data.Code.Where(c => c.ParentEntry is null).ToList();

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
					LayerType = layer.LayerType,
					XOffset = layer.XOffset,
					YOffset = layer.YOffset,
					HSpeed = layer.HSpeed,
					VSpeed = layer.VSpeed,
					IsVisible = layer.IsVisible
				};

				if (layer.LayerType == UndertaleRoom.LayerType.Instances)
				{
					foreach (var instance in layer.InstancesData.Instances)
					{
						var objectAsset = new GameObject
						{
							X = instance.X,
							Y = instance.Y,
							DefinitionID = data.GameObjects.IndexOf(instance.ObjectDefinition),
							InstanceID = (int)instance.InstanceID,
							CreationCodeID = codes.IndexOf(instance.CreationCode),
							ScaleX = instance.ScaleX,
							ScaleY = instance.ScaleY,
							Color = (int)instance.Color,
							Rotation = instance.Rotation,
							PreCreateCodeID = codes.IndexOf(instance.PreCreateCode)
						};

						layerasset.Instances_Objects.Add(objectAsset);
					}
				}
				else if (layer.LayerType == UndertaleRoom.LayerType.Background)
				{
					layerasset.Background_Visible = layer.BackgroundData.Visible;
					layerasset.Background_Foreground = layer.BackgroundData.Foreground;
					layerasset.Background_SpriteID = data.Sprites.IndexOf(layer.BackgroundData.Sprite);
					layerasset.Background_TilingH = layer.BackgroundData.TiledHorizontally;
					layerasset.Background_TilingV = layer.BackgroundData.TiledVertically;
					layerasset.Background_Stretch = layer.BackgroundData.Stretch;
					layerasset.Background_Color = (int)layer.BackgroundData.Color;
					layerasset.Background_FirstFrame = layer.BackgroundData.FirstFrame;
					layerasset.Background_AnimationSpeed = layer.BackgroundData.AnimationSpeed;
					layerasset.Background_AnimationType = layer.BackgroundData.AnimationSpeedType;
				}
				else if (layer.LayerType == UndertaleRoom.LayerType.Assets)
				{
					foreach (var tile in layer.AssetsData.LegacyTiles)
					{
						var legacyTile = new GamemakerTile
						{
							X = tile.X,
							Y = tile.Y,
							Definition = data.Sprites.IndexOf(tile.SpriteDefinition),
							SourceLeft = (int)tile.SourceX,
							SourceTop = (int)tile.SourceY,
							SourceWidth = (int)tile.Width,
							SourceHeight = (int)tile.Height,
							Depth = tile.TileDepth,
							InstanceID = (int)tile.InstanceID,
							ScaleX = tile.ScaleX,
							ScaleY = tile.ScaleY,
							Color = (int)tile.Color
						};

						layerasset.Assets_LegacyTiles.Add(legacyTile);
					}

					foreach (var sprite in layer.AssetsData.Sprites)
					{
						// uhh
						DebugLog.LogError($"Room:{room.Name.Content} Layer:{layer.LayerName.Content} Sprite:{sprite.Name.Content}");
					}
				}
				else
				{
					DebugLog.LogError($"Don't know how to handle layer type {layer.LayerType}");
				}

				asset.Layers.Add(layerasset);
			}

			var saveDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Output", "Rooms", $"{asset.Name}.json");
			File.WriteAllText(saveDirectory, JsonConvert.SerializeObject(asset, Formatting.Indented));
		}
		Console.WriteLine(" Done!");
	}

	public static void ExportFonts(UndertaleData data)
	{
		Console.Write($"Exporting fonts...");
		var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "Output", "Fonts");
		Directory.CreateDirectory(outputPath);

		foreach (var item in data.Fonts)
		{
			var fontAsset = new FontAsset();
			fontAsset.name = item.Name.Content;
			fontAsset.AssetIndex = data.Fonts.IndexOf(item);

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

			var saveDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Output", "Fonts", $"{fontAsset.name}.json");
			File.WriteAllText(saveDirectory, JsonConvert.SerializeObject(fontAsset, Formatting.Indented));
		}
		Console.WriteLine(" Done!");
	}

	public static void ExportSounds(UndertaleData data)
	{
		Console.Write($"Exporting sounds...");
		var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "Output", "Sounds");
		Directory.CreateDirectory(outputPath);

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
					asset.File = $"{asset.Name}.ogg";
					File.Copy(asset.File, Path.Combine(outputPath, asset.File));
				}
				else if (item.GroupID == data.GetBuiltinSoundGroupID())
				{
					// embedded .wav
					asset.File = $"{asset.Name}.wav";
					var embeddedAudio = data.EmbeddedAudio;
					File.WriteAllBytes(Path.Combine(outputPath, asset.File), embeddedAudio[item.AudioID].Data);
				}
				else
				{
					// .wav in some audio group file
					asset.File = $"{asset.Name}.wav";

					var audioGroupPath = $"audiogroup{item.GroupID}.dat";
					using var stream = new FileStream(audioGroupPath, FileMode.Open, FileAccess.Read);
					using var audioGroupData = UndertaleIO.Read(stream);

					var embeddedAudio = audioGroupData.EmbeddedAudio;
					File.WriteAllBytes(Path.Combine(outputPath, asset.File), embeddedAudio[item.AudioID].Data);
				}
			}

			var saveDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Output", "Sounds", $"{item.Name.Content}.json");
			File.WriteAllText(saveDirectory, JsonConvert.SerializeObject(asset, Formatting.Indented));
		}
		Console.WriteLine(" Done!");
	}

	public static void ExportTextureGroups(UndertaleData data)
	{
		Console.Write($"Exporting texture groups...");
		var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "Output", "TexGroups");
		Directory.CreateDirectory(outputPath);

		foreach (var group in data.TextureGroupInfo)
		{
			var asset = new TextureGroup();

			asset.GroupName = group.Name.Content;
			asset.TexturePages = group.TexturePages.Select(x => x.Resource.Name.Content).ToArray();
			asset.Sprites = group.Sprites.Select(x => data.Sprites.IndexOf(x.Resource)).ToArray();
			asset.Fonts = group.Fonts.Select(x => data.Fonts.IndexOf(x.Resource)).ToArray();

			var saveDirectory = Path.Combine(outputPath, $"{group.Name.Content}.json");
			File.WriteAllText(saveDirectory, JsonConvert.SerializeObject(asset, Formatting.Indented));
		}

		Console.WriteLine(" Done!");
	}
}
