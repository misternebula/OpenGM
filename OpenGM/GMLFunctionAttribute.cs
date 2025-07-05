namespace OpenGM
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class GMLFunctionAttribute(string functionName, GMLFunctionFlags functionFlags = 0) : Attribute
    {
	    public string FunctionName { get; private set; } = functionName;
	    public GMLFunctionFlags FunctionFlags { get; private set; } = functionFlags;
    }

    [Flags]
    public enum GMLFunctionFlags
    {
        // write new entries like 1 << 1, 1 << 2...
        Stub = 1 << 0
    }
}
