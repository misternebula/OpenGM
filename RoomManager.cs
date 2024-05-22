using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Models;

namespace DELTARUNITYStandalone;

public static class RoomManager
{
	public static bool ChangeRoomAfterEventExecution = false;

	/// <summary>
	/// What script called the function to change rooms.
	/// </summary>
	public static string ScriptName = null;

	/// <summary>
	/// The room to change to.
	/// </summary>
	public static Room RoomToChangeTo = null;

	public static Room CurrentRoom;

	public static Dictionary<int, Room> RoomList = new();

	public static void ChangeToWaitingRoom()
	{
		DebugLog.Log($"Changing room to {RoomToChangeTo.Name}...");
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

			DrawManager.Unregister(instance);
			//Destroy(instance.gameObject);
		}

		InstanceManager.instances = InstanceManager.instances.Where(x => x != null && x.persistent).ToList();

		ChangeRoomAfterEventExecution = false;
		ScriptName = null;
		CurrentRoom = RoomToChangeTo;
		RoomToChangeTo = null;

		OnRoomChanged();
	}

	private static void OnRoomChanged()
	{
		CustomWindow.Instance.SetResolution(CurrentRoom.CameraWidth, CurrentRoom.CameraHeight);

		foreach (var layer in CurrentRoom.Layers)
		{
			if (layer.Instances_Objects != null)
			{
				foreach (var item in layer.Instances_Objects)
				{
					InstanceManager.instance_create_depth(item.X, item.Y, layer.LayerDepth, item.DefinitionID);
				}
			}
		}
	}

	public static void ChangeRoomAfterEvent(int index)
	{
		ChangeRoomAfterEvent(RoomList[index]);
	}

	public static void ChangeRoomAfterEvent(Room roomName)
	{
		if (VMExecutor.currentExecutingScript.Count > 0)
		{
			ScriptName = VMExecutor.currentExecutingScript.Peek().Name;
		}

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
		ChangeRoomAfterEvent(RoomList[RoomList.Values.ToList().IndexOf(CurrentRoom) + 1]);
	}

	public static void room_goto_previous()
	{
		ChangeRoomAfterEvent(RoomList[RoomList.Values.ToList().IndexOf(CurrentRoom) - 1]);
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
