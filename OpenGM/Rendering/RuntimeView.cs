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
    public Vector2i ViewPosition = Vector2i.Zero;
    public Vector2i ViewSize;
    public Vector2i PortPosition = Vector2i.Zero;
    public Vector2i PortSize;
    public double Angle;
    public Vector2d Border = new Vector2d(32, 32);
    public Vector2i Speed = new Vector2i(-1, -1);
    // index
    // surface
    public Camera? Camera = null;
}
