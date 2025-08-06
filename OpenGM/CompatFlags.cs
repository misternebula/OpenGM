using OpenGM.IO;

namespace OpenGM;

public static class CompatFlags
{
    /// <summary>
    /// Legacy bounding box and collision logic, using integer math over doubles. 
    /// </summary>
    [GMCompatFlag(before: "2022.1")]
    public static bool LegacyCollision = false;

    /// <summary>
    /// Whether # should be treated as a newline or not in text rendering.
    /// </summary>
    [GMCompatFlag(before: "2.0")]
    public static bool HashNewlines = false;

    /// <summary>
    /// If true, script_execute will return 0 by default instead of null.
    /// </summary>
    [GMCompatFlag(before: "2.3")]
    public static bool ZeroReturnValue = false;

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

                if (attribute.SinceVersion is not null && VersionManager.EngineVersion < attribute.SinceVersion)
                {
                    continue;
                }
                
                if (attribute.BeforeVersion is not null && VersionManager.EngineVersion >= attribute.BeforeVersion)
                {
                    continue;
                }

                fieldInfo.SetValue(null, attribute.NewValue);
            }

            DebugLog.LogVerbose($"Compat flag \"{fieldInfo.Name}\": {fieldInfo.GetValue(null)}");
        }
    }
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public class GMCompatFlagAttribute(
    string? since = null,
    string? before = null,
    bool value = true
) : Attribute
{
    public Version? SinceVersion { get; private set; } = (since != null) ? new(since) : null;
    public Version? BeforeVersion { get; private set; } = (before != null) ? new(before) : null;
    public bool NewValue { get; private set; } = value;
}