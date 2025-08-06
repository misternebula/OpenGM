using OpenGM.IO;

namespace OpenGM;

public static class CompatFlags
{
    /// <summary>
    /// Legacy bounding box and collision logic, using integer math over doubles. 
    /// </summary>
    [GMCompatFlag(before: "2022.1")]
    public static bool LegacyCollision = false;

    // --------------------------------------

    public static void Init()
    {
        var fields = typeof(CompatFlags).GetFields()
            .Where(prop => prop.IsDefined(typeof(GMCompatFlagAttribute), false));

        foreach (var fieldInfo in fields)
        {
            var attributes = (GMCompatFlagAttribute[])fieldInfo.GetCustomAttributes(typeof(GMCompatFlagAttribute), false);
            foreach (var attribute in attributes)
            {
                if (attribute.BeforeVersion is null && attribute.SinceVersion is null)
                {
                    continue;
                }
                
                if (attribute.BeforeVersion is not null && VersionManager.EngineVersion >= attribute.BeforeVersion)
                {
                    continue;
                }

                if (attribute.SinceVersion is not null && VersionManager.EngineVersion < attribute.SinceVersion)
                {
                    continue;
                }

                fieldInfo.SetValue(null, true);
            }

            DebugLog.LogVerbose($"Compat flag \"{fieldInfo.Name}\": {fieldInfo.GetValue(null)}");
        }
    }
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public class GMCompatFlagAttribute(
    string? since = null,
    string? before = null
) : Attribute
{
    public Version? SinceVersion { get; private set; } = (since != null) ? new(since) : null;
    public Version? BeforeVersion { get; private set; } = (before != null) ? new(before) : null;
}