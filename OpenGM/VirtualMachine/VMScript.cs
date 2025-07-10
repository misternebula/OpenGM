using MemoryPack;
using OpenGM.Loading;

namespace OpenGM.VirtualMachine;

/// <summary>
/// VMCode but with a different index and name.
/// gml can only reference scripts, not code.
/// code is only directly used in object, room, and global init. everything else is script.
/// </summary>
[MemoryPackable]
public partial class VMScript
{
    public int AssetIndex;
    public string Name = null!;
    public int CodeIndex = -1;

    public VMCode? GetCode() => CodeIndex == -1 ? null : GameLoader.Codes[CodeIndex];
}
