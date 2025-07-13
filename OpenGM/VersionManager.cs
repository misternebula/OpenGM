using OpenGM.Loading;

namespace OpenGM
{
    public static class VersionManager
    {
        /// <summary>
        /// GameMaker version as detected by UTMT.
        /// </summary>
        public static Version EngineVersion = new();

        public static void Init()
        {
            EngineVersion = new Version(
                (int)GameLoader.GeneralInfo.Major,
                (int)GameLoader.GeneralInfo.Minor,
                (int)GameLoader.GeneralInfo.Release,
                (int)GameLoader.GeneralInfo.Build
            );

            // TODO: the conditions for compat mode are for sure more nuanced than this
            if (
                !CollisionManager.CompatModeOverridden &&
                EngineVersion < new Version(2022,1,0,0)
            )
            {
                CollisionManager.CompatMode = true;
            }
        }
    }
}
