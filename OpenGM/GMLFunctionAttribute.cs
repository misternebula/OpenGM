using OpenGM.IO;

namespace OpenGM
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class GMLFunctionAttribute(
        string functionName, 
        GMLFunctionFlags functionFlags = 0,
        string? since = null,
        string? before = null,
        DebugLog.LogType stubLogType = DebugLog.LogType.Verbose
    ) : Attribute
    {
        public string FunctionName { get; private set; } = functionName;
        public GMLFunctionFlags FunctionFlags { get; private set; } = functionFlags;
        public Version? AddedVersion { get; private set; } = (since != null) ? new(since) : null;
        public Version? RemovedVersion { get; private set; } = (before != null) ? new(before) : null;
        public DebugLog.LogType StubLogType { get; private set; } = stubLogType;
    }

    [Flags]
    public enum GMLFunctionFlags
    {
        // write new entries like 1 << 1, 1 << 2...
        Stub = 1 << 0
    }
}
