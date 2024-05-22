using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DELTARUNITYStandalone;

[Serializable]
public abstract class DrawWithDepth
{
	public int instanceId;
	public double depth;

	public abstract void Draw();
}
