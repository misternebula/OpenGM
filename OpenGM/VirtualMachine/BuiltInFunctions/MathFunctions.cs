﻿using System.Collections;
using System.Text;

namespace OpenGM.VirtualMachine;

public static partial class ScriptResolver
{
	public static object is_real(object?[] args) => args[0] is int or long or short or double or float;

	public static object is_string(object?[] args) => args[0] is string;

	public static object is_undefined(object?[] args) => args[0] is null;

	public static object array_length_1d(object?[] args)
	{
		var array = args[0].Conv<IList>();
		return array.Count;
	}

	public static object array_height_2d(object?[] args)
	{
		var array = args[0].Conv<IList>();
		return array.Count;
	}

	public static object array_length_2d(object?[] args)
	{
		var array = args[0].Conv<IList>();
		var index0 = array[0];
		return (index0 as IList)!.Count;
	}

	public static object array_length(object?[] args)
	{
		var array = args[0].Conv<IList>();
		return array.Count;
	}

	public static object random(object?[] args)
	{
		var upper = args[0].Conv<double>();
		return GMRandom.fYYRandom() * upper;
	}

	public static object random_range(object?[] args)
	{
		var n1 = args[0].Conv<double>();
		var n2 = args[1].Conv<double>();

		if (n1 == n2)
		{
			return n1;
		}

		if (n1 > n2)
		{
			// swap if wrong order
			(n1, n2) = (n2, n1);
		}

		var rand = GMRandom.fYYRandom();
		var result = n1 + (rand * (n2 - n1));

		// HTML does another call to rand() here, without using the result. Why???

		return result;
	}

	public static object irandom(object?[] args)
	{
		/*
		 * JS does not support int64, so the HTML runner uses a single rand() call
		 * to create an int32, then calls rand() again to preserve state parity with C++.
		 *
		 * This means that numbers generated by irandom() are completely different between the runners,
		 * but the internal RNG state remains the same.
		 *
		 * I've tested this returns the same numbers as the C++ runner.
		 */

		var n = args[0].Conv<long>();

		var nSign = 1;
		if (n < 0)
		{
			nSign = -1;
		}

		var rand1 = GMRandom.YYRandom();
		var rand2 = GMRandom.YYRandom();
		var doubleSign = Math.Sign(nSign + n);

		var combined = ((long)rand2 << 32) | rand1; // combine rand1 and rand2 into a single Int64
		var anded = combined & 0x7fffffffffffffff;
		return anded % (doubleSign * (nSign + n)) * doubleSign;
	}

	public static object irandom_range(object?[] args)
	{
		var lower = args[0].Conv<long>();
		var upper = args[1].Conv<long>();

		long difference;
		if (lower < upper)
		{
			difference = upper - lower;
		}
		else
		{
			difference = lower - upper;
			(lower, upper) = (upper, lower);
		}

		var rand1 = GMRandom.YYRandom();
		var rand2 = GMRandom.YYRandom();
		var doubleSign = Math.Sign(difference + 1);

		var combined = ((long)rand2 << 32) | rand1; // combine rand1 and rand2 into a single Int64
		var anded = combined & 0x7fffffffffffffff;
		return (anded % (doubleSign * (difference + 1)) * doubleSign) + lower;
	}

	public static object? randomize(object?[] args)
	{
		// todo : implement
		//throw new NotImplementedException();
		return 0;
	}

	public static object abs(object?[] args)
	{
		var val = args[0].Conv<double>();
		return Math.Abs(val);
	}

	public static object round(object?[] args)
	{
		var num = args[0].Conv<double>();
		return CustomMath.RoundToInt((float)num); // BUG: shouldnt this just do Math.Round?
	}

	public static object floor(object?[] args)
	{
		var n = args[0].Conv<double>();
		return Math.Floor(n);
	}

	public static object ceil(object?[] args)
	{
		var n = args[0].Conv<double>();
		return Math.Ceiling(n);
	}

	public static object sign(object?[] args)
	{
		// TODO : handle NaN somehow????
		var n = args[0].Conv<double>();
		return Math.Sign(n);
	}

	public static object sin(object?[] args)
	{
		var val = args[0].Conv<double>();
		return Math.Sin(val);
	}

	public static object cos(object?[] args)
	{
		var val = args[0].Conv<double>();
		return Math.Cos(val);
	}

	public static object arcsin(object?[] args)
	{
		var x = args[0].Conv<double>(); // in radians

		if (x < -1 || x > 1)
		{
			throw new NotSupportedException($"x is {x}");
		}

		return Math.Asin(x);
	}

