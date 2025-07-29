using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenGM.Rendering;
public class RuntimeView
{
    public bool Visible = false;
    // TODO: camera area stuff instead of view here
    //       view.view seems to be old version stuff. view_xview etc doesnt even exist in docs now
    public Vector2 ViewPosition
    {
        get => new(Camera.ViewX, Camera.ViewY);
        set
        {
            // BUG: doesnt update matrices??? is this bad??? i dont know what gamemaker does
            Camera.ViewX = value.X;
            Camera.ViewY = value.Y;
        }
    }

    public Vector2 ViewSize
    {
        get => new(Camera.ViewWidth, Camera.ViewHeight);
        set
        {
            Camera.ViewWidth = value.X;
            Camera.ViewHeight = value.Y;
        }
    }

    public Vector2i PortPosition = Vector2i.Zero;
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
