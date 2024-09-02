using MemoryPack;

namespace OpenGM.VirtualMachine;

[MemoryPackable]
public partial class VMCode
{
	public int AssetId;
	public string Name = null!;
	public int ParentAssetId = -1;
	public List<string> LocalVariables = null!;
	public Dictionary<int, int> Labels = new();
	public List<FunctionDefinition> Functions = new();
	public List<VMCodeInstruction> Instructions = new();
}

[MemoryPackable]
public partial class FunctionDefinition
{
	public int InstructionIndex;
	public string FunctionName = null!;
}
