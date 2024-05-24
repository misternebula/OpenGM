using DELTARUNITYStandalone.SerializedFiles;
using Newtonsoft.Json.Linq;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using System;
using System.Diagnostics;
using System.Text;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace DELTARUNITYStandalone.VirtualMachine;
public static class ScriptResolver
{
	public static Dictionary<string, VMScript> Scripts = new();
	public static List<VMScript> GlobalInitScripts = new();

	public static Dictionary<string, Func<Arguments, object>> BuiltInFunctions = new()
	{
		{ "layer_force_draw_depth", layer_force_draw_depth },
		{ "array_length_1d", array_length_1d },
		{ "@@NewGMLArray@@", newgmlarray },
		{ "asset_get_index", asset_get_index },
		{ "event_inherited", event_inherited },

		#region ini_
		{ "ini_open", ini_open },
		{ "ini_read_string", ini_read_string },
		{ "ini_write_string", ini_write_string },
		{ "ini_read_real", ini_read_real },
		{ "ini_write_real", ini_write_real },
		{ "ini_close", ini_close },
		#endregion

		{ "variable_global_exists", variable_global_exists },
		{ "show_debug_message", show_debug_message },
		{ "json_decode", json_decode },
		{ "font_add_sprite_ext", font_add_sprite_ext },
		{ "merge_colour", merge_colour },
		{ "merge_color", merge_colour },
		{ "window_set_caption", window_set_caption },
		{ "window_get_caption", window_get_caption },
		{ "view_get_camera", view_get_camera },
		{ "event_user", event_user },
		{ "place_meeting", place_meeting },
		{ "collision_rectangle", collision_rectangle },

		#region file_
		{ "file_text_open_read", file_text_open_read },
		{ "file_text_open_write", file_text_open_write },
		{ "file_text_close", file_text_close },
		{ "file_text_eof", file_text_eof },
		{ "file_exists", file_exists },
		{ "file_text_readln", file_text_readln },
		{ "file_text_writeln", file_text_writeln },
		{ "file_text_read_string", file_text_read_string },
		{ "file_text_write_string", file_text_write_string },
		{ "file_text_read_real", file_text_read_real },
		{ "file_text_write_real", file_text_write_real },
		{ "file_delete", file_delete },
		{ "file_copy", file_copy },
		#endregion

		#region room_
		{ "room_goto", room_goto },
		{ "room_goto_next", room_goto_next },
		{ "room_goto_previous", room_goto_previous },
		{ "room_next", room_next },
		{ "room_previous", room_previous },
		#endregion

		#region ds_
		{ "ds_map_create", ds_map_create },
		{ "ds_map_destroy", ds_map_destroy },
		{ "ds_map_add", ds_map_add },
		{ "ds_map_size", ds_map_size },
		{ "ds_list_create", ds_list_create },
		{ "ds_list_destroy", ds_list_destroy },
		{ "ds_list_add", ds_list_add },
		{ "ds_map_find_value", ds_map_find_value },
		#endregion

		#region string_
		{ "string", _string },
		{ "string_length", string_length },
		{ "string_char_at", string_char_at },
		{ "string_width", string_width },
		{ "string_copy", string_copy },
		{ "string_insert", string_insert },
		{ "string_delete", string_delete },
		{ "string_replace_all", string_replace_all },
		{ "string_hash_to_newline", string_hash_to_newline },
		{ "string_pos", string_pos },
		#endregion

		#region instance_
		{ "instance_exists", instance_exists },
		{ "instance_create_depth", instance_create_depth },
		{ "instance_number", instance_number },
		{ "instance_destroy", instance_destroy },
		#endregion

		#region Math
		{ "floor", floor },
		{ "ceil", ceil },
		{ "abs", abs },
		{ "sin", sin },
		{ "cos", cos },
		{ "random", random },
		{ "random_range", random_range },
		{ "irandom", irandom },
		{ "irandom_range", irandom_range },
		{ "round", round },
		{ "min", min },
		{ "max", max },
		#endregion

		{ "keyboard_check", keyboard_check },
		{ "keyboard_check_pressed", keyboard_check_pressed },
		{ "display_get_height", display_get_height },
		{ "display_get_width", display_get_width },
		{ "window_set_size", window_set_size },
		{ "window_center", window_center },
		{ "gamepad_button_check", gamepad_button_check },
		{ "gamepad_axis_value", gamepad_axis_value },
		{ "gamepad_is_connected", gamepad_is_connected },

		#region draw_
		{ "draw_set_colour", draw_set_colour },
		{ "draw_set_color", draw_set_colour }, // mfw
		{ "draw_get_colour", draw_get_colour },
		{ "draw_get_color", draw_get_colour },
		{ "draw_set_alpha", draw_set_alpha },
		{ "draw_set_font", draw_set_font },
		{ "draw_set_halign", draw_set_halign },
		{ "draw_set_valign", draw_set_valign },
		{ "draw_rectangle", draw_rectangle },
		{ "draw_text", draw_text },
		{ "draw_text_transformed", draw_text_transformed },
		{ "draw_sprite", draw_sprite },
		{ "draw_sprite_ext", draw_sprite_ext },
		{ "draw_sprite_part_ext", draw_sprite_part_ext },
		{ "draw_sprite_part", draw_sprite_part },
		{ "draw_self", draw_self },
		{ "draw_sprite_stretched", draw_sprite_stretched },
		{ "draw_text_color", draw_text_colour },
		{ "draw_text_colour", draw_text_colour },
		{ "draw_sprite_tiled_ext", draw_sprite_tiled_ext },
		#endregion

		#region camera_
		{ "camera_get_view_x", camera_get_view_x },
		{ "camera_get_view_y", camera_get_view_y },
		{ "camera_get_view_width", camera_get_view_width },
		{ "camera_get_view_height", camera_get_view_height },
		{ "camera_set_view_target", camera_set_view_target },
		//{ "camera_get_view_target", camera_get_view_target },
		{ "camera_set_view_pos", camera_set_view_pos },
		#endregion

		#region audio_
		{ "audio_create_stream", audio_create_stream },
		{ "audio_destroy_stream", audio_destroy_stream },
		{ "audio_play_sound", audio_play_sound },
		{ "audio_sound_gain", audio_sound_gain },
		{ "audio_sound_pitch", audio_sound_pitch},
		{ "audio_stop_all", audio_stop_all },
		{ "audio_stop_sound", audio_stop_sound },
		{ "audio_group_load", audio_group_load },
		{ "audio_is_playing", audio_is_playing },
		{ "audio_group_set_gain", audio_group_set_gain },
		{ "audio_set_master_gain", audio_set_master_gain },
		{ "audio_pause_sound", audio_pause_sound },
		{ "audio_resume_sound", audio_resume_sound },
		{ "audio_sound_set_track_position", audio_sound_set_track_position },
		{ "audio_group_is_loaded", audio_group_is_loaded },
		#endregion
	};

