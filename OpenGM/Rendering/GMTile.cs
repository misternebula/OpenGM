using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using OpenTK.Mathematics;

namespace OpenGM.Rendering;
public class GMTile : DrawWithDepth
{
    public float X;
    public float Y;

    public int Definition;

    public int left;
    public int top;
    public int width;
    public int height;

    public float XScale;
    public float YScale;
    public uint Color;

    public GMTile()
    {
        DrawManager.Register(this);
    }

    public override void Draw()
    {
        if (Definition == -1)
        {
            return;
        }

		var sprite = SpriteManager.GetSpritePage(Definition, 0);
		CustomWindow.Draw(new GMSpritePartJob()
		{
			texture = sprite,
			screenPos = new Vector2d(X, Y),
			blend = Color.ABGRToCol4(),
			origin = Vector2.Zero,
			left = left,
			top = top,
			width = width,
			height = height
		});
	}

    public override void Destroy()
    {
        DrawManager.Unregister(this);
    }
}
