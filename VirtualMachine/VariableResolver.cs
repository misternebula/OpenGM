namespace DELTARUNITYStandalone.VirtualMachine;
public static class VariableResolver
{
	public static object ArrayGet(int index,
		Func<List<object>> get)
	{
		return get()[index];
	}

	public static void ArraySet(int index, object value,
		Func<object> get,
		Action<List<object>> set,
		Func<bool> contains)
	{
		List<object> list;
		if (!contains() || get() is not List<object>)
		{
			list = new List<object>();
			set(list);
		}
		else
		{
			list = (List<object>)get();
		}

		if (index >= list.Count)
		{
			list.AddRange(new object[index - list.Count + 1]);
		}

		list[index] = value;
	}

	public static readonly Dictionary<string, object> GlobalVariables = new();

	public static void SetGlobalArrayIndex(string name, int index, object value)
	{
		ArraySet(index, value,
			() => GetGlobalVariable(name),
			list => SetGlobalVariable(name, list),
			() => GlobalVariables.ContainsKey(name));
	}

	public static object GetGlobalArrayIndex(string name, int index)
	{
		try
		{
			return ArrayGet(index,
				() => (List<object>)GlobalVariables[name]);
		}
		catch (Exception e)
		{
			DebugLog.LogError($"Tried to index {name} with out-of-bounds index {index} (Count is {((List<object>)GlobalVariables[name]).Count})");
			throw;
		}
	}

	public static void SetGlobalVariable(string name, object value)
	{
		GlobalVariables[name] = value;
	}

	public static object GetGlobalVariable(string name)
	{
		if (!GlobalVariableExists(name))
		{
			DebugLog.LogError($"Trying to get global variable {name} which doesn't exist.");
			return null;
		}

		return GlobalVariables[name];
	}

	public static bool GlobalVariableExists(string name)
	{
		return GlobalVariables.ContainsKey(name);
	}

	public static object GetSelfVariable(GamemakerObject self, Dictionary<string, object> locals, string name)
	{
		if (name == "argument_count")
		{
			return ((List<object>)locals["arguments"]).Count;
		}

		if (name == "argument")
		{
			return (List<object>)locals["arguments"];
		}

		if (name.StartsWith("argument"))
		{
			var withoutArgument = name.Substring("argument".Length);
			if (int.TryParse(withoutArgument, out var index))
			{
				return ((List<object>)locals["arguments"])[index];
			}
		}

		// global builtins are also self for some reason
		if (BuiltInVariables.ContainsKey(name))
		{
			return BuiltInVariables[name].getter(self);
		}

		return self.SelfVariables[name];
	}

	public static void SetSelfVariable(GamemakerObject self, string name, object value)
	{
		// TODO: should this also set arguments????

		if (BuiltInVariables.ContainsKey(name))
		{
			BuiltInVariables[name].setter(self, value);
			return;
		}

		if (!self.SelfVariables.ContainsKey(name))
		{
			//DebugLog.LogWarning($"Creating variable {name} with value of {value} for {self.object_index}");
		}

		self.SelfVariables[name] = value;
	}

	public static bool ContainsSelfVariable(GamemakerObject self, Dictionary<string, object> locals, string name)
	{
		if (name.StartsWith("argument"))
		{
			return locals.ContainsKey("arguments");
			// TODO: should this check for index (eg argument3 if less than 3 arguments)????
			// should this even be checked at all?? idk
		}

		return BuiltInVariables.ContainsKey(name) || self.SelfVariables.ContainsKey(name);
	}

