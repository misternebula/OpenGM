﻿using Newtonsoft.Json.Linq;
using OpenGM.Rendering;
using System.Collections;
using System.Diagnostics;
using OpenGM.IO;

namespace OpenGM.VirtualMachine;

public static class VariableResolver
{
	/// <summary>
	/// general form of the array index setting logic.
	/// `getter` should do trygetvalue and "as" cast to return null instead of throwing.
	/// `getter` SHOULD NOT COPY.
	/// </summary>
	public static void ArraySet(int index, object? value,
		Func<IList?> getter,
		Action<IList> setter,
		bool onlyGrow = false)
	{
		var array = getter();
		if (array == null)
		{
			array = new List<object?>();
			setter(array);
		}

		if (index >= array.Count)
		{
			var numToAdd = index - array.Count + 1;
			for (var i = 0; i < numToAdd; i++)
			{
				// no clue if this is correct
				// we want new list for every element here so its in the for loop
				array.Add(value switch
				{
					int or short or long or float or double => 0,
					bool => false,
					string => "",
					IList => new List<object?>(),
					null => null,
					_ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
				});
			}
		}

		if (onlyGrow) return;

		array[index] = value;
	}

	public static readonly Dictionary<string, object?> GlobalVariables = new();

	public static Dictionary<string, object?> CustomBuiltInVariableValues = new();
	public static Dictionary<string, (Func<object?> getter, Action<object?>? setter)> BuiltInVariables = new()
	{
		{ "working_directory", (get_working_directory, null) },
		{ "fps", (get_fps, null) },
		{ "room_width", (get_room_width, null) },
		{ "room_height", (get_room_height, null) },
		{ "room", (get_room, set_room) },
		{ "room_speed", (get_room_speed, set_room_speed) },
		{ "os_type", (get_os_type, null) },
		{ "application_surface", (get_application_surface, null) },
		{ "argument_count", (get_argument_count, null) },
		{ "argument", (get_argument, null) },
		{ "argument0", (get_argument_0, null) },
		{ "argument1", (get_argument_1, null) },
		{ "argument2", (get_argument_2, null) },
		{ "argument3", (get_argument_3, null) },
		{ "argument4", (get_argument_4, null) },
		{ "argument5", (get_argument_5, null) },
		{ "argument6", (get_argument_6, null) },
		{ "argument7", (get_argument_7, null) },
		{ "argument8", (get_argument_8, null) },
		{ "argument9", (get_argument_9, null) },
		{ "argument10", (get_argument_10, null) },
		{ "argument11", (get_argument_11, null) },
		{ "argument12", (get_argument_12, null) },
		{ "argument13", (get_argument_13, null) },
		{ "argument14", (get_argument_14, null) },
		{ "argument15", (get_argument_15, null) },
		// { "room_persistent", (get_room_persistent, set_room_persistent)},
		{ "undefined", (get_undefined, null) },
		{ "view_current", (get_view_current, null)},
		{ "view_wport", (get_view_wport, set_view_wport)},
		{ "view_hport", (get_view_hport, set_view_hport)},
		{ "view_xview", (get_view_xview, set_view_xview)},
		{ "view_yview", (get_view_yview, set_view_yview)},
		{ "view_wview", (get_view_wview, set_view_wview)},
		{ "view_hview", (get_view_hview, set_view_hview)},
		{ "view_camera", (get_view_camera, set_view_camera)},
		{ "pointer_null", (get_pointer_null, null)},
		{ "instance_count", (get_instance_count, null)},
		{ "current_time", (get_current_time, null)},
		{ "debug_mode", (get_debug_mode, null)}
	};

