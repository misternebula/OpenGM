using OpenGM.SerializedFiles;
using System.Xml.Linq;
using System;

namespace OpenGM.Rendering;

public class GMBackground : DrawWithDepth
{
	private CLayerBackgroundElement _element;

	public GMBackground(CLayerBackgroundElement element)
	{
		DrawManager.Register(this);
		_element = element;
		_currentFrame = element.FirstFrame;
	}

	private int _currentFrame;

	public override void Draw()
	{
		if (!_element.Visible || !_element.Layer.Visible)
		{
			return;
		}

		// TODO : account for tiling
		// TODO : account for stretch
		// TODO : work out what foreground does
		// TODO : account for animations

		var sprite = SpriteManager.GetSpritePage(_element.Index, _currentFrame);
		var origin = SpriteManager.GetSpriteOrigin(_element.Index);

		CustomWindow.RenderJobs.Add(new GMSpriteJob()
		{
			texture = sprite,
			origin = origin,
			screenPos = new OpenTK.Mathematics.Vector2d(_element.Layer.X, _element.Layer.Y),
			scale = OpenTK.Mathematics.Vector2d.One,
			angle = 0,
			blend = _element.Color.ABGRToCol4(),
			alpha = _element.Color.ABGRToCol4().A
		});
	}

	public override void Destroy()
	{
		DrawManager.Unregister(this);
	}
}
