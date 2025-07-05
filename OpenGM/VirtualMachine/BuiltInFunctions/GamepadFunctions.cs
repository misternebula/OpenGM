using OpenGM.IO;

namespace OpenGM.VirtualMachine.BuiltInFunctions
{
    public static class GamepadFunctions
    {
		// gamepad_is_supported

		[GMLFunction("gamepad_get_device_count", GMLFunctionFlags.Stub)]
		public static object gamepad_get_device_count(object?[] args)
		{
			// TODO : implement
			return 0;
		}

		[GMLFunction("gamepad_is_connected", GMLFunctionFlags.Stub)]
		public static object gamepad_is_connected(object?[] args)
		{
			var device = args[0].Conv<int>();
			return false; // TODO : implement
		}

		// gamepad_get_description
		// gamepad_get_button_threshold
		// gamepad_set_button_threshold
		// gamepad_get_axis_deadzone

		[GMLFunction("gamepad_set_axis_deadzone", GMLFunctionFlags.Stub)]
		public static object? gamepad_set_axis_deadzone(object?[] args)
		{
			return null;
		}

		// gamepad_button_count

		[GMLFunction("gamepad_button_check", GMLFunctionFlags.Stub)]
		public static object gamepad_button_check(object?[] args)
        {
	        // TODO : implement?
	        return false;
        }

		[GMLFunction("gamepad_button_check_pressed", GMLFunctionFlags.Stub)]
		public static object gamepad_button_check_pressed(object?[] args)
		{
			// TODO : implement
			return false;
		}

		// gamepad_button_check_released
		// gamepad_button_value
		// gamepad_axis_count

		[GMLFunction("gamepad_axis_value", GMLFunctionFlags.Stub)]
		public static object gamepad_axis_value(object?[] args)
		{
			// TODO : implement?
			return 0;
		}

		// gamepad_hat_value
		// gamepad_hat_count
		// gamepad_remove_mapping
		// gamepad_test_mapping
		// gamepad_get_mapping
		// gamepad_get_guid
		// gamepad_set_vibration
		// gamepad_add_hardware_mapping_from_string
		// gamepad_add_hardware_mapping_from_file
		// gamepad_get_hardware_mappings
		// gamepad_set_color
		// gamepad_set_colour
		// gamepad_set_option
		// gamepad_get_option
	}
}