	public static Dictionary<string, (Func<GamemakerObject, object> getter, Action<GamemakerObject, object?>? setter)> BuiltInSelfVariables = new()
	{
		{ "x", (get_x, set_x) },
		{ "y", (get_y, set_y) },
		{ "image_index", (get_image_index, set_image_index) },
		{ "sprite_index", (get_sprite_index, set_sprite_index) },
		{ "sprite_height", (get_sprite_height, null) },
		{ "sprite_width", (get_sprite_width, null) },
		{ "xstart", (get_xstart, set_xstart) },
		{ "ystart", (get_ystart, set_ystart) },
		{ "object_index", (get_object_index, null) },
		{ "image_blend", (get_image_blend, set_image_blend) },
		{ "depth", (get_depth, set_depth) },
		{ "bbox_bottom", (get_bbox_bottom, null) },
		{ "bbox_top", (get_bbox_top, null) },
		{ "bbox_left", (get_bbox_left, null) },
		{ "bbox_right", (get_bbox_right, null) },
		{ "image_yscale", (get_image_yscale, set_image_yscale) },
		{ "image_xscale", (get_image_xscale, set_image_xscale) },
		{ "image_speed", (get_image_speed, set_image_speed) },
		{ "visible", (get_visible, set_visible) },
		{ "image_alpha", (get_image_alpha, set_image_alpha) },
		{ "image_angle", (get_image_angle, set_image_angle) },
		{ "speed", (get_speed, set_speed) },
		{ "hspeed", (get_hspeed, set_hspeed) },
		{ "vspeed", (get_vspeed, set_vspeed) },
		{ "direction", (get_direction, set_direction) },
		{ "persistent", (get_persistent, set_persistent) },
		{ "id", (get_id, null) },
		{ "gravity", (get_gravity, set_gravity) },
		{ "friction", (get_friction, set_friction) },
		{ "gravity_direction", (get_gravity_direction, set_gravity_direction) },
		{ "image_number", (get_image_number, null) },
		{ "alarm", (get_alarm, set_alarm) },
		{ "xprevious", (get_xprevious, set_xprevious)},
		{ "yprevious", (get_yprevious, set_yprevious)},
		{ "sprite_xoffset", (get_sprite_xoffset, null)},
		{ "sprite_yoffset", (get_sprite_yoffset, null)},
		{ "path_index", (get_path_index, null)},
		{ "path_position", (get_path_position, set_path_position)},
		{ "path_speed", (get_path_speed, set_path_speed)},
		{ "path_scale", (get_path_scale, set_path_scale)}
	};

	public static object get_working_directory()
	{
		return Entry.DataWinFolder + Path.DirectorySeparatorChar;
	}

	public static object get_fps()
	{
		return Entry.GameSpeed; // TODO : this shouldnt be the desired fps, but the current fps (fluctuating)
	}

	public static object get_x(GamemakerObject instance) => instance.x;
	public static void set_x(GamemakerObject instance, object? value) => instance.x = value.Conv<double>();

	public static object get_y(GamemakerObject instance) => instance.y;
	public static void set_y(GamemakerObject instance, object? value) => instance.y = value.Conv<double>();

	public static object get_room_width() => (double)RoomManager.CurrentRoom.SizeX;
	public static object get_room_height() => (double)RoomManager.CurrentRoom.SizeY;

	public static object get_image_index(GamemakerObject instance) => instance.image_index;
	public static void set_image_index(GamemakerObject instance, object? value) => instance.image_index = value.Conv<double>();

	public static object get_sprite_index(GamemakerObject instance) => instance.sprite_index;
	public static void set_sprite_index(GamemakerObject instance, object? value) => instance.sprite_index = value.Conv<int>();

	public static object get_sprite_width(GamemakerObject instance) => instance.sprite_width;
	public static object get_sprite_height(GamemakerObject instance) => instance.sprite_height;

	public static object get_xstart(GamemakerObject instance) => instance.xstart;
	public static void set_xstart(GamemakerObject instance, object? value) => instance.xstart = value.Conv<double>();
	public static object get_ystart(GamemakerObject instance) => instance.ystart;
	public static void set_ystart(GamemakerObject instance, object? value) => instance.ystart = value.Conv<double>();

	public static object get_object_index(GamemakerObject instance) => instance.object_index;

	public static object get_image_blend(GamemakerObject instance) => instance.image_blend;
	public static void set_image_blend(GamemakerObject instance, object? value) => instance.image_blend = value.Conv<int>();

	public static object get_depth(GamemakerObject instance) => instance.depth;
	public static void set_depth(GamemakerObject instance, object? value) => instance.depth = value.Conv<double>();

	public static object get_room() => RoomManager.CurrentRoom.AssetId;
	public static void set_room(object? value) => RoomManager.New_Room = value.Conv<int>();

	public static object get_bbox_bottom(GamemakerObject instance) => (instance.sprite_index == -1 && instance.mask_index == -1) ? instance.y : instance.bbox_bottom;
	public static object get_bbox_top(GamemakerObject instance) => (instance.sprite_index == -1 && instance.mask_index == -1) ? instance.y : instance.bbox_top;
	public static object get_bbox_left(GamemakerObject instance) => (instance.sprite_index == -1 && instance.mask_index == -1) ? instance.x : instance.bbox_left;
	public static object get_bbox_right(GamemakerObject instance) => (instance.sprite_index == -1 && instance.mask_index == -1) ? instance.x : instance.bbox_right;

