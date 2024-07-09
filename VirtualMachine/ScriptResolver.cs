using DELTARUNITYStandalone.SerializedFiles;
using Newtonsoft.Json.Linq;
using NVorbis;
using OpenTK.Audio.OpenAL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System;
using System.Diagnostics;
using System.Text;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using static UndertaleModLib.Models.UndertaleRoom;

using OpenTK.Graphics.OpenGL;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DELTARUNITYStandalone.VirtualMachine;
public static partial class ScriptResolver
{
	public static Dictionary<string, VMScript> Scripts = new();
	public static List<VMScript> GlobalInitScripts = new();

	public static Dictionary<string, (VMScript script, int index)> ScriptFunctions = new Dictionary<string, (VMScript script, int index)>();

	public static Dictionary<string, Func<Arguments, object>> BuiltInFunctions = new()
	{
		#region Game
		{ "place_meeting", place_meeting },
		{ "move_towards_point", move_towards_point },
		{ "distance_to_point", distance_to_point },
		{ "collision_rectangle", collision_rectangle },
		{ "instance_exists", instance_exists },
		{ "instance_number", instance_number },
		{ "instance_create_depth", instance_create_depth },
		{ "instance_destroy", instance_destroy },
		{ "room_goto", room_goto },
		{ "room_goto_previous", room_goto_previous },
		{ "room_goto_next", room_goto_next },
		{ "room_previous", room_previous },
		{ "room_next", room_next },
		#endregion

		#region Math
		{ "array_length_1d", array_length_1d },
		{ "random", random },
		{ "random_range", random_range },
		{ "irandom", irandom },
		{ "irandom_range", irandom_range },
		{ "abs", abs },
		{ "round", round },
		{ "floor", floor },
		{ "ceil", ceil },
		{ "sin", sin },
		{ "cos", cos },
		{ "min", min },
		{ "max", max },
		{ "choose", choose },
		{ "string", @string },
		{ "string_length", string_length },
		{ "string_pos", string_pos },
		{ "string_copy", string_copy },
		{ "string_char_at", string_char_at },
		{ "string_delete", string_delete },
		{ "string_insert", string_insert },
		{ "string_replace_all", string_replace_all },
		{ "string_hash_to_newline", string_hash_to_newline },
		{ "point_distance", point_distance },
		{ "point_direction", point_direction },
		#endregion

		#region Graphic
		{ "display_get_width", display_get_width },
		{ "display_get_height", display_get_height },
		{ "window_set_fullscreen", window_set_fullscreen },
		{ "window_get_fullscreen", window_get_fullscreen },
		{ "window_set_caption", window_set_caption },
		{ "window_get_caption", window_get_caption },
		{ "window_set_size", window_set_size },
		{ "window_center", window_center },
		{ "draw_set_colour", draw_set_colour },
		{ "draw_set_color", draw_set_colour }, // mfw
		{ "draw_set_alpha", draw_set_alpha },
		{ "draw_get_colour", draw_get_colour },
		{ "draw_get_color", draw_get_colour },
		{ "merge_colour", merge_colour },
		{ "merge_color", merge_colour },
		{ "draw_line_width", draw_line_width },
		{ "draw_rectangle", draw_rectangle },
		{ "draw_set_font", draw_set_font },
		{ "draw_set_halign", draw_set_halign },
		{ "draw_set_valign", draw_set_valign },
		{ "string_width", string_width },
		{ "draw_text", draw_text },
		{ "draw_text_transformed", draw_text_transformed },
		{ "draw_text_color", draw_text_colour },
		{ "draw_text_colour", draw_text_colour },
		{ "draw_self", draw_self },
		{ "draw_sprite", draw_sprite },
		{ "draw_sprite_ext", draw_sprite_ext },
		{ "draw_sprite_stretched", draw_sprite_stretched },
		{ "draw_sprite_part", draw_sprite_part },
		{ "draw_sprite_part_ext", draw_sprite_part_ext },
		{ "draw_sprite_tiled_ext", draw_sprite_tiled_ext },
		#endregion

		#region File
		{ "file_text_open_read", file_text_open_read },
		{ "file_text_open_write", file_text_open_write },
		{ "file_text_close", file_text_close },
		{ "file_text_read_string", file_text_read_string },
		{ "file_text_read_real", file_text_read_real },
		{ "file_text_readln", file_text_readln },
		{ "file_text_eof", file_text_eof },
		{ "file_text_write_string", file_text_write_string },
		{ "file_text_write_real", file_text_write_real },
		{ "file_text_writeln", file_text_writeln },
		{ "file_exists", file_exists },
		{ "file_delete", file_delete },
		{ "file_copy", file_copy },
		{ "ini_open", ini_open },
		{ "ini_close", ini_close },
		{ "ini_read_string", ini_read_string },
		{ "ini_read_real", ini_read_real },
		{ "ini_write_string", ini_write_string },
		{ "ini_write_real", ini_write_real },
		{ "json_decode", json_decode },
		#endregion

		#region Resource
		{ "sprite_get_number", sprite_get_number },
		{ "font_add_sprite_ext", font_add_sprite_ext },
		{ "script_execute", script_execute },
		{ "asset_get_index", asset_get_index },
		#endregion

		#region Interaction
		{ "keyboard_check", keyboard_check },
		{ "keyboard_check_pressed", keyboard_check_pressed },
		#endregion

		#region 3D
		{ "gpu_set_fog", gpu_set_fog },
		#endregion

		#region Misc
		{ "event_inherited", event_inherited },
		{ "event_user", event_user },
		{ "show_debug_message", show_debug_message },
		{ "variable_global_exists", variable_global_exists },
		#endregion

		#region DS
		{ "ds_list_create", ds_list_create },
		{ "ds_list_destroy", ds_list_destroy },
		{ "ds_list_add", ds_list_add },
		{ "ds_map_create", ds_map_create },
		{ "ds_map_destroy", ds_map_destroy },
		{ "ds_map_size", ds_map_size },
		{ "ds_map_add", ds_map_add },
		{ "ds_map_find_value", ds_map_find_value },
		#endregion

		#region Sound
		{ "audio_play_sound", audio_play_sound },
		{ "audio_stop_sound", audio_stop_sound },
		{ "audio_pause_sound", audio_pause_sound },
		{ "audio_resume_sound", audio_resume_sound },
		{ "audio_is_playing", audio_is_playing },
		{ "audio_sound_gain", audio_sound_gain },
		{ "audio_sound_pitch", audio_sound_pitch},
		{ "audio_stop_all", audio_stop_all },
		{ "audio_set_master_gain", audio_set_master_gain },
		{ "audio_sound_set_track_position", audio_sound_set_track_position },
		{ "audio_group_load", audio_group_load },
		{ "audio_group_is_loaded", audio_group_is_loaded },
		{ "audio_group_set_gain", audio_group_set_gain },
		{ "audio_create_stream", audio_create_stream },
		{ "audio_destroy_stream", audio_destroy_stream },
		#endregion

		#region Gamepad
		{ "gamepad_is_connected", gamepad_is_connected },
		{ "gamepad_button_check", gamepad_button_check },
		{ "gamepad_axis_value", gamepad_axis_value },
		#endregion

		#region YoYo
		{ "@@NewGMLArray@@", newgmlarray },
		#endregion

		#region Layer
		{ "layer_force_draw_depth", layer_force_draw_depth },
		#endregion

		#region Camera
		{ "camera_set_view_pos", camera_set_view_pos },
		{ "camera_set_view_target", camera_set_view_target },
		{ "camera_get_view_x", camera_get_view_x },
		{ "camera_get_view_y", camera_get_view_y },
		{ "camera_get_view_width", camera_get_view_width },
		{ "camera_get_view_height", camera_get_view_height },
		//{ "camera_get_view_target", camera_get_view_target },
		{ "view_get_camera", view_get_camera },
		#endregion

		{ "lengthdir_x", lengthdir_x },
		{ "lengthdir_y", lengthdir_y },
		{ "object_get_sprite", object_get_sprite },
		{ "layer_get_all", layer_get_all },
		{ "layer_get_all_elements", layer_get_all_elements },
		{ "layer_get_depth", layer_get_depth },
		{ "layer_tile_alpha", layer_tile_alpha },
		{ "layer_get_element_type", layer_get_element_type },
		{ "layer_get_name", layer_get_name },
		{ "layer_create", layer_create },
		{ "layer_x", layer_x },
		{ "layer_y", layer_y },
		{ "layer_get_x", layer_get_x },
		{ "layer_get_y", layer_get_y },
		{ "layer_hspeed", layer_hspeed },
		{ "layer_vspeed", layer_vspeed },
		{ "layer_get_hspeed", layer_get_hspeed },
		{ "layer_get_vspeed", layer_get_vspeed },
		/*{ "layer_background_create", layer_background_create },
		{ "layer_background_visible", layer_background_visible },
		{ "layer_background_htiled", layer_background_htiled },
		{ "layer_background_vtiled", layer_background_vtiled },
		{ "layer_background_xscale", layer_background_xscale },
		{ "layer_background_yscale", layer_background_yscale },
		{ "layer_background_stretch", layer_background_stretch },
		{ "layer_background_blend", layer_background_blend },
		{ "layer_background_alpha", layer_background_alpha },
		{ "layer_background_exists", layer_background_exists },*/
		{ "layer_depth", layer_depth },
		{ "real", real },
		{ "instance_find", instance_find },
		{ "draw_arrow", draw_arrow },
		{ "make_color_hsv", make_color_hsv },
		{ "make_colour_hsv", make_color_hsv },
		{ "gpu_set_blendmode", gpu_set_blendmode},
		{ "draw_circle", draw_circle },
		{ "draw_triangle", draw_triangle },
		{ "angle_difference", angle_difference },
		{ "distance_to_object", distance_to_object },
		{ "texturegroup_get_textures", texturegroup_get_textures},
		{ "array_length", array_length},
		{ "texture_prefetch", texture_prefetch},
		{ "window_get_width", window_get_width},
		{ "window_get_height", window_get_height},
		{ "surface_get_width", surface_get_width},
		{ "surface_get_height", surface_get_height},
		{ "os_get_language", os_get_language},
		{ "surface_resize", surface_resize },
		{ "lerp", lerp },
		{ "clamp", clamp },
		{ "gamepad_button_check_pressed", gamepad_button_check_pressed},
		{ "sprite_create_from_surface", sprite_create_from_surface},
		{ "sprite_set_offset", sprite_set_offset },
		{ "gamepad_get_device_count", gamepad_get_device_count},
		{ "draw_text_ext", draw_text_ext },
		{ "ord", ord},
		{ "texture_flush", texture_flush},
		{ "room_get_name", room_get_name},
		{ "buffer_load", buffer_load },
		{ "buffer_read", buffer_read },
		{ "buffer_delete", buffer_delete}
	};

