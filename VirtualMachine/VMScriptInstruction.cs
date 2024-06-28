namespace DELTARUNITYStandalone.VirtualMachine;

[Serializable]
public class VMScriptInstruction
{
	public string Raw;

	public VMOpcode Opcode;
	public VMType TypeOne = VMType.None;
	public VMType TypeTwo = VMType.None;

	// we could just store StringData and then parse it in opcodes, but this works too 
	public int IntData;
	public double DoubleData;
	public string StringData;
	public bool BoolData;
	public long LongData;

	public string FunctionName;
	public int FunctionArgumentCount;

	public bool JumpToEnd;

	public bool Drop;

	public int SecondIntData;

	public VMComparison Comparison = VMComparison.None;
}
