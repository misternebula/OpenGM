namespace DELTARUNITYStandalone;
public static class CustomMath
{
	public const double Rad2Deg = 57.2957795131;
	public const double Deg2Rad = 0.01745329251;

	public static double Epsilon = 0.00001;

	public static double Min(params double[] values)
	{
		return values.Min();
	}

	public static double Max(params double[] values)
	{
		return values.Max();
	}

	public static int FloorToInt(double value)
	{
		return (int)Math.Floor(value);
	}

	public static int CeilToInt(double value)
	{
		return (int)Math.Ceiling(value);
	}

	public static int RoundToInt(double value)
	{
		return (int)Math.Round(value);
	}

	/// <summary>
	/// like Math.Sign but returns 1 when passed 0 (matches unity impl)
	/// </summary>
	public static double Sign(double value)
	{
		return value == 0 ? 1 : Math.Sign(value);
	}

	public static bool ApproxEqual(double a, double b)
	{
		return Math.Abs(a - b) <= Epsilon;
	}
}
