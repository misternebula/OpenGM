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
            var splitVersion = version.Split('.');
            var versionParts = splitVersion.Select(uint.Parse).ToArray();

            Major = versionParts[0];
            Minor = versionParts[1];
            Release = versionParts[2];
            Build = versionParts[3];
        }

        public override readonly string ToString() => 
            $"{Major}.{Minor}.{Release}.{Build}";

        public override readonly int GetHashCode() => 
            HashCode.Combine(Major, Minor, Release, Build);

        public override readonly bool Equals(object? obj)
        {
            if (base.Equals(obj))
            {
                return true;
            }
            
            if (obj is not null and GMVersion other)
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
            if (obj is not null and GMVersion other)
            {
                return (
                    Major < other.Major ||
                    Minor < other.Minor ||
                    Release < other.Release ||
                    Build < other.Build
                );
            }

            return false;
        }

        public readonly bool GreaterThan(object? obj)
        {
            if (obj is not null and GMVersion other)
            {
                return (
                    Major > other.Major ||
                    Minor > other.Minor ||
                    Release > other.Release ||
                    Build > other.Build
                );
            }

            return false;
        }

        public static bool operator ==(GMVersion? left, GMVersion? right) => 
            left is null ? right is null : left.Equals(right);

        public static bool operator !=(GMVersion? left, GMVersion? right) => 
            left is null ? right is not null : !left.Equals(right);

        public static bool operator <(GMVersion left, GMVersion right) => 
            left.LessThan(right);

        public static bool operator >(GMVersion left, GMVersion right) => 
            left.GreaterThan(right);

        public static bool operator <=(GMVersion left, GMVersion right) => 
            left.Equals(right) || left.LessThan(right);

        public static bool operator >=(GMVersion left, GMVersion right) => 
            left.Equals(right) || left.GreaterThan(right);
    }

    public static class VersionManager
    {
        /// <summary>
        /// GameMaker version as detected by UTMT.
        /// </summary>
        public static GMVersion EngineVersion;

        public static void Init()
        {
            EngineVersion = new GMVersion(
                GameLoader.GeneralInfo.Major,
                GameLoader.GeneralInfo.Minor,
                GameLoader.GeneralInfo.Release,
                GameLoader.GeneralInfo.Build
            );
        }
    }
}
