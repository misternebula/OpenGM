namespace OpenGM.IO;

public class IniFile
{
    public string Name = null!;
    public List<IniSection> Sections = new();
}

public class IniSection
{
    public string Name;
    public Dictionary<string, string> Dict = new();

    public IniSection(string name) => Name = name;
}