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
        public uint Major; // also known as Year in the newer version format
        public uint Minor;
        public uint Release;
        public uint Build;

        public GMVersion(uint major, uint minor, uint release, uint build) 
        {
            Major = major;
            Minor = minor;
            Release = release;
            Build = build;
        }

        public GMVersion(string version) 
        {
            string[] splitVersion = version.Split('.');
            uint[] versionParts = splitVersion.Select(uint.Parse).ToArray();

            Major = versionParts[0];
            Minor = versionParts[1];
            Release = versionParts[2];
            Build = versionParts[3];
        }

        public readonly bool IsGMS1() => Major == 1;
        public readonly bool IsGMS2() => Major == 2;

        public override readonly string ToString()
        {
            return $"{Major}.{Minor}.{Release}.{Build}";
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(Major, Minor, Release, Build);
        }

        public override readonly bool Equals(object? obj)
        {
            if (base.Equals(obj))
            {
                return true;
            }
            
            if (obj != null && obj is GMVersion other)
            {
                return (
                    Major == other.Major &&
                    Minor == other.Minor &&
                    Release == other.Release &&
                    Build == other.Build
                );
            }

            return false;
        }

        public readonly bool LessThan(object? obj)
        {
            if (obj != null && obj is GMVersion other)
            {
                return (
                    Major < other.Major &&
                    Minor < other.Minor &&
                    Release < other.Release &&
                    Build < other.Build
                );
            }

            return false;
        }

        public readonly bool GreaterThan(object? obj)
        {
            if (obj != null && obj is GMVersion other)
            {
                return (
                    Major > other.Major &&
                    Minor > other.Minor &&
                    Release > other.Release &&
                    Build > other.Build
                );
            }

            return false;
        }

        public static bool operator ==(GMVersion? left, GMVersion? right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }

        public static bool operator !=(GMVersion? left, GMVersion? right)
        {
            if (left is null)
            {
                return right is not null;
            }

            return !left.Equals(right);
        }

        public static bool operator <(GMVersion left, GMVersion right)
        {
            return left.LessThan(right);
        }

        public static bool operator >(GMVersion left, GMVersion right)
        {
            return left.GreaterThan(right);
        }

        public static bool operator <=(GMVersion left, GMVersion right)
        {
            return left.Equals(right) || left.LessThan(right);
        }

        public static bool operator >=(GMVersion left, GMVersion right)
        {
            return left.Equals(right) || left.GreaterThan(right);
        }

        public static readonly GMVersion GMS1 = new("1.0.0.0");
        public static readonly GMVersion GMS2 = new("2.0.0.0");
    }

    public static class VersionManager
    {
		/// <summary>
		/// Version stored in the WAD, stuck at 2.0.0.0 after GMS2.
		/// </summary>
	    public static GMVersion WADVersion;

	    public static void Init()
	    {
		    WADVersion = new GMVersion(
			    GameLoader.GeneralInfo.Major,
			    GameLoader.GeneralInfo.Minor,
			    GameLoader.GeneralInfo.Release,
			    GameLoader.GeneralInfo.Build
			);
	    }

	    public static bool IsGMS1() => WADVersion.IsGMS1();
		public static bool IsGMS2() => WADVersion.IsGMS2();
    }
}
