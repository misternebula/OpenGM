namespace DELTARUNITYStandalone.VirtualMachine;

[Serializable]
public class VMScript
{
	public int AssetId;
	public string Name;
	public bool IsGlobalInit;
	public List<string> LocalVariables;
	public Dictionary<int, Label> Labels = new();
	public List<VMScriptInstruction> Instructions = new();
}

[Serializable]
public class Label
{
	public int InstructionIndex;
	public string FunctionName = null;
}
