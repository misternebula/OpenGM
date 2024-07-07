namespace DELTARUNITYStandalone.VirtualMachine;

/// <summary>
/// stack that does extra sanity checks
/// </summary>
public class DataStack : Stack<object>
{
	public DataStack() : base() { }
	public DataStack(DataStack stack) : base(stack) { } // maybe cloning isnt needed

	public new void Push(object value)
	{
		// sanity check
		if (value is not (int or long or double or bool or string or RValue))
			throw new ArgumentException($"bad value {value} pushed to data stack");

		base.Push(value);
	}

	// TODO: use this instead of PopType
	public T Pop<T>(VMType typeToPop)
	{
		return (T)base.Pop();
	}
}
