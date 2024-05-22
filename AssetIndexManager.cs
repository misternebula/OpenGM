namespace DELTARUNITYStandalone;

public enum AssetType
{
	sounds,
	sprites,
	backgrounds,
	paths,
	fonts,
	objects,
	timelines,
	rooms,
	shaders,
	extensions,
	code
}

public static class AssetIndexManager
{
	public static Dictionary<AssetType, Dictionary<string, int>> AssetList = new();
	public static Dictionary<string, int> NameToIndex = new();

	public static void LoadAssetIndexes()
	{
		Console.Write($"Loading asset order...");
		var file = Path.Combine(Directory.GetCurrentDirectory(), "Output", "asset_names.txt");

		var lines = File.ReadAllLines(file);
		var headerLineNumber = 0;
		AssetType currentAssetType = 0;
		for (int i = 0; i < lines.Length; i++)
		{
			var line = lines[i];
			if (line.StartsWith("@@") && line.EndsWith("@@"))
			{
				headerLineNumber = i;
				currentAssetType = Enum.Parse<AssetType>(line.Trim('@'));
				continue;
			}

			NameToIndex.Add(line, i - headerLineNumber - 1);

			if (!AssetList.ContainsKey(currentAssetType))
			{
				AssetList.Add(currentAssetType, new Dictionary<string, int>());
			}

			AssetList[currentAssetType].Add(line, i - headerLineNumber - 1);
		}
		Console.WriteLine($" Done!");
	}

	public static int GetIndex(string name)
	{
		return NameToIndex.TryGetValue(name, out var index) ? index : -1;
	}

	public static int Register(AssetType type, string name)
	{
		if (!AssetList.ContainsKey(type))
		{
			AssetList.Add(type, new Dictionary<string, int>());
		}

		var highestIndex = AssetList[type].Values.Max();
		AssetList[type].Add(name, highestIndex + 1);
		return highestIndex + 1;
	}

	public static void Unregister(AssetType type, string name)
	{
		if (!AssetList.ContainsKey(type))
		{
			return;
		}

		if (!AssetList[type].ContainsKey(name))
		{
			return;
		}

		AssetList[type].Remove(name);
	}
}
