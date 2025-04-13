using OpenGM.IO;
using OpenGM.Loading;
using OpenGM.Rendering;
using OpenGM.SerializedFiles;
using OpenGM.VirtualMachine;
using UndertaleModLib.Models;
using EventType = OpenGM.VirtualMachine.EventType;

namespace OpenGM;

public static class RoomManager
{
	/// <summary>
	/// The room to change to.
	/// </summary>
	//public static Room? RoomToChangeTo = null;
	public static int New_Room;
	public static RoomContainer CurrentRoom = null!; // its set to room on start

	public static Dictionary<int, Room> RoomList = new();
	public static bool RoomLoaded = false;
	public static bool FirstRoom = false;

	public static void EndGame()
	{
		foreach (var (instanceId, instance) in InstanceManager.instances)
		{
			instance.Destroy();
		}

		InstanceManager.instances.Clear();
	}

	public static void StartGame()
	{
		FirstRoom = true;
		New_Room = 0;
		ChangeToWaitingRoom();
	}

	public static void ChangeToWaitingRoom()
	{
		if (New_Room == GMConstants.ROOM_ENDOFGAME || New_Room == GMConstants.ROOM_ABORTGAME)
		{
			EndGame();
			return;
		}
		else if (New_Room == GMConstants.ROOM_RESTARTGAME)
		{
			EndGame();
			StartGame();
			return;
		}
		else if (New_Room == GMConstants.ROOM_LOADGAME)
		{
			return;
		}

		var room = RoomList[New_Room];
		New_Room = -1;

		DebugLog.LogInfo($"Changing to {room!.Name}");

		if (CurrentRoom != null && CurrentRoom.Persistent)
		{
			// uh oh.
			throw new NotImplementedException();
		}
		else if (CurrentRoom != null)
		{
			// remove everything that isn't persistent

			// events could destroy other objects, cant modify during iteration
			var instanceList = new List<GamemakerObject>(InstanceManager.instances.Values);

			foreach (var instance in instanceList)
			{
				if (instance == null)
				{
					continue;
				}

				if (instance.persistent)
				{
					continue;
				}

				GamemakerObject.ExecuteEvent(instance, instance.Definition, EventType.Other, (int)EventSubtypeOther.RoomEnd);
				GamemakerObject.ExecuteEvent(instance, instance.Definition, EventType.Destroy);
				GamemakerObject.ExecuteEvent(instance, instance.Definition, EventType.CleanUp);

				instance.Destroy();
				//Destroy(instance.gameObject);
			}

			InstanceManager.instances = InstanceManager.instances.Where(x => x.Value != null && !x.Value.Destroyed && x.Value.persistent).ToDictionary();

			foreach (var item in CurrentRoom.Tiles)
			{
				item.Destroy();
			}
			CurrentRoom.Tiles.Clear();

			CurrentRoom.OldBackgrounds.Clear();

			foreach (var layer in CurrentRoom.Layers)
			{
				foreach (var item in layer.Value.ElementsToDraw)
				{
					if (item is GamemakerObject gm && gm.persistent)
					{
						continue;
					}

					item.Destroy();
				}
			}

			foreach (var obj in CurrentRoom.LooseObjects)
			{
				if (obj.persistent)
				{
					continue;
				}

				obj.Destroy();
			}
		}

		RoomLoaded = false;

		CurrentRoom = new RoomContainer(room);

		OnRoomChanged();
	}

