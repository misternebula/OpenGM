using System.Diagnostics;
using OpenGM.IO;
using OpenGM.Rendering;
using OpenGM.SerializedFiles;
using OpenGM.VirtualMachine;
using UndertaleModLib.Models;
using EventType = OpenGM.VirtualMachine.EventType;

namespace OpenGM;

public static class RoomManager
{
	public static bool ChangeRoomAfterEventExecution = false;

	/// <summary>
	/// The room to change to.
	/// </summary>
	public static Room? RoomToChangeTo = null;
	public static RoomContainer CurrentRoom = null!; // its set to room on start

	public static Dictionary<int, Room> RoomList = new();
	public static bool RoomLoaded = false;

	public static void ChangeToWaitingRoom()
	{
		DebugLog.LogInfo($"Changing to {RoomToChangeTo!.Name}");
		ChangeRoomAfterEventExecution = false;

		if (CurrentRoom != null && CurrentRoom.Persistent)
		{
			// uh oh.
			throw new NotImplementedException();
		}
		else if (CurrentRoom != null)
		{
			// remove everything that isn't persistent

			// events could destroy other objects, cant modify during iteration
			var instanceList = new List<GamemakerObject>(InstanceManager.instances);

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

				GamemakerObject.ExecuteScript(instance, instance.Definition, EventType.Destroy);
				GamemakerObject.ExecuteScript(instance, instance.Definition, EventType.CleanUp);

				instance.Destroy();
				//Destroy(instance.gameObject);
			}

			InstanceManager.instances = InstanceManager.instances.Where(x => x != null && !x.Destroyed && x.persistent).ToList();

			foreach (var item in CurrentRoom.Tiles)
			{
				item.Destroy();
			}
			CurrentRoom.Tiles.Clear();

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
		}

		RoomLoaded = false;

		CurrentRoom = new RoomContainer(RoomToChangeTo);

		RoomToChangeTo = null;

		OnRoomChanged();
	}

	private static void OnRoomChanged()
	{
		CustomWindow.Instance.SetResolution(CurrentRoom.CameraWidth, CurrentRoom.CameraHeight);
		CustomWindow.Instance.SetPosition(0, 0);

		SurfaceManager.application_surface = SurfaceManager.CreateSurface(CurrentRoom.CameraWidth, CurrentRoom.CameraHeight, 0);

		var createdObjects = new List<(GamemakerObject gm, int pcc, int cc)>();

		InstanceManager.RoomChange();
		CollisionManager.RoomChange();

		foreach (var layer in CurrentRoom.RoomAsset.Layers)
		{
			var layerContainer = new LayerContainer(layer);

			foreach (var element in layer.Elements)
			{
				element.Layer = layerContainer;

				if (element.Type == ElementType.Instance)
				{
					var item = (element as GameObject)!;

					var definition = InstanceManager.ObjectDefinitions[item.DefinitionID];
					var newGM = new GamemakerObject(definition, item.X, item.Y, item.DefinitionID, item.InstanceID, definition.sprite, definition.visible, definition.persistent, definition.textureMaskId);

					newGM._createRan = true;
					newGM.depth = layer.LayerDepth;
					newGM.image_xscale = item.ScaleX;
					newGM.image_yscale = item.ScaleY;
					newGM.image_blend = (int)item.Color;
					newGM.image_angle = item.Rotation;

					createdObjects.Add((newGM, item.PreCreateCodeID, item.CreationCodeID));

					layerContainer.ElementsToDraw.Add(newGM);
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
				else
				{
					DebugLog.LogError($"Don't know how to load element type {element.Type}!");
				}
			}

			CurrentRoom.Layers.Add(layerContainer.ID, layerContainer);
		}

		var currentInstances = InstanceManager.instances.ToList();
		foreach (var obj in currentInstances)
		{
			GamemakerObject.ExecuteScript(obj, obj.Definition, EventType.Other, (int)EventSubtypeOther.RoomStart);
		}

		foreach (var (obj, pcc, cc) in createdObjects)
		{
			GamemakerObject.ExecuteScript(obj, obj.Definition, EventType.PreCreate);
		}

		foreach (var (obj, pcc, cc) in createdObjects)
		{
			GamemakerObject.ExecuteScript(obj, obj.Definition, EventType.Create);
		}

		VMScript? GetVMScriptFromCodeIndex(int codeIndex)
		{
			if (codeIndex == -1)
			{
				return null;
			}

			return ScriptResolver.Scripts.Values.Single(x => x.AssetId == codeIndex);
		}

		foreach (var (obj, pcc, cc) in createdObjects)
		{
			var preCreateCode = GetVMScriptFromCodeIndex(pcc);
			if (preCreateCode != null)
			{
				DebugLog.LogInfo($"RUNNING PCC {preCreateCode.Name}");
				VMExecutor.ExecuteScript(preCreateCode, obj, obj.Definition);
			}
		}

		foreach (var (obj, pcc, cc) in createdObjects)
		{
			var createCode = GetVMScriptFromCodeIndex(cc);
			if (createCode != null)
			{
				DebugLog.LogInfo($"RUNNING CC {createCode.Name}");
				VMExecutor.ExecuteScript(createCode, obj, obj.Definition);
			}
		}
		
		GC.Collect();

		DebugLog.LogInfo($"- Finished room change.");

		RoomLoaded = true;
	}

	public static void ChangeRoomAfterEvent(int index)
	{
		ChangeRoomAfterEvent(RoomList[index]);
	}

	public static void ChangeRoomAfterEvent(Room roomName)
	{
		ChangeRoomAfterEventExecution = true;
		RoomToChangeTo = roomName;
		
		if (CurrentRoom != null && CurrentRoom.Persistent)
		{
			// oh god we gotta save the current scene aaaaaaaa
			throw new NotImplementedException();
		}

		// events could destroy other objects, cant modify during iteration
		var instanceList = new List<GamemakerObject>(InstanceManager.instances);

		foreach (var instance in instanceList)
		{
			if (instance == null)
			{
				continue;
			}

			GamemakerObject.ExecuteScript(instance, instance.Definition, EventType.Other, (int)EventSubtypeOther.RoomEnd);
		}
	}

	public static void room_goto_next()
	{
		ChangeRoomAfterEvent(RoomList[CurrentRoom.AssetId + 1]);
	}

	public static void room_goto_previous()
	{
		ChangeRoomAfterEvent(RoomList[CurrentRoom.AssetId -1]);
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
