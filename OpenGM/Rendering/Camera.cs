using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenGM.Rendering;
public class Camera
{
    public int ID;

    public double ViewX;
    public double ViewY;
    public double ViewWidth;
    public double ViewHeight;
    public double SpeedX;
    public double SpeedY;
    public double BorderX;
    public double BorderY;
    public double ViewAngle;
    public GamemakerObject? TargetInstance = null;
}