	public static object get_image_yscale(GamemakerObject instance) => instance.image_yscale;
	public static void set_image_yscale(GamemakerObject instance, object? value) => instance.image_yscale = value.Conv<double>();

	public static object get_image_xscale(GamemakerObject instance) => instance.image_xscale;
	public static void set_image_xscale(GamemakerObject instance, object? value) => instance.image_xscale = value.Conv<double>();

	public static object get_image_speed(GamemakerObject instance) => instance.image_speed;
	public static void set_image_speed(GamemakerObject instance, object? value) => instance.image_speed = value.Conv<double>();

	public static object get_visible(GamemakerObject instance) => instance.visible;
	public static void set_visible(GamemakerObject instance, object? value) => instance.visible = value.Conv<bool>();

	public static object get_image_alpha(GamemakerObject instance) => instance.image_alpha;
	public static void set_image_alpha(GamemakerObject instance, object? value) => instance.image_alpha = value.Conv<double>();

	public static object get_image_angle(GamemakerObject instance) => instance.image_angle;
	public static void set_image_angle(GamemakerObject instance, object? value) => instance.image_angle = value.Conv<double>();

	public static object get_speed(GamemakerObject instance) => instance.speed;
	public static void set_speed(GamemakerObject instance, object? value) => instance.speed = value.Conv<double>();

	public static object get_hspeed(GamemakerObject instance) => instance.hspeed;
	public static void set_hspeed(GamemakerObject instance, object? value) => instance.hspeed = value.Conv<double>();

	public static object get_vspeed(GamemakerObject instance) => instance.vspeed;
	public static void set_vspeed(GamemakerObject instance, object? value)
	{
		//DebugLog.Log($"{instance.Definition.Name} set vspeed to {value}");
		instance.vspeed = value.Conv<double>();
	}

	public static object get_direction(GamemakerObject instance) => instance.direction;
	public static void set_direction(GamemakerObject instance, object? value) => instance.direction = value.Conv<double>();

	public static object get_view_current() => 0; // TODO : aghhhhh viewports aghhh

	public static object get_persistent(GamemakerObject instance) => instance.persistent;
	public static void set_persistent(GamemakerObject instance, object? value) => instance.persistent = value.Conv<bool>();

	public static object get_id(GamemakerObject instance) => instance.instanceId;

	public static object get_gravity(GamemakerObject instance) => instance.gravity;
	public static void set_gravity(GamemakerObject instance, object? value) => instance.gravity = value.Conv<double>();

	public static object get_friction(GamemakerObject instance) => instance.friction;
	public static void set_friction(GamemakerObject instance, object? value) => instance.friction = value.Conv<double>();

	public static object get_gravity_direction(GamemakerObject instance) => instance.gravity_direction;
	public static void set_gravity_direction(GamemakerObject instance, object? value) => instance.gravity_direction = value.Conv<double>();

	public static object get_image_number(GamemakerObject instance) => SpriteManager.GetNumberOfFrames(instance.sprite_index);

	// public static object get_room_persistent() => RoomManager.CurrentRoom.Persistent;
	// public static void set_room_persistent(object? value) => RoomManager.CurrentRoom.Persistent = VMExecutor.Conv<bool>(value);

	public static object get_room_speed() => Entry.GameSpeed;
	public static void set_room_speed(object? value) => Entry.SetGameSpeed(value.Conv<int>());

	public static object get_os_type() => 0; // TODO : Check if this is actually os_windows

	public static object get_application_surface() => SurfaceManager.application_surface;

	public static object get_alarm(GamemakerObject instance) => instance.alarm;
	public static void set_alarm(GamemakerObject instance, object? value) => instance.alarm = value.Conv<IList>().Cast<object?>().ToArray();

