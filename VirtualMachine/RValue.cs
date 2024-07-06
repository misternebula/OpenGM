namespace DELTARUNITYStandalone.VirtualMachine;

/// <summary>
/// a wrapper around an object to mark that it is the v type on the stack.
/// also used whenever a value escapes (is stored outside of) the stack (e.g. variables).
/// </summary>
public readonly struct RValue
{
	public readonly object? Value;

	public RValue(object? value)
	{
		// unwrap nested rvalues
		var curValue = value;
		while (curValue is RValue r)
		{
			curValue = r.Value;
		}

		Value = curValue;
	}

	public override string ToString()
	{
		return $"RValue({Value})";
	}
}
