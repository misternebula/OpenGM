namespace DELTARUNITYStandalone.VirtualMachine;

/// <summary>
/// stack that does extra sanity checks
/// </summary>
public class DataStack : Stack<object>
{
	public DataStack() : base() { }
	public DataStack(DataStack stack) : base(stack) { } // maybe cloning isnt needed

	/// <summary>
	/// checks size before popping
	/// </summary>
	public new void Push(object value)
	{
		// sanity check
		if (value is not (int or long or double or bool or string or RValue))
			throw new ArgumentException($"bad value {value.GetType()} {value} pushed to data stack");

		base.Push(value);
	}

	/// <summary>
	/// checks size before popping
	/// </summary>
	public T Pop<T>(VMType typeToPop)
	{
		var poppedValue = base.Pop();
		var typeOfPopped = VMExecutor.GetTypeOfObject(poppedValue);

		// do strict size checking
		if (VMExecutor.VMTypeToSize(typeOfPopped) != VMExecutor.VMTypeToSize(typeToPop))
		{
			throw new NotImplementedException($"Popped value {poppedValue} is type {typeOfPopped}, which can't be converted to {typeToPop}!");
		}

		// then do loose conv to cover b to i etc
		// technically wrong if doing s to i (parses string as int) but whatever
		// TODO: make it use Conv<T> since we already did the strict type check above LOL
		return (T)VMExecutor.ConvertTypes(poppedValue, typeOfPopped, typeToPop);
	}

	public object Pop(VMType typeToPop) => Pop<object>(typeToPop);

	[Obsolete("use Pop(VMType) or Pop<T>(VMType)")]
	public new object Pop() => base.Pop();
}
