using MemoryPack;
using OpenGM.Loading;

namespace OpenGM.VirtualMachine;

/// <summary>
/// VMCode but with a different index and name.
/// sometimes these arent associated with code so the index is different.
/// script_execute uses this index, and i think thats it. its weird
/// </summary>
[MemoryPackable]
public partial class VMScript
{
	public int AssetIndex;
	public string Name = null!;
	public int CodeIndex = -1;

	public VMCode? GetCode() => CodeIndex == -1 ? null : GameLoader.Codes[CodeIndex];
}
