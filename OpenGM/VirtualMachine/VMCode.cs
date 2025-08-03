namespace OpenGM.VirtualMachine;

public class VMCode
{
    public int AssetId;
    public string Name = null!;
    /// <summary>
    /// in deltarune, script files have functions.
    /// the script function VMCode is empty and has parent = script asset VMCode.
    /// the script asset VMCode has all the actual code.
    /// </summary>
    public int ParentAssetId = -1;
    public List<string> LocalVariables = null!;
    public Dictionary<int, int> Labels = new();
    public List<FunctionDefinition> Functions = new();
    public List<VMCodeInstruction> Instructions = new();

    public event Action OnCodeExecuted = () => { };

    public void CodeExecuted() => OnCodeExecuted?.Invoke();
}

public class FunctionDefinition
{
    public int InstructionIndex;
    public string FunctionName = null!;

    public bool HasStaticInitRan;
    public Dictionary<string, object?> StaticVariables = new();
}
