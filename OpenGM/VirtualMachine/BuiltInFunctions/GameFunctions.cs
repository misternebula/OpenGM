using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenGM.IO;
using OpenGM.Rendering;
using OpenTK.Mathematics;

namespace OpenGM.VirtualMachine;

public static partial class ScriptResolver
{
	public static bool DrawCollisionChecks = false;

	public static object place_meeting(object?[] args)
	{
		var x = args[0].Conv<double>();
		var y = args[1].Conv<double>();
		var obj = args[2].Conv<int>(); // TODO : this can be an array, or "all" or "other", or tile map stuff

		if (obj < 0)
		{
			throw new NotImplementedException($"{obj} given to place_meeting");
		}

		if (obj < GMConstants.FIRST_INSTANCE_ID)
		{
			return CollisionManager.place_meeting_assetid(x, y, obj, VMExecutor.Ctx.GMSelf);
		}
		else
		{
			return CollisionManager.place_meeting_instanceid(x, y, obj, VMExecutor.Ctx.GMSelf);
		}
	}

	public static object? move_towards_point(object?[] args)
	{
		var targetx = args[0].Conv<double>();
		var targety = args[1].Conv<double>();
		var sp = args[2].Conv<double>();

		VMExecutor.Ctx.GMSelf.direction = (double)point_direction(VMExecutor.Ctx.GMSelf.x, VMExecutor.Ctx.GMSelf.y, targetx, targety);
		VMExecutor.Ctx.GMSelf.speed = sp;

		return null;
	}

	public static object distance_to_point(object?[] args)
	{
		var x = args[0].Conv<double>();
		var y = args[1].Conv<double>();

		var self = VMExecutor.Ctx.GMSelf;

		if (VMExecutor.Ctx.GMSelf.mask_id == -1 && VMExecutor.Ctx.GMSelf.sprite_index == -1)
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

	public static object distance_to_object(object?[] args)
	{
		var obj = args[0].Conv<int>();

		GamemakerObject objToCheck = null!;

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
			objToCheck = InstanceManager.FindByInstanceId(obj)!;
		}

		return CollisionManager.DistanceToObject(VMExecutor.Ctx.GMSelf, objToCheck);
	}

	public static object? path_end(object?[] args)
	{
		VMExecutor.Ctx.GMSelf.path_index = -1;
		return null;
	}

	public static object? collision_point(object?[] args)
	{
		var x = args[0].Conv<double>();
		var y = args[1].Conv<double>();
		var obj = args[2].Conv<int>(); // TODO : this can be an array, or "all" or "other", or tile map stuff
		var prec = args[3].Conv<bool>();
		var notme = args[4].Conv<bool>();

		if (obj == -3)
		{
			throw new NotImplementedException($"{obj} given to collision_point!");
		}
		else if (obj < GMConstants.FIRST_INSTANCE_ID)
		{
			// asset id
		}
		else
		{
			// instance id
			if (!notme || VMExecutor.Ctx.GMSelf.instanceId != obj)
			{
				var testObj = InstanceManager.FindByInstanceId(obj);
				if (testObj != null)
				{
					if (CollisionManager.CollisionPoint(testObj, x, y, prec))
					{
						return testObj;
					}
				}
			}
		}

		return null;
	}

	public static object collision_rectangle(object?[] args)
	{
		var x1 = args[0].Conv<double>();
		var y1 = args[1].Conv<double>();
		var x2 = args[2].Conv<double>();
		var y2 = args[3].Conv<double>();
		var obj = args[4].Conv<int>(); // TODO : this can be an array, or "all" or "other", or tile map stuff
		var prec = args[5].Conv<bool>();
		var notme = args[6].Conv<bool>();

		if (obj < 0)
		{
			throw new NotImplementedException($"{obj} given to collision_rectangle!");
		}

		if (obj < GMConstants.FIRST_INSTANCE_ID)
		{
			return CollisionManager.collision_rectangle_assetid(x1, y1, x2, y2, obj, prec, notme, VMExecutor.Ctx.GMSelf);
		}
		else
		{
			return CollisionManager.collision_rectangle_instanceid(x1, y1, x2, y2, obj, prec, notme, VMExecutor.Ctx.GMSelf);
		}
	}

	public static object? collision_line(object?[] args)
	{
		var x1 = args[0].Conv<double>();
		var y1 = args[1].Conv<double>();
		var x2 = args[2].Conv<double>();
		var y2 = args[3].Conv<double>();
		var obj = args[4].Conv<int>(); // TODO : this can be an array, or "all" or "other", or tile map stuff
		var prec = args[5].Conv<bool>();
		var notme = args[6].Conv<bool>();

		if (obj < 0)
		{
			throw new NotImplementedException($"{obj} given to collision_line!");
		}

