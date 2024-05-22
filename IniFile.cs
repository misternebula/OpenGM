using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DELTARUNITYStandalone;

public class IniFile
{
	public string Name;
	public List<IniSection> Sections = new();
}

public class IniSection
{
	public string Name;
	public Dictionary<string, string> Dict = new();

	public IniSection(string name) => Name = name;
}