namespace OpenGM.VirtualMachine.BuiltInFunctions;

public static class GameFunctions
{
	// move_random
	// place_free
	// place_empty

	[GMLFunction("place_meeting")]
	public static object place_meeting(object?[] args)
	{
		var x = args[0].Conv<double>();
		var y = args[1].Conv<double>();
		var obj = args[2].Conv<int>(); // TODO : this can be an array, or "all" or "other", or tile map stuff

		var result = CollisionManager.Command_InstancePlace(VMExecutor.Self.GMSelf, x, y, obj);

		return result >= 0;
	}

	// place_snapped
	// move_snap

	[GMLFunction("move_towards_point")]
	public static object? move_towards_point(object?[] args)
	{
		var targetx = args[0].Conv<double>();
		var targety = args[1].Conv<double>();
		var sp = args[2].Conv<double>();

		VMExecutor.Self.GMSelf.direction = MathFunctions.point_direction(VMExecutor.Self.GMSelf.x, VMExecutor.Self.GMSelf.y, targetx, targety).Conv<double>();
		VMExecutor.Self.GMSelf.speed = sp;

		return null;
	}

	// move_contact_solid
	// move_contact_all
	// move_outside_solid
	// move_outside_all
	// move_bounce_solid
	// move_bounce_all
	// move_wrap
	// motion_set

	[GMLFunction("motion_add")]
	public static object? motion_add(object?[] args)
	{
		double ClampFloat(double value)
		{
			return Math.Floor(value * 1000000) / 1000000.0;
		}

		var dir = args[0].Conv<double>();
		var speed = args[1].Conv<double>();

		var self = VMExecutor.Self.GMSelf;

		self.hspeed += speed * ClampFloat(Math.Cos(dir * CustomMath.Deg2Rad));
		self.vspeed -= speed * ClampFloat(Math.Sin(dir * CustomMath.Deg2Rad));

		return null;
	}

	[GMLFunction("distance_to_point")]
	public static object distance_to_point(object?[] args)
	{
		var x = args[0].Conv<double>();
		var y = args[1].Conv<double>();

		var self = VMExecutor.Self.GMSelf;

		if (self.mask_index == -1 && self.sprite_index == -1)
		{
			// TODO : Docs just say this means the result will be "incorrect". Wtf does that mean???
			// just assuming it does point_distance

			var horizDistance = Math.Abs(self.x - x);
			var vertDistance = Math.Abs(self.y - y);

			return Math.Sqrt((horizDistance * horizDistance) + (vertDistance * vertDistance));
		}

		var centerX = (self.bbox_left + self.bbox_right) / 2.0;
		var centerY = (self.bbox_top + self.bbox_bottom) / 2.0;
		var width = self.bbox_right - self.bbox_left;
		var height = self.bbox_bottom - self.bbox_top;

		var dx = Math.Max(Math.Abs(x - centerX) - (width / 2.0), 0);
		var dy = Math.Max(Math.Abs(y - centerY) - (height / 2.0), 0);
		return Math.Sqrt((dx * dx) + (dy * dy));
	}

	[GMLFunction("distance_to_object")]
	public static object distance_to_object(object?[] args)
	{
		var obj = args[0].Conv<int>();

		GamemakerObject objToCheck = null!;

		if (obj == GMConstants.other)
		{
			throw new NotImplementedException();
		}
		else if (obj < GMConstants.FIRST_INSTANCE_ID)
		{
			// object index
			objToCheck = InstanceManager.FindByAssetId(obj).First();
		}
		else
		{
			// instance id
			objToCheck = InstanceManager.FindByInstanceId(obj)!;
		}

		// compute bounding boxes if needed

		var self = VMExecutor.Self.GMSelf;

		var xd = 0d;
		var yd = 0d;

		if (self.bbox_left > objToCheck.bbox_right)
		{
			xd = self.bbox_left - objToCheck.bbox_right;
		}

		if (self.bbox_right < objToCheck.bbox_left)
		{
			xd = self.bbox_right - objToCheck.bbox_left;
		}

		if (self.bbox_top > objToCheck.bbox_bottom)
		{
			yd = self.bbox_top - objToCheck.bbox_bottom;
		}

		if (self.bbox_bottom < objToCheck.bbox_top)
		{
			yd = self.bbox_bottom - objToCheck.bbox_top;
		}

		return Math.Sqrt((xd * xd) + (yd * yd));
	}