	private static object layer_force_draw_depth(Arguments args)
	{
		var force = args.Args[0].Conv<bool>();
		var depth = args.Args[1].Conv<int>();
		//Debug.Log($"layer_force_draw_depth force:{force} depth:{depth}");

		// not implementing yet because uhhhhhhhhhhhhhhhhhhh

		return null!;
	}

	public static object draw_set_colour(Arguments args)
	{
		var color = args.Args[0].Conv<int>();
		SpriteManager.DrawColor = color;
		return null!;
	}

	public static object draw_get_colour(Arguments args)
	{
		return SpriteManager.DrawColor;
	}

	public static object draw_set_alpha(Arguments args)
	{
		var alpha = args.Args[0].Conv<double>();
		SpriteManager.DrawAlpha = alpha;
		return null!;
	}

	

	public static object newgmlarray(Arguments args)
	{
		return new List<object>();
	}

	public static object asset_get_index(Arguments args)
	{
		var name = args.Args[0].Conv<string>();
		return AssetIndexManager.GetIndex(name);
	}

	public static object event_inherited(Arguments args)
	{
		if (args.Ctx.ObjectDefinition.parent == null)
		{
			return null!;
		}

		GamemakerObject.ExecuteScript(args.Ctx.Self, args.Ctx.ObjectDefinition.parent, args.Ctx.EventType, args.Ctx.EventIndex);
		return null!;
	}

	private static IniFile _iniFile;

	public static object ini_open(Arguments args)
	{
		var name = (string)args.Args[0];

		DebugLog.Log($"ini_open {name}");

		if (_iniFile != null)
		{
			throw new Exception("Cannot open a new .ini file while an old one is still open!");
		}

		var filepath = Path.Combine(Directory.GetCurrentDirectory(), name);

		if (!File.Exists(filepath))
		{
			_iniFile = new IniFile { Name = name };
			return null!;
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

		return null!;
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

		return null!;
	}

	public static object ini_read_real(Arguments args)
	{
		var section = (string)args.Args[0];
		var key = (string)args.Args[1];
		var value = args.Args[2].Conv<double>();

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
		var value = args.Args[2].Conv<double>();

		var sectionClass = _iniFile.Sections.FirstOrDefault(x => x.Name == section);

		if (sectionClass == null)
		{
			sectionClass = new IniSection(section);
			_iniFile.Sections.Add(sectionClass);
		}

		sectionClass.Dict[key] = value.ToString();

		return null!;
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

		return text; // BUG: this does NOT return the written text
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

		DebugLog.Log($"file_text_open_read {filepath}");

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

		return null!;
	}

