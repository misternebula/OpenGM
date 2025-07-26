using OpenGM.IO;
using System.Collections;
using System.Text;

namespace OpenGM.VirtualMachine.BuiltInFunctions;

public static class MathFunctions
{
    [GMLFunction("is_bool")]
    public static object is_bool(object?[] args) => args[0] is bool;

    [GMLFunction("is_real")]
    public static object is_real(object?[] args) => args[0] is int or long or short or double or float;

    // is_numeric

    [GMLFunction("is_string")]
    public static object is_string(object?[] args) => args[0] is string;

    // is_array

    [GMLFunction("is_undefined")]
    public static object is_undefined(object?[] args) => args[0] is null;

    // is_int32
    // is_int64
    // is_ptr
    // is_vec3
    // is_vec4
    // is_matrix

    [GMLFunction("is_struct")]
    public static object is_struct(object?[] args) => args[0] is GMLObject;

    // yyAsm

    [GMLFunction("method")]
    public static object? method(object?[] args)
    {
        // seems to always be self, static, or null.
        // https://github.com/YoYoGames/GameMaker-HTML5/blob/develop/scripts/yyVariable.js#L279
        var struct_ref_or_instance_id = args[0];
        
        var method = args[1] switch {
            // pass by id
            int value => new Method(ScriptResolver.ScriptsByIndex[value.Conv<int>()]),
            // pass method directly
            Method value => value,
            // idk
            _ => throw new NotImplementedException($"Don't know what to do with type {args[1]?.GetType().ToString() ?? "null"}")
        };

        if (struct_ref_or_instance_id is null)
        {
            method.inst = null;
        }
        else if (struct_ref_or_instance_id is bool or int or short or long or double or float)
        {
            var num = struct_ref_or_instance_id.Conv<int>();
            if (num == GMConstants.self)
            {
                method.inst = VMExecutor.Self.Self;
            }
            else if (num == GMConstants.@static)
            {
                /*
                 * TODO : there are static functions in DR, but they're never called (vector2/3 add and scale)
                 * just dummy implementing this so it'll run the initialize part of the constructor
                 */
                method.inst = null;
                DebugLog.LogWarning("Method() called with -16 (static) struct ref - not implemented.");
            }
            else
            {
                method.inst = InstanceManager.Find(num);
            }
        }
        else if (struct_ref_or_instance_id is GMLObject gmlo)
        {
            method.inst = gmlo;
        }
        else
        {
            throw new NotImplementedException();
        }

        return method;
    }

    // method_get_index
    // method_get_self
    // is_method
    // is_nan
    // is_infinity
    // typeof
    // instanceof

    [GMLFunction("array_length")]
    [GMLFunction("array_length_1d")]
    public static object? array_length(object?[] args)
    {
        if (args[0] is null)
        {
            return null;
        }

        var array = args[0].Conv<IList>();
        return array.Count;
    }

    [GMLFunction("array_length_2d")]
    public static object? array_length_2d(object?[] args)
    {
        if (args[0] is null)
        {
            return null;
        }

        var array = args[0].Conv<IList>();
        var index0 = array[0];
        return (index0 as IList)!.Count;
    }

    [GMLFunction("array_height_2d")]
    public static object? array_height_2d(object?[] args)
    {
        if (args[0] is null)
        {
            return null;
        }

        var array = args[0].Conv<IList>();
        return array.Count;
    }

    [GMLFunction("array_get")]
    public static object? array_get(object?[] args)
    {
        var array = args[0].Conv<IList>();
        var index = args[1].Conv<int>();
        return array[index];
    }

    // array_set
    // array_set_pre
    // array_set_post
    // array_get_2D
    // array_set_2D
    // array_set_2D_pre
    // array_set_2D_post
    // array_equals

    [GMLFunction("array_create")]
    public static object? array_create(object?[] args)
    {
        var size = args[0].Conv<int>();

        object? value = 0;
        if (args.Length > 1)
        {
            value = args[1];
        }

