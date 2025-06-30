namespace OpenGM.VirtualMachine
{
	public class DummyInstance : IStackContextSelf
	{
		public Dictionary<string, object?> SelfVariables { get; }

		public DummyInstance() => SelfVariables = new();
	}
}