	public static object file_text_eof(Arguments args)
	{
		var fileid = args.Args[0].Conv<int>();
		var reader = _fileHandles[fileid].Reader;
		return reader.EndOfStream;
	}

	public static object file_exists(Arguments args)
	{
		var fname = args.Args[0].Conv<string>();
		var filepath = Path.Combine(Directory.GetCurrentDirectory(), fname);
		return File.Exists(filepath);
	}

	public static object file_text_readln(Arguments args)
	{
		var fileid = args.Args[0].Conv<int>();
		var reader = _fileHandles[fileid].Reader;
		return reader.ReadLine(); // BUG: returns null if eof
	}

	public static object file_text_writeln(Arguments args)
	{
		var fileid = args.Args[0].Conv<int>();
		var writer = _fileHandles[fileid].Writer;
		writer.WriteLine();
		return null!;
	}

	public static object file_text_read_string(Arguments args)
	{
		var fileid = args.Args[0].Conv<int>();
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
		var fileid = args.Args[0].Conv<int>();
		var str = args.Args[1].Conv<string>();
		var writer = _fileHandles[fileid].Writer;
		writer.Write(str);
		return null!;
	}

	public static object file_text_read_real(Arguments args)
	{
		var fileid = args.Args[0].Conv<int>();
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
		var fileid = args.Args[0].Conv<int>();
		var val = args.Args[1];
		var writer = _fileHandles[fileid].Writer;

		if (val is not int and not double and not float)
		{
			// i have no fucking idea
			writer.Write(0);
			return null!;
		}

		writer.Write(val.Conv<double>());
		return null!;
	}

	public static object file_delete(Arguments args)
	{
		var fname = args.Args[0].Conv<string>();
		var filepath = Path.Combine(Directory.GetCurrentDirectory(), fname);
		File.Delete(filepath);
		return true; // TODO : this should return false if this fails.
	}

	public static object file_copy(Arguments args)
	{
		var fname = args.Args[0].Conv<string>();
		var newname = args.Args[1].Conv<string>();

		fname = Path.Combine(Directory.GetCurrentDirectory(), fname);
		newname = Path.Combine(Directory.GetCurrentDirectory(), newname);

		if (File.Exists(newname))
		{
			throw new Exception("File already exists.");
		}

		File.Copy(fname, newname);

		return null!;
	}

	public static object variable_global_exists(Arguments args)
	{
		var name = args.Args[0].Conv<string>();
		return VariableResolver.GlobalVariables.ContainsKey(name);
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
		var index = args.Args[0].Conv<int>();
		_dsMapDict.Remove(index);
		return null!;
	}

	public static object ds_map_add(Arguments args)
	{
		var id = args.Args[0].Conv<int>();
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
		var id = args.Args[0].Conv<int>();
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
		var index = args.Args[0].Conv<int>();
		_dsListDict.Remove(index);
		return null!;
	}

	public static object ds_list_add(Arguments args)
	{
		var id = args.Args[0].Conv<int>();
		var values = args.Args[1..];

		if (!_dsListDict.ContainsKey(id))
		{
			return null!;
		}

		var list = _dsListDict[id];
		list.AddRange(values);
		return null!;
	}

	public static object ds_map_find_value(Arguments args)
	{
		var id = args.Args[0].Conv<int>();
		var key = args.Args[1];

		if (!_dsMapDict.ContainsKey(id))
		{
			return null!;
		}

		var dict = _dsMapDict[id];
		if (!dict.ContainsKey(key))
		{
			return null!;
		}

		return dict[key];
	}

	public static object show_debug_message(Arguments args)
	{
		DebugLog.Log(args.Args[0].ToString());
		return null!;
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

		var @string = args.Args[0].Conv<string>();
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

	public static object font_add_sprite_ext(Arguments args)
	{
		var spriteAssetIndex = args.Args[0].Conv<int>();
		var string_map = args.Args[1].Conv<string>();
		var prop = args.Args[2].Conv<bool>();
		var sep = args.Args[3].Conv<int>();

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
		var x1 = args.Args[0].Conv<double>();
		var y1 = args.Args[1].Conv<double>();
		var x2 = args.Args[2].Conv<double>();
		var y2 = args.Args[3].Conv<double>();
		var outline = args.Args[4].Conv<bool>();

		if (outline)
		{
			draw(x1, y1, x2, y1 + 1);
			draw(x2 - 1, y1 + 1, x2, y2);
			draw(x1, y2 - 1, x2, y2);
			draw(x1, y1, x1 + 1, y2);
			return null!;
		}

		draw(x1, y1, x2, y2);
		return null!;

		static void draw(double x1, double y1, double x2, double y2)
		{
			CustomWindow.RenderJobs.Add(new GMPolygonJob()
			{
				blend = SpriteManager.DrawColor.BGRToColor(),
				alpha = SpriteManager.DrawAlpha,
				Vertices = new Vector2d[]
				{
					new(x1, y1),
					new(x2, y1),
					new(x2, y2),
					new(x1, y2)
				}
			});
		}
	}

	public static object draw_set_font(Arguments args)
	{
		var font = args.Args[0].Conv<int>();

		var library = TextManager.FontAssets;
		var fontAsset = library.FirstOrDefault(x => x.AssetIndex == font);
		TextManager.fontAsset = fontAsset;
		return null!;
	}

	public static object draw_text(Arguments args)
	{
		var x = args.Args[0].Conv<double>();
		var y = args.Args[1].Conv<double>();
		var str = args.Args[2].Conv<string>();
		TextManager.DrawText(x, y, str);
		return null!;
	}

	public static object merge_colour(Arguments args)
	{
		var col1 = args.Args[0].Conv<int>();
		var col2 = args.Args[1].Conv<int>();
		var amount = args.Args[2].Conv<double>();

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
		var halign = args.Args[0].Conv<int>();
		TextManager.halign = (HAlign)halign;
		return null!;
	}

	public static object draw_set_valign(Arguments args)
	{
		var valign = args.Args[0].Conv<int>();
		TextManager.valign = (VAlign)valign;
		return null!;
	}

	public static object draw_sprite(Arguments args)
	{
		var sprite = args.Args[0].Conv<int>();
		var subimg = args.Args[1].Conv<int>();
		var x = args.Args[2].Conv<double>();
		var y = args.Args[3].Conv<double>();

		SpriteManager.DrawSprite(sprite, subimg, x, y);
		return null!;
	}

	public static object window_set_caption(Arguments args)
	{
		var caption = args.Args[0].Conv<string>();

		CustomWindow.Instance.Title = caption;
		return null!;
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
		var key = args.Args[0].Conv<int>();

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
		var key = args.Args[0].Conv<int>();

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
		var w = args.Args[0].Conv<int>();
		var h = args.Args[1].Conv<int>();

		DebugLog.Log($"window_set_size {w} {h}");