	private static void OnRoomChanged()
	{
		DebugLog.Log($"Changing camera...");
		if (CustomWindow.Instance != null) // only null in tests.
		{
			// reset view
			CustomWindow.Instance.SetPosition(0, 0);
			CustomWindow.Instance.SetResolution(CurrentRoom.CameraWidth, CurrentRoom.CameraHeight);
			CustomWindow.Instance.FollowInstance = CurrentRoom.FollowObject;
			//CustomWindow.Instance.UpdateInstanceFollow();
		}

		var createdObjects = new List<(GamemakerObject gm, GameObject go)>();

		InstanceManager.RoomChange();

		void RunObjEvents(GamemakerObject obj, GameObject go)
		{
			GamemakerObject.ExecuteEvent(obj, obj.Definition, EventType.PreCreate);
			
			var preCreateCode = GetCodeFromCodeIndex(go.PreCreateCodeID);
			if (preCreateCode != null)
			{
				VMExecutor.ExecuteCode(preCreateCode, obj, obj.Definition);
			}

			GamemakerObject.ExecuteEvent(obj, obj.Definition, EventType.Create);
			obj._createRan = true;

			var createCode = GetCodeFromCodeIndex(go.CreationCodeID);
			if (createCode != null)
			{
				VMExecutor.ExecuteCode(createCode, obj, obj.Definition);
			}
		}

		foreach (var layer in CurrentRoom.RoomAsset.Layers)
		{
			DebugLog.LogInfo($"Creating layer {layer.LayerName}...");

			var layerContainer = new LayerContainer(layer);

			foreach (var element in layer.Elements)
			{
				element.Layer = layerContainer;

				if (element.Type == ElementType.Instance)
				{
					var item = (element as GameObject)!;

					var definition = InstanceManager.ObjectDefinitions[item.DefinitionID];
					var newGM = new GamemakerObject(definition, item.X, item.Y, item.DefinitionID, item.InstanceID, definition.sprite, definition.visible, definition.persistent, definition.textureMaskId);

					//newGM._createRan = true;
					newGM.depth = layer.LayerDepth;
					newGM.image_xscale = item.ScaleX;
					newGM.image_yscale = item.ScaleY;
					newGM.image_blend = item.Color;
					newGM.image_angle = item.Rotation;
					newGM.image_index = item.FrameIndex;
					newGM.image_speed = item.ImageSpeed;

					createdObjects.Add((newGM, item));

					layerContainer.ElementsToDraw.Add(newGM);

					//RunObjEvents(newGM, item);
				}
				else if (element.Type == ElementType.Tilemap)
				{
					var item = (element as CLayerTilemapElement)!;

					if (item.BackgroundIndex == -1)
					{
						continue;
					}

					var tilesLayer = new GMTilesLayer(item)
					{
						depth = layer.LayerDepth
					};

					layerContainer.ElementsToDraw.Add(tilesLayer);
				}
				else if (element.Type == ElementType.Background)
				{
					var item = (element as CLayerBackgroundElement)!;

					if (item.Index == -1)
					{
						DebugLog.LogWarning($"Background {item.Name} with null index!");
						continue;
					}

					var background = new GMBackground(item)
					{
						depth = layer.LayerDepth
					};

					layerContainer.ElementsToDraw.Add(background);
				}
				else if (element.Type == ElementType.Tile)
				{
					var tile = (element as CLayerTileElement)!;

					var newTile = new GMTile()
					{
						X = tile.X,
						Y = tile.Y,
						Definition = tile.Definition,
						left = tile.SourceLeft,
						top = tile.SourceTop,
						width = tile.SourceWidth,
						height = tile.SourceHeight,
						depth = layer.LayerDepth,
						instanceId = tile.Id, // todo : this is almost definitely very wrong
						XScale = tile.ScaleX,
						YScale = tile.ScaleY,
						Color = tile.Color
					};

					layerContainer.ElementsToDraw.Add(newTile);
				}
				else if (element.Type == ElementType.Sprite)
				{
					var sprite = (element as CLayerSpriteElement)!;

					var newSprite = new GMSprite(sprite)
					{
						Definition = sprite.Definition,
						X = sprite.X,
						Y = sprite.Y,
						XScale = sprite.ScaleX,
						YScale = sprite.ScaleY,
						Color = sprite.Color,
						AnimationSpeed = sprite.AnimationSpeed,
						AnimationSpeedType = sprite.AnimationSpeedType,
						FrameIndex = sprite.FrameIndex,
						Rotation = sprite.Rotation,
						instanceId = sprite.Id,
						depth = layer.LayerDepth
					};

					layerContainer.ElementsToDraw.Add(newSprite);
				}
				else
				{
					DebugLog.LogError($"Don't know how to load element type {element.Type}!");
				}
			}

			CurrentRoom.Layers.Add(layerContainer.ID, layerContainer);
		}

		DebugLog.LogInfo($"Creating loose objects...");
		foreach (var item in CurrentRoom.RoomAsset.LooseObjects)
		{
			var definition = InstanceManager.ObjectDefinitions[item.DefinitionID];
			var newGM = new GamemakerObject(definition, item.X, item.Y, item.DefinitionID, item.InstanceID, definition.sprite, definition.visible, definition.persistent, definition.textureMaskId);

			//newGM._createRan = true;
			//newGM.depth = layer.LayerDepth;
			newGM.image_xscale = item.ScaleX;
			newGM.image_yscale = item.ScaleY;
			newGM.image_blend = (int)item.Color;
			newGM.image_angle = item.Rotation;
			newGM.image_index = item.FrameIndex;
			newGM.image_speed = item.ImageSpeed;

			createdObjects.Add((newGM, item));

			CurrentRoom.LooseObjects.Add(newGM);

			//RunObjEvents(newGM, item);
		}

		// instance_exists will still return true for all objects even in Create of the first object.... ugh
		foreach (var item in createdObjects)
		{
			RunObjEvents(item.gm, item.go);
		}

		DebugLog.LogInfo($"Creating loose tiles...");
		foreach (var item in CurrentRoom.RoomAsset.Tiles)
		{
			var newTile = new GMTile()
			{
				X = item.X,
				Y = item.Y,
				left = item.SourceLeft,
				top = item.SourceTop,
				width = item.SourceWidth,
				height = item.SourceHeight,
				depth = item.Depth,
				instanceId = item.InstanceID,
				XScale = item.ScaleX,
				YScale = item.ScaleY,
				Color = item.Color,
				Definition = 0
			};

			CurrentRoom.Tiles.Add(newTile);
		}

		foreach (var item in CurrentRoom.RoomAsset.OldBackgrounds)
		{
			var oldBackground = new GMOldBackground()
			{
				Enabled = item.Enabled,
				Foreground = item.Foreground,
				Definition = item.Definition,
				Position = item.Position,
				TilingX = item.TilingX,
				TilingY = item.TilingY,
				Speed = item.Speed,
				Stretch = item.Stretch
			};

			CurrentRoom.OldBackgrounds.Add(oldBackground);
		}

		var currentInstances = InstanceManager.instances.Values.ToList();

		if (FirstRoom)
		{
			FirstRoom = false;
			DebugLog.LogInfo($"Calling GameStart...");
			foreach (var obj in currentInstances)
			{
				GamemakerObject.ExecuteEvent(obj, obj.Definition, EventType.Other, (int)EventSubtypeOther.GameStart);
			}
		}

		DebugLog.LogInfo($"Calling RoomStart...");
		foreach (var obj in currentInstances)
		{
			GamemakerObject.ExecuteEvent(obj, obj.Definition, EventType.Other, (int)EventSubtypeOther.RoomStart);
		}

		VMCode? GetCodeFromCodeIndex(int codeIndex)
		{
			if (codeIndex == -1)
			{
				return null;
			}

			return GameLoader.Codes[codeIndex];
		}
		
		GC.Collect(); // gc on load boundary

		DebugLog.LogInfo($"- Finished room change.");

		RoomLoaded = true;
	}

	public static void room_goto_next()
	{
		New_Room = CurrentRoom.AssetId + 1;
	}

	public static void room_goto_previous()
	{
		New_Room = CurrentRoom.AssetId - 1;
	}

	public static int room_next(int numb)
	{
		if (RoomList.Count > numb + 1)
		{
			return numb + 1;
		}

		return -1;
	}

	public static int room_previous(int numb)
	{
		if (numb == 0)
		{
			return -1;
		}

		return numb - 1;
	}
}
