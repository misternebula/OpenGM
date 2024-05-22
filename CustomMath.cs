using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DELTARUNITYStandalone;
public static class CustomMath
{
	public const double Rad2Deg = 57.2957795131;
	public const double Deg2Rad = 0.01745329251;

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
}
