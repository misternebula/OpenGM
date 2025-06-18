namespace OpenGM
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class GMLFunctionAttribute(string functionName) : Attribute
    {
	    public string FunctionName { get; private set; } = functionName;
    }
}