	[GMLFunction("path_start")]
	public static object? path_start(object?[] args)
	{
		var path = args[0].Conv<int>();
		var speed = args[1].Conv<double>();
		var endaction = (PathEndAction)args[2].Conv<int>();
		var absolute = args[3].Conv<bool>();

		VMExecutor.Self.GMSelf.AssignPath(path, speed, 1, 0, absolute, endaction);

		return null;
	}

	[GMLFunction("path_end")]
	public static object? path_end(object?[] args)
	{
		VMExecutor.Self.GMSelf.path_index = -1;
		return null;
	}

	// mp_linear_step
	// mp_linear_path
	// mp_linear_step_object
	// mp_linear_path_object
	// mp_potential_settings
	// mp_potential_step
	// mp_potential_path
	// mp_potential_step_object
	// mp_potential_path_object

	// ...

	[GMLFunction("collision_point")]
	public static object? collision_point(object?[] args)
	{
		var x = args[0].Conv<double>();
		var y = args[1].Conv<double>();
		var obj = args[2].Conv<int>(); // TODO : this can be an array, or "all" or "other", or tile map stuff
		var prec = args[3].Conv<bool>();
		var notme = args[4].Conv<bool>();

		return CollisionManager.Command_CollisionPoint(VMExecutor.Self.GMSelf, x, y, obj, prec, notme);
	}

	// collision_point_list

	[GMLFunction("collision_rectangle")]
	public static object collision_rectangle(object?[] args)
	{
		var x1 = args[0].Conv<double>();
		var y1 = args[1].Conv<double>();
		var x2 = args[2].Conv<double>();
		var y2 = args[3].Conv<double>();
		var obj = args[4].Conv<int>(); // TODO : this can be an array, or "all" or "other", or tile map stuff
		var prec = args[5].Conv<bool>();
		var notme = args[6].Conv<bool>();

		return CollisionManager.Command_CollisionRectangle(VMExecutor.Self.GMSelf, x1, y1, x2, y2, obj, prec, notme);
	}

	// collision_rectangle_list

	[GMLFunction("collision_circle")]
	public static object? collision_circle(object?[] args)
	{
		var x = args[0].Conv<double>();
		var y = args[1].Conv<double>();
		var rad = args[2].Conv<double>();
		var obj = args[3].Conv<int>();
		var prec = args[4].Conv<bool>();
		var notme = args[5].Conv<bool>();

		return CollisionManager.Command_CollisionCircle(VMExecutor.Self.GMSelf, x, y, rad, obj, prec, notme);
	}

	// collision_circle_list

	[GMLFunction("collision_ellipse")]
	public static object? collision_ellipse(object?[] args)
	{
		var x1 = args[0].Conv<double>();
		var y1 = args[1].Conv<double>();
		var x2 = args[2].Conv<double>();
		var y2 = args[3].Conv<double>();
		var obj = args[4].Conv<int>();
		var prec = args[5].Conv<bool>();
		var notme = args[6].Conv<bool>();

		return CollisionManager.Command_CollisionEllipse(VMExecutor.Self.GMSelf, x1, y1, x2, y2, obj, prec, notme);
	}

	// collision_ellipse_list

	[GMLFunction("collision_line")]
	public static object? collision_line(object?[] args)
	{
		var x1 = args[0].Conv<double>();
		var y1 = args[1].Conv<double>();
		var x2 = args[2].Conv<double>();
		var y2 = args[3].Conv<double>();
		var obj = args[4].Conv<int>(); // TODO : this can be an array, or "all" or "other", or tile map stuff
		var prec = args[5].Conv<bool>();
		var notme = args[6].Conv<bool>();

		return CollisionManager.Command_CollisionLine(VMExecutor.Self.GMSelf, x1, y1, x2, y2, obj, prec, notme);
	}

	// collision_line_list

	[GMLFunction("instance_find")]
	public static object instance_find(object?[] args)
	{
		var obj = args[0].Conv<int>();
		var n = args[1].Conv<int>();

		/*
		 * todo : this is really fucked.
		 * "You specify the object that you want to find the instance of and a number,
		 * and if there is an instance at that position in the instance list then the function
		 * returns the id of that instance, and if not it returns the special keyword noone.
		 *
		 * You can also use the keyword all to iterate through all the instances in a room,
		 * as well as a parent object to iterate through all the instances that are part of
		 * that parent / child hierarchy, and you can even specify an instance (if you have its id)
		 * as a check to see if it actually exists in the current room."
		 */