		if (obj < GMConstants.FIRST_INSTANCE_ID)
		{
			var instances = InstanceManager.FindByAssetId(obj);

			foreach (var instance in instances)
			{
				if (instance == VMExecutor.Ctx.GMSelf && notme)
				{
					continue;
				}

				var col = CollisionManager.colliders.Single(b => b.GMObject == instance);

				if (CollisionManager.CheckColliderAgainstLine(col, new Vector2d(x1, y1), new Vector2d(x2, y2), prec))
				{
					return instance.instanceId;
				}
			}

			return GMConstants.noone;
		}
		else
		{
			var instance = InstanceManager.FindByInstanceId(obj);

			if (instance == null)
			{
				return GMConstants.noone;
			}

			if (instance == VMExecutor.Ctx.GMSelf && notme)
			{
				return GMConstants.noone;
			}

			var col = CollisionManager.colliders.Single(b => b.GMObject == instance);

			return CollisionManager.CheckColliderAgainstLine(col, new Vector2d(x1, y1), new Vector2d(x2, y2), prec)
				? instance.instanceId
				: GMConstants.noone;
		}
	}

	private static object instance_find(object?[] args)
	{
		var obj = args[0].Conv<int>();
		var n = args[1].Conv<int>();

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
			throw new NotImplementedException();
		}
		else
		{
			// is an object index
			return InstanceManager.instances.Where(x => x.object_index == obj).ElementAt(n).instanceId;
		}

		//return GMConstants.noone;
	}

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

	public static object instance_number(object?[] args)
	{
		var obj = args[0].Conv<int>();
		return InstanceManager.instance_number(obj);
	}

	public static object instance_nearest(object?[] args)
	{
		var x = args[0].Conv<double>();
		var y = args[1].Conv<double>();
		var obj = args[2].Conv<int>();

		var id = GMConstants.noone;
		var distance = 10000000000d;
		foreach (var instance in InstanceManager.instances)
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

	public static object instance_create_depth(object?[] args)
	{
		var x = args[0].Conv<double>();
		var y = args[1].Conv<double>();
		var depth = args[2].Conv<int>();
		var obj = args[3].Conv<int>();

		return InstanceManager.instance_create_depth(x, y, depth, obj);
	}

	public static object? instance_destroy(object?[] args)
	{
		if (args.Length == 0)
		{
			GamemakerObject.ExecuteEvent(VMExecutor.Ctx.GMSelf, VMExecutor.Ctx.ObjectDefinition, EventType.Destroy);
			GamemakerObject.ExecuteEvent(VMExecutor.Ctx.GMSelf, VMExecutor.Ctx.ObjectDefinition, EventType.CleanUp);
			InstanceManager.instance_destroy(VMExecutor.Ctx.GMSelf);
			return null;
		}

		var id = args[0].Conv<int>();
		var execute_event_flag = true;

		if (args.Length == 2)
		{
			execute_event_flag = args[1].Conv<bool>();
		}

		if (id < GMConstants.FIRST_INSTANCE_ID)
		{
			// asset index
			var instances = InstanceManager.FindByAssetId(id);

			foreach (var instance in instances)
			{
				if (execute_event_flag)
				{
					GamemakerObject.ExecuteEvent(instance, instance.Definition, EventType.Destroy);
				}

				GamemakerObject.ExecuteEvent(instance, instance.Definition, EventType.CleanUp);

				InstanceManager.instance_destroy(instance);
			}
		}
		else
		{
			// instance id
			var instance = InstanceManager.FindByInstanceId(id);

			if (instance == null)
			{
				DebugLog.LogError($"Tried to run instance_destroy on an instanceId that no longer exists!!!");
				return null;
			}

			if (execute_event_flag)
			{
				GamemakerObject.ExecuteEvent(instance!, instance!.Definition, EventType.Destroy);
			}

			GamemakerObject.ExecuteEvent(instance!, instance!.Definition, EventType.CleanUp);

			InstanceManager.instance_destroy(instance);
		}

		return null;
	}

	public static object? room_goto(object?[] args)
	{
		var index = args[0].Conv<int>();
		RoomManager.ChangeRoomAfterEvent(index);
		return null;
	}

	public static object? room_goto_previous(object?[] args)
	{
		RoomManager.room_goto_previous();
		return null;
	}

	public static object? room_goto_next(object?[] args)
	{
		RoomManager.room_goto_next();
		return null;
	}

	public static object room_previous(object?[] args)
	{
		var numb = args[0].Conv<int>();
		return RoomManager.room_previous(numb);
	}

	public static object room_next(object?[] args)
	{
		var numb = args[0].Conv<int>();
		return RoomManager.room_next(numb);
	}

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
}