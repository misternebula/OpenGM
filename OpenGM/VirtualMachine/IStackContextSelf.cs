namespace OpenGM.VirtualMachine;
public interface IStackContextSelf
{
    public Dictionary<string, object?> SelfVariables { get; }
}
