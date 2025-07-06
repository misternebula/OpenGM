using System.Globalization;
using OpenGM.Loading;

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

		[GMLFunction("os_get_config")]
		public static object os_get_config(object?[] args)
		{
			return GameLoader.GeneralInfo.Config;
		}

		// os_get_info

		[GMLFunction("os_get_language", GMLFunctionFlags.Stub)]
		public static object os_get_language(object?[] args)
		{
			var lang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
			return (lang == "iv") ? "en" : lang; //just in case it returns iv
		}

		[GMLFunction("os_get_region", GMLFunctionFlags.Stub)]
		public static object os_get_region(object?[] args)
		{
			bool invariant = (CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "iv"); //just in case it returns iv
			return ((invariant) ? "GB" : (new RegionInfo(CultureInfo.CurrentCulture.Name).TwoLetterISORegionName));
		}

		// os_request_permission
		// os_check_permision

		[GMLFunction("code_is_compiled")]
		public static object code_is_compiled(object?[] args)
		{
			return GameLoader.GeneralInfo.IsYYC;
		}

		// ...
	}
}
