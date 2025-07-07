using OpenTK.Mathematics;

namespace OpenGM;
public static class CustomMath
{
	public const double Rad2Deg = 57.2957795131;
	public const double Deg2Rad = 0.01745329251;

	public static double Epsilon = 0.00001; // not const, as this value can be changed

	public static double Min(params double[] values)
	{
		if (values.Length == 2)
		{
			// Avoid expensive LINQ query in most common situation
			return values[0] < values[1] ? values[0] : values[1];
		}

		return values.Min();
	}

	public static int Min(params int[] values)
	{
		if (values.Length == 2)
		{
			// Avoid expensive LINQ query in most common situation
			return values[0] < values[1] ? values[0] : values[1];
		}

		return values.Min();
	}

	public static double Max(params double[] values)
	{
		if (values.Length == 2)
		{
			// Avoid expensive LINQ query in most common situation
			return values[0] > values[1] ? values[0] : values[1];
		}

		return values.Max();
	}

	public static int Max(params int[] values)
	{
		if (values.Length == 2)
		{
			// Avoid expensive LINQ query in most common situation
			return values[0] > values[1] ? values[0] : values[1];
		}

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

	public static bool ApproxEqual(double a, double b) => Math.Abs(a - b) <= Epsilon;

	public static bool ApproxGreaterThan(double a, double b) => a > b + Epsilon;
	public static bool ApproxGreaterThanEqual(double a, double b) => ApproxEqual(a, b) || ApproxGreaterThan(a, b);

	public static bool ApproxLessThan(double a, double b) => a + Epsilon < b;
	public static bool ApproxLessThanEqual(double a, double b) => ApproxEqual(a, b) || ApproxLessThan(a, b);

	public static double Mod(double a, double b)
	{
		// % is actually remainder in C/C#, and will return a negative number
		
		/*
		 * Remainder and Modulus have the same value for positive values.
		 * % in C# is NOT modulo - it's remainder.
		 * Modulus always has the same sign as the divisor, and remainder has the same sign as the dividend
		 * (dividend / divisor = quotient)
		 * 10 REM 3 = 1
		 * -10 REM 3 = -1
		 * 10 REM -3 = 1
		 * -10 REM -3 = -1
		 * 10 MOD 3 = 1
		 * -10 MOD 3 = 2
		 * 10 MOD -3 = -2
		 * -10 MOD -3 = -1
		 */
		
		return ((a % b) + b) % b;
	}

	public static Vector2d RotateAroundPoint(this Vector2d p, Vector2d pivot, double angleAntiClockwise)
	{
		var sin = Math.Sin(Deg2Rad * -angleAntiClockwise);
		var cos = Math.Cos(Deg2Rad * -angleAntiClockwise);

		(p.X, p.Y) = (p.X - pivot.X, p.Y - pivot.Y); // translate matrix
		(p.X, p.Y) = (p.X * cos - p.Y * sin, p.X * sin + p.Y * cos); // rotate matrix
		(p.X, p.Y) = (p.X + pivot.X, p.Y + pivot.Y); // translate matrix

		return p;
	}

	public static Color4 BlendBetweenPoints(Vector2d val, Vector2d[] points, Color4[] colors)
	{
		var distances = new double[points.Length];
		for (var i = 0; i < distances.Length; i++)
		{
			distances[i] = Vector2d.Distance(val, points[i]);
		}

		var weights = new double[distances.Length];
		for (var i = 0; i < weights.Length; i++)
		{
			weights[i] = 1.0 / (distances[i] + 0.00001); // avoid division by 0
		}

		var weightTotal = weights.Sum();

		for (var i = 0; i < weights.Length; i++)
		{
			// normalize
			weights[i] /= weightTotal;
		}

		var r = 0f;
		var g = 0f;
		var b = 0f;
		var a = 0f;

		for (var i = 0; i < weights.Length; i++)
		{
			var c = colors[i].Scale(weights[i]);
			r += c.R;
			g += c.G;
			b += c.B;
			a += c.A;
		}

		return new(r, g, b, a);
	}

	/*
	 * JS Functions
	 * Functions that replicate JS math functions for HTML referenced code.
	 */

	// TODO : replace these with better implementations

	public static float FMod(float x, float y)
	{
		if (x == 0)
		{
			return 0;
		}

		var t = (x * 0x1000000) % (y * 0x1000000);
		t /= 0x1000000;
		return t;
	}

	public static double Round(double n) => Math.Round(n, MidpointRounding.ToPositiveInfinity);

	public static float ClampFloat(float f) => DoubleTilde(f * 1000000) / 1000000.0f;

	// Substitute for double bitwise NOT (~~).
	public static int DoubleTilde(float f) => (int)Math.Round(f, MidpointRounding.ToZero);
	public static int DoubleTilde(double d) => (int)Math.Round(d, MidpointRounding.ToZero);
}