		CustomWindow.Instance.ClientSize = new Vector2i(w, h);

		return null!;
	}

	public static object window_center(Arguments args)
	{
		CustomWindow.Instance.CenterWindow();

		return null!;
	}

	public static object window_get_fullscreen(Arguments args)
	{
		return CustomWindow.Instance.IsFullscreen;
	}

	public static object window_set_fullscreen(Arguments args)
	{
		var full = args.Args[0].Conv<bool>();
		CustomWindow.Instance.WindowState = full ? WindowState.Fullscreen : WindowState.Normal;
		// BUG: this fucks resolution
		return null!;
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
		var device = args.Args[0].Conv<int>();
		return false; // TODO : implement
	}

	public static object draw_sprite_ext(Arguments args)
	{
		var sprite = args.Args[0].Conv<int>();
		var subimg = args.Args[1].Conv<int>();
		var x = args.Args[2].Conv<double>();
		var y = args.Args[3].Conv<double>();
		var xscale = args.Args[4].Conv<double>();
		var yscale = args.Args[5].Conv<double>();
		var rot = args.Args[6].Conv<double>();
		var colour = args.Args[7].Conv<int>();
		var alpha = args.Args[8].Conv<double>();

		SpriteManager.DrawSpriteExt(sprite, subimg, x, y, xscale, yscale, rot, colour, alpha);
		return null!;
	}

	public static object draw_text_transformed(Arguments args)
	{
		var x = args.Args[0].Conv<double>();
		var y = args.Args[1].Conv<double>();
		var str = args.Args[2].Conv<string>();
		var xscale = args.Args[3].Conv<double>();
		var yscale = args.Args[4].Conv<double>();
		var angle = args.Args[5].Conv<double>();
		TextManager.DrawTextTransformed(x, y, str, xscale, yscale, angle);
		return null!;
	}

	public static object draw_sprite_part_ext(Arguments args)
	{
		var sprite = args.Args[0].Conv<int>();
		var subimg = args.Args[1].Conv<int>();
		var left = args.Args[2].Conv<int>();
		var top = args.Args[3].Conv<int>();
		var width = args.Args[4].Conv<int>();
		var height = args.Args[5].Conv<int>();
		var x = args.Args[6].Conv<double>();
		var y = args.Args[7].Conv<double>();
		var xscale = args.Args[8].Conv<double>();
		var yscale = args.Args[9].Conv<double>();
		var colour = args.Args[10].Conv<int>();
		var alpha = args.Args[11].Conv<double>();

		SpriteManager.DrawSpritePartExt(sprite, subimg, left, top, width, height, x, y, xscale, yscale, colour, alpha);

		return null!;
	}

	public static object draw_sprite_part(Arguments args)
	{
		var sprite = args.Args[0].Conv<int>();
		var subimg = args.Args[1].Conv<int>();
		var left = args.Args[2].Conv<int>();
		var top = args.Args[3].Conv<int>();
		var width = args.Args[4].Conv<int>();
		var height = args.Args[5].Conv<int>();
		var x = args.Args[6].Conv<double>();
		var y = args.Args[7].Conv<double>();

		SpriteManager.DrawSpritePart(sprite, subimg, left, top, width, height, x, y);

		return null!;
	}

	public static object draw_self(Arguments args)
	{
		SpriteManager.DrawSelf(args.Ctx.Self);
		return null!;
	}

	public static object draw_sprite_stretched(Arguments args)
	{
		var sprite = args.Args[0].Conv<int>();
		var subimg = args.Args[1].Conv<int>();
		var x = args.Args[2].Conv<double>();
		var y = args.Args[3].Conv<double>();
		var w = args.Args[4].Conv<double>();
		var h = args.Args[5].Conv<double>();

		SpriteManager.draw_sprite_stretched(sprite, subimg, x, y, w, h);
		return null!;
	}

	public static object draw_text_colour(Arguments args)
	{
		var x = args.Args[0].Conv<double>();
		var y = args.Args[1].Conv<double>();
		var str = args.Args[2].Conv<string>();
		var c1 = args.Args[3].Conv<int>();
		var c2 = args.Args[4].Conv<int>();
		var c3 = args.Args[5].Conv<int>();
		var c4 = args.Args[6].Conv<int>();
		var alpha = args.Args[7].Conv<double>();

		TextManager.DrawTextColor(x, y, str, c1, c2, c3, c4, alpha);
		
		return null!;
	}

	public static object draw_sprite_tiled_ext(Arguments args)
	{
		var sprite = args.Args[0].Conv<int>();
		var subimg = args.Args[1].Conv<int>();
		var x = args.Args[2].Conv<double>();
		var y = args.Args[3].Conv<double>();
		var xscale = args.Args[4].Conv<double>();
		var yscale = args.Args[5].Conv<double>();
		var colour = args.Args[6].Conv<int>();
		var alpha = args.Args[7].Conv<double>();

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

		return null!;
	}

	public static object audio_group_set_gain(Arguments args)
	{
		// TODO : actually implement this properly? DELTARUNITY doesnt use audio groups or any GM storage files (yet?)
		return null!;
	}

	public static object audio_play_sound(Arguments args)
	{
		var index = args.Args[0].Conv<int>();
		var priority = args.Args[1].Conv<double>();
		var loop = args.Args[2].Conv<bool>();
		var asset = AudioManager.GetAudioAsset(index);
		var gain = asset.Gain;
		var offset = asset.Offset;
		var pitch = asset.Pitch;
		var listener_mask = 0; // TODO : work out what the hell this is for
		if (args.Args.Length > 3)
		{
			gain = args.Args[3].Conv<double>();
		}

		if (args.Args.Length > 4)
		{
			offset = args.Args[4].Conv<double>();
		}

		if (args.Args.Length > 5)
		{
			pitch = args.Args[5].Conv<double>();
		}

		if (args.Args.Length > 6)
		{
			listener_mask = args.Args[6].Conv<int>();
		}

		var ret = AudioManager.audio_play_sound(index, priority, loop, gain, offset, pitch);
		return ret;
	}

	public static object audio_set_master_gain(Arguments args)
	{
		var listenerIndex = args.Args[0].Conv<double>(); // deltarune doesnt use other listeners rn so i dont care
		var gain = args.Args[1].Conv<double>();
		AL.Listener(ALListenerf.Gain, (float)gain);
		return null!;
	}

	public static object view_get_camera(Arguments args)
	{
		// TODO : ughhh implement multiple cameras
		return 0;
	}

	public static object camera_get_view_x(Arguments args)
	{
		var camera_id = args.Args[0].Conv<int>();

		if (camera_id > 0)
		{
			// TODO : ughhh implement multiple cameras
			throw new NotImplementedException();
		}

		return CustomWindow.Instance.X;
	}

