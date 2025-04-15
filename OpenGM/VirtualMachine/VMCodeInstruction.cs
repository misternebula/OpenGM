using MemoryPack;

namespace OpenGM.VirtualMachine;

[MemoryPackable]
public partial class VMCodeInstruction
{
	public string Raw = null!;

	public VMOpcode Opcode;
	public VMType TypeOne = VMType.None;
	public VMType TypeTwo = VMType.None;

	public string variableName = null!;
	public VariableType variableType;
	public VariablePrefix variablePrefix;
	public int assetId;

	// we could just store StringData and then parse it in opcodes, but this works too 
	public int IntData;
	public short ShortData;
	public double DoubleData;
	public string StringData = null!;
	public bool BoolData;
	public long LongData;

	public string FunctionName = null!;
	public int FunctionArgumentCount;

	public bool JumpToEnd;

	public bool Drop;

	public int SecondIntData;

	public VMComparison Comparison = VMComparison.None;
}
