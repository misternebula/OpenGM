using System.Runtime.CompilerServices;
using MemoryPack;
using MessagePack;
using OpenGM.Loading;
using OpenGM.Rendering;
using OpenGM.SerializedFiles;
using OpenGM.VirtualMachine;

namespace OpenGM.SaveState;

[MessagePackObject(true)]
public class SaveState
{
    public byte[] GameBytes = null!;
    public Dictionary<string, object?> GlobalVars = null!;
#pragma warning disable CS0414 // Field is assigned but its value is never used
    public SSRoom Room = null!;
#pragma warning restore CS0414 // Field is assigned but its value is never used
    public Dictionary<int, SSObject> Instances = null!;

    public static SaveState From()
    {
        // TODO: global state
        return new SaveState
        {
            GameBytes = MemoryPackSerializer.Serialize(GameLoader.GeneralInfo),

            GlobalVars = VariableResolver.GlobalVariables.Where(x => x.Value is not Method).ToDictionary(), // ignore method I HATE THEM
            // _room = SSRoom.From(RoomManager.CurrentRoom),
            Instances = InstanceManager.instances.ToDictionary(x => x.Key, x => SSObject.From(x.Value)),
        };
    }

    public void Into()
    {
        var gameBytes = MemoryPackSerializer.Serialize(GameLoader.GeneralInfo);
        if (!GameBytes.SequenceEqual(gameBytes))
        {
            throw new NotImplementedException("tried to load save state on a different game!");
        }

        VariableResolver.GlobalVariables = GlobalVars;
        
        // RoomManager.CurrentRoom = _room.Into();
        
        // scary!
        InstanceManager.instances.Clear();
        DrawManager._drawObjects.Clear();
        var instances = Instances.ToDictionary(x => x.Key, x => x.Value.Into());
        foreach (var x in instances)
        {
            InstanceManager.instances.Add(x.Key, x.Value);
            DrawManager._drawObjects.Add(x.Value);
        }
    }
}

[MessagePackObject(true)]
public class SSRoom
{
    [IgnoreMember]
    public Room Room = null!;
    public Dictionary<int, SSLayer> Layers = null!;

    public static SSRoom From(RoomContainer room)
    {
        return new SSRoom
        {
            Room = room.RoomAsset,
            Layers = room.Layers.ToDictionary(x => x.Key, x => SSLayer.From(x.Value))
        };
    }

    public RoomContainer Into()
    {
        return new RoomContainer(Room)
        {
            Layers = Layers.ToDictionary(x => x.Key, x => x.Value.Into())
        };
    }
}

[MessagePackObject(true)]
public class SSLayer
{
    public static SSLayer From(LayerContainer layer)
    {
        throw new NotImplementedException();
    }

    public LayerContainer Into()
    {
        throw new NotImplementedException();
    }
}

[MessagePackObject(true)]
public class SSObject
{
    public int Id;
    public int ObjDef;
    public Dictionary<string, object?> SelfVars = null!;
    public Dictionary<string, object> BuilInVars = null!;

    public static SSObject From(GamemakerObject obj)
    {
        return new SSObject()
        {
            Id = obj.instanceId,
            ObjDef = obj.Definition.AssetId,
            SelfVars = obj.SelfVariables,
            BuilInVars = VariableResolver.BuiltInSelfVariables
                .Where(x => x.Value.setter != null)
                .ToDictionary(x => x.Key, x => x.Value.getter(obj))
        };
    }

    public GamemakerObject Into()
    {
        // scary!
        var obj = (GamemakerObject)RuntimeHelpers.GetUninitializedObject(typeof(GamemakerObject));
        obj.Definition = InstanceManager.ObjectDefinitions[ObjDef];
        obj.SelfVariables = SelfVars.ToDictionary(x => x.Key, x => x.Value);
        foreach (var x in BuilInVars)
        {
            VariableResolver.BuiltInSelfVariables[x.Key].setter!(obj, x.Value);
        }
        obj._createRan = true;

        return obj;
    }
}