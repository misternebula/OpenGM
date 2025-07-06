using OpenGM.IO;
using UndertaleModLib.Models;

namespace OpenGM.VirtualMachine.BuiltInFunctions
{
    public static class MiscFunctions
    {
	    [GMLFunction("event_inherited")]
		public static object? event_inherited(object?[] args)
	    {
		    if (VMExecutor.Self.ObjectDefinition?.parent == null)
		    {
			    return null;
		    }

		    GamemakerObject.ExecuteEvent(VMExecutor.Self.GMSelf, VMExecutor.Self.ObjectDefinition.parent, VMExecutor.Call.EventType, VMExecutor.Call.EventIndex);
		    return null;
	    }

		[GMLFunction("event_perform")]
		public static object? event_perform(object?[] args)
		{
			var type = args[0].Conv<int>();
			var numb = args[1].Conv<int>();

			GamemakerObject.ExecuteEvent(VMExecutor.Self.GMSelf, VMExecutor.Self.ObjectDefinition, (EventType)type + 1, numb);
			return null;
		}

		// event_perform_async

		[GMLFunction("event_user")]
		public static object? event_user(object?[] args)
		{
			var numb = args[0].Conv<int>();
			GamemakerObject.ExecuteEvent(VMExecutor.Self.GMSelf, VMExecutor.Self.ObjectDefinition, EventType.Other, (int)EventSubtypeOther.User0 + numb);
			return null;
		}

		// event_perform_object

		// external_define
		// external_call
		// external_free

		// window_handle
		// window_device

		[GMLFunction("show_debug_message")]
		public static object? show_debug_message(object?[] args)
		{
			DebugLog.Log(args[0]?.ToString() ?? "undefined");
			return null;
		}

		// show_debug_overlay
		// debug_event
		// debug_get_callstack

		// gif_add_surface
		// gif_save
		// gif_save_buffer
		// gif_open

		// alarm_set
		// alarm_get

		[GMLFunction("variable_global_exists")]
		public static object variable_global_exists(object?[] args)
		{
			var name = args[0].Conv<string>();
			return VariableResolver.GlobalVariables.ContainsKey(name);
		}

		// variable_global_get
		// variable_global_set

		[GMLFunction("variable_instance_exists")]
		public static object variable_instance_exists(object?[] args)
		{
			var instance_id = args[0].Conv<int>();
			var name = args[1].Conv<string>();

			var instance = InstanceManager.FindByInstanceId(instance_id);

			if (instance == null)
			{
				return false;
			}

			if (instance.SelfVariables.ContainsKey(name))
			{
				return true;
			}

			return false;
		}

		[GMLFunction("variable_instance_get")]
		public static object? variable_instance_get(object?[] args)
		{
			var instanceId = args[0].Conv<int>();
			var name = args[1].Conv<string>();

			GamemakerObject? instance;

			if (instanceId == GMConstants.global)
			{
				throw new NotImplementedException();
			}
			else if (instanceId < GMConstants.FIRST_INSTANCE_ID)
			{
				// todo : first how?
				instance = InstanceManager.FindByAssetId(instanceId).First();
			}
			else
			{
				instance = InstanceManager.FindByInstanceId(instanceId);
			}

			if (instance == null)
			{
				return null;
			}

			if (VariableResolver.BuiltInSelfVariables.ContainsKey(name))
			{
				var (getter, setter) = VariableResolver.BuiltInSelfVariables[name];
				return getter(instance);
			}

			return instance.SelfVariables.TryGetValue(name, out var value) ? value : null;
		}

		[GMLFunction("variable_instance_set")]
		public static object? variable_instance_set(object?[] args)
		{
			var instanceId = args[0].Conv<int>();
			var name = args[1].Conv<string>();
			var value = args[2];

			if (instanceId == GMConstants.global)
			{
				throw new NotImplementedException();
			}
			else if (instanceId < GMConstants.FIRST_INSTANCE_ID)
			{
				// asset id
				// TODO : does this actually iterate? html seems to, but not sure about c++
				var instances = InstanceManager.FindByAssetId(instanceId);

				foreach (var instance in instances)
				{
					if (VariableResolver.BuiltInSelfVariables.TryGetValue(name, out var getset))
					{
						var (getter, setter) = getset;
						setter?.Invoke(instance, value);

						return null;
					}

					instance.SelfVariables[name] = value;
				}
			}
			else
			{
				// instance id
				var instance = InstanceManager.FindByInstanceId(instanceId)!;

				if (VariableResolver.BuiltInSelfVariables.TryGetValue(name, out var getset))
				{
					var (getter, setter) = getset;
					setter?.Invoke(instance, value);

					return null;
				}

				instance.SelfVariables[name] = value;
			}

			return null;
		}

		// variable_instance_get_names
		// variable_instance_names_count
		// variable_struct_exists
		// variable_struct_get
		// variable_struct_set
		// variable_struct_set_pre
		// variable_struct_set_post
		// variable_struct_get_names
		// variable_struct_names_count
		// variable_struct_remove
		// gc_collect
		// gc_enable
		// gc_is_enabled
		// gc_get_stats
		// gc_target_frame_time
		// gc_get_target_frame_time
		// clipboard_has_text
		// clipboard_set_text
		// clipboard_get_text

