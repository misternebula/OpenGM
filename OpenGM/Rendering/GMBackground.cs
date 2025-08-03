using OpenGM.SerializedFiles;

namespace OpenGM.Rendering;

public class GMBackground : DrawWithDepth
{
    public CLayerBackgroundElement Element;

    public GMBackground(CLayerBackgroundElement element)
    {
        DrawManager.Register(this);
        Element = element;
        FrameIndex = element.FirstFrame;
    }

    public int FrameIndex;

    public override void Draw()
    {
        if (Element == null || !Element.Visible || !Element.Layer.Visible)
        {
            return;
        }

        // TODO : account for stretch
        // TODO : work out what foreground does
        // TODO : account for animations

        var sprite = SpriteManager.GetSpritePageItem(Element.Index, FrameIndex);
        var origin = SpriteManager.GetSpriteOrigin(Element.Index);

        var c = Element.Color.ABGRToCol4(Element.Alpha);

        var camWidth = ViewportManager.CurrentRenderingView!.ViewSize.X;
        var camHeight = ViewportManager.CurrentRenderingView!.ViewSize.Y;
        var camX = ViewportManager.CurrentRenderingView!.ViewPosition.X;
        var camY = ViewportManager.CurrentRenderingView!.ViewPosition.Y;

        var offsetX = 0;
        var offsetY = 0;
        var layerX = Element.Layer.X;
        var layerY = Element.Layer.Y;

        float TotalX() => offsetX + layerX + origin.X;
        float TotalY() => offsetY + layerY + origin.Y;

        if (Element.HTiled)
        {
            layerX %= sprite.BoundingWidth;
            if (layerX > 0)
            {
                layerX -= sprite.BoundingWidth;
            }
        }

        if (Element.VTiled)
        {
            layerY %= sprite.BoundingHeight;
            if (layerY > 0)
            {
                layerY -= sprite.BoundingHeight;
            }
        }
        
        do
        {
            do
            {
                CustomWindow.Draw(new GMSpriteJob()
                {
                    texture = sprite,
                    origin = origin,
                    screenPos = new(TotalX(), TotalY()),
                    scale = OpenTK.Mathematics.Vector2d.One,
                    angle = 0,
                    Colors = [c, c, c, c]
                });

                offsetY += sprite.BoundingHeight;
            } while (Element.VTiled && TotalY() < camY + camHeight);

            offsetY = 0;
            offsetX += sprite.BoundingWidth;
        } while (Element.HTiled && TotalX() < camX + camWidth);

        // TODO: is this how scrolling works?
        Element.Layer.X += Element.Layer.HSpeed;
        Element.Layer.Y += Element.Layer.VSpeed;

        if (Element.Layer.HSpeed != 0)
        {
            Element.Layer.X %= sprite.BoundingWidth;
        }

        if (Element.Layer.VSpeed != 0)
        {
            Element.Layer.Y %= sprite.BoundingHeight;
        }
    }

    public override void Destroy()
    {
        DrawManager.Unregister(this);
    }
}
