namespace DELTARUNITYStandalone.VirtualMachine;

public class RValue
{
	public object Value;

	public RValue(object value)
	{
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
