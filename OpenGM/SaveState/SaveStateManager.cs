namespace OpenGM.SaveState;

/// <summary>
/// handles saving and loading save states.
/// the goal is to be small enough to upload to github issues.
/// </summary>
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
            writer.WriteMemoryPack(saveState);
        }

        if (_doLoad)
        {
            _doLoad = false;

            using var stream = File.OpenRead("savestate.bin");
            using var reader = new BinaryReader(stream);
            var saveState = reader.ReadMemoryPack<SaveState>();
            saveState.Into();
        }
    }
}