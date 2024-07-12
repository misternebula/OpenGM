using OpenTK.Mathematics;

namespace OpenGM;
public static class CustomMath
{
	public const double Rad2Deg = 57.2957795131;
	public const double Deg2Rad = 0.01745329251;

	public static double Epsilon = 0.00001; // not const, as this value can be changed

	public static double Min(params double[] values)
	{
		return values.Min();
	}

	public static int Min(params int[] values)
	{
		return values.Min();
	}

	public static double Max(params double[] values)
	{
		return values.Max();
	}

	public static int Max(params int[] values)
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

	public static double Mod(double a, double b)
	{
		// % is actually remainder in C/C#, and will return a negative number
		return ((a % b) + b) % b;
	}

	public static Vector2d RotateAroundPoint(this Vector2d p, Vector2d pivot, double angleAntiClockwise)
	{
		// todo : maybe work out the actual formula for (+y = south) so we dont have to invert the angle

		var sin = Math.Sin(Deg2Rad * -angleAntiClockwise);
		var cos = Math.Cos(Deg2Rad * -angleAntiClockwise);

		(p.X, p.Y) = (p.X - pivot.X, p.Y - pivot.Y); // translate matrix
		(p.X, p.Y) = (p.X * cos - p.Y * sin, p.X * sin + p.Y * cos); // rotate matrix
		(p.X, p.Y) = (p.X + pivot.X, p.Y + pivot.Y); // translate matrix

		return p;
	}
}
