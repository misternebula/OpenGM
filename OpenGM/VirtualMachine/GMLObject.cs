namespace OpenGM.VirtualMachine;

/// <summary>
/// a struct
/// </summary>
internal class GMLObject : IStackContextSelf
{
    public Dictionary<string, object?> SelfVariables { get; } = new();

    public override string ToString()
    {
        var ret = "Struct";
        if (SelfVariables.Count > 0)
        {
            var first = SelfVariables.Keys.First();
            ret += $" ({SelfVariables.Count} entries, \"{first}\"...)";
        }
        else
        {
            ret += " (no entries)";
        }

        return ret;
    }
}
