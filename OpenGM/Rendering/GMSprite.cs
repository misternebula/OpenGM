using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenGM.SerializedFiles;
using OpenTK.Mathematics;
using UndertaleModLib.Models;

namespace OpenGM.Rendering;
public class GMSprite : DrawWithDepth
{
	public int Definition;
	public int X;
	public int Y;
	public double XScale;
	public double YScale;
	public int Color;
	public double AnimationSpeed;
	public AnimationSpeedType AnimationSpeedType;
	public double FrameIndex;
	public double Rotation;

	public CLayerSpriteElement Element;

	public GMSprite(CLayerSpriteElement element)
	{
		Element = element;
		DrawManager.Register(this);

		if (AnimationSpeedType == AnimationSpeedType.FPS)
		{
			_timing = new Stopwatch();
			_timing.Start();
		}

		_spriteFrames = SpriteManager.GetNumberOfFrames(Definition);
	}

	private int _spriteFrames;
	private Stopwatch? _timing = null;

	public override void Draw()
	{
		if (Definition == -1)
		{
			return;
		}

		if (AnimationSpeedType == AnimationSpeedType.FPS)
		{
			// Frames per second
			var frameTime = 1.0 / AnimationSpeed;
			if (_timing!.Elapsed.TotalSeconds >= frameTime)
			{
				FrameIndex++;
			}
		}
		else
		{
			// Frames per game frame
			FrameIndex += AnimationSpeed;
		}

		if (FrameIndex >= _spriteFrames)
		{
			FrameIndex -= _spriteFrames;
		}

		SpriteManager.DrawSpriteExt(Definition, FrameIndex, X, Y, XScale, YScale, Rotation, Color, 1.0); // TODO : alpha is probably packed into color...
	}

	public override void Destroy()
	{
		DrawManager.Unregister(this);
	}
}