		if (obj == GMConstants.self)
		{
			return VMExecutor.Self.GMSelf.instanceId;
		}
		else if (obj == GMConstants.other)
		{
			return VMExecutor.Other.GMSelf.instanceId;
		}
		else if (obj == GMConstants.all)
		{
			return InstanceManager.instances.ElementAt(n).Value.instanceId;
		}
		else if (obj >= GMConstants.FIRST_INSTANCE_ID)
		{
			// is an instance id
			// todo : implement
			throw new NotImplementedException();
		}
		else
		{
			// is an object index
			var instances = InstanceManager.instances.Values.Where(x => x.object_index == obj).ToArray();

			if (n >= instances.Length)
			{
				return GMConstants.noone;
			}

			var instance = instances[n];
			return instance.instanceId;
		}

		//return GMConstants.noone;
	}

	[GMLFunction("instance_exists")]
	public static object instance_exists(object?[] args)
	{
		var obj = args[0].Conv<int>();

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

	[GMLFunction("instance_number")]
	public static object instance_number(object?[] args)
	{
		var obj = args[0].Conv<int>();
		return InstanceManager.instance_number(obj);
	}

	// instance_position
	// instance_position_list

	[GMLFunction("instance_nearest")]
	public static object instance_nearest(object?[] args)
	{
		var x = args[0].Conv<double>();
		var y = args[1].Conv<double>();
		var obj = args[2].Conv<int>();

		var id = GMConstants.noone;
		var distance = 10000000000d;
		foreach (var (instanceId, instance) in InstanceManager.instances)
		{
			if (!InstanceManager.HasAssetInParents(instance.Definition, obj))
			{
				continue;
			}

			var dx = x - instance.x;
			var dy = y - instance.y;
			var dist = Math.Sqrt(dx * dx + dy * dy);
			if (dist < distance)
			{
				id = instance.instanceId;
				distance = dist;
			}
		}

		return id;
	}

	// instance_furthest

	[GMLFunction("instance_place")]
	public static object instance_place(object?[] args)
	{
		var x = args[0].Conv<double>();
		var y = args[1].Conv<double>();
		var obj = args[2].Conv<int>(); // TODO : this can be an array, or "all" or "other", or tile map stuff

		return CollisionManager.Command_InstancePlace(VMExecutor.Self.GMSelf, x, y, obj);
	}

	// instance_place_list

	[GMLFunction("instance_create_depth")]
	public static object instance_create_depth(object?[] args)
	{
		var x = args[0].Conv<double>();
		var y = args[1].Conv<double>();
		var depth = args[2].Conv<int>();
		var obj = args[3].Conv<int>();

		return InstanceManager.instance_create_depth(x, y, depth, obj);
	}

	// instance_create_layer
	// instance_copy

	[GMLFunction("instance_change")]
	public static object? instance_change(object?[] args)
	{
		var obj = args[0].Conv<int>();
		var perf = args[1].Conv<bool>();

		var self = VMExecutor.Self.GMSelf;

		if (perf)
		{
			GamemakerObject.ExecuteEvent(self, self.Definition, EventType.Destroy);
			GamemakerObject.ExecuteEvent(self, self.Definition, EventType.CleanUp);
		}

		var definition = InstanceManager.ObjectDefinitions[obj];

		self.Definition = definition;
		self.sprite_index = definition.sprite;
		self.bbox_dirty = true;

		if (perf)
		{
			GamemakerObject.ExecuteEvent(self, self.Definition, EventType.PreCreate);
			GamemakerObject.ExecuteEvent(self, self.Definition, EventType.Create);
		}

		return null;
	}

	[GMLFunction("instance_destroy")]
	public static object? instance_destroy(object?[] args)
	{
		if (args.Length == 0)
		{
			//GamemakerObject.ExecuteEvent(VMExecutor.Self.GMSelf, VMExecutor.Self.ObjectDefinition, EventType.Destroy);
			//GamemakerObject.ExecuteEvent(VMExecutor.Self.GMSelf, VMExecutor.Self.ObjectDefinition, EventType.CleanUp);
			//InstanceManager.instance_destroy(VMExecutor.Self.GMSelf);
			InstanceManager.MarkForDestruction(VMExecutor.Self.GMSelf, true);
			return null;
		}

		var id = args[0].Conv<int>();
		var execute_event_flag = true;