	private static T Conv<T>(object obj) => VMExecutor.Conv<T>(obj);

	private static object layer_force_draw_depth(Arguments args)
	{
		var force = Conv<bool>(args.Args[0]);
		var depth = Conv<int>(args.Args[1]);
		//Debug.Log($"layer_force_draw_depth force:{force} depth:{depth}");

		// not implementing yet because uhhhhhhhhhhhhhhhhhhh

		return null;
	}

	public static object draw_set_colour(Arguments args)
	{
		var color = Conv<int>(args.Args[0]);
		SpriteManager.DrawColor = color;
		return null;
	}

	public static object draw_get_colour(Arguments args)
	{
		return SpriteManager.DrawColor;
	}

	public static object draw_set_alpha(Arguments args)
	{
		var alpha = Conv<double>(args.Args[0]);
		SpriteManager.DrawAlpha = alpha;
		return null;
	}

	public static object array_length_1d(Arguments args)
	{
		var array = (List<object>)args.Args[0];
		return array.Count;
	}

	public static object newgmlarray(Arguments args)
	{
		return new List<object>();
	}

	public static object asset_get_index(Arguments args)
	{
		var name = (string)args.Args[0];
		return AssetIndexManager.GetIndex(name);
	}

	public static object event_inherited(Arguments args)
	{
		if (args.Ctx.ObjectDefinition.parent == null)
		{
			return null;
		}

		GamemakerObject.ExecuteScript(args.Ctx.Self, args.Ctx.ObjectDefinition.parent, args.Ctx.EventType, args.Ctx.EventIndex);
		return null;
	}

	private static IniFile _iniFile;

	public static object ini_open(Arguments args)
	{
		var name = (string)args.Args[0];

		if (_iniFile != null)
		{
			throw new Exception("Cannot open a new .ini file while an old one is still open!");
		}

		var filepath = Path.Combine(Directory.GetCurrentDirectory(), name);

		if (!File.Exists(filepath))
		{
			_iniFile = new IniFile { Name = name };
			return null;
		}

		var lines = File.ReadAllLines(filepath);

		KeyValuePair<string, string> ParseKeyValue(string line)
		{
			var lineByEquals = line.Split('=');
			var key = lineByEquals[0].Trim();
			var value = lineByEquals[1].Trim();
			value = value.Trim('"');
			return new KeyValuePair<string, string>(key, value);
		}

		_iniFile = new IniFile { Name = name };
		IniSection currentSection = null;

		for (var i = 0; i < lines.Length; i++)
		{
			var currentLine = lines[i];
			if (currentLine.StartsWith('[') && currentLine.EndsWith("]"))
			{
				currentSection = new IniSection(currentLine.TrimStart('[').TrimEnd(']'));
				_iniFile.Sections.Add(currentSection);
				continue;
			}

			if (string.IsNullOrEmpty(currentLine))
			{
				continue;
			}

			var keyvalue = ParseKeyValue(currentLine);
			currentSection.Dict.Add(keyvalue.Key, keyvalue.Value);
		}

		return null;
	}

	public static object ini_read_string(Arguments args)
	{
		var section = (string)args.Args[0];
		var key = (string)args.Args[1];
		var value = (string)args.Args[2];

		var sectionClass = _iniFile.Sections.FirstOrDefault(x => x.Name == section);

		if (sectionClass == null)
		{
			return value;
		}

		if (!sectionClass.Dict.ContainsKey(key))
		{
			return value;
		}

		return sectionClass.Dict[key];
	}

	public static object ini_write_string(Arguments args)
	{
		var section = (string)args.Args[0];
		var key = (string)args.Args[1];
		var value = (string)args.Args[2];

		var sectionClass = _iniFile.Sections.FirstOrDefault(x => x.Name == section);

		if (sectionClass == null)
		{
			sectionClass = new IniSection(section);
			_iniFile.Sections.Add(sectionClass);
		}

		sectionClass.Dict[key] = value;

		return null;
	}

	public static object ini_read_real(Arguments args)
	{
		var section = (string)args.Args[0];
		var key = (string)args.Args[1];
		var value = Conv<double>(args.Args[2]);

		var sectionClass = _iniFile.Sections.FirstOrDefault(x => x.Name == section);

		if (sectionClass == null)
		{
			return value;
		}

		if (!sectionClass.Dict.ContainsKey(key))
		{
			return value;
		}

		if (!double.TryParse(sectionClass.Dict[key], out var _res))
		{
			// TODO : check what it does here. maybe it only parses up to an invalid character?
			return value;
		}

		return _res;
	}

	public static object ini_write_real(Arguments args)
	{
		var section = (string)args.Args[0];
		var key = (string)args.Args[1];
		var value = Conv<double>(args.Args[2]);

		var sectionClass = _iniFile.Sections.FirstOrDefault(x => x.Name == section);

		if (sectionClass == null)
		{
			sectionClass = new IniSection(section);
			_iniFile.Sections.Add(sectionClass);
		}

		sectionClass.Dict[key] = value.ToString();

		return null;
	}

	public static object ini_close(Arguments args)
	{
		var filepath = Path.Combine(Directory.GetCurrentDirectory(), _iniFile.Name);
		File.Delete(filepath);
		var fileStream = new FileStream(filepath, FileMode.Append, FileAccess.Write);
		var streamWriter = new StreamWriter(fileStream);