	public static Dictionary<string, (Func<GamemakerObject, object> getter, Action<GamemakerObject, object> setter)> BuiltInVariables = new()
	{
		{ "working_directory", (get_working_directory, null) },
		{ "fps", (get_fps, null) },
		{ "x", (get_x, set_x) },
		{ "y", (get_y, set_y) },
		{ "room_width", (get_room_width, null)},
		{ "room_height", (get_room_height, null)},
		{ "image_index", (get_image_index, set_image_index)},
		{ "sprite_index", (get_sprite_index, set_sprite_index)},
		{ "sprite_height", (get_sprite_height, null)},
		{ "sprite_width", (get_sprite_width, null)},
		{ "xstart", (get_xstart, set_xstart)},
		{ "ystart", (get_ystart, set_ystart)},
		{ "object_index", (get_object_index, null)},
		{ "image_blend", (get_image_blend, set_image_blend)},
		{ "depth", (get_depth, set_depth)},
		{ "room", (get_room, set_room)},
		{ "bbox_bottom", (get_bbox_bottom, null)},
		{ "bbox_top", (get_bbox_top, null)},
		{ "bbox_left", (get_bbox_left, null)},
		{ "bbox_right", (get_bbox_right, null)},
		{ "image_yscale", (get_image_yscale, set_image_yscale)},
		{ "image_xscale", (get_image_xscale, set_image_xscale)},
		{ "image_speed", (get_image_speed, set_image_speed)},
		{ "visible", (get_visible, set_visible)},
		{ "image_alpha", (get_image_alpha, set_image_alpha)},
		{ "image_angle", (get_image_angle, set_image_angle)},
		{ "speed", (get_speed, set_speed)},
		{ "hspeed", (get_hspeed, set_hspeed)},
		{ "vspeed", (get_vspeed, set_vspeed)},
		{ "direction", (get_direction, set_direction)},
		{ "view_current", (get_view_current, null)},
		{ "persistent", (get_persistent, set_persistent) },
		{ "id", (get_id, null) },
		{ "gravity", (get_gravity, set_gravity) },
		{ "friction", (get_friction, set_friction) },
		{ "gravity_direction", (get_gravity_direction, set_gravity_direction)},
		{ "image_number", (get_image_number, null)},
		{ "room_speed", (get_room_speed, set_room_speed)},
		{ "os_type", (get_os_type, null)},
		{ "application_surface", (get_application_surface, null)},
		//{ "room_persistent", (get_room_persistent, set_room_persistent)}
	};

	public static object get_working_directory(GamemakerObject instance)
	{
		return Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar;
	}

	public static object get_fps(GamemakerObject instance)
	{
		return Entry.GameSpeed; // TODO : this shouldnt be the desired fps, but the current fps (fluctuating)
	}

	public static object get_x(GamemakerObject instance) => instance.x;

	public static void set_x(GamemakerObject instance, object value) => instance.x = VMExecutor.Conv<double>(value);
	public static object get_y(GamemakerObject instance) => instance.y;
	public static void set_y(GamemakerObject instance, object value) => instance.y = VMExecutor.Conv<double>(value);

	public static object get_room_width(GamemakerObject instance) => (double)RoomManager.CurrentRoom.SizeX;
	public static object get_room_height(GamemakerObject instance) => (double)RoomManager.CurrentRoom.SizeY;

	public static object get_image_index(GamemakerObject instance) => instance.image_index;
	public static void set_image_index(GamemakerObject instance, object value) => instance.image_index = VMExecutor.Conv<double>(value);

	public static object get_sprite_index(GamemakerObject instance) => instance.sprite_index;
	public static void set_sprite_index(GamemakerObject instance, object value) => instance.sprite_index = VMExecutor.Conv<int>(value);

	public static object get_sprite_width(GamemakerObject instance) => instance.sprite_width;
	public static object get_sprite_height(GamemakerObject instance) => instance.sprite_height;

	public static object get_xstart(GamemakerObject instance) => instance.xstart;
	public static void set_xstart(GamemakerObject instance, object value) => instance.xstart = VMExecutor.Conv<double>(value);
	public static object get_ystart(GamemakerObject instance) => instance.ystart;
	public static void set_ystart(GamemakerObject instance, object value) => instance.ystart = VMExecutor.Conv<double>(value);

	public static object get_object_index(GamemakerObject instance) => instance.object_index;

	public static object get_image_blend(GamemakerObject instance) => instance.image_blend;
	public static void set_image_blend(GamemakerObject instance, object value) => instance.image_blend = VMExecutor.Conv<int>(value);

	public static object get_depth(GamemakerObject instance) => instance.depth;
	public static void set_depth(GamemakerObject instance, object value) => instance.depth = VMExecutor.Conv<double>(value);

	public static object get_room(GamemakerObject instance) => RoomManager.CurrentRoom.AssetId;

