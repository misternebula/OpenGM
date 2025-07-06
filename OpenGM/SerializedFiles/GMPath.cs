using MemoryPack;

namespace OpenGM.SerializedFiles;

[MemoryPackable]
public partial class GMPath
{
	public string Name = null!;
	public bool IsSmooth;
	public bool IsClosed;
	public int Precision;
	public List<PathPoint> Points = new();
}

[MemoryPackable]
public partial class PathPoint
{
	public double x;
	public double y;
	public double speed;
}
