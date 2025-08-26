using OpenGM.SerializedFiles;
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

    public GMSprite(CLayerSpriteElement element, int layerDepth)
    {
        Element = element;
        DrawManager.Register(this);

        var c = element.Color;
        var blend = (int)(c & 0x00FFFFFF);
        var alpha = ((c & 0xFF000000) >> (4 * 6)) / 255.0;

        Definition = element.Definition;
        X = element.X;
        Y = element.Y;
        XScale = element.ScaleX;
        YScale = element.ScaleY;
        Blend = blend;
        Alpha = alpha;
        AnimationSpeed = element.AnimationSpeed;
        AnimationSpeedType = element.AnimationSpeedType;
        FrameIndex = element.FrameIndex;
        Rotation = element.Rotation;
        instanceId = element.Id;
        depth = layerDepth;
    }

    private int _spriteFrames;

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

        var fps = TimingManager.FPS;

        if (AnimationSpeedType != AnimationSpeedType.FPS)
        {
            fps = 1;
        }

        FrameIndex += (AnimationSpeed / fps) * SpriteManager.GetSpriteAsset(Definition)!.PlaybackSpeed;

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