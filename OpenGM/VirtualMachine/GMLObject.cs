namespace OpenGM.VirtualMachine;

/// <summary>
/// a struct
/// </summary>
internal class GMLObject : IStackContextSelf
{
	public Dictionary<string, object?> SelfVariables { get; } = new();
}