	public static object get_argument_count() => VMExecutor.CurrentCall.Locals["arguments"].Conv<IList>().Count;
	public static object get_argument() => VMExecutor.CurrentCall.Locals["arguments"]!;
	public static object? get_argument_0() => VMExecutor.CurrentCall.Locals["arguments"].Conv<IList>()[0];
	public static object? get_argument_1() => VMExecutor.CurrentCall.Locals["arguments"].Conv<IList>()[1];
	public static object? get_argument_2() => VMExecutor.CurrentCall.Locals["arguments"].Conv<IList>()[2];
	public static object? get_argument_3() => VMExecutor.CurrentCall.Locals["arguments"].Conv<IList>()[3];
	public static object? get_argument_4() => VMExecutor.CurrentCall.Locals["arguments"].Conv<IList>()[4];
	public static object? get_argument_5() => VMExecutor.CurrentCall.Locals["arguments"].Conv<IList>()[5];
	public static object? get_argument_6() => VMExecutor.CurrentCall.Locals["arguments"].Conv<IList>()[6];
	public static object? get_argument_7() => VMExecutor.CurrentCall.Locals["arguments"].Conv<IList>()[7];
	public static object? get_argument_8() => VMExecutor.CurrentCall.Locals["arguments"].Conv<IList>()[8];
	public static object? get_argument_9() => VMExecutor.CurrentCall.Locals["arguments"].Conv<IList>()[9];
	public static object? get_argument_10() => VMExecutor.CurrentCall.Locals["arguments"].Conv<IList>()[10];
	public static object? get_argument_11() => VMExecutor.CurrentCall.Locals["arguments"].Conv<IList>()[11];
	public static object? get_argument_12() => VMExecutor.CurrentCall.Locals["arguments"].Conv<IList>()[12];
	public static object? get_argument_13() => VMExecutor.CurrentCall.Locals["arguments"].Conv<IList>()[13];
	public static object? get_argument_14() => VMExecutor.CurrentCall.Locals["arguments"].Conv<IList>()[14];
	public static object? get_argument_15() => VMExecutor.CurrentCall.Locals["arguments"].Conv<IList>()[15];

	public static object? get_undefined() => null;

	public static object get_view_wport() => ViewportManager.view_wport;
	public static void set_view_wport(object? value) => ViewportManager.view_wport = value.Conv<IList>().Cast<int>().ToArray();

	public static object get_view_hport() => ViewportManager.view_hport;
	public static void set_view_hport(object? value) => ViewportManager.view_hport = value.Conv<IList>().Cast<int>().ToArray();

	public static object get_view_xview() => ViewportManager.view_xview;
	public static void set_view_xview(object? value) => ViewportManager.view_xview = value.Conv<IList>().Cast<double>().ToArray();

	public static object get_view_yview() => ViewportManager.view_yview;
	public static void set_view_yview(object? value) => ViewportManager.view_yview = value.Conv<IList>().Cast<double>().ToArray();

	public static object get_view_wview() => ViewportManager.view_wview;
	public static void set_view_wview(object? value) => ViewportManager.view_wview = value.Conv<IList>().Cast<int>().ToArray();

	public static object get_view_hview() => ViewportManager.view_hview;
	public static void set_view_hview(object? value) => ViewportManager.view_hview = value.Conv<IList>().Cast<int>().ToArray();

	public static object get_view_camera() => ViewportManager.view_camera;
	public static void set_view_camera(object? value) => ViewportManager.view_camera = value.Conv<IList>().Cast<int>().ToArray();

	public static object? get_pointer_null() => null;

	public static object get_xprevious(GamemakerObject instance) => instance.xprevious;
	public static void set_xprevious(GamemakerObject instance, object? value) => instance.xprevious = value.Conv<double>();

	public static object get_yprevious(GamemakerObject instance) => instance.yprevious;
	public static void set_yprevious(GamemakerObject instance, object? value) => instance.yprevious = value.Conv<double>();

	public static object get_sprite_xoffset(GamemakerObject instance) => instance.sprite_xoffset;
	public static object get_sprite_yoffset(GamemakerObject instance) => instance.sprite_yoffset;

	public static object get_path_index(GamemakerObject instance) => instance.path_index;

	public static object get_path_position(GamemakerObject instance) => instance.path_position;
	public static void set_path_position(GamemakerObject instance, object? value) => instance.path_position = value.Conv<double>();

	public static object get_path_speed(GamemakerObject instance) => instance.path_speed;
	public static void set_path_speed(GamemakerObject instance, object? value) => instance.path_speed = value.Conv<double>();

	public static object get_path_scale(GamemakerObject instance) => instance.path_scale;
	public static void set_path_scale(GamemakerObject instance, object? value) => instance.path_scale = value.Conv<double>();

	public static object get_instance_count() => InstanceManager.instances.Count; // TODO : this should only count instances at the START of the step

	public static object get_current_time() => (DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime()).TotalMilliseconds; // TODO : do this in a better way

	public static object get_debug_mode() => false;
}