	public static object camera_get_view_y(Arguments args)
	{
		var camera_id = args.Args[0].Conv<int>();

		if (camera_id > 0)
		{
			// TODO : ughhh implement multiple cameras
			throw new NotImplementedException();
		}

		return CustomWindow.Instance.Y;
	}

	public static object camera_get_view_width(Arguments args)
	{
		var camera_id = args.Args[0].Conv<int>();

		if (camera_id > 0)
		{
			// TODO : ughhh implement multiple cameras
			throw new NotImplementedException();
		}

		return RoomManager.CurrentRoom.CameraWidth;
	}

	public static object camera_get_view_height(Arguments args)
	{
		var camera_id = args.Args[0].Conv<int>();

		if (camera_id > 0)
		{
			// TODO : ughhh implement multiple cameras
			throw new NotImplementedException();
		}

		return RoomManager.CurrentRoom.CameraHeight;
	}

	public static object camera_set_view_target(Arguments args)
	{
		var camera_id = args.Args[0].Conv<int>();

		if (camera_id > 0)
		{
			// TODO : ughhh implement multiple cameras
			throw new NotImplementedException();
		}

		var id = args.Args[1].Conv<int>();

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

		return null!;
	}

	/*public static object camera_get_view_target(Arguments args)
	{
		var camera_id = args.Args[0].Conv<int>();

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
		var camera_id = args.Args[0].Conv<int>();

		if (camera_id > 0)
		{
			// TODO : ughhh implement multiple cameras
			throw new NotImplementedException();
		}

		var x = args.Args[1].Conv<double>();
		var y = args.Args[2].Conv<double>();

		CustomWindow.Instance.SetPosition(x, y);

		return null!;
	}

	public static object audio_create_stream(Arguments args)
	{
		var filename = args.Args[0].Conv<string>();

		var assetName = Path.GetFileNameWithoutExtension(filename);
		var existingIndex = AssetIndexManager.GetIndex(assetName);
		if (existingIndex != -1)
		{
			// happens in deltarune on battle.ogg
			DebugLog.LogWarning($"audio_create_stream on {filename} already registered with index {existingIndex}");
			return existingIndex;
		}

		// maybe this should be moved to RegisterAudioClip
		using var reader = new VorbisReader(filename);
		var data = new float[reader.TotalSamples * reader.Channels]; // is this correct length?
		reader.ReadSamples(data, 0, data.Length);
		var stereo = reader.Channels == 2;
		var freq = reader.SampleRate;

		var buffer = AL.GenBuffer();
		AudioManager.CheckALError();
		AL.BufferData(buffer, stereo ? ALFormat.StereoFloat32Ext : ALFormat.MonoFloat32Ext, data, freq);
		AudioManager.CheckALError();

		return AudioManager.RegisterAudioClip(new()
		{
			// RegisterAudioClip sets AssetIndex
			Name = assetName,
			Clip = buffer,
			Gain = 1,
			Pitch = 1,
			Offset = 0,
		});
	}

	public static object audio_destroy_stream(Arguments args)
	{
		var index = args.Args[0].Conv<int>();
		AudioManager.UnregisterAudio(index);
		return null!;
	}

	public static object audio_sound_gain(Arguments args)
	{
		var index = args.Args[0].Conv<int>();
		var volume = args.Args[1].Conv<double>();
		var time = args.Args[2].Conv<double>();

		if (index >= GMConstants.FIRST_INSTANCE_ID)
		{
			// instance id
			var soundAsset = AudioManager.GetAudioInstance(index);
			if (soundAsset == null)
			{
				return null!;
			}

			AudioManager.ChangeGain(soundAsset.Source, volume, time);
		}
		else
		{
			// sound asset index
			AudioManager.SetAssetGain(index, volume);

			foreach (var item in AudioManager.GetAudioInstances(index))
			{
				AudioManager.ChangeGain(item.Source, volume, time);
			}
		}

		return null!;
	}

	public static object audio_sound_pitch(Arguments args)
	{
		var index = args.Args[0].Conv<int>();
		var pitch = args.Args[1].Conv<double>();

		pitch = Math.Clamp(pitch, 1.0 / 256.0, 256.0);

		if (index >= GMConstants.FIRST_INSTANCE_ID)
		{
			// instance id
			var soundAsset = AudioManager.GetAudioInstance(index);
			AL.Source(soundAsset.Source, ALSourcef.Pitch, (float)pitch);
			AudioManager.CheckALError();
		}
		else
		{
			// sound asset index
			AudioManager.SetAssetPitch(index, pitch);

			foreach (var item in AudioManager.GetAudioInstances(index))
			{
				AL.Source(item.Source, ALSourcef.Pitch, (float)pitch);
				AudioManager.CheckALError();
			}
		}

		return null!;
	}

	public static object audio_stop_all(Arguments args)
	{
		DebugLog.Log($"audio_stop_all");
		AudioManager.StopAllAudio();
		return null!;
	}

	public static object audio_stop_sound(Arguments args)
	{
		var id = args.Args[0].Conv<int>();
		DebugLog.Log($"audio_stop_sound id:{id}");

		if (id < GMConstants.FIRST_INSTANCE_ID)
		{
			foreach (var item in AudioManager.GetAudioInstances(id))
			{
				AL.SourceStop(item.Source);
				AudioManager.CheckALError();
			}
		}
		else
		{
			var soundAsset = AudioManager.GetAudioInstance(id);
			AL.SourceStop(soundAsset.Source);
			AudioManager.CheckALError();
		}
		AudioManager.Update(); // hack: deletes the sources. maybe make official stop and delete function
		
		return null!;
	}

	public static object audio_pause_sound(Arguments args)
	{
		var index = args.Args[0].Conv<int>();

		if (index < GMConstants.FIRST_INSTANCE_ID)
		{
			foreach (var item in AudioManager.GetAudioInstances(index))
			{
				AL.SourcePause(item.Source);
				AudioManager.CheckALError();
			}
		}
		else
		{
			var instance = AudioManager.GetAudioInstance(index);
			if (instance != null)
			{
				AL.SourcePause(instance.Source);
				AudioManager.CheckALError();
			}
				
		}
		
		return null!;
	}

	public static object audio_resume_sound(Arguments args)
	{
		var index = args.Args[0].Conv<int>();

		if (index < GMConstants.FIRST_INSTANCE_ID)
		{
			foreach (var item in AudioManager.GetAudioInstances(index))
			{
				AL.SourcePlay(item.Source);
				AudioManager.CheckALError();
			}
		}
		else
		{
			AL.SourcePlay(AudioManager.GetAudioInstance(index).Source);
			AudioManager.CheckALError();
		}

		return null!;
	}

	public static object audio_sound_set_track_position(Arguments args)
	{
		var index = args.Args[0].Conv<int>();
		var time = args.Args[1].Conv<double>();

		if (index < GMConstants.FIRST_INSTANCE_ID)
		{
			AudioManager.SetAssetOffset(index, time);
			
			// unlike gain and pitch, this doesnt change currently playing instances
		}
		else
		{
			AL.Source(AudioManager.GetAudioInstance(index).Source, ALSourcef.SecOffset, (float)time);
		}
		
		return null!;
	}

	public static object audio_is_playing(Arguments args)
	{
		var index = args.Args[0].Conv<int>();

		if (index < GMConstants.FIRST_INSTANCE_ID)
		{
			foreach (var item in AudioManager.GetAudioInstances(index))
			{
				if (item != null)
				{
					return true;
				}
			}

			return false;
		}
		else
		{
			var instance = AudioManager.GetAudioInstance(index);
			return instance != null;
		}
	}

	public static object string_width(Arguments args)
	{
		var str = args.Args[0].Conv<string>();

		return TextManager.StringWidth(str);
	}

	public static object event_user(Arguments args)
	{
		var numb = args.Args[0].Conv<int>();
		GamemakerObject.ExecuteScript(args.Ctx.Self, args.Ctx.ObjectDefinition, EventType.Other, (int)EventSubtypeOther.User0 + numb);
		return null!;
	}

	public static object script_execute(Arguments args)
	{
		var scriptAssetId = args.Args[0].Conv<int>();
		var scriptArgs = args.Args[1..];

		var script = Scripts.First(x => x.Value.AssetId == scriptAssetId).Value;
		return VMExecutor.ExecuteScript(script, args.Ctx.Self, args.Ctx.ObjectDefinition, arguments: new Arguments { Args = scriptArgs });
	}

	public static object draw_line_width(Arguments args)
	{
		var x1 = args.Args[0].Conv<double>();
		var y1 = args.Args[1].Conv<double>();
		var x2 = args.Args[2].Conv<double>();
		var y2 = args.Args[3].Conv<double>();
		var w = args.Args[4].Conv<int>();
			
		CustomWindow.RenderJobs.Add(new GMLineJob()
		{
			blend = SpriteManager.DrawColor.BGRToColor(),
			alpha = SpriteManager.DrawAlpha,
			start = new Vector2((float)x1, (float)y1),
			end = new Vector2((float)x2, (float)y2),
			width = w
		});

		return null!;
	}

	public static object gpu_set_fog(Arguments args)
	{
		var enable = args.Args[0].Conv<bool>();
		var colour = args.Args[1].Conv<int>();
		var start = args.Args[2].Conv<double>();
		var end = args.Args[3].Conv<double>();

		if ((start != 0 && start != 1) || (end != 0 && end != 1))
		{
			throw new NotImplementedException("actual fog");
		}

		SpriteManager.FogEnabled = enable;
		SpriteManager.FogColor = colour;

		return null!;
	}

	public static object sprite_get_number(Arguments args)
	{
		var index = args.Args[0].Conv<int>();
		return SpriteManager.GetNumberOfFrames(index);
	}

	public static object lengthdir_x(Arguments args)
	{
		var len = args.Args[0].Conv<double>();
		var dir = args.Args[1].Conv<double>();

		return len * Math.Cos(dir * CustomMath.Deg2Rad);
	}

	public static object lengthdir_y(Arguments args)
	{
		var len = args.Args[0].Conv<double>();
		var dir = args.Args[1].Conv<double>();

		return -len * Math.Sin(dir * CustomMath.Deg2Rad);
	}

	public static object object_get_sprite(Arguments args)
	{
		var obj = args.Args[0].Conv<int>();
		return InstanceManager.ObjectDefinitions[obj].sprite;
	}

	public static object layer_get_all(Arguments args)
	{
		return RoomManager.CurrentRoom.Layers.Values.Select(x => (object)x.ID).ToList();
	}

	public static object layer_get_name(Arguments args)
	{
		var layer_id = args.Args[0].Conv<int>();
		return RoomManager.CurrentRoom.Layers[layer_id].Name;
	}

	public static object real(Arguments args)
	{
		var str = args.Args[0].Conv<string>();
		return str.Conv<double>();
	}

	private static object layer_get_depth(Arguments args)
	{
		var layer_id = args.Args[0].Conv<int>();
		var layer = RoomManager.CurrentRoom.Layers[layer_id];
		return layer.Depth;
	}

	private static object layer_x(Arguments args)
	{
		var layer_id = args.Args[0].Conv<int>();
		var x = args.Args[1].Conv<double>();

		var layer = RoomManager.CurrentRoom.Layers[layer_id];
		layer.X = (float)x;
		return null!;
	}

	private static object layer_y(Arguments args)
	{
		var layer_id = args.Args[0].Conv<int>();
		var y = args.Args[1].Conv<double>();

		var layer = RoomManager.CurrentRoom.Layers[layer_id];
		layer.Y = (float)y;
		return null!;
	}

	private static object layer_get_x(Arguments args)
	{
		var layer_id = args.Args[0].Conv<int>();

		var layer = RoomManager.CurrentRoom.Layers[layer_id];
		return layer.X;
	}

	private static object layer_get_y(Arguments args)
	{
		var layer_id = args.Args[0].Conv<int>();

		var layer = RoomManager.CurrentRoom.Layers[layer_id];
		return layer.Y;
	}

	private static object layer_hspeed(Arguments args)
	{
		var layer_id = args.Args[0].Conv<int>();
		var hspd = args.Args[1].Conv<double>();

		var layer = RoomManager.CurrentRoom.Layers[layer_id];
		layer.HSpeed = (float)hspd;
		return null!;
	}

	private static object layer_vspeed(Arguments args)
	{
		var layer_id = args.Args[0].Conv<int>();
		var vspd = args.Args[1].Conv<double>();

		var layer = RoomManager.CurrentRoom.Layers[layer_id];
		layer.VSpeed = (float)vspd;
		return null!;
	}

	private static object layer_get_vspeed(Arguments args)
	{
		var layer_id = args.Args[0].Conv<int>();

		var layer = RoomManager.CurrentRoom.Layers[layer_id];
		return layer.VSpeed;
	}

	private static object layer_get_hspeed(Arguments args)
	{
		var layer_id = args.Args[0].Conv<int>();

		var layer = RoomManager.CurrentRoom.Layers[layer_id];
		return layer.HSpeed;
	}

	private static object layer_depth(Arguments args)
	{
		var layer_id = args.Args[0].Conv<int>();
		var depth = args.Args[1].Conv<int>();

		var layer = RoomManager.CurrentRoom.Layers[layer_id];
		layer.Depth = depth;
		return null!;
	}

	private static object layer_get_all_elements(Arguments args)
	{
		var layer_id = args.Args[0].Conv<int>();
		var layer = RoomManager.CurrentRoom.Layers[layer_id];
		return layer.Elements.Select(x => (object)x.instanceId).ToList();
	}

	private static object layer_get_element_type(Arguments args)
	{
		var element_id = args.Args[0].Conv<int>();
		var element = TileManager.Tiles.First(x => x.instanceId == element_id);

		if (element is GMTile)
		{
			return 7;
		}
		else
		{
			return 9; // undefined
		}
	}

	private static object layer_tile_alpha(Arguments args)
	{
		var __index = args.Args[0].Conv<int>();
		var __alpha = args.Args[1].Conv<double>();
		// TODO : implement
		//(TileManager.Tiles.First(x => x.instanceId == __index) as GMTile).Alpha = __alpha;
		return null!;
	}

	private static object layer_create(Arguments args)
	{
		var depth = args.Args[0].Conv<int>();
		var name = "";
		if (args.Args.Length > 1)
		{
			name = args.Args[1].Conv<string>();
		}

		var newLayerId = RoomManager.CurrentRoom.Layers.Keys.Max() + 1;

		if (string.IsNullOrEmpty(name))
		{
			name = $"_layer_{Guid.NewGuid()}";
		}

		var layerContainer = new LayerContainer(new SerializedFiles.Layer() { LayerName = name, LayerDepth = depth, LayerID = newLayerId });

		RoomManager.CurrentRoom.Layers.Add(newLayerId, layerContainer);

		return newLayerId;
	}

	private static object instance_find(Arguments args)
	{
		var obj = args.Args[0].Conv<int>();
		var n = args.Args[1].Conv<int>();

		/*
		 * todo : this is really fucked.
		 * "You specify the object that you want to find the instance of and a number,
		 * and if there is an instance at that position in the instance list then the function
		 * returns the id of that instance, and if not it returns the special keyword noone.
		 * You can also use the keyword all to iterate through all the instances in a room,
		 * as well as a parent object to iterate through all the instances that are part of
		 * that parent / child hierarchy, and you can even specify an instance (if you have its id)
		 * as a check to see if it actually exists in the current room."
		 */

