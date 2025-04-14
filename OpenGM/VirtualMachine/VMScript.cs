using MemoryPack;
using OpenGM.Loading;

namespace OpenGM.VirtualMachine;

/// <summary>
/// VMCode but with a different index and name.
/// gml can only reference scripts, not code.
/// e.g. script_execute, NewGMLObject, method, push.i [string]
/// </summary>
[MemoryPackable]
public partial class VMScript
{
	public int AssetIndex;
	public string Name = null!;
	public int CodeIndex = -1;

	public VMCode? GetCode() => CodeIndex == -1 ? null : GameLoader.Codes[CodeIndex];
}
