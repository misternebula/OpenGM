using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
