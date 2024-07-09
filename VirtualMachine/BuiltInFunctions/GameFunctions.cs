using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DELTARUNITYStandalone.VirtualMachine;

public static partial class ScriptResolver
{
	public static object place_meeting(object[] args)
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
			return CollisionManager.place_meeting_assetid(x, y, obj, VMExecutor.Ctx.Self);
		}
		else
		{
			return CollisionManager.place_meeting_instanceid(x, y, obj, VMExecutor.Ctx.Self);
		}
	}

	public static object move_towards_point(object[] args)
	{
		var targetx = args[0].Conv<double>();
		var targety = args[1].Conv<double>();
		var sp = args[2].Conv<double>();

		VMExecutor.Ctx.Self.direction = (double)point_direction(VMExecutor.Ctx.Self.x, VMExecutor.Ctx.Self.y, targetx, targety);
		VMExecutor.Ctx.Self.speed = sp;

		return null!;
	}

	public static object distance_to_point(object[] args)
	{
		var x = args[0].Conv<double>();
		var y = args[1].Conv<double>();

		var self = VMExecutor.Ctx.Self;

		if (VMExecutor.Ctx.Self.mask_id == -1 && VMExecutor.Ctx.Self.sprite_index == -1)
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

	public static object collision_rectangle(object[] args)
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
			return CollisionManager.collision_rectangle_assetid(x1, y1, x2, y2, obj, prec, notme, VMExecutor.Ctx.Self);
		}
		else
		{
			return CollisionManager.collision_rectangle_instanceid(x1, y1, x2, y2, obj, prec, notme, VMExecutor.Ctx.Self);
		}
	}

	public static object instance_exists(object[] args)
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

	public static object instance_number(object[] args)
	{
		var obj = args[0].Conv<int>();
		return InstanceManager.instance_number(obj);
	}

	public static object instance_create_depth(object[] args)
	{
		var x = args[0].Conv<double>();
		var y = args[1].Conv<double>();
		var depth = args[2].Conv<int>();
		var obj = args[3].Conv<int>();

		return InstanceManager.instance_create_depth(x, y, depth, obj);
	}

	public static object instance_destroy(object[] args)
	{
		if (args.Length == 0)
		{
			GamemakerObject.ExecuteScript(VMExecutor.Ctx.Self, VMExecutor.Ctx.ObjectDefinition, EventType.Destroy);
			GamemakerObject.ExecuteScript(VMExecutor.Ctx.Self, VMExecutor.Ctx.ObjectDefinition, EventType.CleanUp);
			InstanceManager.instance_destroy(VMExecutor.Ctx.Self);
			return null!;
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

		return null!;
	}

	public static object room_goto(object[] args)
	{
		var index = args[0].Conv<int>();
		RoomManager.ChangeRoomAfterEvent(index);
		return null!;
	}

	public static object room_goto_previous(object[] args)
	{
		RoomManager.room_goto_previous();
		return null!;
	}

	public static object room_goto_next(object[] args)
	{
		RoomManager.room_goto_next();
		return null!;
	}

	public static object room_previous(object[] args)
	{
		var numb = args[0].Conv<int>();
		return RoomManager.room_previous(numb);
	}

	public static object room_next(object[] args)
	{
		var numb = args[0].Conv<int>();
		return RoomManager.room_next(numb);
	}
}