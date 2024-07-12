using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DELTARUNITYStandalone.VirtualMachine;

public static partial class ScriptResolver
{
	static Random rnd = new Random();

	public static object array_length_1d(object?[] args)
	{
		var array = args[0].Conv<IList>();
		return array.Count;
	}

	public static object array_length(object?[] args)
	{
		var array = args[0].Conv<IList>();
		return array.Count;
	}

	public static object random(object?[] args)
	{
		var upper = args[0].Conv<double>();
		return rnd.NextDouble() * upper;
	}

	public static object random_range(object?[] args)
	{
		var n1 = args[0].Conv<double>();
		var n2 = args[1].Conv<double>();
		return rnd.NextDouble() * (n2 - n1) + n1;
	}

	public static object irandom(object?[] args)
	{
		var n = realToInt(args[0].Conv<double>());
		return rnd.Next(0, n + 1);
	}

	public static object irandom_range(object?[] args)
	{
		var n1 = realToInt(args[0].Conv<double>());
		var n2 = realToInt(args[1].Conv<double>());

		return rnd.Next(n1, n2 + 1);
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
		return args[rnd.Next(0, args.Length)];
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

	public static object string_length(object?[] args)
	{
		var str = args[0].Conv<string>();

		if (string.IsNullOrEmpty(str))
		{
			return 0;
		}

		return str.Length;
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

	public static object string_copy(object?[] args)
	{
		var str = args[0].Conv<string>();
		var index = args[1].Conv<int>();
		var count = args[2].Conv<int>();

		return str.Substring(index - 1, count);
	}

	public static object string_insert(object?[] args)
	{
		var substr = args[0].Conv<string>();
		var str = args[1].Conv<string>();
		var index = args[2].Conv<int>();

		return str.Insert(index - 1, substr);
	}

	public static object string_delete(object?[] args)
	{
		var str = args[0].Conv<string>();
		var index = args[1].Conv<int>();
		var count = args[2].Conv<int>();

		return str.Remove(index - 1, count);
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

		return text.Replace("#", Environment.NewLine);
	}

	public static object string_pos(object?[] args)
	{
		var substr = args[0].Conv<string>();
		var str = args[1].Conv<string>();

		return str.IndexOf(substr) + 1;
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
}