        var newArray = new object?[size];
        Array.Fill(newArray, value);

        return newArray;
    }

    // array_copy

    [GMLFunction("array_resize")]
    public static object? array_resize(object?[] args)
    {
        var array_index = args[0].Conv<IList>();
        var new_size = args[1].Conv<int>();
        var oldSize = array_index.Count;

        if (new_size > oldSize)
        {
            var amountToAdd = new_size - oldSize;
            for (var i = 0; i < amountToAdd; i++)
            {
                array_index.Add(0);
            }
        }
        else
        {
            for (var i = new_size; i < oldSize; i++)
            {
                array_index.RemoveAt(i);
            }
        }

        return null;
    }

    [GMLFunction("array_push")]
    public static object? array_push(object?[] args)
    {
        var variable = args[0].Conv<IList>();
        for (var i = 1; i < args.Length; i++)
        {
            variable.Add(args[i]);
        }

        return null;
    }

    // array_pop
    // array_insert

    [GMLFunction("array_delete")]
    public static object? array_delete(object?[] args)
    {
        var variable = args[0].Conv<IList>();
        var index = args[1].Conv<int>();
        var number = args[2].Conv<int>();

        if (number < 0)
        {
            index += number + 1;
            number = -number;
        }

        for (var i = index; i < index + number; i++)
        {
            variable.RemoveAt(i);
        }

        return null;
    }

    [GMLFunction("array_sort", GMLFunctionFlags.Stub, stubLogType: DebugLog.LogType.Warning)]
    public static object? array_sort(object?[] args)
    {
        var variable = args[0].Conv<IList>();
        var sorttype_or_function = args[1];

        // Strings sorted alphabetically with default asc/desc functionality
        // uses qsort

        if (sorttype_or_function is bool)
        {
            var ascending = sorttype_or_function.Conv<bool>();
        }
        else
        {
            // function ugh

            /*
             * arguments: CURRENT ELEMENT and NEXT ELEMENT
             * returns:
             *    0        : elements equal
             *    <= -1    : current element goes before next element
             *  >= 1    : current element goes after next element
             */
        }
        
        return null;
    }

    // @@array_set_owner@@

    [GMLFunction("random")]
    public static object random(object?[] args)
    {
        var upper = args[0].Conv<double>();
        return GMRandom.fYYRandom() * upper;
    }

    [GMLFunction("random_range")]
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

    [GMLFunction("irandom")]
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

    [GMLFunction("irandom_range")]
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

    [GMLFunction("random_set_seed")]
    public static object? random_set_seed(object?[] args)
    {
        var val = args[0].Conv<uint>();
        GMRandom.Seed = val;
        return null;
    }

    [GMLFunction("random_get_seed")]
    public static object? random_get_seed(object?[] args)
    {
        return GMRandom.Seed;
    }

    [GMLFunction("randomise", GMLFunctionFlags.Stub)]
    [GMLFunction("randomize", GMLFunctionFlags.Stub)]
    public static object? randomize(object?[] args)
    {
        // todo : implement
        //throw new NotImplementedException();
        return 0;
    }

    [GMLFunction("abs")]
    public static object abs(object?[] args)
    {
        var val = args[0].Conv<double>();
        return Math.Abs(val);
    }

    [GMLFunction("round")]
    public static object round(object?[] args)
    {
        var num = args[0].Conv<double>();
        return CustomMath.RoundToInt((float)num); // BUG: shouldnt this just do Math.Round?
    }

    [GMLFunction("floor")]
    public static object floor(object?[] args)
    {
        var n = args[0].Conv<double>();
        return Math.Floor(n);
    }

    [GMLFunction("ceil")]
    public static object ceil(object?[] args)
    {
        var n = args[0].Conv<double>();
        return Math.Ceiling(n);
    }

    [GMLFunction("sign")]
    public static object sign(object?[] args)
    {
        // TODO : handle NaN somehow????
        var n = args[0].Conv<double>();
        return Math.Sign(n);
    }

