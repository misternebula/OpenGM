﻿using OpenGM.SerializedFiles;
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
    public int Blend;
    public double Alpha;
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

        var layer = Element.Layer;

        SpriteManager.DrawSpriteExt(Definition, FrameIndex, layer.X + X, layer.Y + Y, XScale, YScale, Rotation, Blend, Alpha);
    }

    public void SetSprite(int definition)
    {
        var sprite = SpriteManager.GetSpriteAsset(definition)!;

        // TODO: probably need to change more than that
        Definition = definition;
        AnimationSpeed = sprite.PlaybackSpeed;
        AnimationSpeedType = (AnimationSpeedType)sprite.PlaybackSpeedType;

        _spriteFrames = SpriteManager.GetNumberOfFrames(definition);
    }

    public override void Destroy()
    {
        DrawManager.Unregister(this);
    }
}