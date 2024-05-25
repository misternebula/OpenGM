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

	public uint left;
	public uint top;
	public uint width;
	public uint height;

	public float XScale;
	public float YScale;
	public uint Color;

	public GMTile()
	{
		DrawManager.Register(this);
	}

	public override void Draw()
	{
		//SpriteManager.DrawSpritePartExt(Definition, 0, (int)left, (int)top, (int)width, (int)height, X, Y, 1, 1, Color, Alpha);
		SpriteManager.DrawSpritePart(Definition, 0, (int)left, (int)top, (int)width, (int)height, X, Y);
	}
}