		if (obj == GMConstants.all)
		{
			return InstanceManager.instances.ElementAt(n).instanceId;
		}
		else if (obj >= GMConstants.FIRST_INSTANCE_ID)
		{
			// is an instance id
			// todo : implement
		}
		else
		{
			// is an object index
			return InstanceManager.instances.Where(x => x.object_index == obj).ElementAt(n).instanceId;
		}

		return GMConstants.noone;
	}

	private static object draw_arrow(Arguments args)
	{
		var x1 = args.Args[0].Conv<double>();
		var y1 = args.Args[1].Conv<double>();
		var x2 = args.Args[2].Conv<double>();
		var y2 = args.Args[3].Conv<double>();
		var size = args.Args[4].Conv<double>();

		// todo : name all these variables better and refactor this

		var height = y2 - y1;
		var length = x2 - x1;

		var magnitude = Math.Sqrt((height * height) + (length * length));

		if (magnitude != 0)
		{
			// draw body of arrow

			CustomWindow.RenderJobs.Add(new GMLineJob()
			{
				blend = SpriteManager.DrawColor.BGRToColor(),
				alpha = SpriteManager.DrawAlpha,
				width = 1,
				start = new Vector2((float)x1, (float)y1),
				end = new Vector2((float)x2, (float)y2)
			});

			// draw head of arrow

			var headSize = magnitude;
			if (size <= magnitude)
			{
				headSize = size;
			}

			var headLength = (length * headSize) / magnitude;
			var headHeight = (height * headSize) / magnitude;

			var a = (x2 - headLength) - headHeight / 3;
			var b = (headLength / 3) + (y2 - headHeight);

			var c = (headHeight / 3) + (x2 - headLength);
			var d = (y2 - headHeight) - headLength / 3;

			CustomWindow.RenderJobs.Add(new GMPolygonJob()
			{
				blend = SpriteManager.DrawColor.BGRToColor(),
				alpha = SpriteManager.DrawAlpha,
				Vertices = new[] { new Vector2d(x2, y2), new Vector2d(a, b), new Vector2d(c, d) }
			});
		}

