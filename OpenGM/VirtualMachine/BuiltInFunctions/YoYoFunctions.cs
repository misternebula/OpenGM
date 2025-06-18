namespace OpenGM.VirtualMachine.BuiltInFunctions
{
    public static class YoYoFunctions
    {
		// ...

		// get_timer
		// os_get_config
		// os_get_info

		[GMLFunction("os_get_language")]
		public static object os_get_language(object?[] args)
		{
			return "en"; // TODO : actually implement
		}

		[GMLFunction("os_get_region")]
		public static object os_get_region(object?[] args)
		{
			// TODO : implement
			return "GB";
		}

		// os_request_permission
		// os_check_permision
		// code_is_compiled

		// ...
	}
}
