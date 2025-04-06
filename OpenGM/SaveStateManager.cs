namespace OpenGM;

/// <summary>
/// handles saving and loading save states.
/// the goal is to be small enough to upload to github issues.
/// </summary>
public static class SaveStateManager
{
    /*
     * store:
     * - the entire room
     *   so all layers/tiles since they can be created dynamically,
     *   or otherwise track which guy are dynamic and only store those, depending on memory
     *   would have to load the room without calling the creation scripts, bleh
     * - all non room objects and their variables (builtin with setter or not)
     *   so actually we would be storing all objects anyway i guess, probably not too much memory
     * - dynamically created assets
     *   other otherwise all of them except sound/texture/code since they are the largest assets i think
     * - surface stuff (i guess that counts as an asset)
     * - builtins with setters
     * - anything else not covered by builtins (i think that is nothing though, i hope)
     * - NOT the data/call/environment stack since those are all ephemeral (empty when the frame is over)
     */
}