namespace OpenGM.IO;

public class IniFile
{
    public string? Name = null;
    public List<IniSection> Sections = new();

    public static IniFile FromContent(string content)
    {
        KeyValuePair<string, string> ParseKeyValue(string line)
        {
            var lineByEquals = line.Split('=');
            var key = lineByEquals[0].Trim();
            var value = lineByEquals[1].Trim();
            value = value.Trim('"');
            return new KeyValuePair<string, string>(key, value);
        }

        var lines = content.SplitLines();

        var iniFile = new IniFile { };
        IniSection? currentSection = null;

        for (var i = 0; i < lines.Length; i++)
        {
            var currentLine = lines[i];
            if (currentLine.StartsWith('[') && currentLine.EndsWith(']'))
            {
                currentSection = new IniSection(currentLine.TrimStart('[').TrimEnd(']'));
                iniFile.Sections.Add(currentSection);
                continue;
            }

            if (string.IsNullOrEmpty(currentLine?.Trim()))
            {
                continue;
            }

            Console.WriteLine(currentLine.Length);
            var keyvalue = ParseKeyValue(currentLine);
            currentSection?.Dict.Add(keyvalue.Key, keyvalue.Value);
        }

        return iniFile;
    }

    public override string ToString()
    {
        var text = "";
        foreach (var section in Sections)
        {
            text += $"[{section.Name}]\n";
            foreach (var kv in section.Dict)
            {
                text += $"{kv.Key}=\"{kv.Value}\"\n";
            }
        }

        return text;
    }
}

public class IniSection
{
    public string Name;
    public Dictionary<string, string> Dict = new();

    public IniSection(string name) => Name = name;
}