	public static object get_bbox_bottom(GamemakerObject instance) => (instance.sprite_index == -1 && instance.mask_id == -1) ? instance.y : instance.bbox_bottom;
	public static object get_bbox_top(GamemakerObject instance) => (instance.sprite_index == -1 && instance.mask_id == -1) ? instance.y : instance.bbox_top;
	public static object get_bbox_left(GamemakerObject instance) => (instance.sprite_index == -1 && instance.mask_id == -1) ? instance.x : instance.bbox_left;
	public static object get_bbox_right(GamemakerObject instance) => (instance.sprite_index == -1 && instance.mask_id == -1) ? instance.x : instance.bbox_right;

	public static object get_image_yscale(GamemakerObject instance) => instance.image_yscale;
	public static void set_image_yscale(GamemakerObject instance, object value) => instance.image_yscale = VMExecutor.Conv<double>(value);

	public static object get_image_xscale(GamemakerObject instance) => instance.image_xscale;
	public static void set_image_xscale(GamemakerObject instance, object value) => instance.image_xscale = VMExecutor.Conv<double>(value);

	public static void set_room(GamemakerObject instance, object value)
	{
		RoomManager.ChangeRoomAfterEvent(VMExecutor.Conv<int>(value));
	}

	public static object get_image_speed(GamemakerObject instance) => instance.image_speed;
	public static void set_image_speed(GamemakerObject instance, object value) => instance.image_speed = VMExecutor.Conv<double>(value);

	public static object get_visible(GamemakerObject instance) => instance.visible;

	public static void set_visible(GamemakerObject instance, object value)
	{
		instance.visible = VMExecutor.Conv<bool>(value);
	}

	public static object get_image_alpha(GamemakerObject instance) => instance.image_alpha;
	public static void set_image_alpha(GamemakerObject instance, object value) => instance.image_alpha = VMExecutor.Conv<double>(value);

	public static object get_image_angle(GamemakerObject instance) => instance.image_angle;
	public static void set_image_angle(GamemakerObject instance, object value) => instance.image_angle = VMExecutor.Conv<double>(value);

	public static object get_speed(GamemakerObject instance) => instance.speed;
	public static void set_speed(GamemakerObject instance, object value) => instance.speed = VMExecutor.Conv<double>(value);

	public static object get_hspeed(GamemakerObject instance) => instance.hspeed;
	public static void set_hspeed(GamemakerObject instance, object value) => instance.hspeed = VMExecutor.Conv<double>(value);

	public static object get_vspeed(GamemakerObject instance) => instance.vspeed;
	public static void set_vspeed(GamemakerObject instance, object value) => instance.vspeed = VMExecutor.Conv<double>(value);

	public static object get_direction(GamemakerObject instance) => instance.direction;
	public static void set_direction(GamemakerObject instance, object value) => instance.direction = VMExecutor.Conv<double>(value);

	public static object get_view_current(GamemakerObject instance) => 0; // TODO : aghhhhh viewports aghhh

	public static object get_persistent(GamemakerObject instance) => instance.persistent;
	public static void set_persistent(GamemakerObject instance, object value) => instance.persistent = VMExecutor.Conv<bool>(value);

	public static object get_id(GamemakerObject instance) => instance.instanceId;

	public static object get_gravity(GamemakerObject instance) => instance.gravity;
	public static void set_gravity(GamemakerObject instance, object value) => instance.gravity = VMExecutor.Conv<double>(value);

	public static object get_friction(GamemakerObject instance) => instance.friction;
	public static void set_friction(GamemakerObject instance, object value) => instance.friction = VMExecutor.Conv<double>(value);

	public static object get_gravity_direction(GamemakerObject instance) => instance.gravity_direction;
	public static void set_gravity_direction(GamemakerObject instance, object value) => instance.gravity_direction = VMExecutor.Conv<double>(value);

	public static object get_image_number(GamemakerObject instance) => SpriteManager.GetNumberOfFrames(instance.sprite_index);

	//public static object get_room_persistent(GamemakerObject instance) => RoomManager.CurrentRoom.Persistent;
	//public static void set_room_persistent(GamemakerObject instance, object value) => RoomManager.CurrentRoom.Persistent = VMExecutor.Conv<bool>(value);

	public static object get_room_speed(GamemakerObject instance) => Entry.GameSpeed;
	public static void set_room_speed(GamemakerObject instance, object value) => Entry.SetGameSpeed(VMExecutor.Conv<int>(value));

	public static object get_os_type(GamemakerObject instance) => 0; // TODO : Check if this is actually os_windows

	public static object get_application_surface(GamemakerObject instance) => SurfaceManager.application_surface;
}
