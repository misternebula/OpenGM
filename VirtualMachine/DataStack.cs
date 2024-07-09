namespace DELTARUNITYStandalone.VirtualMachine;

/// <summary>
/// stack that also stores the type
/// </summary>
public class DataStack : Stack<(object? value, VMType type)>
{
	public DataStack() : base() { }
	public DataStack(DataStack stack) : base(stack) { } // maybe cloning isnt needed

	public void Push(object? value, VMType type)
	{
		base.Push((value, type));
	}

	/// <summary>
	/// checks size before popping
	/// </summary>
	public T Pop<T>(VMType typeToPop)
	{
		var (poppedValue, typeOfPopped) = base.Pop();

		// do strict size checking
		if (VMExecutor.VMTypeToSize(typeOfPopped) != VMExecutor.VMTypeToSize(typeToPop))
		{
			throw new NotImplementedException($"Popped value {poppedValue} is type {typeOfPopped}, which can't be converted to {typeToPop}!");
		}

		// then do loose conv to cover b to i etc
		// technically wrong if doing s to i (parses string as int) but whatever
		return poppedValue.Conv<T>();
	}

	/// <summary>
	/// checks size before popping
	/// </summary>
	public object? Pop(VMType typeToPop) => Pop<object?>(typeToPop);

	[Obsolete("dont use this version")]
	public new void Push((object? value, VMType type) x) => base.Push(x);

	[Obsolete("dont use this version")]
	public new (object? value, VMType type) Pop() => base.Pop();
}
