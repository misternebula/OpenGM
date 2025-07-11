namespace OpenGM.SerializedFiles;

public class GMPath
{
    public string Name = null!;
    public bool IsSmooth;
    public bool IsClosed;
    public int Precision;
    public List<PathPoint> Points = new();
}

public class PathPoint
{
    public double x;
    public double y;
    public double speed;
}
