using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenGM.Loading;

namespace OpenGM
{
	public struct GMVersion
	{
		public uint Major;
		public uint Minor;
		public uint Release;
		public uint Build;
	}

    public static class VersionManager
    {
		/// <summary>
		/// Version stored in the WAD, stuck at 2.0.0.0 after GMS2.
		/// </summary>
	    public static GMVersion WADVersion;

	    public static void Init()
	    {
		    WADVersion = new GMVersion()
		    {
			    Major = GameLoader.GeneralInfo.Major,
			    Minor = GameLoader.GeneralInfo.Minor,
			    Release = GameLoader.GeneralInfo.Release,
			    Build = GameLoader.GeneralInfo.Build
		    };
	    }

	    public static bool IsGMS1() => WADVersion.Major == 1;
		public static bool IsGMS2() => WADVersion.Major == 2;
    }
}