		foreach (var section in _iniFile.Sections)
		{
			streamWriter.WriteLine($"[{section.Name}]");
			foreach (var kv in section.Dict)
			{
				streamWriter.WriteLine($"{kv.Key}=\"{kv.Value}\"");
			}
		}

		var text = streamWriter.ToString();

		streamWriter.Close();
		streamWriter.Dispose();
		_iniFile = null;

		return text;
	}

	public static object audio_group_is_loaded(Arguments args)
	{
		// TODO : actually implement this properly? DELTARUNITY doesnt use audio groups or any GM storage files (yet?)
		return true;
	}

	private static readonly Dictionary<int, FileHandle> _fileHandles = new(32);

	public static object file_text_open_read(Arguments args)
	{
		var fname = (string)args.Args[0];
		var filepath = Path.Combine(Directory.GetCurrentDirectory(), fname);

		if (!File.Exists(filepath))
		{
			return -1;
		}

		var fileStream = new FileStream(filepath, FileMode.Open, FileAccess.Read);

		if (_fileHandles.Count == 32)
		{
			fileStream.Close();
			return -1;
		}

		var highestIndex = -1;
		if (_fileHandles.Count > 0)
		{
			highestIndex = _fileHandles.Keys.Max();
		}

		var handle = new FileHandle
		{
			Reader = new StreamReader(fileStream)
		};

		_fileHandles.Add(highestIndex + 1, handle);
		return highestIndex + 1;
	}

	public static object file_text_open_write(Arguments args)
	{
		if (_fileHandles.Count == 32)
		{
			return -1;
		}

		var fname = (string)args.Args[0];
		var filepath = Path.Combine(Directory.GetCurrentDirectory(), fname);

		File.Delete(filepath);
		var fileStream = new FileStream(filepath, FileMode.Create, FileAccess.Write);

		var highestIndex = -1;
		if (_fileHandles.Count > 0)
		{
			highestIndex = _fileHandles.Keys.Max();
		}

		var handle = new FileHandle
		{
			Writer = new StreamWriter(fileStream)
		};

		_fileHandles.Add(highestIndex + 1, handle);
		return highestIndex + 1;
	}

	public static object file_text_close(Arguments args)
	{
		var index = (int)args.Args[0];

		if (_fileHandles.ContainsKey(index))
		{
			if (_fileHandles[index].Reader != null)
			{
				_fileHandles[index].Reader.Close();
				_fileHandles[index].Reader.Dispose();
			}

			if (_fileHandles[index].Writer != null)
			{
				_fileHandles[index].Writer.Close();
				_fileHandles[index].Writer.Dispose();
			}

			_fileHandles.Remove(index);
		}

		return null;
	}

	public static object file_text_eof(Arguments args)
	{
		var fileid = (int)args.Args[0];
		var reader = _fileHandles[fileid].Reader;
		return reader.EndOfStream;
	}

	public static object file_exists(Arguments args)
	{
		var fname = (string)args.Args[0];
		var filepath = Path.Combine(Directory.GetCurrentDirectory(), fname);
		return File.Exists(filepath);
	}

	public static object file_text_readln(Arguments args)
	{
		var fileid = (int)args.Args[0];
		var reader = _fileHandles[fileid].Reader;
		return reader.ReadLine();
	}

	public static object file_text_writeln(Arguments args)
	{
		var fileid = (int)args.Args[0];
		var writer = _fileHandles[fileid].Writer;
		writer.WriteLine();
		return null;
	}

	public static object file_text_read_string(Arguments args)
	{
		var fileid = (int)args.Args[0];
		var reader = _fileHandles[fileid].Reader;

		var result = "";
		while (reader.Peek() != 0x0D && reader.Peek() >= 0)
		{
			result += (char)reader.Read();
		}

		return result;
	}

	public static object file_text_write_string(Arguments args)
	{
		var fileid = (int)args.Args[0];
		var str = Conv<string>(args.Args[1]);
		var writer = _fileHandles[fileid].Writer;
		writer.Write(str);
		return null;
	}

	public static object file_text_read_real(Arguments args)
	{
		var fileid = (int)args.Args[0];
		var reader = _fileHandles[fileid].Reader;

		var result = "";
		while (reader.Peek() != 0x0D && reader.Peek() >= 0)
		{
			result += (char)reader.Read();
		}

		return double.Parse(result);
	}

	public static object file_text_write_real(Arguments args)
	{
		var fileid = (int)args.Args[0];
		var val = args.Args[1];
		var writer = _fileHandles[fileid].Writer;

		if (val is not int and not double and not float)
		{
			// i have no fucking idea
			writer.Write(0);
			return null;
		}

		writer.Write(Conv<double>(val));
		return null;
	}

	public static object file_delete(Arguments args)
	{
		var fname = (string)args.Args[0];
		var filepath = Path.Combine(Directory.GetCurrentDirectory(), fname);
		File.Delete(filepath);
		return true; // TODO : this should return false if this fails.
	}

	public static object file_copy(Arguments args)
	{
		var fname = (string)args.Args[0];
		var newname = (string)args.Args[1];

		fname = Path.Combine(Directory.GetCurrentDirectory(), fname);
		newname = Path.Combine(Directory.GetCurrentDirectory(), newname);

		if (File.Exists(newname))
		{
			throw new Exception("File already exists.");
		}

		File.Copy(fname, newname);

		return null;
	}

	public static object room_goto(Arguments args)
	{
		var index = Conv<int>(args.Args[0]);
		RoomManager.ChangeRoomAfterEvent(index);
		return null;
	}

	public static object room_goto_next(Arguments args)
	{
		RoomManager.room_goto_next();
		return null;
	}

	public static object room_goto_previous(Arguments args)
	{
		RoomManager.room_goto_previous();
		return null;
	}

	public static object room_next(Arguments args)
	{
		var numb = Conv<int>(args.Args[0]);
		return RoomManager.room_next(numb);
	}

	public static object room_previous(Arguments args)
	{
		var numb = Conv<int>(args.Args[0]);
		return RoomManager.room_previous(numb);
	}

