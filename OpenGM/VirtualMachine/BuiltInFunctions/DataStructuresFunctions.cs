using OpenGM.IO;
using System.Text;

namespace OpenGM.VirtualMachine.BuiltInFunctions
{
    public static class DataStructuresFunctions
    {
	    private static Dictionary<int, List<object?>> _dsListDict = new();
	    private static Dictionary<int, Dictionary<object, object?>> _dsMapDict = new();
	    private static Dictionary<int, PriorityQueue<object, double>> _priorityDict = new();

		// ...

		[GMLFunction("ds_list_create")]
		public static object ds_list_create(params object?[] args)
	    {
		    var highestIndex = -1;
		    if (_dsListDict.Count > 0)
		    {
			    highestIndex = _dsListDict.Keys.Max();
		    }

		    _dsListDict.Add(highestIndex + 1, new());
		    return highestIndex + 1;
	    }

		[GMLFunction("ds_list_destroy")]
		public static object? ds_list_destroy(object?[] args)
		{
			var index = args[0].Conv<int>();
			_dsListDict.Remove(index);
			return null;
		}

		[GMLFunction("ds_list_clear")]
		public static object? ds_list_clear(object?[] args)
		{
			var id = args[0].Conv<int>();
			if (!_dsListDict.ContainsKey(id)) return null;
			_dsListDict[id] = new List<object?>();
			return null;
		}

		[GMLFunction("ds_list_copy")]
		public static object? ds_list_copy(object?[] args)
		{
			var id = args[0].Conv<int>();
			var source = args[1].Conv<int>();
			if ((!_dsListDict.ContainsKey(id)) || (!_dsListDict.ContainsKey(source))) return null;
			_dsListDict[id] = [.. _dsListDict[source]];
			return null;
		}

		[GMLFunction("ds_list_size")]
		public static object? ds_list_size(object?[] args)
		{
			var id = args[0].Conv<int>();

			if (!_dsListDict.ContainsKey(id))
			{
				DebugLog.LogError($"Data structure with index {id} does not exist.");
				return 0;
			}

			return _dsListDict[id].Count;
		}

		[GMLFunction("ds_list_empty")]
		public static object? ds_list_empty(params object?[] args) => (_dsListDict[args[0].Conv<int>()].Count == 0);

		[GMLFunction("ds_list_add")]
		public static object? ds_list_add(params object?[] args)
		{
			var id = args[0].Conv<int>();
			var values = args[1..];

			if (!_dsListDict.ContainsKey(id))
			{
				return null;
			}

			var list = _dsListDict[id];
			list.AddRange(values!);
			return null;
		}

		[GMLFunction("ds_list_insert")]
		public static object? ds_list_insert(params object?[] args)
		{
			var id = args[0].Conv<int>();
			var pos = args[1].Conv<int>();
			var val = args[2];
			if (!_dsListDict.ContainsKey(id)) return null;

			var list = _dsListDict[id];
			list.Insert(pos, val);
			return null;
		}

		[GMLFunction("ds_list_replace")]
		public static object? ds_list_replace(params object?[] args)
		{
			var id = args[0].Conv<int>();
			var pos = args[1].Conv<int>();
			var val = args[2];
			if (!_dsListDict.ContainsKey(id)) return null;

			var list = _dsListDict[id];
			list[pos] = val;
			return null;
		}

		[GMLFunction("ds_list_delete")]
		public static object? ds_list_delete(params object?[] args)
		{
			var id = args[0].Conv<int>();
			var pos = args[1].Conv<int>();

			if (!_dsListDict.ContainsKey(id)) return null;
			_dsListDict[id].RemoveAt(pos);
			return null;
		}

		[GMLFunction("ds_list_find_index")]
		public static object? ds_list_find_index(params object?[] args)
		{
			var id = args[0].Conv<int>();
			var value = args[1];

			if (!_dsListDict.ContainsKey(id))
			{
				return -1;
			}

			if (value is null) 
			{
				return -1;
			}

			var list = _dsListDict[id];

			if (id >= list.Count)
			{
				return -1;
			}

			return list.IndexOf(value);
		}

		[GMLFunction("ds_list_find_value")]
		public static object? ds_list_find_value(params object?[] args)
		{
			var id = args[0].Conv<int>();
			var pos = args[1].Conv<int>();

