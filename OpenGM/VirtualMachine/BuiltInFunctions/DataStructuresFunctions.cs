using OpenGM.IO;

namespace OpenGM.VirtualMachine.BuiltInFunctions
{
    public static class DataStructuresFunctions
    {
	    private static Dictionary<int, List<object>> _dsListDict = new();
	    private static Dictionary<int, Dictionary<object, object>> _dsMapDict = new();

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

		// ds_list_clear
		// ds_list_copy

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

		// ds_list_empty

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

		// ds_list_insert
		// ds_list_replace
		// ds_list_delete
		// ds_list_find_index

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

		// ds_list_write
		// ds_list_read
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

		// ds_map_clear
		// ds_map_copy

		[GMLFunction("ds_map_size")]
		public static object ds_map_size(object?[] args)
		{
			var id = args[0].Conv<int>();
			return _dsMapDict[id].Count;
		}

		// ds_map_empty

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
		// ds_map_delete

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

		// ...
	}
}