	public static object variable_global_exists(Arguments args)
	{
		var name = Conv<string>(args.Args[0]);
		return VariableResolver.GlobalVariableExists(name);
	}

	private static Dictionary<int, Dictionary<object, object>> _dsMapDict = new();

	public static object ds_map_create(Arguments args)
	{
		var highestIndex = -1;
		if (_dsMapDict.Count > 0)
		{
			highestIndex = _dsMapDict.Keys.Max();
		}

		_dsMapDict.Add(highestIndex + 1, new());
		return highestIndex + 1;
	}

	public static object ds_map_destroy(Arguments args)
	{
		var index = Conv<int>(args.Args[0]);
		_dsMapDict.Remove(index);
		return null;
	}

	public static object ds_map_add(Arguments args)
	{
		var id = Conv<int>(args.Args[0]);
		var key = args.Args[1];
		var value = args.Args[2];

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

	public static object ds_map_size(Arguments args)
	{
		var id = (int)args.Args[0];
		return _dsMapDict[id].Count;
	}

	private static Dictionary<int, List<object>> _dsListDict = new();

	public static object ds_list_create(Arguments args)
	{
		var highestIndex = -1;
		if (_dsListDict.Count > 0)
		{
			highestIndex = _dsListDict.Keys.Max();
		}

		_dsListDict.Add(highestIndex + 1, new());
		return highestIndex + 1;
	}

	public static object ds_list_destroy(Arguments args)
	{
		var index = Conv<int>(args.Args[0]);
		_dsListDict.Remove(index);
		return null;
	}

	public static object ds_list_add(Arguments args)
	{
		var id = Conv<int>(args.Args[0]);
		var values = args.Args[1..];

		if (!_dsListDict.ContainsKey(id))
		{
			return null;
		}

		var list = _dsListDict[id];
		list.AddRange(values);
		return null;
	}

	public static object ds_map_find_value(Arguments args)
	{
		var id = Conv<int>(args.Args[0]);
		var key = args.Args[1];

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

	public static object show_debug_message(Arguments args)
	{
		DebugLog.Log(args.Args[0].ToString());
		return null;
	}

