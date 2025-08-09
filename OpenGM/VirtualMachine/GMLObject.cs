using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace OpenGM.VirtualMachine;

/// <summary>
/// a struct. stores variables
///
/// YYObjectBase in cpp
/// </summary>
public class GMLObject : IStackContextSelf, IDictionary<string, object?>
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

    // ----- pass IDictionary through to SelfVariables -----

    public object? this[string index]
    {
        get => SelfVariables[index];
        set => SelfVariables[index] = value;
    }
    
    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() 
        => SelfVariables.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => SelfVariables.GetEnumerator();

    public ICollection<string> Keys 
        => SelfVariables.Keys;

    public ICollection<object?> Values 
        => SelfVariables.Values;

    public int Count 
        => SelfVariables.Count;

    public bool IsReadOnly 
        => false;

    public void Add(string key, object? value) 
        => SelfVariables.Add(key, value);
        
    public bool ContainsKey(string key) 
        => SelfVariables.ContainsKey(key);

    public bool Remove(string key) 
        => SelfVariables.Remove(key);

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out object? value) 
        => SelfVariables.TryGetValue(key, out value);

    public object? GetValueOrDefault(string key) 
        => SelfVariables.GetValueOrDefault(key);

    public object? GetValueOrDefault(string key, object def) 
        => SelfVariables.GetValueOrDefault(key, def);

    public void Add(KeyValuePair<string, object?> item) 
        => SelfVariables.Add(item.Key, item.Value);

    public void Clear() 
        => SelfVariables.Clear();

    public bool Contains(KeyValuePair<string, object?> item) 
        => SelfVariables.Contains(item);

    public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex) 
        => ((ICollection<KeyValuePair<string, object?>>)SelfVariables).CopyTo(array, arrayIndex);

    public bool Remove(KeyValuePair<string, object?> item) 
        => SelfVariables.Remove(item.Key);
}
