using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DELTARUNITYStandalone.VirtualMachine;

public static partial class ScriptResolver
{
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

	public static object? move_towards_point(Arguments args)
	{
		var targetx = Conv<double>(args.Args[0]);
		var targety = Conv<double>(args.Args[1]);
		var sp = Conv<double>(args.Args[2]);

		args.Ctx.Self.direction = (double)point_direction(new Arguments() { Args = new object[] { args.Ctx.Self.x, args.Ctx.Self.y, targetx, targety } });
		args.Ctx.Self.speed = sp;

		return null;
	}

	public static object distance_to_point(Arguments args)
	{
		var x = Conv<double>(args.Args[0]);
		var y = Conv<double>(args.Args[1]);

		var self = args.Ctx.Self;

		if (args.Ctx.Self.mask_id == -1 && args.Ctx.Self.sprite_index == -1)
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

	public static object instance_number(Arguments args)
	{
		var obj = Conv<int>(args.Args[0]);
		return InstanceManager.instance_number(obj);
	}

	public static object instance_create_depth(Arguments args)
	{
		var x = Conv<double>(args.Args[0]);
		var y = Conv<double>(args.Args[1]);
		var depth = Conv<int>(args.Args[2]);
		var obj = Conv<int>(args.Args[3]);

		return InstanceManager.instance_create_depth(x, y, depth, obj);
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

	public static object room_goto(Arguments args)
	{
		var index = Conv<int>(args.Args[0]);
		RoomManager.ChangeRoomAfterEvent(index);
		return null;
	}

	public static object room_goto_previous(Arguments args)
	{
		RoomManager.room_goto_previous();
		return null;
	}

	public static object room_goto_next(Arguments args)
	{
		RoomManager.room_goto_next();
		return null;
	}

	public static object room_previous(Arguments args)
	{
		var numb = Conv<int>(args.Args[0]);
		return RoomManager.room_previous(numb);
	}

	public static object room_next(Arguments args)
	{
		var numb = Conv<int>(args.Args[0]);
		return RoomManager.room_next(numb);
	}
}