using MemoryPack;
using OpenGM.Loading;

namespace OpenGM.VirtualMachine;

[MemoryPackable]
public partial class VMScript
{
	public int AssetIndex;
	public string Name = null!;
	public int CodeIndex = -1;
	public bool IsGlobalInit;

	public VMCode? GetCode() => CodeIndex == -1 ? null : GameLoader.Codes[CodeIndex];
}
