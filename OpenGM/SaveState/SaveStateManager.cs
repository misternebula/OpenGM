using System.Diagnostics;
using MessagePack;
using OpenGM.IO;

namespace OpenGM.SaveState;

/// <summary>
/// handles saving and loading save states.
/// the goal is to be small enough to upload to github issues.
/// </summary>
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
public static class SaveStateManager
{
    private static bool _doSave;
    private static bool _doLoad;

    public static void QueueSave() => _doSave = true;

    public static void QueueLoad() => _doLoad = true;

    /// <summary>
    /// do the save or load action at the very end of the loop so we're not in the middle of any ephemeral state
    /// </summary>
    public static void DoQueuedAction()
    {
        if (_doSave)
        {
            _doSave = false;

            using var stream = File.OpenWrite("savestate.bin");
            using var writer = new BinaryWriter(stream);
            var saveState = SaveState.From();
            var bytes = MessagePackSerializer.Serialize(saveState);
            writer.Write(bytes.Length);
            writer.Write(bytes);
            
            DebugLog.Log(MessagePackSerializer.ConvertToJson(bytes));
        }

        if (_doLoad)
        {
            _doLoad = false;

            using var stream = File.OpenRead("savestate.bin");
            using var reader = new BinaryReader(stream);
            var length = reader.ReadInt32();
            var bytes = reader.ReadBytes(length);
            var saveState = MessagePackSerializer.Deserialize<SaveState>(bytes)!;
            saveState.Into();
            
            Debugger.Break();
        }
    }
}