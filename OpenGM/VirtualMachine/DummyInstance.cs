namespace OpenGM.VirtualMachine
{
	public class DummyInstance : IStackContextSelf
	{
		public Dictionary<string, object?> SelfVariables { get; } = new();
	}
}
