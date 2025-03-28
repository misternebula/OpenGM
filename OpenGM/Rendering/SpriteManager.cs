using OpenGM.SerializedFiles;
using System.Drawing;
using OpenTK.Mathematics;
using OpenGM.IO;

namespace OpenGM.Rendering;

public static class SpriteManager
{
    // https://manual.gamemaker.io/monthly/en/GameMaker_Language/GML_Reference/Drawing/Colour_And_Alpha/draw_set_colour.htm
    public static int DrawColor = 16777215;
    // https://manual.gamemaker.io/monthly/en/GameMaker_Language/GML_Reference/Drawing/Colour_And_Alpha/draw_set_alpha.htm
    public static double DrawAlpha = 1;
    public static int FogColor = 0;
    public static bool FogEnabled = false;

    public static Dictionary<int, SpriteData> _spriteDict = new();

    public static SpriteData? GetSpriteAsset(int name)
    {
        if (name == -1)
        {
            return null;
        }

        if (_spriteDict.ContainsKey(name))
        {
            return _spriteDict[name];
        }

        DebugLog.LogError($"Sprite Dictionary does not contain {name}");
        return null;
    }

    public static Vector2i GetSpriteOrigin(int name)
    {
	    if (name == -1)
	    {
            DebugLog.LogWarning($"Tried to get origin of null sprite");
		    return Vector2i.Zero;
	    }

        return _spriteDict[name].Origin;
    }

    public static SpritePageItem GetSpritePage(string name, double index)
    {
        return GetSpritePage(AssetIndexManager.GetIndex(name), index);
    }

    public static SpritePageItem GetSpritePage(int id, double index)
    {
        var subimages = _spriteDict[id].Textures;
        index %= subimages.Count;
        var floored = CustomMath.FloorToInt(index);

        if (floored < 0)
        {
	        throw new NotImplementedException($"Trying to get SpritePageItem for index {index} floored:{floored}");
        }

		return subimages[floored];
    }

    public static void DrawSprite(int name, double index, double x, double y)
        => DrawSpriteExt(name, index, x, y, 1, 1, 0, 16777215, 1);

    public static void DrawSpriteExt(int name, double index, double x, double y, double xscale, double yscale, double rot, int blend, double alpha)
    {
        var sprite = GetSpritePage(name, index);
        var origin = GetSpriteOrigin(name);

        CustomWindow.Draw(new GMSpriteJob()
        {
            texture = sprite,
            screenPos = new Vector2d(x, y),
            angle = rot,
            scale = new Vector2d(xscale, yscale),
            blend = blend.ABGRToCol4(),
            alpha = alpha,
            origin = origin,
            fogEnabled = FogEnabled,
            fogColor = FogColor.ABGRToCol4()
        });
    }

    public static void DrawSpritePart(int name, int index, int left, int top, int width, int height, double x, double y)
    {
        DrawSpritePartExt(name, index, left, top, width, height, x, y, 1, 1, 16777215, 1);
    }

    public static void DrawSpritePartExt(int name, double index, int left, int top, int width, int height, double x, double y, double xscale, double yscale, int blend, double alpha)
    {
        //var sprite = SpritePart(name, index, left, top, width, height);
        var sprite = GetSpritePage(name, index);
        CustomWindow.Draw(new GMSpritePartJob()
        {
            texture = sprite,
            screenPos = new Vector2d(x, y),
            angle = 0,
            scale = new Vector2d(xscale, yscale),
            blend = blend.ABGRToCol4(),
            alpha = alpha,
            origin = Vector2.Zero,
            left = left,
            top = top,
            width = width,
            height = height
        });
    }

    public static void DrawSelf(GamemakerObject obj)
    {
        if (!obj.visible || obj.sprite_index == -1)
        {
            return;
        }

        var image_index = GetIndexFromImageIndex(obj.image_index + obj.frame_overflow, GetNumberOfFrames(obj.sprite_index));
        obj.frame_overflow = 0;

        DrawSpriteExt(obj.sprite_index, image_index, obj.x, obj.y, obj.image_xscale, obj.image_yscale, obj.image_angle, obj.image_blend, obj.image_alpha);
    }

    private static int GetIndexFromImageIndex(double index, int imageNumber)
    {
	    var ind = CustomMath.FloorToInt(index) % imageNumber;
	    if (ind < 0)
	    {
		    ind += imageNumber;
	    }

	    return ind;
    }

    public static void draw_sprite_stretched(int name, int index, double x, double y, double w, double h, int color, double alpha)
    {
        var sprite = GetSpritePage(name, index);

        var spriteWidth = sprite.TargetSizeX;
        var spriteHeight = sprite.TargetSizeY;

        CustomWindow.Draw(new GMSpriteJob()
        {
            texture = sprite,
            screenPos = new Vector2d(x, y),
            angle = 0,
            scale = new Vector2d(w / spriteWidth, h / spriteHeight),
            blend = color.ABGRToCol4(),
            alpha = alpha,
            origin = Vector2.Zero
        });
    }

    public static int GetNumberOfFrames(int name)
    {
        return _spriteDict[name].Textures.Count;
    }
}
