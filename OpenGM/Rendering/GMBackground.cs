﻿using OpenGM.SerializedFiles;
using System.Xml.Linq;
using System;

namespace OpenGM.Rendering;

public class GMBackground : DrawWithDepth
{
	public CLayerBackgroundElement Element;

	public GMBackground(CLayerBackgroundElement element)
	{
		DrawManager.Register(this);
		Element = element;
		_currentFrame = element.FirstFrame;
	}

	private int _currentFrame;

	public override void Draw()
	{
		if (Element == null || !Element.Visible || !Element.Layer.Visible)
		{
			return;
		}

		// TODO : account for tiling
		// TODO : account for stretch
		// TODO : work out what foreground does
		// TODO : account for animations

		var sprite = SpriteManager.GetSpritePage(Element.Index, _currentFrame);
		var origin = SpriteManager.GetSpriteOrigin(Element.Index);

		CustomWindow.Draw(new GMSpriteJob()
		{
			texture = sprite,
			origin = origin,
			screenPos = new OpenTK.Mathematics.Vector2d(Element.Layer.X, Element.Layer.Y),
			scale = OpenTK.Mathematics.Vector2d.One,
			angle = 0,
			blend = Element.Color.ABGRToCol4(Element.Alpha)
		});
	}

	public override void Destroy()
	{
		DrawManager.Unregister(this);
	}
}
