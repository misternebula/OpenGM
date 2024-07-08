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
			throw new ArgumentException($"bad value {value.GetType()} {value} pushed to data stack");

		base.Push(value);
	}

	public T Pop<T>(VMType typeToPop)
	{
		var poppedValue = base.Pop();
		var typeOfPopped = VMExecutor.GetTypeOfObject(poppedValue);
		var sizeOfPopped = VMExecutor.VMTypeToSize(typeOfPopped);

		if (sizeOfPopped != VMExecutor.VMTypeToSize(typeToPop))
		{
			throw new NotImplementedException($"Popped value {poppedValue} is type {typeOfPopped}, which can't be converted to {typeToPop}!");
		}

		// TODO: this should bitcast instead of conving, right??
		return (T)VMExecutor.ConvertTypes(poppedValue, typeOfPopped, typeToPop);
	}

	public object Pop(VMType typeToPop) => Pop<object>(typeToPop);

	[Obsolete("pass in VMType")]
	public new object Pop() => base.Pop();
}
