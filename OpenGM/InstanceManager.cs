using OpenGM.IO;
using OpenGM.SerializedFiles;
using OpenGM.VirtualMachine;

namespace OpenGM;
public static class InstanceManager
{
	public static Dictionary<int, GamemakerObject> instances = new();
	public static Dictionary<int, ObjectDefinition> ObjectDefinitions = new();

	public static int NextInstanceID;

	public static void RegisterInstance(GamemakerObject obj)
	{
		if (instances.ContainsKey(obj.instanceId))
		{
			DebugLog.LogWarning($"Tried to register another object with instanceId:{obj.instanceId}\nExisting:{instances[obj.instanceId].Definition.Name}\nNew:{obj.Definition.Name}");

			DebugLog.LogError($"--Stacktrace--");
			foreach (var item in VMExecutor.CallStack)
			{
				DebugLog.LogError($" - {item.CodeName}");
			}

			return;
		}

		if (obj == null)
		{
			DebugLog.LogError($"Tried to register a null instance!");
			return;
		}

		if (obj.Definition == null)
		{
			DebugLog.LogError($"Tried to register an instance with no definition! obj:{obj}");
			return;
		}

		instances.Add(obj.instanceId, obj);
	}

	public static int instance_create(double x, double y, int obj)
	{
		var definition = ObjectDefinitions[obj];

		// is 0 depth right? no idea
		var newGM = new GamemakerObject(definition, x, y, 0, NextInstanceID++, definition.sprite, definition.visible, definition.persistent, definition.textureMaskId);

		GamemakerObject.ExecuteEvent(newGM, definition, EventType.PreCreate);
		GamemakerObject.ExecuteEvent(newGM, definition, EventType.Create);
		newGM._createRan = true;
		return newGM.instanceId;
	}

	public static int instance_create_depth(double x, double y, int depth, int obj)
	{
		var definition = ObjectDefinitions[obj];

		var newGM = new GamemakerObject(definition, x, y, depth, NextInstanceID++, definition.sprite, definition.visible, definition.persistent, definition.textureMaskId);

		GamemakerObject.ExecuteEvent(newGM, definition, EventType.PreCreate);
		GamemakerObject.ExecuteEvent(newGM, definition, EventType.Create);
		newGM._createRan = true;
		return newGM.instanceId;
	}

	public static int instance_number(int obj)
	{
		//instances.RemoveAll(x => x == null);
		return instances.Values.Count(x => HasAssetInParents(x.Definition, obj));
	}

	public static bool HasAssetInParents(ObjectDefinition definition, int id)
	{
		var currentDefinition = definition;
		while (currentDefinition != null)
		{
			if (currentDefinition.AssetId == id)
			{
				return true;
			}

			currentDefinition = currentDefinition.parent;
		}

		return false;
	}

	public static List<GamemakerObject> FindByAssetId(int assetId)
	{
		if (assetId < 0)
		{
			throw new Exception($"Tried to find instances with asset id {assetId}");
		}

		var result = new List<GamemakerObject>();
		foreach (var (instanceId, instance) in instances)
		{
			var definition = instance.Definition;
			while (definition != null)
			{
				if (definition.AssetId == assetId)
				{
					result.Add(instance);
					break; // continue for loop
				}
				definition = definition.parent;
			}
		}

		return result;
	}

	public static GamemakerObject? FindByInstanceId(int instanceId)
	{
		if (instanceId == GMConstants.self)
		{
			return VMExecutor.Self.GMSelf;
		}

		if (instanceId < GMConstants.FIRST_INSTANCE_ID)
		{
			throw new Exception($"Tried to find instance by asset id {instanceId}");
		}

		return !instances.TryGetValue(instanceId, out var value) ? null : value;
	}

	public static bool instance_exists_instanceid(int instanceId)
	{
		return instances.ContainsKey(instanceId);
	}

	public static bool instance_exists_index(int assetIndex)
	{
		foreach (var (instanceId, instance) in instances)
		{
			var definition = instance.Definition;
			while (definition != null)
			{
				if (definition.AssetId == assetIndex)
				{
					return true;
				}
				definition = definition.parent;
			}
		}

		return false;
	}

	/*public static void instance_destroy(GamemakerObject obj)
	{
		if (obj != null)
		{
			DebugLog.Log($"INSTANCE_DESTROY {obj.Definition.Name}");
			obj.Destroy();
			instances.Remove(obj.instanceId);
		}
	}*/

	public static void MarkForDestruction(GamemakerObject? obj, bool executeEvent)
	{
		if (obj == null)
		{
			return;
		}

		if (!obj.Marked && obj.Active)
		{
			if (executeEvent)
			{
				GamemakerObject.ExecuteEvent(obj, obj.Definition, EventType.Destroy);
			}

			GamemakerObject.ExecuteEvent(obj, obj.Definition, EventType.CleanUp);
			obj.Marked = true;
		}
	}

	public static void RoomChange()
	{
		foreach (var (instanceId, instance) in instances)
		{
			if (!instance.persistent)
			{
				instance.Destroy();
			}
		}

		instances = instances.Where(x => x.Value != null && x.Value.persistent).ToDictionary();
	}
}