	public static object json_decode(Arguments args)
	{
		// is recursive weeeeeeeeeeee
		static object Parse(JToken jToken)
		{
			switch (jToken)
			{
				case JValue jValue:
					return jValue.Value;
				case JArray jArray:
				{
					var dsList = (int)ds_list_create(null);
					foreach (var item in jArray)
					{
						// TODO: make and call the proper function for maps and lists
						ds_list_add(new Arguments { Args = new object[] { dsList, Parse(item) } });
					}
					return dsList;
				}
				case JObject jObject:
				{
					var dsMap = (int)ds_map_create(null);
					foreach (var (name, value) in jObject)
					{
						// TODO: make and call the proper function for maps and lists
						ds_map_add(new Arguments { Args = new object[] { dsMap, name, Parse(value) } });
					}
					return dsMap;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		var @string = (string)args.Args[0];
		var jToken = JToken.Parse(@string);

		switch (jToken)
		{
			case JValue jValue:
			{
				var dsMap = (int)ds_map_create(null);
				ds_map_add(new Arguments { Args = new object[] { dsMap, "default", Parse(jValue) } });
				return dsMap;
			}
			case JArray jArray:
			{
				var dsMap = (int)ds_map_create(null);
				ds_map_add(new Arguments { Args = new object[] { dsMap, "default", Parse(jArray) } });
				return dsMap;
			}
			case JObject jObject:
			{
				return Parse(jObject);
			}
			default:
				throw new ArgumentOutOfRangeException();
		}
	}

	public static object _string(Arguments args)
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
			var format = (string)valueOrFormat;

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
						result.Append(values[bracesNumber]);
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

			if (valueOrFormat is List<object> list)
			{
				// array
				// is any of this right? not sure.
				var index = 0;
				var result = new StringBuilder("[");
				foreach (var item in list)
				{
					var elementString = (string)_string(new Arguments { Args = new object[] { item } });

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
			else if (valueOrFormat is bool)
			{
				return Conv<string>(valueOrFormat);
			}
			else if (valueOrFormat is string)
			{
				return valueOrFormat;
			}
			else
			{
				// real
				var num = Conv<double>(valueOrFormat);
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
		var str = Conv<string>(args.Args[0]);

		if (string.IsNullOrEmpty(str))
		{
			return 0;
		}

		return str.Length;
	}

	public static object string_char_at(Arguments args)
	{
		var str = Conv<string>(args.Args[0]);
		var index = Conv<int>(args.Args[1]);

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

	/*public static object string_width(Arguments args)
	{
		var str = Conv<string>(args.Args[0]);

		return TextManager.TextManager.StringWidth(str);
	}*/

	public static object string_copy(Arguments args)
	{
		var str = Conv<string>(args.Args[0]);
		var index = Conv<int>(args.Args[1]);
		var count = Conv<int>(args.Args[2]);

		return str.Substring(index - 1, count);
	}

	public static object string_insert(Arguments args)
	{
		var substr = Conv<string>(args.Args[0]);
		var str = Conv<string>(args.Args[1]);
		var index = Conv<int>(args.Args[2]);

		return str.Insert(index - 1, substr);
	}

	public static object string_delete(Arguments args)
	{
		var str = Conv<string>(args.Args[0]);
		var index = Conv<int>(args.Args[1]);
		var count = Conv<int>(args.Args[2]);

		return str.Remove(index - 1, count);
	}

	public static object string_replace_all(Arguments args)
	{
		var str = Conv<string>(args.Args[0]);
		var substr = Conv<string>(args.Args[1]);
		var newstr = Conv<string>(args.Args[2]);

		return str.Replace(substr, newstr);
	}

	public static object string_hash_to_newline(Arguments args)
	{
		var text = Conv<string>(args.Args[0]);

		if (string.IsNullOrEmpty(text))
		{
			return text;
		}

		return text.Replace("#", Environment.NewLine);
	}

	public static object string_pos(Arguments args)
	{
		var substr = Conv<string>(args.Args[0]);
		var str = Conv<string>(args.Args[1]);

		return str.IndexOf(substr) + 1;
	}

	public static object font_add_sprite_ext(Arguments args)
	{
		var spriteAssetIndex = Conv<int>(args.Args[0]);
		var string_map = Conv<string>(args.Args[1]);
		var prop = Conv<bool>(args.Args[2]);
		var sep = Conv<int>(args.Args[3]);

		var spriteAsset = SpriteManager.GetSpriteAsset(spriteAssetIndex);

		var index = AssetIndexManager.Register(AssetType.fonts, $"fnt_{spriteAsset.Name}");

		var newFont = new FontAsset
		{
			AssetIndex = index,
			name = $"fnt_{spriteAsset.Name}",
			spriteIndex = spriteAssetIndex,
			sep = sep
		};

		for (var i = 0; i < string_map.Length; i++)
		{
			var fontAssetEntry = new Glyph
			{
				characterIndex = string_map[i]
			};

			newFont.entries.Add(fontAssetEntry);
		}

		TextManager.FontAssets.Add(newFont);

		return newFont.AssetIndex;
	}

	public static object draw_rectangle(Arguments args)
	{
		var x1 = Conv<double>(args.Args[0]);
		var y1 = Conv<double>(args.Args[1]);
		var x2 = Conv<double>(args.Args[2]);
		var y2 = Conv<double>(args.Args[3]);
		var outline = Conv<bool>(args.Args[4]);

		if (outline)
		{
			draw(x1, y1, x2, y1 + 1);
			draw(x2 - 1, y1 + 1, x2, y2);
			draw(x1, y2 - 1, x2, y2);
			draw(x1, y1, x1 + 1, y2);
			return null;
		}

		draw(x1, y1, x2, y2);
		return null;

		static void draw(double x1, double y1, double x2, double y2)
		{
			var width = (x2 - x1);
			var height = (y2 - y1);

			if (height < 0)
			{
				height = -height;
				y1 -= height;
			}

			CustomWindow.RenderJobs.Add(new GMRectangleJob()
			{
				width = (float)width,
				height = (float)height,
				screenPos = new Vector2((float)x1, (float)y1),
				blend = SpriteManager.DrawColor.BGRToColor(),
				alpha = SpriteManager.DrawAlpha
			});
		}
	}

	public static object instance_exists(Arguments args)
	{
		var obj = Conv<int>(args.Args[0]);

		if (obj > GMConstants.FIRST_INSTANCE_ID)
		{
			// instance id was passed
			return InstanceManager.instance_exists_instanceid(obj);
		}
		else
		{
			// asset index was passed
			return InstanceManager.instance_exists_index(obj);
		}
	}

	public static object instance_create_depth(Arguments args)
	{
		var x = Conv<double>(args.Args[0]);
		var y = Conv<double>(args.Args[1]);
		var depth = Conv<int>(args.Args[2]);
		var obj = Conv<int>(args.Args[3]);

		return InstanceManager.instance_create_depth(x, y, depth, obj);
	}

	public static object instance_number(Arguments args)
	{
		var obj = Conv<int>(args.Args[0]);
		return InstanceManager.instance_number(obj);
	}

	public static object draw_set_font(Arguments args)
	{
		var font = Conv<int>(args.Args[0]);

		var library = TextManager.FontAssets;
		var fontAsset = library.FirstOrDefault(x => x.AssetIndex == font);
		TextManager.fontAsset = fontAsset;
		return null;
	}

	public static object draw_text(Arguments args)
	{
		var x = Conv<double>(args.Args[0]);
		var y = Conv<double>(args.Args[1]);
		var str = Conv<string>(args.Args[2]);
		TextManager.DrawText(x, y, str);
		return null;
	}

	public static object merge_colour(Arguments args)
	{
		var col1 = Conv<int>(args.Args[0]);
		var col2 = Conv<int>(args.Args[1]);
		var amount = Conv<double>(args.Args[2]);

		/*
		 * GameMaker stores colors in 3 bytes - BGR
		 * RED		: 255		: 00 00 FF
		 * ORANGE	: 4235519	: 40 A0 FF
		 * Alpha (or "blend") is not stored in colors.
		 */

		var oneBytes = BitConverter.GetBytes(col1);
		var twoBytes = BitConverter.GetBytes(col2);
		amount = Math.Clamp(amount, 0, 1);
		var mr = oneBytes[0] + (twoBytes[0] - oneBytes[0]) * amount;
		var mg = oneBytes[1] + (twoBytes[1] - oneBytes[1]) * amount;
		var mb = oneBytes[2] + (twoBytes[2] - oneBytes[2]) * amount;

		return BitConverter.ToInt32(new[] { (byte)mr, (byte)mg, (byte)mb, (byte)255 }, 0);
	}

	public static object draw_set_halign(Arguments args)
	{
		var halign = Conv<int>(args.Args[0]);
		TextManager.halign = (HAlign)halign;
		return null;
	}

	public static object draw_set_valign(Arguments args)
	{
		var valign = Conv<int>(args.Args[0]);
		TextManager.valign = (VAlign)valign;
		return null;
	}

	public static object floor(Arguments args)
	{
		var n = Conv<double>(args.Args[0]);
		return Math.Floor(n);
	}

	public static object ceil(Arguments args)
	{
		var n = Conv<double>(args.Args[0]);
		return Math.Ceiling(n);
	}

	public static object abs(Arguments args)
	{
		var val = Conv<double>(args.Args[0]);
		return Math.Abs(val);
	}

	public static object sin(Arguments args)
	{
		var val = Conv<double>(args.Args[0]);
		return Math.Sin(val);
	}

	public static object cos(Arguments args)
	{
		var val = Conv<double>(args.Args[0]);
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
			arguments[i] = Conv<double>(args.Args[i]);
		}

		return arguments.Min();
	}

	public static object max(Arguments args)
	{
		var arguments = new double[args.Args.Length];
		for (var i = 0; i < args.Args.Length; i++)
		{
			arguments[i] = Conv<double>(args.Args[i]);
		}

		return arguments.Max();
	}

	public static object draw_sprite(Arguments args)
	{
		var sprite = Conv<int>(args.Args[0]);
		var subimg = Conv<int>(args.Args[1]);
		var x = Conv<double>(args.Args[2]);
		var y = Conv<double>(args.Args[3]);

		SpriteManager.DrawSprite(sprite, subimg, x, y);
		return null;
	}

	public static object window_set_caption(Arguments args)
	{
		var caption = Conv<string>(args.Args[0]);

		CustomWindow.Instance.Title = caption;
		return null;
	}

	public static object window_get_caption(Arguments args)
	{
		return CustomWindow.Instance.Title;
	}

	public static object audio_group_load(Arguments args)
	{
		// TODO : actually implement this properly? DELTARUNITY doesnt use audio groups or any GM storage files (yet?)
		return true;
	}

	public static object keyboard_check(Arguments args)
	{
		var key = Conv<int>(args.Args[0]);

		// from disassembly
		switch (key)
		{
			case 0:
			{
				var result = true;
				for (var i = 0; i <= 255; ++i)
				{
					result = KeyboardHandler.KeyDown[i] != true && result;
				}
				return result;
			}
			case 1:
			{
				var result = false;
				for (var i = 0; i <= 255; ++i)
				{
					result = KeyboardHandler.KeyDown[i] || result;
				}
				return result;
			}
			case > 255:
				return false;
			default:
				return KeyboardHandler.KeyDown[key];
		}
	}

	public static object keyboard_check_pressed(Arguments args)
	{
		var key = Conv<int>(args.Args[0]);

		// from disassembly
		switch (key)
		{
			case 0:
			{
				var result = true;
				for (var i = 0; i <= 255; ++i)
				{
					result = KeyboardHandler.KeyPressed[i] != true && result;
				}
				return result;
			}
			case 1:
			{
				var result = false;
				for (var i = 0; i <= 255; ++i)
				{
					result = KeyboardHandler.KeyPressed[i] || result;
				}
				return result;
			}
			case > 255:
				return false;
			default:
				return KeyboardHandler.KeyPressed[key];
		}
	}

	public static object display_get_width(Arguments args)
	{
		return Monitors.GetPrimaryMonitor().HorizontalResolution;
	}

	public static object display_get_height(Arguments args)
	{
		return Monitors.GetPrimaryMonitor().VerticalResolution;
	}

	public static object window_set_size(Arguments args)
	{
		var w = Conv<int>(args.Args[0]);
		var h = Conv<int>(args.Args[1]);
		// TODO : implement
		return null;
	}

	public static object window_center(Arguments args)
	{
		// TODO : implement
		return null;
	}

	public static object gamepad_button_check(Arguments args)
	{
		// TODO : implement?
		return false;
	}

	public static object gamepad_axis_value(Arguments args)
	{
		// TODO : implement?
		return 0;
	}

	public static object gamepad_is_connected(Arguments args)
	{
		var device = Conv<int>(args.Args[0]);
		return false; // TODO : implement
	}

	public static object draw_sprite_ext(Arguments args)
	{
		var sprite = Conv<int>(args.Args[0]);
		var subimg = Conv<int>(args.Args[1]);
		var x = Conv<double>(args.Args[2]);
		var y = Conv<double>(args.Args[3]);
		var xscale = Conv<double>(args.Args[4]);
		var yscale = Conv<double>(args.Args[5]);
		var rot = Conv<double>(args.Args[6]);
		var colour = Conv<int>(args.Args[7]);
		var alpha = Conv<double>(args.Args[8]);

		SpriteManager.DrawSpriteExt(sprite, subimg, x, y, xscale, yscale, rot, colour, alpha);
		return null;
	}

	public static object draw_text_transformed(Arguments args)
	{
		var x = Conv<double>(args.Args[0]);
		var y = Conv<double>(args.Args[1]);
		var str = Conv<string>(args.Args[2]);
		var xscale = Conv<double>(args.Args[3]);
		var yscale = Conv<double>(args.Args[4]);
		var angle = Conv<double>(args.Args[5]);
		TextManager.DrawTextTransformed(x, y, str, xscale, yscale, angle);
		return null;
	}

	public static object draw_sprite_part_ext(Arguments args)
	{
		var sprite = Conv<int>(args.Args[0]);
		var subimg = Conv<int>(args.Args[1]);
		var left = Conv<int>(args.Args[2]);
		var top = Conv<int>(args.Args[3]);
		var width = Conv<int>(args.Args[4]);
		var height = Conv<int>(args.Args[5]);
		var x = Conv<double>(args.Args[6]);
		var y = Conv<double>(args.Args[7]);
		var xscale = Conv<double>(args.Args[8]);
		var yscale = Conv<double>(args.Args[9]);
		var colour = Conv<int>(args.Args[10]);
		var alpha = Conv<double>(args.Args[11]);

		SpriteManager.DrawSpritePartExt(sprite, subimg, left, top, width, height, x, y, xscale, yscale, colour, alpha);

		return null;
	}

	public static object draw_sprite_part(Arguments args)
	{
		var sprite = Conv<int>(args.Args[0]);
		var subimg = Conv<int>(args.Args[1]);
		var left = Conv<int>(args.Args[2]);
		var top = Conv<int>(args.Args[3]);
		var width = Conv<int>(args.Args[4]);
		var height = Conv<int>(args.Args[5]);
		var x = Conv<double>(args.Args[6]);
		var y = Conv<double>(args.Args[7]);

		SpriteManager.DrawSpritePart(sprite, subimg, left, top, width, height, x, y);

		return null;
	}

	public static object draw_self(Arguments args)
	{
		SpriteManager.DrawSelf(args.Ctx.Self);
		return null;
	}

	public static object draw_sprite_stretched(Arguments args)
	{
		var sprite = Conv<int>(args.Args[0]);
		var subimg = Conv<int>(args.Args[1]);
		var x = Conv<double>(args.Args[2]);
		var y = Conv<double>(args.Args[3]);
		var w = Conv<double>(args.Args[4]);
		var h = Conv<double>(args.Args[5]);

		SpriteManager.draw_sprite_stretched(sprite, subimg, x, y, w, h);
		return null;
	}

	public static object draw_text_colour(Arguments args)
	{
		var x = Conv<double>(args.Args[0]);
		var y = Conv<double>(args.Args[1]);
		var str = Conv<string>(args.Args[2]);
		var c1 = Conv<int>(args.Args[3]);
		var c2 = Conv<int>(args.Args[4]);
		var c3 = Conv<int>(args.Args[5]);
		var c4 = Conv<int>(args.Args[6]);
		var alpha = Conv<double>(args.Args[7]);

		TextManager.DrawTextColor(x, y, str, c1, c2, c3, c4, alpha);
		
		return null;
	}

	public static object draw_sprite_tiled_ext(Arguments args)
	{
		var sprite = Conv<int>(args.Args[0]);
		var subimg = Conv<int>(args.Args[1]);
		var x = Conv<double>(args.Args[2]);
		var y = Conv<double>(args.Args[3]);
		var xscale = Conv<double>(args.Args[4]);
		var yscale = Conv<double>(args.Args[5]);
		var colour = Conv<int>(args.Args[6]);
		var alpha = Conv<double>(args.Args[7]);

		var spriteTex = SpriteManager.GetSpritePage(sprite, subimg);

		var sizeWidth = spriteTex.TargetSizeX;
		var sizeHeight = spriteTex.TargetSizeY;

		var tempX = x;
		var tempY = y;

		var viewTopLeftX = CustomWindow.Instance.X;
		var viewTopLeftY = CustomWindow.Instance.Y;

		while (tempX > viewTopLeftX)
		{
			tempX -= sizeWidth;
		}

		while (tempY > viewTopLeftY)
		{
			tempY -= sizeHeight;
		}

		// tempX and tempY are now the topleft-most co-ords that are offscreen

		var xOffscreenValue = viewTopLeftX - tempX;
		var yOffscreenValue = viewTopLeftY - tempY;

		var countToDrawHoriz = CustomMath.CeilToInt((RoomManager.CurrentRoom.CameraWidth + (float)xOffscreenValue) / sizeWidth);
		var countToDrawVert = CustomMath.CeilToInt((RoomManager.CurrentRoom.CameraHeight + (float)yOffscreenValue) / sizeHeight);

		for (var i = 0; i < countToDrawVert; i++)
		{
			for (var j = 0; j < countToDrawHoriz; j++)
			{
				SpriteManager.DrawSpriteExt(sprite, subimg, tempX + (j * sizeWidth), tempY + (i * sizeHeight), xscale, yscale, 0, colour, alpha);
			}
		}

		return null;
	}

	public static object audio_group_set_gain(Arguments args)
	{
		// TODO : actually implement this properly? DELTARUNITY doesnt use audio groups or any GM storage files (yet?)
		return null;
	}

	public static object audio_play_sound(Arguments args)
	{
		// TODO : implement
		return -1;
	}

	public static object audio_set_master_gain(Arguments args)
	{
		// TODO : implement
		return null;
	}

	public static object instance_destroy(Arguments args)
	{
		if (args.Args.Length == 0)
		{
			GamemakerObject.ExecuteScript(args.Ctx.Self, args.Ctx.ObjectDefinition, EventType.Destroy);
			GamemakerObject.ExecuteScript(args.Ctx.Self, args.Ctx.ObjectDefinition, EventType.CleanUp);
			InstanceManager.instance_destroy(args.Ctx.Self);
			return null;
		}

		var id = Conv<int>(args.Args[0]);
		var execute_event_flag = true;

		if (args.Args.Length == 2)
		{
			execute_event_flag = Conv<bool>(args.Args[1]);
		}

		if (id < GMConstants.FIRST_INSTANCE_ID)
		{
			// asset index
			var instances = InstanceManager.FindByAssetId(id);

			foreach (var instance in instances)
			{
				if (execute_event_flag)
				{
					GamemakerObject.ExecuteScript(instance, instance.Definition, EventType.Destroy);
				}

				GamemakerObject.ExecuteScript(instance, instance.Definition, EventType.CleanUp);

				InstanceManager.instance_destroy(instance);
			}
		}
		else
		{
			// instance id
			var instance = InstanceManager.FindByInstanceId(id);

			if (execute_event_flag)
			{
				GamemakerObject.ExecuteScript(instance, instance.Definition, EventType.Destroy);
			}

			GamemakerObject.ExecuteScript(instance, instance.Definition, EventType.CleanUp);

			InstanceManager.instance_destroy(instance);
		}

		return null;
	}

	public static object view_get_camera(Arguments args)
	{
		// TODO : ughhh implement multiple cameras
		return 0;
	}

	public static object camera_get_view_x(Arguments args)
	{
		var camera_id = Conv<int>(args.Args[0]);

		if (camera_id > 0)
		{
			// TODO : ughhh implement multiple cameras
			throw new NotImplementedException();
		}

		return CustomWindow.Instance.X;
	}

	public static object camera_get_view_y(Arguments args)
	{
		var camera_id = Conv<int>(args.Args[0]);

		if (camera_id > 0)
		{
			// TODO : ughhh implement multiple cameras
			throw new NotImplementedException();
		}

		return CustomWindow.Instance.Y;
	}

	public static object camera_get_view_width(Arguments args)
	{
		var camera_id = Conv<int>(args.Args[0]);

		if (camera_id > 0)
		{
			// TODO : ughhh implement multiple cameras
			throw new NotImplementedException();
		}

		return RoomManager.CurrentRoom.CameraWidth;
	}

	public static object camera_get_view_height(Arguments args)
	{
		var camera_id = Conv<int>(args.Args[0]);

		if (camera_id > 0)
		{
			// TODO : ughhh implement multiple cameras
			throw new NotImplementedException();
		}

		return RoomManager.CurrentRoom.CameraHeight;
	}

	public static object camera_set_view_target(Arguments args)
	{
		var camera_id = Conv<int>(args.Args[0]);

		if (camera_id > 0)
		{
			// TODO : ughhh implement multiple cameras
			throw new NotImplementedException();
		}

		var id = Conv<int>(args.Args[1]);

		GamemakerObject instance = null;

		if (id < GMConstants.FIRST_INSTANCE_ID)
		{
			instance = InstanceManager.FindByAssetId(id).FirstOrDefault();
		}
		else
		{
			instance = InstanceManager.FindByInstanceId(id);
		}

		DebugLog.Log($"camera_set_view_target {instance}");

		//GamemakerCamera.Instance.ObjectToFollow = instance;

		return null;
	}

	/*public static object camera_get_view_target(Arguments args)
	{
		var camera_id = Conv<int>(args.Args[0]);

		if (camera_id > 0)
		{
			// TODO : ughhh implement multiple cameras
			throw new NotImplementedException();
		}

		// TODO : this can apparently return either an instance id or object index????
		return GamemakerCamera.Instance.ObjectToFollow == null ? -1 : GamemakerCamera.Instance.ObjectToFollow.instanceId;
	}*/

	public static object camera_set_view_pos(Arguments args)
	{
		var camera_id = Conv<int>(args.Args[0]);

		if (camera_id > 0)
		{
			// TODO : ughhh implement multiple cameras
			throw new NotImplementedException();
		}

		var x = Conv<double>(args.Args[1]);
		var y = Conv<double>(args.Args[2]);

		CustomWindow.Instance.SetPosition(x, y);

		return null;
	}

	public static object audio_create_stream(Arguments args)
	{
		var filename = Conv<string>(args.Args[0]);

		// TODO: implement

		return -1;
	}

	// docs say this is passed the file path, but in DR its passed the asset index of the stream... no idea
	public static object audio_destroy_stream(Arguments args)
	{
		var index = Conv<int>(args.Args[0]);
		//TODO : implement
		return null;
	}

	public static object audio_sound_gain(Arguments args)
	{
		var index = Conv<int>(args.Args[0]);
		var volume = Conv<double>(args.Args[1]);
		var time = Conv<double>(args.Args[2]);

		// todo : implement

		return null;
	}

	public static object audio_sound_pitch(Arguments args)
	{
		var index = Conv<int>(args.Args[0]);
		var pitch = Conv<double>(args.Args[1]);

		// todo : implement

		return null;
	}

	public static object audio_stop_all(Arguments args)
	{
		// todo : implement
		return null;
	}

	public static object audio_stop_sound(Arguments args)
	{
		var id = Conv<int>(args.Args[0]);
		// todo : implement
		return null;
	}

	public static object audio_pause_sound(Arguments args)
	{
		var index = Conv<int>(args.Args[0]);
		// todo : implement
		return null;
	}

	public static object audio_resume_sound(Arguments args)
	{
		var index = Conv<int>(args.Args[0]);
		// todo : implement
		return null;
	}

	public static object audio_sound_set_track_position(Arguments args)
	{
		var index = Conv<int>(args.Args[0]);
		var time = Conv<double>(args.Args[1]);
		// todo : implement
		return null;
	}

	public static object audio_is_playing(Arguments args)
	{
		var index = Conv<int>(args.Args[0]);
		// todo: implement
		return false;
	}

	static Random rnd = new Random();

	public static object random(Arguments args)
	{
		var upper = Conv<double>(args.Args[0]);
		return rnd.NextDouble() * upper;
	}

	public static object random_range(Arguments args)
	{
		var n1 = Conv<double>(args.Args[0]);
		var n2 = Conv<double>(args.Args[1]);
		return rnd.NextDouble() * (n2 - n1) + n1;
	}

	public static object irandom(Arguments args)
	{
		var n = realToInt(Conv<double>(args.Args[0]));
		return rnd.Next(0, n + 1);
	}

	public static object irandom_range(Arguments args)
	{
		var n1 = realToInt(Conv<double>(args.Args[0]));
		var n2 = realToInt(Conv<double>(args.Args[1]));

		return rnd.Next(n1, n2 + 1);
	}

	public static object round(Arguments args)
	{
		var num = Conv<double>(args.Args[0]);
		return CustomMath.RoundToInt((float)num);
	}

	public static object string_width(Arguments args)
	{
		var str = Conv<string>(args.Args[0]);

		return TextManager.StringWidth(str);
	}

	public static object event_user(Arguments args)
	{
		var numb = Conv<int>(args.Args[0]);
		GamemakerObject.ExecuteScript(args.Ctx.Self, args.Ctx.ObjectDefinition, EventType.Other, (uint)(EventSubtypeOther.User0 + (uint)numb));
		return null;
	}

	public static object place_meeting(Arguments args)
	{
		var x = Conv<double>(args.Args[0]);
		var y = Conv<double>(args.Args[1]);
		var obj = Conv<int>(args.Args[2]); // TODO : this can be an array, or "all" or "other", or tile map stuff

		if (obj < 0)
		{
			throw new NotImplementedException($"{obj} given to place_meeting");
		}

		if (obj < GMConstants.FIRST_INSTANCE_ID)
		{
			return CollisionManager.place_meeting_assetid(x, y, obj, args.Ctx.Self);
		}
		else
		{
			return CollisionManager.place_meeting_instanceid(x, y, obj, args.Ctx.Self);
		}
	}

	public static object collision_rectangle(Arguments args)
	{
		var x1 = Conv<double>(args.Args[0]);
		var y1 = Conv<double>(args.Args[1]);
		var x2 = Conv<double>(args.Args[2]);
		var y2 = Conv<double>(args.Args[3]);
		var obj = Conv<int>(args.Args[4]); // TODO : this can be an array, or "all" or "other", or tile map stuff
		var prec = Conv<bool>(args.Args[5]);
		var notme = Conv<bool>(args.Args[6]);

		if (obj < 0)
		{
			throw new NotImplementedException($"{obj} given to collision_rectangle!");
		}

		if (obj < GMConstants.FIRST_INSTANCE_ID)
		{
			return CollisionManager.collision_rectangle_assetid(x1, y1, x2, y2, obj, prec, notme, args.Ctx.Self);
		}
		else
		{
			return CollisionManager.collision_rectangle_instanceid(x1, y1, x2, y2, obj, prec, notme, args.Ctx.Self);
		}
	}
}

public class FileHandle
{
	public StreamReader Reader;
	public StreamWriter Writer;
}