		return null!;
	}

	public static object make_color_hsv(Arguments args)
	{
		var hue = args.Args[0].Conv<double>();
		var sat = args.Args[1].Conv<double>() / 255;
		var val = args.Args[2].Conv<double>() / 255;

		var hueDegree = (hue / 255) * 360;

		var chroma = val * sat;

		var hPrime = hueDegree / 60;

		var x = chroma * (1 - Math.Abs((hPrime % 2) - 1));

		var r = 0.0;
		var g = 0.0;
		var b = 0.0;

		switch (hPrime)
		{
			case >= 0 and < 1:
				r = chroma;
				g = x;
				b = 0;
				break;
			case >= 1 and < 2:
				r = x;
				g = chroma;
				b = 0;
				break;
			case >= 2 and < 3:
				r = 0;
				g = chroma;
				b = x;
				break;
			case >= 3 and < 4:
				r = 0;
				g = x;
				b = chroma;
				break;
			case >= 4 and < 5:
				r = x;
				g = 0;
				b = chroma;
				break;
			case >= 5 and < 6:
				r = chroma;
				g = 0;
				b = x;
				break;
		}

		var m = val - chroma;
		r += m;
		g += m;
		b += m;

		var rByte = (byte)(r * 255);
		var gByte = (byte)(g * 255);
		var bByte = (byte)(b * 255);

		return (bByte << 16) + (byte)(gByte << 8) + rByte;
	}

	public static object gpu_set_blendmode(Arguments args)
	{
		var mode = args.Args[0].Conv<int>();

		switch (mode)
		{
			case 0:
				// bm_normal
				GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
				GL.BlendEquation(BlendEquationMode.FuncAdd);
				break;
			case 1:
				// bm_add
				GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
				GL.BlendEquation(BlendEquationMode.FuncAdd);
				break;
			case 2:
				// bm_subtract
				GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
				GL.BlendEquation(BlendEquationMode.FuncSubtract);
				break;
			case 3:
				// bm_reverse_subtract
				GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
				GL.BlendEquation(BlendEquationMode.FuncReverseSubtract);
				break;
			case 4:
				// bm_min
				GL.BlendFunc(BlendingFactor.One, BlendingFactor.One);
				GL.BlendEquation(BlendEquationMode.Min);
				break;
			case 5:
				// bm_max
				GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcColor);
				GL.BlendEquation(BlendEquationMode.FuncAdd);
				break;
		}

		return null!;
	}

	public static object draw_circle(Arguments args)
	{
		var x = args.Args[0].Conv<double>();
		var y = args.Args[1].Conv<double>();
		var r = args.Args[2].Conv<double>();
		var outline = args.Args[3].Conv<bool>();

		var angle = 360 / DrawManager.CirclePrecision;