	public static object arccos(object?[] args)
	{
		var x = args[0].Conv<double>(); // in radians

		if (x < -1 || x > 1)
		{
			throw new NotSupportedException($"x is {x}");
		}

		return Math.Acos(x);
	}

	public static object dsin(object?[] args)
	{
		var a = args[0].Conv<double>(); // degrees
		return Math.Sin(a * CustomMath.Deg2Rad);
	}

	public static object dcos(object?[] args)
	{
		var val = args[0].Conv<double>(); // degrees
		return Math.Cos(val * CustomMath.Deg2Rad);
	}

	public static object degtorad(object?[] args)
	{
		var deg = args[0].Conv<double>();
		return deg * double.Pi / 180;
	}

	public static object radtodeg(object?[] args)
	{
		var rad = args[0].Conv<double>();
		return rad * 180 / double.Pi;
	}

	public static object power(object?[] args)
	{
		var x = args[0].Conv<double>();
		var n = args[1].Conv<double>();

		return Math.Pow(x, n);
	}

	private static int realToInt(double value)
	{
		return value < 0 ? CustomMath.CeilToInt((float)value) : CustomMath.FloorToInt((float)value);
	}

	public static object min(object?[] args)
	{
		var arguments = new double[args.Length];
		for (var i = 0; i < args.Length; i++)
		{
			arguments[i] = args[i].Conv<double>();
		}

		return arguments.Min();
	}

	public static object max(object?[] args)
	{
		var arguments = new double[args.Length];
		for (var i = 0; i < args.Length; i++)
		{
			arguments[i] = args[i].Conv<double>();
		}

		return arguments.Max();
	}

	public static object? choose(object?[] args)
	{
		return args[GMRandom.YYRandom(args.Length)];
	}

	public static object? clamp(object?[] args)
	{
		var val = args[0].Conv<double>();
		var min = args[1].Conv<double>();
		var max = args[2].Conv<double>();

		// TODO : do we need to use Epsilon here?

		if (val <= min)
		{
			return min;
		}

		if (val >= max)
		{
			return max;
		}

		return val;
	}

	public static object? lerp(object?[] args)
	{
		var a = args[0].Conv<double>();
		var b = args[1].Conv<double>();
		var amt = args[2].Conv<double>();

		return a + ((b - a) * amt);
	}

	public static object real(object?[] args)
	{
		var str = args[0].Conv<string>();
		return str.Conv<double>();
	}

	public static object @string(params object?[] args)
	{
		var valueOrFormat = args[0];

		var values = new object?[] { };
		if (args.Length > 1)
		{
			values = args[1..];
		}

		if (args.Length > 1)
		{
			// format
			var format = (string)valueOrFormat!;

			// doing this like im in c lol
			var result = new StringBuilder();
			var bracesString = new StringBuilder();

			var inBraces = false;
			foreach (var formatChar in format)
			{
				if (!inBraces)
				{
					if (formatChar == '{')
					{
						inBraces = true;
					}
					else
					{
						result.Append(formatChar);
					}
				}
				else
				{
					if (formatChar == '}')
					{
						inBraces = false;
						var bracesNumber = int.Parse(bracesString.ToString());
						bracesString.Clear();
						result.Append(@string(values[bracesNumber]));
					}
					else
					{
						bracesString.Append(formatChar);
					}
				}
			}
			if (inBraces)
			{
				result.Append(bracesString);
			}

			return result.ToString();
		}
		else
		{
			// value

			if (valueOrFormat is IList array)
			{
				// array
				// is any of this right? not sure.
				var index = 0;
				var result = new StringBuilder("[");
				foreach (var item in array)
				{
					var elementString = (string)@string(item);

					result.Append(elementString);
					if (index < array.Count - 1)
					{
						result.Append(", ");
					}
					index++;
				}

				result.Append("]");
				return result.ToString();
			}
			else if (valueOrFormat is null)
			{
				return "undefined";
			}
			else if (valueOrFormat is bool b)
			{
				return b.Conv<string>();
			}
			else if (valueOrFormat is string s)
			{
				return s;
			}
			else
			{
				// real
				var num = valueOrFormat.Conv<double>();
				var afterTwoDigits = num % 0.01f;
				var truncated = num - afterTwoDigits;

				return (truncated % 1) == 0
					? truncated.ToString()
					: Math.Round(truncated, 2).ToString();
			}
		}
	}

	public static object ord(object?[] args)
	{
		var str = args[0].Conv<string>();

		return (int)Encoding.UTF8.GetBytes(str)[0];
	}

