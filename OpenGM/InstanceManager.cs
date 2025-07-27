using OpenGM.IO;
using OpenGM.Rendering;
using OpenGM.SerializedFiles;
using OpenGM.VirtualMachine;
using UndertaleModLib.Models;
using EventType = OpenGM.VirtualMachine.EventType;

namespace OpenGM;

// this could also be in ObjectDefinition. it dont matter
public class ObjectDictEntry
{
    public List<GamemakerObject> Instances = new();
    public List<int> ChildrenIndexes = new();
}

public static class InstanceManager
{
    public static Dictionary<int, GamemakerObject> instances = new();
    public static Dictionary<int, ObjectDefinition> ObjectDefinitions = new();

    public static List<int> LastDeactivatedIDs = [];

    public static int NextInstanceID;

    /// <summary>
    /// for faster lookup when finding instances by asset id.
    /// gamemaker does something similar and this was a bottleneck in one of nebula's test programs.
    /// </summary>
    public static Dictionary<int, ObjectDictEntry> ObjectMap = new();

    public static void InitObjectMap()
    {
        ObjectMap.Clear();

        foreach (var item in ObjectDefinitions)
        {
            var entry = new ObjectDictEntry
            {
                ChildrenIndexes = ObjectDefinitions
                    .Where(x => x.Value.parent == item.Value)
                    .Select(x => x.Key).ToList()
            };

            ObjectMap.Add(item.Key, entry);
        }
    }

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

        var entry = ObjectMap[obj.Definition.AssetId];
        entry.Instances.Add(obj);
    }

    public static int instance_create(double x, double y, int obj)
    {
        var definition = ObjectDefinitions[obj];

        var newGM = new GamemakerObject(definition, x, y, definition.depth, NextInstanceID++, definition.sprite, definition.visible, definition.persistent, definition.textureMaskId);

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

    public static GamemakerObject? Find(int id, int index = 0)
    {
        if (id == GMConstants.global)
        {
            throw new NotImplementedException("Tried to find global instance.");
        }
        else if (id == GMConstants.self)
        {
            return VMExecutor.Self.GMSelf;
        }
        else if (id == GMConstants.other)
        {
            return VMExecutor.Other.GMSelf;
        }
        else if (id < 0)
        {
            throw new NotImplementedException($"Tried to find instance with id {id}");
        }
        else if (id < GMConstants.FIRST_INSTANCE_ID)
        {
            // asset id
            return FindByAssetId(id)[index];
        }
        else
        {
            // instance id
            return instances.GetValueOrDefault(id);
        }
    }

    public static List<GamemakerObject> FindByAssetId(int assetId)
    {
        var result = new List<GamemakerObject>();

        if (assetId >= 0)
        {
            AddChild(assetId);
            return result;
        }

        switch (assetId)
        {
            case GMConstants.self:
                result.Add(VMExecutor.Self.GMSelf);
                return result;
            case GMConstants.other:
                var myId = VMExecutor.Self.GMSelf.instanceId;
                result.AddRange(instances.Where(e => e.Key != myId).Select(e => e.Value));
                return result;
            case GMConstants.all:
                result.AddRange(instances.Select(e => e.Value));
                return result;
            case GMConstants.noone:
                return result;
            default:
                throw new Exception($"Tried to find instances with asset id {assetId}");
        }

        void AddChild(int id)
        {
            result.AddRange(ObjectMap[id].Instances);
            foreach (var child in ObjectMap[id].ChildrenIndexes)
            {
                AddChild(child);
            }
        }
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
        if (!instances.ContainsKey(instanceId))
        {
            return false;
        }

        var instance = instances[instanceId];

        return instance is { Marked: false, Destroyed: false };
    }

    public static bool instance_exists_index(int assetIndex)
    {
        foreach (var (instanceId, instance) in instances)
        {
            var definition = instance.Definition;
            while (definition != null)
            {
                if (definition.AssetId == assetIndex && !instance.Marked && !instance.Destroyed)
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
            obj.Marked = true;
            if (executeEvent)
            {
                GamemakerObject.ExecuteEvent(obj, obj.Definition, EventType.Destroy);
            }

            GamemakerObject.ExecuteEvent(obj, obj.Definition, EventType.CleanUp);
        }
    }

    public static void ClearNullInstances() // TODO: we dont need to null check instances??????
    {
        var toRemove = instances.Where(x => x.Value == null).Select(x => x.Key);

        if (toRemove.Any())
        {
            DebugLog.LogError("Found null instances in instance list!");
            foreach (var item in toRemove)
            {
                DebugLog.LogError($" - {item}");
            }
        }

        ClearInstances(toRemove);
    }

    public static void ClearNonPersistent()
    {
        var toRemove = instances.Where(x => !x.Value.persistent).Select(x => x.Key);
        ClearInstances(toRemove);
    }

    public static void ClearInstances(IEnumerable<int> toRemove)
    {
        foreach (var id in toRemove)
        {
            var instance = instances[id];

            if (instance != null)
            {
                ObjectMap[instance.Definition.AssetId].Instances.Remove(instance);
            }

            instances.Remove(id);
        }
    }

    public static void ClearInstances(IEnumerable<GamemakerObject?> toRemove) // this doesnt need to be nullable but i dont care enough to change and test it
    {
        ClearInstances(toRemove.Where(o => o != null).Select(o => o!.instanceId));
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

        //instances = instances.Where(x => x.Value != null && x.Value.persistent).ToDictionary();
        ClearNullInstances();
        ClearNonPersistent();
    }

    public static void RememberOldPositions()
    {
        var instanceList = instances.Values.ToList(); // .Animate() can change instance list
        foreach (var item in instanceList)
        {
            item.xprevious = item.x;
            item.yprevious = item.y;
            item.path_previousposition = item.path_position;

            item.Animate();
        }
    }

    public static void UpdateImages()
    {
        var instanceList = InstanceManager.instances.Values.ToList();
        foreach (var item in instanceList)
        {
            if (item.Marked)
            {
                continue;
            }

            if (!item.Active)
            {
                continue;
            }

            var sprite = SpriteManager.GetSpriteAsset(item.sprite_index);

            if (sprite == null)
            {
                continue;
            }

            var num = sprite.Textures.Count;

            if (item.image_index >= num)
            {
                item.frame_overflow += num;
                item.image_index -= num;
            }
            else if (item.image_index < 0)
            {
                item.frame_overflow -= num;
                item.image_index += num;
            }
        }
    }

    public static void UpdatePositions()
    {
        foreach (var (index, item) in InstanceManager.instances)
        {
            item.AdaptSpeed();

            if (item.AdaptPath())
            {
                GamemakerObject.ExecuteEvent(item, item.Definition, EventType.Other, (int)EventSubtypeOther.EndOfPath);
            }

            if (item.hspeed != 0 || item.vspeed != 0)
            {
                item.x += item.hspeed;
                item.y += item.vspeed;
                item.bbox_dirty = true;
            }
        }
    }
}