    [GMLFunction("frac")]
    public static object? frac(object?[] args)
    {
        var n = args[0].Conv<double>();
        var integral = Math.Truncate(n); // with sign
        return n - integral;
    }

    [GMLFunction("sqrt")]
    public static object sqrt(object?[] args)
    {
        var val = args[0].Conv<double>();

        // TODO : Docs say that values [-epsilon,0) are set to 0, probably added in newer GM versions

        return Math.Sqrt(val);
    }

    [GMLFunction("sqr")]
    public static object sqr(object?[] args)
    {
        var val = args[0].Conv<double>();
        return val * val;
    }

    // exp
    // ln

    [GMLFunction("log2")]
    public static object log2(object?[] args)
    {
        var n = args[0].Conv<double>();
        return Math.Log2(n);
    }

    [GMLFunction("log10")]
    public static object log10(object?[] args)
    {
        var n = args[0].Conv<double>();
        return Math.Log10(n);
    }

    [GMLFunction("sin")]
    public static object sin(object?[] args)
    {
        var val = args[0].Conv<double>();
        return Math.Sin(val);
    }

    [GMLFunction("cos")]
    public static object cos(object?[] args)
    {
        var val = args[0].Conv<double>();
        return Math.Cos(val);
    }

    // tan

    [GMLFunction("arcsin")]
    public static object arcsin(object?[] args)
    {
        var x = args[0].Conv<double>(); // in radians

        if (x < -1 || x > 1)
        {
            throw new NotSupportedException($"x is {x}");
        }

        return Math.Asin(x);
    }

    [GMLFunction("arccos")]
    public static object arccos(object?[] args)
    {
        var x = args[0].Conv<double>(); // in radians

        if (x < -1 || x > 1)
        {
            throw new NotSupportedException($"x is {x}");
        }

        return Math.Acos(x);
    }

    // arctan
    // arctan2

    [GMLFunction("dsin")]
    public static object dsin(object?[] args)
    {
        var a = args[0].Conv<double>(); // degrees
        return Math.Sin(a * CustomMath.Deg2Rad);
    }

    [GMLFunction("dcos")]
    public static object dcos(object?[] args)
    {
        var val = args[0].Conv<double>(); // degrees
        return Math.Cos(val * CustomMath.Deg2Rad);
    }

    // dtan
    // darcsin
    // darccos
    // darctan
    // darctan2

    [GMLFunction("degtorad")]
    public static object degtorad(object?[] args)
    {
        var deg = args[0].Conv<double>();
        return deg * double.Pi / 180;
    }

    [GMLFunction("radtodeg")]
    public static object radtodeg(object?[] args)
    {
        var rad = args[0].Conv<double>();
        return rad * 180 / double.Pi;
    }

    [GMLFunction("power")]
    public static object power(object?[] args)
    {
        var x = args[0].Conv<double>();
        var n = args[1].Conv<double>();

        return Math.Pow(x, n);
    }

    [GMLFunction("logn")]
    public static object logn(object?[] args)
    {
        var n = args[0].Conv<double>();
        var val = args[1].Conv<double>();

        return Math.Log(val, n);
    }

    [GMLFunction("min")]
    public static object min(object?[] args)
    {
        var arguments = new double[args.Length];
        for (var i = 0; i < args.Length; i++)
        {
            arguments[i] = args[i].Conv<double>();
        }

        return arguments.Min();
    }

    [GMLFunction("max")]
    public static object max(object?[] args)
    {
        var arguments = new double[args.Length];
        for (var i = 0; i < args.Length; i++)
        {
            arguments[i] = args[i].Conv<double>();
        }

        return arguments.Max();
    }

    // mean

    [GMLFunction("median")]
    public static object median(object?[] args)
    {
        if (args.Length == 0)
        {
            return 0;
        }

        var realValues = new double[args.Length];
        for (var i = 0; i < args.Length; i++)
        {
            realValues[i] = args[i].Conv<double>();
        }