	public static object string_length(object?[] args)
	{
		var str = args[0].Conv<string>();

		if (string.IsNullOrEmpty(str))
		{
			return 0;
		}

		return str.Length;
	}

	public static object string_pos(object?[] args)
	{
		var substr = args[0].Conv<string>();
		var str = args[1].Conv<string>();

		return str.IndexOf(substr) + 1;
	}

	public static object string_copy(object?[] args)
	{
		var str = args[0].Conv<string>();
		var index = args[1].Conv<int>();
		var count = args[2].Conv<int>();

		if (index < 1)
		{
			index = 1;
		}

		var maxLength = str.Length - (index - 1);

		// no idea if this is what GM does, but i THINK it is
		if (count > maxLength)
		{
			count = maxLength;
		}

		return str.Substring(index - 1, count);
	}

	public static object string_char_at(object?[] args)
	{
		var str = args[0].Conv<string>();
		var index = args[1].Conv<int>();

		if (string.IsNullOrEmpty(str) || index > str.Length)
		{
			return "";
		}

		if (index <= 0)
		{
			return str[0].ToString();
		}

		// guh index starts at one? goofy gamemaker
		return str[index - 1].ToString();
	}

	public static object string_delete(object?[] args)
	{
		var str = args[0].Conv<string>();
		var index = args[1].Conv<int>();
		var count = args[2].Conv<int>();

		return str.Remove(index - 1, count);
	}

	public static object string_insert(object?[] args)
	{
		var substr = args[0].Conv<string>();
		var str = args[1].Conv<string>();
		var index = args[2].Conv<int>();

		return str.Insert(index - 1, substr);
	}

	public static object string_lower(object?[] args)
	{
		var str = args[0].Conv<string>();
		// TODO : only do the 26 english alphabet letters
		return str.ToLower();
	}

	public static object string_upper(object?[] args)
	{
		var str = args[0].Conv<string>();
		// TODO : only do the 26 english alphabet letters
		return str.ToUpper();
	}

	public static object string_replace_all(object?[] args)
	{
		var str = args[0].Conv<string>();
		var substr = args[1].Conv<string>();
		var newstr = args[2].Conv<string>();

		return str.Replace(substr, newstr);
	}

	public static object string_hash_to_newline(object?[] args)
	{
		var text = args[0].Conv<string>();

		if (string.IsNullOrEmpty(text))
		{
			return text;
		}

		return text.Replace('#', '\n');
	}

	public static object point_distance(object?[] args)
	{
		var x1 = args[0].Conv<double>();
		var y1 = args[1].Conv<double>();
		var x2 = args[2].Conv<double>();
		var y2 = args[3].Conv<double>();

		var horizDistance = Math.Abs(x2 - x1);
		var vertDistance = Math.Abs(y2 - y1);

		return Math.Sqrt((horizDistance * horizDistance) + (vertDistance * vertDistance));
	}

	public static object point_direction(params object?[] args)
	{
		var x1 = args[0].Conv<double>();
		var y1 = args[1].Conv<double>();
		var x2 = args[2].Conv<double>();
		var y2 = args[3].Conv<double>();

		// TODO : simplify this mess lol

		var gmHoriz = x2 - x1;
		var gmVert = y2 - y1;

		if (gmHoriz >= 0 && gmVert == 0)
		{
			return 0;
		}

		if (gmHoriz > 0 && gmVert == 0)
		{
			return 0;
		}

		if (gmHoriz == 0 && gmVert < 0)
		{
			return 90;
		}

		// +gmVert means down, -gmVert means up
		gmVert = -gmVert;

		var angle = Math.Atan(gmVert / gmHoriz) * CustomMath.Rad2Deg;

		if (gmVert > 0)
		{
			if (gmHoriz > 0)
			{
				return angle;
			}

			return angle + 180;
		}

		if (gmHoriz > 0)
		{
			return 360 + angle;
		}

		return 180 + angle;
	}

	public static object? lengthdir_x(object?[] args)
	{
		var len = args[0].Conv<double>();
		var dir = args[1].Conv<double>();

		return len * Math.Cos(dir * CustomMath.Deg2Rad);
	}

	public static object? lengthdir_y(object?[] args)
	{
		var len = args[0].Conv<double>();
		var dir = args[1].Conv<double>();

		return -len * Math.Sin(dir * CustomMath.Deg2Rad);
	}

	public static object? angle_difference(object?[] args)
	{
		var dest = args[0].Conv<double>();
		var src = args[1].Conv<double>();
		return CustomMath.Mod(dest - src + 180, 360) - 180;
	}
}
