using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;

namespace DELTARUNITYStandalone;
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
	public int Color;

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

		//SpriteManager.DrawSpritePartExt(Definition, 0, (int)left, (int)top, (int)width, (int)height, X, Y, 1, 1, Color, Alpha);
		SpriteManager.DrawSpritePart(Definition, 0, (int)left, (int)top, (int)width, (int)height, X, Y);
	}

	public override void Destroy()
	{
		DrawManager.Unregister(this);
	}
}