        Array.Sort(realValues);

        return realValues[CustomMath.FloorToInt(args.Length / 2f)];
    }

    [GMLFunction("choose")]
    public static object? choose(object?[] args)
    {
        return args[GMRandom.YYRandom(args.Length)];
    }

    [GMLFunction("clamp")]
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

    [GMLFunction("lerp")]
    public static object? lerp(object?[] args)
    {
        var a = args[0].Conv<double>();
        var b = args[1].Conv<double>();
        var amt = args[2].Conv<double>();

        return a + ((b - a) * amt);
    }

    [GMLFunction("real")]
    public static object real(object?[] args)
    {
        var str = args[0].Conv<string>();
        return str.Conv<double>();
    }

    // bool

    [GMLFunction("string")]
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
            else if (valueOrFormat is int or short or long or double or float)
            {
                // real
                var num = valueOrFormat.Conv<double>();
                var afterTwoDigits = num % 0.01f;
                var truncated = num - afterTwoDigits;

                return (truncated % 1) == 0
                    ? truncated.ToString()
                    : Math.Round(truncated, 2).ToString();
            }
            else
            {
                return (string)valueOrFormat;
            }
        }
    }

    // int64
    // ptr

    [GMLFunction("string_format")]
    public static object? string_format(object?[] args)
    {
        var val = args[0].Conv<double>();
        var total = args[1].Conv<int>();
        var dec = args[2].Conv<int>();

        // TODO : is a negative sign included in the number of digits??

        var integral = Math.Truncate(val);
        var integralString = integral.ToString().PadLeft(total);

        // if val is an integer
        if (val.ToString().Length == integral.ToString().Length)
        {
            return integralString;
        }

        var fractional = val - integral;
        var fractionalString = fractional.ToString().PadRight(dec, '0');

        return integralString + '.' + fractionalString;
    }

    [GMLFunction("chr")]
    public static object chr(object?[] args)
    {
        var val = args[0].Conv<int>();

        // TODO : "This character depends on the current drawing fonts character set code page and if no font is set, it will use the default code page for the machine."
        // what the fuck does this mean

        /*var currentFont = TextManager.fontAsset;

        if (currentFont.entriesDict.ContainsKey(val))
        {
            return Convert.ToChar(val).ToString();
        }
        else
        {
            throw new NotImplementedException();
        }*/

        return Convert.ToChar(val).ToString();
    }

    // ansi_char

    [GMLFunction("ord")]
    public static object ord(object?[] args)
    {
        var str = args[0].Conv<string>();

        return (int)Encoding.UTF8.GetBytes(str)[0];
    }

    [GMLFunction("string_length")]
    public static object string_length(object?[] args)
    {
        var str = args[0].Conv<string>();

        if (string.IsNullOrEmpty(str))
        {
            return 0;
        }

        return str.Length;
    }

    [GMLFunction("string_pos")]
    public static object string_pos(object?[] args)
    {
        var substr = args[0].Conv<string>();
        var str = args[1].Conv<string>();

        return str.IndexOf(substr) + 1;
    }

    // string_pos_ext
    // string_last_pos
    // string_last_pos_ext

    [GMLFunction("string_copy")]
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

    [GMLFunction("string_char_at")]
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

    // string_ord_at
    // string_byte_length
    // string_byte_at
    // string_set_byte_at

    [GMLFunction("string_delete")]
    public static object string_delete(object?[] args)
    {
        var str = args[0].Conv<string>();
        var index = args[1].Conv<int>();
        var count = args[2].Conv<int>();

        return str.Remove(index - 1, count);
    }

    [GMLFunction("string_insert")]
    public static object string_insert(object?[] args)
    {
        var substr = args[0].Conv<string>();
        var str = args[1].Conv<string>();
        var index = args[2].Conv<int>();

        return str.Insert(index - 1, substr);
    }

    [GMLFunction("string_lower")]
    public static object string_lower(object?[] args)
    {
        var str = args[0].Conv<string>();
        // TODO : only do the 26 english alphabet letters
        return str.ToLower();
    }

    [GMLFunction("string_upper")]
    public static object string_upper(object?[] args)
    {
        var str = args[0].Conv<string>();
        // TODO : only do the 26 english alphabet letters
        return str.ToUpper();
    }

    [GMLFunction("string_repeat")]
    public static object? string_repeat(object?[] args)
    {
        var str = args[0].Conv<string>();
        var count = args[1].Conv<int>();

        var ret = "";
        for (var i = 0; i < count; i++)
        {
            ret += str;
        }

        return ret;
    }

    [GMLFunction("string_letters")]
    public static object string_letters(object?[] args)
    {
        var str = args[0].Conv<string>();

        var result = "";

        foreach (var c in str)
        {
            if (char.IsAsciiLetter(c))
            {
                result += c;
            }
        }

        return result;
    }

    [GMLFunction("string_digits")]
    public static object string_digits(object?[] args)
    {
        var str = args[0].Conv<string>();

        var result = "";

        foreach (var c in str)
        {
            if (char.IsAsciiDigit(c))
            {
                result += c;
            }
        }

        return result;
    }

    [GMLFunction("string_lettersdigits")]
    public static object string_lettersdigits(object?[] args)
    {
        var str = args[0].Conv<string>();

        var result = "";

        foreach (var c in str)
        {
            if (char.IsAsciiLetterOrDigit(c))
            {
                result += c;
            }
        }

        return result;
    }

    [GMLFunction("string_replace")]
    public static object string_replace(object?[] args)
    {
        var str = args[0].Conv<string>();
        var substr = args[1].Conv<string>();
        var newstr = args[2].Conv<string>();

        var pos = str.IndexOf(substr);
        if (pos == -1)
        {
            return str;
        }

        return string.Concat(str.AsSpan(0, pos), newstr, str.AsSpan(pos + substr.Length));
    }

    [GMLFunction("string_replace_all")]
    public static object string_replace_all(object?[] args)
    {
        var str = args[0].Conv<string>();
        var substr = args[1].Conv<string>();
        var newstr = args[2].Conv<string>();

        return str.Replace(substr, newstr);
    }

    [GMLFunction("string_count")]
    public static object string_count(object?[] args)
    {
        var substr = args[0].Conv<string>();
        var str = args[1].Conv<string>();

        var count = 0;
        var index = 0;

        while ((index = str.IndexOf(substr, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += substr.Length;
        }

        return count;
    }

    [GMLFunction("string_hash_to_newline")]
    public static object string_hash_to_newline(object?[] args)
    {
        var text = args[0].Conv<string>();

        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        return text.Replace('#', '\n');
    }

    [GMLFunction("point_distance")]
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

    [GMLFunction("point_direction")]
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

    [GMLFunction("lengthdir_x")]
    public static object? lengthdir_x(object?[] args)
    {
        var len = args[0].Conv<double>();
        var dir = args[1].Conv<double>();

        return len * Math.Cos(dir * CustomMath.Deg2Rad);
    }

    [GMLFunction("lengthdir_y")]
    public static object? lengthdir_y(object?[] args)
    {
        var len = args[0].Conv<double>();
        var dir = args[1].Conv<double>();

        return -len * Math.Sin(dir * CustomMath.Deg2Rad);
    }

    // point_distance_3d
    // dot_product
    // dot_product_normalised
    // dot_product_3d
    // dot_product_3d_normalised
    // math_set_epsilon
    // math_get_epsilon

    [GMLFunction("angle_difference")]
    public static object? angle_difference(object?[] args)
    {
        var dest = args[0].Conv<double>();
        var src = args[1].Conv<double>();
        return CustomMath.Mod(dest - src + 180, 360) - 180;
    }

    // weak_ref_create
    // weak_ref_alive
    // weak_ref_any_alive
}
