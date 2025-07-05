namespace OpenGM.VirtualMachine.BuiltInFunctions
{
    public static class YoYoFunctions
    {
		// ...

		[GMLFunction("get_timer")]
		public static object get_timer(object?[] args)
		{
			return (int)(DateTime.Now - Entry.GameLoadTime).TotalMicroseconds; // TODO : is this floored? i assume it is
		}

		// os_get_config
		// os_get_info

		[GMLFunction("os_get_language", GMLFunctionFlags.Stub)]
		public static object os_get_language(object?[] args)
		{
			return "en"; // TODO : actually implement
		}

		[GMLFunction("os_get_region", GMLFunctionFlags.Stub)]
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