		// basically copied from https://github.com/YoYoGames/GameMaker-HTML5/blob/965f410a6553dd8e2418006ebeda5a86bd55dba2/scripts/functions/Function_Date.js
		const double MILLISECONDS_IN_A_DAY = 86400000.0;
		const double DAYS_SINCE_1900 = 25569;
		private static readonly int[] monthlen = new[] { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

		private static double FromGMDateTime(double dateTime) => dateTime < DAYS_SINCE_1900
			? dateTime * MILLISECONDS_IN_A_DAY
			: (dateTime - DAYS_SINCE_1900) * MILLISECONDS_IN_A_DAY;

		private static int DayOfYear(Date d)
		{
			var day = 0;
			if (_useLocalTime)
			{
				var monthlens = GetMonthLengths(d.GetFullYear());
				for (var i = 0; i < d.GetMonth(); i++)
					day += monthlens[i];
				day += d.GetDate();
			}
			else
			{
				var monthlens = GetMonthLengths(d.GetUTCFullYear());
				for (var i = 0; i < d.GetUTCMonth(); i++)
					day += monthlens[i];
				day += d.GetUTCDate();
			}

			return day;
		}

		private static int[] GetMonthLengths(int year)
		{
			var monthLengths = monthlen.ToArray(); // copy array
			if (IsLeapYear(year))
			{
				monthLengths[1] = 29;
			}
			return monthLengths;
		}

		private static bool IsLeapYear(int year)
		{
			return year % 400 == 0 || (year % 100 != 0 && year % 4 == 0);
		}

		private static bool _useLocalTime = false;

		[GMLFunction("date_current_datetime")]
		public static object date_current_datetime(object?[] args)
		{
			var dt = new Date();
			var mm = dt.GetMilliseconds();
			var t = dt.GetTime() - mm;
			return (t / MILLISECONDS_IN_A_DAY) + DAYS_SINCE_1900;
		}

		// date_create_datetime
		// date_valid_datetime
		// date_inc_year
		// date_inc_month
		// date_inc_week
		// date_inc_day
		// date_inc_hour
		// date_inc_minute
		// date_inc_second

		[GMLFunction("date_get_year")]
		public static object date_get_year(object?[] args)
		{
			var time = args[0].Conv<double>();
			var d = new Date();
			d.SetTime(FromGMDateTime(time));

			return _useLocalTime ? d.GetFullYear() : d.GetUTCFullYear();
		}

		[GMLFunction("date_get_month")]
		public static object date_get_month(object?[] args)
		{
			var time = args[0].Conv<double>();
			var d = new Date();
			d.SetTime(FromGMDateTime(time));

			return _useLocalTime ? d.GetMonth() + 1 : d.GetUTCMonth() + 1;
		}

		[GMLFunction("date_get_week")]
		public static object date_get_week(object?[] args)
		{
			var time = args[0].Conv<double>();
			var d = new Date();
			d.SetTime(FromGMDateTime(time));

			var w = DayOfYear(d);
			return CustomMath.FloorToInt(w / 7.0);
		}

		[GMLFunction("date_get_day")]
		public static object date_get_day(object?[] args)
		{
			var time = args[0].Conv<double>();
			var d = new Date();
			d.SetTime(FromGMDateTime(time));

			return _useLocalTime ? d.GetDate() : d.GetUTCDate();
		}

		[GMLFunction("date_get_hour")]
		public static object date_get_hour(object?[] args)
		{
			var time = args[0].Conv<double>();
			var d = new Date();
			d.SetTime(FromGMDateTime(time));

			return _useLocalTime ? d.GetHours() : d.GetUTCHours();
		}

		[GMLFunction("date_get_minute")]
		public static object date_get_minute(object?[] args)
		{
			var time = args[0].Conv<double>();
			var d = new Date();
			d.SetTime(FromGMDateTime(time));

			return _useLocalTime ? d.GetMinutes() : d.GetUTCMinutes();
		}

		[GMLFunction("date_get_second")]
		public static object date_get_second(object?[] args)
		{
			var time = args[0].Conv<double>();
			var d = new Date();
			d.SetTime(FromGMDateTime(time));

			return _useLocalTime ? d.GetSeconds() : d.GetUTCSeconds();
		}

		[GMLFunction("date_get_weekday")]
		public static object date_get_weekday(object?[] args)
		{
			var time = args[0].Conv<double>();
			var d = new Date();
			d.SetTime(FromGMDateTime(time));

			return _useLocalTime ? d.GetDay() : d.GetUTCDay();
		}

		// date_get_day_of_year
		// date_get_hour_of_year
		// date_get_minute_of_year
		// date_get_second_of_year
		// date_year_span
		// date_month_span
		// date_week_span
		// date_day_span
		// date_hour_span
		// date_minute_span
		// date_second_span
		// date_compare_datetime
		// date_compare_date
		// date_compare_time
		// date_date_of
		// date_time_of
		// date_datetime_string
		// date_date_string
		// date_time_string
		// date_days_in_month
		// date_days_in_year
		// date_leap_year
		// date_is_today
		// date_set_timezone
		// date_get_timezone
		// game_set_speed

		[GMLFunction("game_get_speed")]
		public static object game_get_speed(object?[] args)
		{
			var type = args[0].Conv<int>();

			if (type == 0)
			{
				// FPS
				return Entry.GameSpeed;
			}
			else
			{
				// microseconds per frame
				return CustomMath.FloorToInt(1000000.0 / Entry.GameSpeed);
			}
		}

		// extension_get_string
	}
}
