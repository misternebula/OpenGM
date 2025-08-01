using MemoryPack;

namespace OpenGM.SerializedFiles;

[MemoryPackable]
public partial class AnimCurve
{
    public int AssetIndex;

    public string Name = "";
    public int Id;
    public List<AnimCurveChannel> Channels = new();
}

[MemoryPackable]
public partial class AnimCurveChannel
{
    public string Name = "";
    public CurveType CurveType;
    public int Iterations;
    public List<AnimCurvePoint> Points = new();
}

[MemoryPackable]
public partial class AnimCurvePoint
{
    public float X; // aka "h"
    public float Y; // aka "Value" or "v"

    public float BezierX0;
    public float BezierY0;
    public float BezierX1;
    public float BezierY1;
}