			/*
			 * TODO - this shit:
			 * Note that if you give a position that is outside of the given list size (i.e.: position 11 in a 10 value list)
			 * then the function may return undefined or 0.
			 * This is because when you create the list, internally the first few entries in the list
			 * are set to 0 to minimize performance issues when initially adding items to the list
			 * (although the ds_list_size() function will still return 0 on a newly created list).
			 */

			if (!_dsListDict.ContainsKey(id))
			{
				return null;
			}

			var list = _dsListDict[id];

			if (id >= list.Count)
			{
				return null;
			}

			return list[id];
		}

		// ds_list_is_map
		// ds_list_is_list
		// ds_list_mark_as_list
		// ds_list_mark_as_map
		// ds_list_sort

		[GMLFunction("ds_list_shuffle")]
		public static object? ds_list_shuffle(params object?[] args)
		{
			// TODO : make this use GMRandom

			var id = args[0].Conv<int>();

			if (!_dsListDict.ContainsKey(id))
			{
				return null;
			}

			var list = _dsListDict[id];

			list.Shuffle();

			return null;
		}

		[GMLFunction("ds_list_write")]
		public static object? ds_list_write(params object?[] args)
		{

			//TODO : Make it suport legacy format;

			var id = args[0].Conv<int>();

			if (!_dsListDict.ContainsKey(id))
			{
				return null;
			}

			var list = _dsListDict[id];

			using var stream = new MemoryStream();
			using var writer = new BinaryWriter(stream);

			// Header
			writer.Write(0x0000012F);
			writer.Write(list.Count);

			foreach (var item in list)
			{
				switch (item)
				{
					case double d when double.IsNaN(d):
						writer.Write(5); // NaN
						break;
					case double d:
						writer.Write(0); // real
						writer.Write(d);
						break;
					case string s:
						writer.Write(1); // string
						byte[] strBytes = Encoding.UTF8.GetBytes(s);
						writer.Write(strBytes.Length);
						writer.Write(strBytes);
						break;
					case long l:
						writer.Write(2); // int64
						writer.Write(l);
						break;
					case int i:
						writer.Write(2); // int64 too
						writer.Write((long)i);
						break;
					case bool b:
						writer.Write(3); // bool
						writer.Write(b ? 1 : 0);
						break;
				}
			}
			return BitConverter.ToString(stream.ToArray()).Replace("-", "");
		}

		[GMLFunction("ds_list_read")]
		public static List<object?> ds_list_read(params object?[] args)
		{

			//TODO : Make it suport legacy format;

			var id = args[0].Conv<int>();
			var str = args[1].Conv<string>();
			//ignoring the [legacy] param

			byte[] data = Convert.FromHexString(str);
			using var stream = new MemoryStream(data);
			using var reader = new BinaryReader(stream);

			int count = reader.ReadInt32();
			var list = new List<object?>(count);

			for (int i = 0; i < count; i++)
			{
				int type = reader.ReadInt32();
				switch (type)
				{
					case 5:
						list.Add(double.NaN); // NaN
						break;
					case 0:
						double value = reader.ReadDouble(); // real
						list.Add(value);
						break;
					case 1:
						int len = reader.ReadInt32(); // string
						byte[] strBytes = reader.ReadBytes(len);
						string text = Encoding.UTF8.GetString(strBytes);
						list.Add(text);
						break;
					case 2:
						long longVal = reader.ReadInt64(); // int64
						list.Add(longVal);
						break;
					case 3:
						int boolVal = reader.ReadInt32(); // boolean
						list.Add(boolVal != 0);
						break;
				}
			}
			return list;
		}

		// ds_list_set
		// ds_list_set_post
		// ds_list_set_pre

		[GMLFunction("ds_map_create")]
		public static object ds_map_create(params object?[] args)
		{
			var highestIndex = -1;
			if (_dsMapDict.Count > 0)
			{
				highestIndex = _dsMapDict.Keys.Max();
			}

			_dsMapDict.Add(highestIndex + 1, new());
			return highestIndex + 1;
		}

		[GMLFunction("ds_map_destroy")]
		public static object? ds_map_destroy(object?[] args)
		{
			var index = args[0].Conv<int>();
			_dsMapDict.Remove(index);
			return null;
		}

		[GMLFunction("ds_map_clear")]
		public static object? ds_map_clear(object?[] args)
		{
			var index = args[0].Conv<int>();
			_dsMapDict[index] = [];
			return null;
		}

