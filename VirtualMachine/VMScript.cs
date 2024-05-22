namespace DELTARUNITYStandalone.VirtualMachine;

[Serializable]
public class VMScript
{
	public int AssetId;
	public string Name;
	public bool IsGlobalInit;
	public List<string> LocalVariables;
	public Dictionary<int, int> Labels = new();
	public List<VMScriptInstruction> Instructions = new();
}
