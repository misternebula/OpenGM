namespace OpenGM
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class GMLFunctionAttribute(
        string functionName, 
        GMLFunctionFlags functionFlags = 0,
        string? since = null,
        string? before = null
    ) : Attribute
    {
	    public string FunctionName { get; private set; } = functionName;
	    public GMLFunctionFlags FunctionFlags { get; private set; } = functionFlags;
	    public GMVersion? AddedVersion { get; private set; } = (since != null) ? new(since) : null;
	    public GMVersion? RemovedVersion { get; private set; } = (before != null) ? new(before) : null;
    }

    [Flags]
    public enum GMLFunctionFlags
    {
        // write new entries like 1 << 1, 1 << 2...
        Stub = 1 << 0
    }
}
