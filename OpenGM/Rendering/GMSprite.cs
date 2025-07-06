using OpenGM.SerializedFiles;
using System.Diagnostics;
using UndertaleModLib.Models;

namespace OpenGM.Rendering;
public class GMSprite : DrawWithDepth
{
	public int Definition;
	public int X;
	public int Y;
	public double XScale;
	public double YScale;
	public uint Color;
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

		if (!Element.Layer.Visible)
		{
			// TODO : does animation still happen if not visible?
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

		var col = (int)(Color & 0x00FFFFFF);
		var alpha = ((Color & 0xFF000000) >> 6) / 255.0;

		SpriteManager.DrawSpriteExt(Definition, FrameIndex, X, Y, XScale, YScale, Rotation, col, alpha);
	}

	public override void Destroy()
	{
		DrawManager.Unregister(this);
	}
}