using System.Runtime.CompilerServices;
using MemoryPack;
using OpenGM.Loading;
using OpenGM.SerializedFiles;
using OpenGM.VirtualMachine;

namespace OpenGM.SaveState;

[MemoryPackable]
public partial class SaveState
{
    private byte[] _game = null!;
#pragma warning disable CS0414 // Field is assigned but its value is never used
    private SSRoom _room = null!;
#pragma warning restore CS0414 // Field is assigned but its value is never used
    private Dictionary<int, SSObject> _instances = null!;

    public static SaveState From()
    {
        // TODO: global state
        return new SaveState
        {
            _game = MemoryPackSerializer.Serialize(GameLoader.GeneralInfo),

            // _room = SSRoom.From(RoomManager.CurrentRoom),
            _instances =
                InstanceManager.instances.ToDictionary(x => x.Key, x => SSObject.From(x.Value)),
        };
    }

    public void Into()
    {
        if (MemoryPackSerializer.Deserialize<GameData>(_game) != GameLoader.GeneralInfo)
        {
            throw new NotImplementedException("tried to load save state on a different game!");
        }

        // RoomManager.CurrentRoom = _room.Into();
        InstanceManager.instances = _instances.ToDictionary(x => x.Key, x => x.Value.Into());
    }
}

[MemoryPackable]
public partial class SSRoom
{
    private Room _room = null!;
    private Dictionary<int, SSLayer> _layers = null!;

    public static SSRoom From(RoomContainer room)
    {
        return new SSRoom
        {
            _room = room.RoomAsset,
            _layers = room.Layers.ToDictionary(x => x.Key, x => SSLayer.From(x.Value))
        };
    }

    public RoomContainer Into()
    {
        return new RoomContainer(_room)
        {
            Layers = _layers.ToDictionary(x => x.Key, x => x.Value.Into())
        };
    }
}

[MemoryPackable]
public partial class SSLayer
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

[MemoryPackable]
public partial class SSObject
{
    private int _objDef;
    private Dictionary<string, SSVar> _selfVars = null!;
    private Dictionary<string, SSVar> _builtinVars = null!;

    public static SSObject From(GamemakerObject obj)
    {
        return new SSObject()
        {
            _objDef = obj.Definition.AssetId,
            _selfVars = obj.SelfVariables.ToDictionary(x => x.Key, x => SSVar.From(x.Value)),
            _builtinVars = VariableResolver.BuiltInSelfVariables
                .Where(x => x.Value.setter != null)
                .ToDictionary(x => x.Key, x => SSVar.From(x.Value.getter(obj)))
        };
    }

    public GamemakerObject Into()
    {
        var obj = (GamemakerObject)RuntimeHelpers.GetUninitializedObject(typeof(GamemakerObject));
        obj.Definition = InstanceManager.ObjectDefinitions[_objDef];
        obj.SelfVariables = _selfVars.ToDictionary(x => x.Key, x => x.Value.Into());
        foreach (var x in _builtinVars)
        {
            VariableResolver.BuiltInSelfVariables[x.Key].setter!(obj, x.Value.Into());
        }

        return obj;
    }
}

// stupid. could replace with BinaryReader/Writer
[MemoryPackable]
public partial class SSVar
{
    private SSVarInner _inner = null!;

    public static SSVar From(object? var)
    {
        switch (var)
        {
            string s =>
        }

        throw new NotImplementedException();
    }

    public object? Into()
    {
        throw new NotImplementedException();
    }
}

[MemoryPackable]
[MemoryPackUnion(0, typeof(SSString))]
[MemoryPackUnion(1, typeof(SSInteger))]
[MemoryPackUnion(2, typeof(SSDecimal))]
[MemoryPackUnion(3, typeof(SSArray))]
[MemoryPackUnion(4, typeof(SSMethod))]
[MemoryPackUnion(4, typeof(SSNull))]
public partial class SSVarInner;

[MemoryPackable]
public partial class SSString : SSVarInner
{
    public string Inner = null!;
}

[MemoryPackable]
public partial class SSInteger : SSVarInner
{
    public long Inner;
}

[MemoryPackable]
public partial class SSDecimal : SSVarInner
{
    public double Inner;
}

[MemoryPackable]
public partial class SSArray : SSVarInner
{
    public List<SSVarInner> Inner = null!;
}

[MemoryPackable]
public partial class SSMethod : SSVarInner; // methods are only used in global scripts. fuck

[MemoryPackable]
public partial class SSNull : SSVarInner;