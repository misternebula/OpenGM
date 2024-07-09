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

	public static object array_length_1d(Arguments args)
	{
		var array = args.Args[0].Conv<ICollection>();
		return array.Count;
	}

	public static object array_length(Arguments args)
	{
		var array = args.Args[0].Conv<ICollection>();
		return array.Count;
	}

	public static object random(Arguments args)
	{
		var upper = args.Args[0].Conv<double>();
		return rnd.NextDouble() * upper;
	}

	public static object random_range(Arguments args)
	{
		var n1 = args.Args[0].Conv<double>();
		var n2 = args.Args[1].Conv<double>();
		return rnd.NextDouble() * (n2 - n1) + n1;
	}

	public static object irandom(Arguments args)
	{
		var n = realToInt(args.Args[0].Conv<double>());
		return rnd.Next(0, n + 1);
	}

	public static object irandom_range(Arguments args)
	{
		var n1 = realToInt(args.Args[0].Conv<double>());
		var n2 = realToInt(args.Args[1].Conv<double>());

		return rnd.Next(n1, n2 + 1);
	}

	public static object abs(Arguments args)
	{
		var val = args.Args[0].Conv<double>();
		return Math.Abs(val);
	}

	public static object round(Arguments args)
	{
		var num = args.Args[0].Conv<double>();
		return CustomMath.RoundToInt((float)num); // BUG: shouldnt this just do Math.Round?
	}

	public static object floor(Arguments args)
	{
		var n = args.Args[0].Conv<double>();
		return Math.Floor(n);
	}

	public static object ceil(Arguments args)
	{
		var n = args.Args[0].Conv<double>();
		return Math.Ceiling(n);
	}

	public static object sin(Arguments args)
	{
		var val = args.Args[0].Conv<double>();
		return Math.Sin(val);
	}

	public static object cos(Arguments args)
	{
		var val = args.Args[0].Conv<double>();
		return Math.Cos(val);
	}

	private static int realToInt(double value)
	{
		return value < 0 ? CustomMath.CeilToInt((float)value) : CustomMath.FloorToInt((float)value);
	}

	public static object min(Arguments args)
	{
		var arguments = new double[args.Args.Length];
		for (var i = 0; i < args.Args.Length; i++)
		{
			arguments[i] = args.Args[i].Conv<double>();
		}

		return arguments.Min();
	}

	public static object max(Arguments args)
	{
		var arguments = new double[args.Args.Length];
		for (var i = 0; i < args.Args.Length; i++)
		{
			arguments[i] = args.Args[i].Conv<double>();
		}

		return arguments.Max();
	}

	public static object choose(Arguments args)
	{
		return args.Args[rnd.Next(0, args.Args.Length)];
	}

	public static object @string(Arguments args)
	{
		var valueOrFormat = args.Args[0];

		var values = new object[] { };
		if (args.Args.Length > 1)
		{
			values = args.Args[1..];
		}

		if (args.Args.Length > 1)
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
						result.Append(@string(new Arguments { Args = new[] { values[bracesNumber] } }));
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

			if (valueOrFormat is List<RValue> list)
			{
				// array
				// is any of this right? not sure.
				var index = 0;
				var result = new StringBuilder("[");
				foreach (var item in list)
				{
					var elementString = (string)@string(new Arguments { Args = new[] { item.Value } });

					result.Append(elementString);
					if (index < list.Count - 1)
					{
						result.Append(", ");
					}
					index++;
				}

				result.Append("]");
				return result.ToString();
			}
			else if (valueOrFormat is Undefined)
			{
				return "undefined"; // no clue if this is correct. maybe it errors here instead?
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

	public static object string_length(Arguments args)
	{
		var str = args.Args[0].Conv<string>();

		if (string.IsNullOrEmpty(str))
		{
			return 0;
		}

		return str.Length;
	}

	public static object string_char_at(Arguments args)
	{
		var str = args.Args[0].Conv<string>();
		var index = args.Args[1].Conv<int>();

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

	public static object string_copy(Arguments args)
	{
		var str = args.Args[0].Conv<string>();
		var index = args.Args[1].Conv<int>();
		var count = args.Args[2].Conv<int>();

		return str.Substring(index - 1, count);
	}

	public static object string_insert(Arguments args)
	{
		var substr = args.Args[0].Conv<string>();
		var str = args.Args[1].Conv<string>();
		var index = args.Args[2].Conv<int>();

		return str.Insert(index - 1, substr);
	}

	public static object string_delete(Arguments args)
	{
		var str = args.Args[0].Conv<string>();
		var index = args.Args[1].Conv<int>();
		var count = args.Args[2].Conv<int>();

		return str.Remove(index - 1, count);
	}

	public static object string_replace_all(Arguments args)
	{
		var str = args.Args[0].Conv<string>();
		var substr = args.Args[1].Conv<string>();
		var newstr = args.Args[2].Conv<string>();

		return str.Replace(substr, newstr);
	}

	public static object string_hash_to_newline(Arguments args)
	{
		var text = args.Args[0].Conv<string>();

		if (string.IsNullOrEmpty(text))
		{
			return text;
		}

		return text.Replace("#", Environment.NewLine);
	}

	public static object string_pos(Arguments args)
	{
		var substr = args.Args[0].Conv<string>();
		var str = args.Args[1].Conv<string>();

		return str.IndexOf(substr) + 1;
	}

	public static object point_distance(Arguments args)
	{
		var x1 = args.Args[0].Conv<double>();
		var y1 = args.Args[1].Conv<double>();
		var x2 = args.Args[2].Conv<double>();
		var y2 = args.Args[3].Conv<double>();

		var horizDistance = Math.Abs(x2 - x1);
		var vertDistance = Math.Abs(y2 - y1);

		return Math.Sqrt((horizDistance * horizDistance) + (vertDistance * vertDistance));
	}

	public static object point_direction(Arguments args)
	{
		var x1 = args.Args[0].Conv<double>();
		var y1 = args.Args[1].Conv<double>();
		var x2 = args.Args[2].Conv<double>();
		var y2 = args.Args[3].Conv<double>();

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
