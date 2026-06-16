using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenGM.Rendering;
public class RuntimeView
{
    public int ViewIndex;

    public bool Visible = false;

    public float ViewX
    {
        get => Camera.ViewX;
        set => Camera.ViewX = value;
    }

    public float ViewY
    {
        get => Camera.ViewY;
        set => Camera.ViewY = value;
    }

    public float ViewW
    {
        get => Camera.ViewWidth;
        set => Camera.ViewWidth = value;
    }

    public float ViewH
    {
        get => Camera.ViewHeight;
        set => Camera.ViewHeight = value;
    }

    public Vector2i PortPosition;
    public Vector2i PortSize;

    public float Angle
    {
        get => Camera.ViewAngle;
        set => Camera.ViewAngle = value;
    }

    public Vector2 Border
    {
        get => new(Camera.BorderX, Camera.BorderY);
        set
        {
            Camera.BorderX = value.X;
            Camera.BorderY = value.Y;
        }
    }
    public Vector2 Speed
    {
        get => new(Camera.SpeedX, Camera.SpeedY);
        set
        {
            Camera.SpeedX = value.X;
            Camera.SpeedY = value.Y;
        }
    }
    // index
    public int SurfaceId = -1; // TODO: set
    public Camera Camera = new();
}
