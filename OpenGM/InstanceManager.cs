using OpenGM.IO;
using OpenGM.SerializedFiles;
using OpenGM.VirtualMachine;

namespace OpenGM;
public static class InstanceManager
{
	public static List<GamemakerObject> instances = new List<GamemakerObject>();
	public static Dictionary<int, ObjectDefinition> ObjectDefinitions = new();

	public static int _highestInstanceId = 103506 + 1; // TODO : this changes per game - get from data.win

	public static void RegisterInstance(GamemakerObject obj)
	{
		if (instances.Contains(obj))
		{
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

		instances.Add(obj);
	}

	public static int instance_create(double x, double y, int obj)
	{
		var definition = ObjectDefinitions[obj];

		// is 0 depth right? no idea
		var newGM = new GamemakerObject(definition, x, y, 0, _highestInstanceId++, definition.sprite, definition.visible, definition.persistent, definition.textureMaskId);

		GamemakerObject.ExecuteEvent(newGM, definition, EventType.PreCreate);
		GamemakerObject.ExecuteEvent(newGM, definition, EventType.Create);
		newGM._createRan = true;
		return newGM.instanceId;
	}

	public static int instance_create_depth(double x, double y, int depth, int obj)
	{
		var definition = ObjectDefinitions[obj];

		var newGM = new GamemakerObject(definition, x, y, depth, _highestInstanceId++, definition.sprite, definition.visible, definition.persistent, definition.textureMaskId);

		GamemakerObject.ExecuteEvent(newGM, definition, EventType.PreCreate);
		GamemakerObject.ExecuteEvent(newGM, definition, EventType.Create);
		newGM._createRan = true;
		return newGM.instanceId;
	}

	public static int instance_number(int obj)
	{
		instances.RemoveAll(x => x == null);
		return instances.Count(x => HasAssetInParents(x.Definition, obj));
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
		foreach (var instance in instances)
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
			return VMExecutor.Ctx.GMSelf;
		}

		if (instanceId < GMConstants.FIRST_INSTANCE_ID)
		{
			throw new Exception($"Tried to find instance by asset id {instanceId}");
		}

		if (instances.Count(x => x.instanceId == instanceId) > 1)
		{
			DebugLog.LogError($"Found more than one object instance with id of {instanceId}.");
			return null;
		}

		var instance = instances.SingleOrDefault(x => x.instanceId == instanceId);
		return instance;
	}

	public static bool instance_exists_instanceid(int instanceId)
	{
		return instances.Any(x => x.instanceId == instanceId);
	}

	public static bool instance_exists_index(int assetIndex)
	{
		foreach (var instance in instances)
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

	public static void instance_destroy(GamemakerObject obj)
	{
		if (obj != null)
		{
			obj.Destroy();
		}

		instances.Remove(obj!);
	}

	public static void RoomChange()
	{
		foreach (var instance in instances)
		{
			if (!instance.persistent)
			{
				instance.Destroy();
			}
		}

		instances = instances.Where(x => x != null && x.persistent).ToList();
	}
}
