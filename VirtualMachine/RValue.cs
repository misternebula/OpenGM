namespace DELTARUNITYStandalone.VirtualMachine;

/// <summary>
/// a wrapper around an object to mark that it is the v type on the stack.
/// also used whenever a value escapes (is stored outside of) the stack (e.g. variables).
/// </summary>
public readonly struct RValue
{
	public readonly object Value;

	public RValue(object value)
	{
		// sanity check
		if (value is not (int or long or double or bool or string or List<RValue> or Undefined))
			throw new ArgumentException($"bad value {value.GetType()} {value} passed into rvalue");

		Value = value;
	}

	public override string ToString()
	{
		return $"RValue({Value})";
	}

	/*
	// prefer using these over accessing Value
	public static implicit operator int(RValue v) => (int)(v.Value ?? throw new NullReferenceException());
	public static implicit operator RValue(int v) => new(v);
	public static implicit operator long(RValue v) => (long)(v.Value ?? throw new NullReferenceException());
	public static implicit operator RValue(long v) => new(v);
	public static implicit operator double(RValue v) => (double)(v.Value ?? throw new NullReferenceException());
	public static implicit operator RValue(double v) => new(v);
	public static implicit operator bool(RValue v) => (bool)(v.Value ?? throw new NullReferenceException());
	public static implicit operator RValue(bool v) => new(v);
	public static implicit operator string(RValue v) => (string)(v.Value ?? throw new NullReferenceException());
	public static implicit operator RValue(string v) => new(v);
	public static implicit operator List<RValue>(RValue v) => (List<RValue>)(v.Value ?? throw new NullReferenceException());
	public static implicit operator RValue(List<RValue> v) => new(v);
	*/
}

/// <summary>
/// use this in RValue instead of null
/// </summary>
public readonly struct Undefined
{
	public static readonly Undefined Value = new();
}
