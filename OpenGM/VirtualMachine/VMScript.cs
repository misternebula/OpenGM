using MemoryPack;

namespace OpenGM.VirtualMachine;

[MemoryPackable]
public partial class VMScript
{
	public int AssetId;
	public string Name = null!;
	public bool IsGlobalInit;
	public List<string> LocalVariables = null!;
	public Dictionary<int, int> Labels = new();
	public List<FunctionDefinition> Functions = new();
	public List<VMScriptInstruction> Instructions = new();
}

[MemoryPackable]
public partial class FunctionDefinition
{
	public int InstructionIndex;
	public string FunctionName = null!;
}