		if (args.Length == 2)
		{
			execute_event_flag = args[1].Conv<bool>();
		}

		if (id == GMConstants.noone)
		{
			// ??? wtf
			return null;
		}

		if (id < GMConstants.FIRST_INSTANCE_ID)
		{
			// asset index
			var instances = InstanceManager.FindByAssetId(id);

			foreach (var instance in instances)
			{
				InstanceManager.MarkForDestruction(instance, execute_event_flag);
				/*if (execute_event_flag)
				{
					GamemakerObject.ExecuteEvent(instance, instance.Definition, EventType.Destroy);
				}

				GamemakerObject.ExecuteEvent(instance, instance.Definition, EventType.CleanUp);

				InstanceManager.instance_destroy(instance);*/
			}
		}
		else
		{
			// instance id
			var instance = InstanceManager.FindByInstanceId(id);

			InstanceManager.MarkForDestruction(instance, execute_event_flag);

			/*if (instance == null)
			{
				DebugLog.LogError($"Tried to run instance_destroy on an instanceId that no longer exists!!!");
				return null;
			}

			if (execute_event_flag)
			{
				GamemakerObject.ExecuteEvent(instance!, instance!.Definition, EventType.Destroy);
			}

			GamemakerObject.ExecuteEvent(instance!, instance!.Definition, EventType.CleanUp);

			InstanceManager.instance_destroy(instance);*/
		}

		return null;
	}

	// position_empty
	// position_meeting
	// position_destroy
	// position_change
	// instance_id_get
	// instance_deactivate_all

	[GMLFunction("instance_deactivate_object")]
	public static object? instance_deactivate_object(object?[] args)
	{
		var obj = args[0].Conv<int>();

		if (obj < GMConstants.FIRST_INSTANCE_ID)
		{
			// asset id
			var instances = InstanceManager.FindByAssetId(obj);
			foreach (var instance in instances)
			{
				instance.Active = false;
			}
		}
		else
		{
			// instance id
			var instance = InstanceManager.FindByInstanceId(obj)!;
			instance.Active = false;
		}

		return null;
	}

	// instance_deactivate_region
	// instance_activate_all

	[GMLFunction("instance_activate_object")]
	public static object? instance_activate_object(object?[] args)
	{
		var obj = args[0].Conv<int>();

		if (obj < GMConstants.FIRST_INSTANCE_ID)
		{
			// asset id
			var instances = InstanceManager.FindByAssetId(obj);
			foreach (var instance in instances)
			{
				instance.Active = true;
			}
		}
		else
		{
			// instance id
			var instance = InstanceManager.FindByInstanceId(obj)!;
			instance.Active = true;
		}

		return null;
	}

	// instance_activate_region

	[GMLFunction("room_goto")]
	public static object? room_goto(object?[] args)
	{
		var index = args[0].Conv<int>();
		RoomManager.New_Room = index;
		return null;
	}

	[GMLFunction("room_goto_previous")]
	public static object? room_goto_previous(object?[] args)
	{
		RoomManager.room_goto_previous();
		return null;
	}

	[GMLFunction("room_goto_next")]
	public static object? room_goto_next(object?[] args)
	{
		RoomManager.room_goto_next();
		return null;
	}

	[GMLFunction("room_previous")]
	public static object room_previous(object?[] args)
	{
		var numb = args[0].Conv<int>();
		return RoomManager.room_previous(numb);
	}

	[GMLFunction("room_next")]
	public static object room_next(object?[] args)
	{
		var numb = args[0].Conv<int>();
		return RoomManager.room_next(numb);
	}

	[GMLFunction("room_restart")]
	public static object? room_restart(object?[] args)
	{
		RoomManager.New_Room = RoomManager.CurrentRoom.AssetId;
		return null;
	}

	// game_end

	[GMLFunction("game_restart")]
	public static object? game_restart(object?[] args)
	{
		RoomManager.New_Room = GMConstants.ROOM_RESTARTGAME;
		return null;
	}

	// game_load
	// game_save
	// game_save_buffer
	// game_load_buffer
	// transition_define
	// transition_exists
	// sleep
	// scheduler_resolution_get
	// scheduler_resolution_set

	[GMLFunction("point_in_rectangle")]
	public static object point_in_rectangle(object?[] args)
	{
		var px = args[0].Conv<double>();
		var py = args[1].Conv<double>();
		var x1 = args[2].Conv<double>();
		var y1 = args[3].Conv<double>();
		var x2 = args[4].Conv<double>();
		var y2 = args[5].Conv<double>();

