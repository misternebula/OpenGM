using System.Drawing;
using OpenGM.IO;
using OpenGM.Loading;
using OpenTK.Mathematics;

namespace OpenGM.Rendering;
public class GMOldBackground
{
    public bool Enabled;
    public bool Foreground;
    public int Definition;
    public Vector2i Position;
    public bool TilingX;
    public bool TilingY;
    public Vector2i Speed;
    public bool Stretch;

    public void Draw()
    {
        if (Definition == -1)
        {
            return;
        }

        var background = GameLoader.Backgrounds[Definition];
        var sprite = background.Texture;

        if (sprite == null)
        {
            DebugLog.Log(" - sprite is null");
            return;
        }

        // TODO : handle tiling
        // TODO : handle strech
        // TODO : handle foreground
        // TODO : handle speed

        CustomWindow.Draw(new GMSpriteJob()
        {
            texture = sprite,
            screenPos = Position,
            Colors = [Color4.White, Color4.White, Color4.White, Color4.White],
            angle = 0,
            origin = Vector2.Zero,
            scale = Vector2d.One
        });
    }
}
