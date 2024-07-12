namespace OpenGM.VirtualMachine;

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
	public object? Pop(VMType typeToPop)
	{
		var (poppedValue, typeOfPopped) = base.Pop();

		// do strict size checking
		if (VMExecutor.VMTypeToSize(typeOfPopped) != VMExecutor.VMTypeToSize(typeToPop))
		{
			throw new NotImplementedException($"Popped value {poppedValue} is type {typeOfPopped}, which can't be converted to {typeToPop}!");
		}

		return poppedValue;
	}

	[Obsolete("dont use this version")]
	public new void Push((object? value, VMType type) x) => base.Push(x);

	[Obsolete("dont use this version")]
	public new (object? value, VMType type) Pop() => base.Pop();
}