		return x1 <= px && px < x2 && y1 <= py && py <= y2;
	}

	// point_in_triangle
	// point_in_circle

	[GMLFunction("rectangle_in_rectangle")]
	public static object? rectangle_in_rectangle(object?[] args)
	{
		var _px1 = args[0].Conv<double>();
		var _py1 = args[1].Conv<double>();
		var _px2 = args[2].Conv<double>();
		var _py2 = args[3].Conv<double>();
		var _x1 = args[4].Conv<double>();
		var _y1 = args[5].Conv<double>();
		var _x2 = args[6].Conv<double>();
		var _y2 = args[7].Conv<double>();

		// https://github.com/YoYoGames/GameMaker-HTML5/blob/773ffbfff0b6d7895fcab664e5190da9001a5491/scripts/functions/Function_Collision.js#L837

		var IN = 0;

		if (_px1 > _px2)
		{
			(_px1, _px2) = (_px2, _px1);
		}

		if (_py1 > _py2)
		{
			(_py1, _py2) = (_py2, _py1);
		}

		if (_x1 > _x2)
		{
			(_x1, _x2) = (_x2, _x1);
		}

		if (_y1 > _y2)
		{
			(_y1, _y2) = (_y2, _y1);
		}

		// Test point in rect
		if ((_px1 >= _x1 && _px1 <= _x2) && (_py1 >= _y1 && _py1 <= _y2))
		{
			IN |= 1;
		}

		if ((_px2 >= _x1 && _px2 <= _x2) && (_py1 >= _y1 && _py1 <= _y2))
		{
			IN |= 2;
		}

		if ((_px2 >= _x1 && _px2 <= _x2) && (_py2 >= _y1 && _py2 <= _y2))
		{
			IN |= 4;
		}

		if ((_px1 >= _x1 && _px1 <= _x2) && (_py2 >= _y1 && _py2 <= _y2))
		{
			IN |= 8;
		}

		var result = 0;

		if (IN == 15)
		{
			result = 1;
		}
		else if (IN == 0)
		{
			result = 0;
			// now for edge cases.. source being intersected by dest 
			IN = 0;
			if ((_x1 >= _px1 && _x1 <= _px2) && (_y1 >= _py1 && _y1 <= _py2))
				IN |= 1;
			if ((_x2 >= _px1 && _x2 <= _px2) && (_y1 >= _py1 && _y1 <= _py2))
				IN |= 2;
			if ((_x2 >= _px1 && _x2 <= _px2) && (_y2 >= _py1 && _y2 <= _py2))
				IN |= 4;
			if ((_x1 >= _px1 && _x1 <= _px2) && (_y2 >= _py1 && _y2 <= _py2))
				IN |= 8;
			if (0 != IN)
				result = 2;
			else
			{ // lets try another case, source goes over dest in x axis
				IN = 0;
				if ((_x1 >= _px1 && _x1 <= _px2) && (_py1 >= _y1 && _py1 <= _y2))
					IN |= 1;
				if ((_x2 >= _px1 && _x2 <= _px2) && (_py1 >= _y1 && _py1 <= _y2))
					IN |= 2;
				if ((_x2 >= _px1 && _x2 <= _px2) && (_py2 >= _y1 && _py2 <= _y2))
					IN |= 4;
				if ((_x1 >= _px1 && _x1 <= _px2) && (_py2 >= _y1 && _py2 <= _y2))
					IN |= 8;
				if (0 != IN)
					result = 2;
				else
				{ // one more case, source goes over dest in y axis
					IN = 0;
					if ((_px1 >= _x1 && _px1 <= _x2) && (_y1 >= _py1 && _y1 <= _py2))
						IN |= 1;
					if ((_px2 >= _x1 && _px2 <= _x2) && (_y1 >= _py1 && _y1 <= _py2))
						IN |= 2;
					if ((_px2 >= _x1 && _px2 <= _x2) && (_y2 >= _py1 && _y2 <= _py2))
						IN |= 4;
					if ((_px1 >= _x1 && _px1 <= _x2) && (_y2 >= _py1 && _y2 <= _py2))
						IN |= 8;
					if (0 != IN)
						result = 2;
				}
			}
		}
		else
		{
			result = 2;
		}

		return result;
	}

	// rectangle_in_triangle
	// rectangle_in_circle
}