using MemoryPack;
using OpenGM.SerializedFiles;

namespace OpenGM.SaveState;

/*
 * store:
 * - the entire room
 *   so all room layers/tiles/objects since they can be created dynamically,
 *   otherwise track which things are dynamic and only store those, depending on memory
 *   would have to load the room without calling the creation scripts, so might as well just store the whole thing because below
 * - all non room objects and their variables (builtin with setter or not)
 *   so actually we would be storing all objects anyway i guess, probably not too much memory
 * - dynamically created assets
 *   otherwise all of them except sound/texture/code since they are the largest assets i think
 * - surface stuff (i guess that counts as an asset)
 * - audio instance (also an asset sorta)
 * - builtins with setters
 * - anything else not covered by builtins:
 *   blend mode
 *
 * - NOT the data/call/environment stack since those are all ephemeral (empty when the frame is over)
 */
[MemoryPackable]
public partial class SaveState
{
    private SaveStateRoom _room = null!;
    private Dictionary<int, SaveStateObject> _instances = null!;

    public static SaveState From()
    {
        // TODO: global state

        return new SaveState
        {
            _room = SaveStateRoom.From(RoomManager.CurrentRoom),
            _instances =
                InstanceManager.instances.ToDictionary(pair => pair.Key, pair => SaveStateObject.From(pair.Value)),
        };
    }

    public void Into()
    {
        RoomManager.CurrentRoom = _room.Into();
        InstanceManager.instances = _instances.ToDictionary(pair => pair.Key, pair => pair.Value.Into());
    }
}

[MemoryPackable]
public partial class SaveStateRoom
{
    private Room _room = null!;
    private Dictionary<int, SaveStateLayer> _layers = null!;

    public static SaveStateRoom From(RoomContainer room)
    {
        return new SaveStateRoom
        {
            _room = room.RoomAsset,
            _layers = room.Layers.ToDictionary(pair => pair.Key, pair => SaveStateLayer.From(pair.Value))
        };
    }

    public RoomContainer Into()
    {
        return new RoomContainer(_room)
        {
            Layers = _layers.ToDictionary(pair => pair.Key, pair => pair.Value.Into())
        };
    }
}

[MemoryPackable]
public partial class SaveStateLayer
{
    public static SaveStateLayer From(LayerContainer layer)
    {
        throw new NotImplementedException();
    }

    public LayerContainer Into()
    {
        throw new NotImplementedException();
    }
}

[MemoryPackable]
public partial class SaveStateObject
{
    public static SaveStateObject From(GamemakerObject obj)
    {
        throw new NotImplementedException();
    }

    public GamemakerObject Into()
    {
        throw new NotImplementedException();
    }
}