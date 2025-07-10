using OpenGM.Loading;
using OpenGM.SerializedFiles;
using OpenTK.Mathematics;

namespace OpenGM.Rendering;
public class GMTile : DrawWithDepth
{
    public double X;
    public double Y;

    public int Definition;
    public required bool SpriteMode;

    public int left;
    public int top;
    public int width;
    public int height;

    public bool Visible;
    public double XScale;
    public double YScale;
    public uint Color;

    public GMTile()
    {
        DrawManager.Register(this);
    }

    public override void Draw()
    {
        if (Definition == -1 || !Visible)
        {
            return;
        }

        SpritePageItem sprite; 

        if (SpriteMode == false)
        {
            var background = GameLoader.Backgrounds[Definition];
            sprite = background.Texture!;
        }
        else
        {
            sprite = SpriteManager.GetSpritePageItem(Definition, 0);
        }

        var c = Color.ABGRToCol4();

        CustomWindow.Draw(new GMSpritePartJob()
        {
            texture = sprite,
            screenPos = new(X, Y),
            Colors = [c, c, c, c],
            origin = Vector2.Zero,
            left = left,
            top = top,
            width = width,
            height = height,
            scale = new(XScale, YScale),
            angle = 0
        });
    }

    public override void Destroy()
    {
        DrawManager.Unregister(this);
    }
}
