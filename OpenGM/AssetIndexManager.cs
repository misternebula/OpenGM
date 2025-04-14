namespace OpenGM;

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
	private static Dictionary<AssetType, Dictionary<string, int>> _assetList = new();
	private static Dictionary<string, int> _nameToIndex = new();

	public static void LoadAssetIndexes(BinaryReader reader)
	{
		Console.Write($"Loading asset order...");

		_assetList.Clear();
		_nameToIndex.Clear();

		var lines = reader.ReadString().SplitLines();
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

			if (!_assetList.ContainsKey(currentAssetType))
			{
				_assetList.Add(currentAssetType, new Dictionary<string, int>());
			}

			_assetList[currentAssetType].Add(line, i - headerLineNumber - 1);
			_nameToIndex.Add(line, i - headerLineNumber - 1);
		}
		Console.WriteLine($" Done!");
	}

	public static int GetIndex(string name)
	{
		return _nameToIndex.TryGetValue(name, out var index) ? index : -1;
	}

	public static string GetName(AssetType type, int index)
	{
		return _assetList[type].First(x => x.Value == index).Key;
	}

	public static int Register(AssetType type, string name)
	{
		if (!_assetList.ContainsKey(type))
		{
			_assetList.Add(type, new Dictionary<string, int>());
		}

		if (_assetList[type].TryGetValue(name, out var index))
		{
			return index;
		}

		var highestIndex = _assetList[type].Values.Max();
		_assetList[type].Add(name, highestIndex + 1);
		_nameToIndex.Add(name, highestIndex + 1);
		return highestIndex + 1;
	}

	public static void Unregister(AssetType type, string name)
	{
		if (!_assetList.ContainsKey(type))
		{
			return;
		}

		if (!_assetList[type].ContainsKey(name))
		{
			return;
		}

		_assetList[type].Remove(name);
		_nameToIndex.Remove(name);
	}
}