		[GMLFunction("ds_map_copy")]
		public static object? ds_map_copy(object?[] args)
		{
			var id = args[0].Conv<int>();
			var source = args[1].Conv<int>();
			if ((!_dsMapDict.ContainsKey(id)) || (!_dsMapDict.ContainsKey(source))) return null;
			_dsMapDict[id] = new Dictionary<object, object?>(_dsMapDict[source]);
			return null;
		}

		[GMLFunction("ds_map_size")]
		public static object ds_map_size(object?[] args)
		{
			var id = args[0].Conv<int>();
			return _dsMapDict[id].Count;
		}

		[GMLFunction("ds_map_empty")]
		public static object ds_map_empty(object?[] args) => (_dsMapDict[args[0].Conv<int>()].Count == 0);

		[GMLFunction("ds_map_add")]
		public static object ds_map_add(params object?[] args)
		{
			var id = args[0].Conv<int>();
			var key = args[1]!;
			var value = args[2]!;

			if (!_dsMapDict.ContainsKey(id))
			{
				return false;
			}

			var dict = _dsMapDict[id];
			if (dict.ContainsKey(key))
			{
				return false;
			}

			dict.Add(key, value);
			return true;
		}

		[GMLFunction("ds_map_set")]
		public static object? ds_map_set(object?[] args)
		{
			var id = args[0].Conv<int>();
			var key = args[1]!;
			var value = args[2]!;

			if (!_dsMapDict.ContainsKey(id))
			{
				return false;
			}

			var dict = _dsMapDict[id];
			dict[key] = value;

			return null;
		}

		// ds_map_set_pre
		// ds_map_set_post
		// ds_map_add_list
		// ds_map_add_map
		// ds_map_replace
		// ds_map_replace_list
		// ds_map_replace_map

		[GMLFunction("ds_map_delete")]
		public static object? ds_map_delete(params object?[] args)
		{
			var id = args[0].Conv<int>();
			var key = args[1].Conv<string>();

			if (!_dsMapDict.ContainsKey(id)) return null;
			_dsMapDict[id].Remove(key);
			return null;
		}

		[GMLFunction("ds_map_exists")]
		public static object? ds_map_exists(object?[] args)
		{
			var id = args[0].Conv<int>();
			var key = args[1].Conv<string>();

			if (!_dsMapDict.ContainsKey(id))
			{
				return false;
			}

			var dict = _dsMapDict[id];
			return dict.ContainsKey(key);
		}

		// ds_map_keys_to_array
		// ds_map_values_to_array

		[GMLFunction("ds_map_find_value")]
		public static object? ds_map_find_value(object?[] args)
		{
			var id = args[0].Conv<int>();
			var key = args[1]!;

			if (!_dsMapDict.ContainsKey(id))
			{
				return null;
			}

			var dict = _dsMapDict[id];
			if (!dict.ContainsKey(key))
			{
				return null;
			}

			return dict[key];
		}

		// ds_map_is_map
		// ds_map_is_list
		// ds_map_find_previous
		// ds_map_find_next
		// ds_map_find_first
		// ds_map_find_last
		// ds_map_write
		// ds_map_read
		// ds_map_secure_save
		// ds_map_secure_load
		// ds_map_secure_load_buffer
		// ds_map_secure_save_buffer

		[GMLFunction("ds_priority_create")]
		public static object? ds_priority_create(object?[] args)
		{
			var highestIndex = -1;
			if (_priorityDict.Count > 0)
			{
				highestIndex = _priorityDict.Keys.Max();
			}

			_priorityDict.Add(highestIndex + 1, new());
			return highestIndex + 1;
		}

		// ds_priority_destroy
		// ds_priority_clear
		// ds_priority_copy
		// ds_priority_size
		// ds_priority_empty
		// ds_priority_add
		// ds_priority_change_priority
		// ds_priority_find_priority
		// ds_priority_delete_value
		// ds_priority_delete_min
		// ds_priority_find_min
		// ds_priority_delete_max
		// ds_priority_write
		// ds_priority_read

		// ds_grid_create
		// ds_grid_destroy
		// ds_grid_copy
		// ds_grid_resize
		// ds_grid_width
		// ds_grid_height
		// ds_grid_clear
		// ds_grid_set
		// ds_grid_set_pre
		// ds_grid_set_post

		// ...
	}
}