		var points = new Vector2d[DrawManager.CirclePrecision];
		for (var i = 0; i < DrawManager.CirclePrecision; i++)
		{
			points[i] = new Vector2d(x + (r * Math.Sin(angle * i)), y + (r * Math.Cos(angle * i)));
		}

		CustomWindow.RenderJobs.Add(new GMPolygonJob()
		{
			blend = SpriteManager.DrawColor.BGRToColor(),
			alpha = SpriteManager.DrawAlpha,
			Vertices = points,
			Outline = outline
		});

		return null!;
	}

	public static object draw_triangle(Arguments args)
	{
		var x1 = args.Args[0].Conv<double>();
		var y1 = args.Args[1].Conv<double>();
		var x2 = args.Args[2].Conv<double>();
		var y2 = args.Args[3].Conv<double>();
		var x3 = args.Args[4].Conv<double>();
		var y3 = args.Args[5].Conv<double>();
		var outline = args.Args[6].Conv<bool>();

		CustomWindow.RenderJobs.Add(new GMPolygonJob()
		{
			blend = SpriteManager.DrawColor.BGRToColor(),
			alpha = SpriteManager.DrawAlpha,
			Vertices = new Vector2d[]
			{
				new Vector2d(x1, y1),
				new Vector2d(x2, y2),
				new Vector2d(x3, y3)
			},
			Outline = outline
		});

		return null!;
	}

	public static object angle_difference(Arguments args)
	{
		var dest = args.Args[0].Conv<double>();
		var src = args.Args[1].Conv<double>();
		return CustomMath.Mod(dest - src + 180, 360) - 180;
	}

	public static object distance_to_object(Arguments args)
	{
		var obj = args.Args[0].Conv<int>();

		GamemakerObject objToCheck = null;

		if (obj == GMConstants.other)
		{
			// todo - what the fuck does this mean gamemaker!? WHAT DO YOU WANT FROM ME
		}
		else if (obj < GMConstants.FIRST_INSTANCE_ID)
		{
			// object index
			objToCheck = InstanceManager.FindByAssetId(obj).First();
		}
		else
		{
			// instance id
			objToCheck = InstanceManager.FindByInstanceId(obj);
		}

		return CollisionManager.DistanceToObject(args.Ctx.Self, objToCheck);
	}

	public static object texturegroup_get_textures(Arguments args)
	{
		var tex_id = args.Args[0].Conv<string>();

		var asset = GameLoader.TexGroups[tex_id];

		return asset.TexturePages.Select(x => new RValue(x)).ToList();
	}

	public static object texture_prefetch(Arguments args)
	{
		var tex_id = args.Args[0].Conv<string>();
		// TODO : Implement? Or not?
		return null!;
	}

	public static object texture_flush(Arguments args)
	{
		var tex_id = args.Args[0].Conv<string>();
		// TODO : Implement? Or not?
		return null!;
	}

	public static object window_get_width(Arguments args)
	{
		return CustomWindow.Instance.ClientSize.X;
	}

	public static object window_get_height(Arguments args)
	{
		return CustomWindow.Instance.ClientSize.Y;
	}

	public static object surface_get_width(Arguments args)
	{
		var surface_id = args.Args[0].Conv<int>();
		return SurfaceManager.GetSurfaceWidth(surface_id);
	}

	public static object surface_get_height(Arguments args)
	{
		var surface_id = args.Args[0].Conv<int>();
		return SurfaceManager.GetSurfaceHeight(surface_id);
	}

	public static object os_get_language(Arguments args)
	{
		return "en"; // TODO : actually implement
	}

	public static object surface_resize(Arguments args)
	{
		var surface_id = args.Args[0].Conv<int>();
		var w = args.Args[1].Conv<int>();
		var h = args.Args[2].Conv<int>();
		SurfaceManager.ResizeSurface(surface_id, w, h);
		return null!;
	}

	public static object lerp(Arguments args)
	{
		var a = args.Args[0].Conv<double>();
		var b = args.Args[1].Conv<double>();
		var amt = args.Args[2].Conv<double>();

		return a + ((b - a) * amt);
	}

	public static object clamp(Arguments args)
	{
		var val = args.Args[0].Conv<double>();
		var min = args.Args[1].Conv<double>();
		var max = args.Args[2].Conv<double>();

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

	public static object gamepad_button_check_pressed(Arguments args)
	{
		// TODO : implement
		return false;
	}

	public static object sprite_create_from_surface(Arguments args)
	{
		// TODO : implement
		return 1;
	}

	public static object sprite_set_offset(Arguments args)
	{
		var ind = args.Args[0].Conv<int>();
		var xoff = args.Args[1].Conv<int>();
		var yoff = args.Args[2].Conv<int>();

		var data = SpriteManager._spriteDict[ind];
		data.OriginX = xoff;
		data.OriginY = yoff;
		return null!;
	}

	public static object gamepad_get_device_count(Arguments args)
	{
		// TODO : implement
		return 0;
	}

	public static object draw_text_ext(Arguments args)
	{
		var x = args.Args[0].Conv<double>();
		var y = args.Args[1].Conv<double>();
		var str = args.Args[2].Conv<string>();
		var sep = args.Args[3].Conv<int>();
		var w = args.Args[4].Conv<double>();

		CustomWindow.RenderJobs.Add(new GMTextJob()
		{
			alpha = SpriteManager.DrawAlpha,
			blend = SpriteManager.DrawColor.BGRToColor(),
			angle = 0,
			asset = TextManager.fontAsset,
			halign = TextManager.halign,
			valign = TextManager.valign,
			sep = sep,
			text = str,
			screenPos = new Vector2d(x, y)
		});

		return null!;
	}

	public static object ord(Arguments args)
	{
		var str = args.Args[0].Conv<string>();

		return (int)Encoding.UTF8.GetBytes(str)[0];
	}

	public static object room_get_name(Arguments args)
	{
		var index = args.Args[0].Conv<int>();

		return RoomManager.RoomList[index].Name;
	}

	public static object buffer_load(Arguments args)
	{
		var filename = args.Args[0].Conv<string>();
		return BufferManager.LoadBuffer(filename);
	}

	public static object buffer_read(Arguments args)
	{
		var bufferIndex = args.Args[0].Conv<int>();
		var type = args.Args[1].Conv<int>();
		return BufferManager.ReadBuffer(bufferIndex, (BufferDataType)type);
	}

	public static object buffer_delete(Arguments args)
	{
		var bufferIndex = args.Args[0].Conv<int>();

		var buffer = BufferManager.Buffers[bufferIndex];
		buffer.Data = null;

		BufferManager.Buffers.Remove(bufferIndex);

		return null!;
	}
}

public class FileHandle
{
	public StreamReader Reader;
	public StreamWriter Writer;
}
