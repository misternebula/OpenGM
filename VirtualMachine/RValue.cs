namespace DELTARUNITYStandalone.VirtualMachine;

/// <summary>
/// a wrapper around an object to mark that it is the v type on the stack.
/// also used whenever a value escapes (is stored outside of) the stack (e.g. variables).
/// </summary>
public readonly struct RValue
{
	/// <summary>
	/// can store: int, long, double, bool, string, array, null
	/// </summary>
	public readonly object? Value;

	public RValue(object? value)
	{
		// sanity check (probably do this for the stack too)
		if (value is not (int or long or double or bool or string or List<RValue> or null))
			throw new ArgumentException($"bad value {value} passed into rvalue");

		Value = value;
	}

	public override string ToString()
	{
		return $"RValue({Value})";
	}
}
