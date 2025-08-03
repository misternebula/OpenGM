using ImageMagick;
using OpenGM.IO;
using OpenGM.SerializedFiles;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using UndertaleModLib.Models;

namespace OpenGM.Rendering;

public static class SpriteManager
{
    // https://manual.gamemaker.io/monthly/en/GameMaker_Language/GML_Reference/Drawing/Colour_And_Alpha/draw_set_colour.htm
    public static int DrawColor = 16777215;
    // https://manual.gamemaker.io/monthly/en/GameMaker_Language/GML_Reference/Drawing/Colour_And_Alpha/draw_set_alpha.htm
    public static double DrawAlpha = 1;

    public static Dictionary<int, SpriteData> _spriteDict = new();

    public static bool SpriteExists(int id) => id != -1 && _spriteDict.ContainsKey(id);

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

    public static SpritePageItem GetSpritePageItem(string name, double index)
    {
        return GetSpritePageItem(AssetIndexManager.GetIndex(AssetType.sprites, name), index);
    }

    public static SpritePageItem GetSpritePageItem(int id, double index)
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
        if (index < 0 || name < 0) 
        {
            DebugLog.LogWarning($"Tried to draw sprite {name} with index {index}.");
            return;
        }

        var sprite = GetSpritePageItem(name, index);
        var origin = GetSpriteOrigin(name);

        var c = blend.ABGRToCol4(alpha);

        CustomWindow.Draw(new GMSpriteJob()
        {
            texture = sprite,
            screenPos = new Vector2d(x, y),
            angle = rot,
            scale = new Vector2d(xscale, yscale),
            Colors = [c, c, c, c],
            origin = origin,
            //fogEnabled = FogEnabled,
            //fogColor = FogColor.ABGRToCol4(1) // https://github.com/YoYoGames/GameMaker-HTML5/blob/develop/scripts/functions/Function_D3D.js#L1095
        });
    }

    public static void DrawSpritePart(int name, int index, int left, int top, int width, int height, double x, double y)
    {
        DrawSpritePartExt(name, index, left, top, width, height, x, y, 1, 1, 16777215, 1);
    }

    public static void DrawSpritePartExt(int name, double index, int left, int top, int width, int height, double x, double y, double xscale, double yscale, int blend, double alpha)
    {
        //var sprite = SpritePart(name, index, left, top, width, height);
        var sprite = GetSpritePageItem(name, index);
        var c = blend.ABGRToCol4(alpha);
        CustomWindow.Draw(new GMSpritePartJob()
        {
            texture = sprite,
            screenPos = new Vector2d(x, y),
            angle = 0,
            scale = new Vector2d(xscale, yscale),
            Colors = [c, c, c, c],
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
        var sprite = GetSpritePageItem(name, index);

        var spriteWidth = sprite.TargetWidth;
        var spriteHeight = sprite.TargetHeight;

        var c = color.ABGRToCol4(alpha);

        CustomWindow.Draw(new GMSpriteJob()
        {
            texture = sprite,
            screenPos = new Vector2d(x, y),
            angle = 0,
            scale = new Vector2d(w / spriteWidth, h / spriteHeight),
            Colors = [c, c, c, c],
            origin = Vector2.Zero
        });
    }

    public static int GetNumberOfFrames(int name)
    {
        if (!_spriteDict.TryGetValue(name, out var value))
        {
            DebugLog.LogWarning($"Tried to get number of frames for sprite {name} (count = {_spriteDict.Count})");
            return 0;
        }

        return value.Textures.Count;
    }


    public static int sprite_create_from_surface(int surfaceId, double x, double y, int w, int h, bool removeback, bool smooth, int xorig, int yorig)
    {
        // https://github.com/YoYoGames/GameMaker-HTML5/blob/develop/scripts/functions/Function_Sprite.js#L485
        // https://github.com/YoYoGames/GameMaker-HTML5/blob/develop/scripts/yyWebGL.js#L4370

        // we have to do a song and dance because sprites require texture pages and whatnot to draw
        
        var spriteId = _spriteDict.Keys.Max() + 1; // this probably breaks sometimes
        var texturePageName = $"sprite_create_from_surface {spriteId}"; // needed for texture page lookup

        // make a copy of the texture. theres better ways to do this but this should work
        SurfaceManager.BindSurfaceTexture(surfaceId);
        var pixels = new byte[w * h * 4];
        unsafe
        {
            fixed (byte* ptr = pixels)
                GL.ReadPixels(0, 0, w, h, PixelFormat.Rgba, PixelType.UnsignedByte, (IntPtr)ptr);
        }
        // store it as a "page". its really just one texture that the sprite will use to draw
        var imageResult = new MagickImage(pixels, new PixelReadSettings((uint)w, (uint)h, StorageType.Char, PixelMapping.RGBA));
        PageManager.UploadTexture(texturePageName, imageResult);

        // create a sprite with the single texture
        var spritePage = new SpritePageItem
        {
            SourceX = 0,
            SourceY = 0,
            SourceWidth = w,
            SourceHeight = h,
            TargetX = 0,
            TargetY = 0,
            TargetWidth = w,
            TargetHeight = h,
            BoundingWidth = w,
            BoundingHeight = h,
            Page = texturePageName
        };
        var sprite = new SpriteData
        {
            AssetIndex = spriteId,
            Name = texturePageName,
            Width = w,
            Height = h,
            MarginLeft = 0,
            MarginRight = 0,
            MarginBottom = 0,
            MarginTop = 0,
            BBoxMode = 0,
            SepMasks = UndertaleSprite.SepMaskType.AxisAlignedRect,
            OriginX = xorig,
            OriginY = yorig,
            Textures = [spritePage],
            CollisionMasks = [], // no idea
            PlaybackSpeed = 0,
            PlaybackSpeedType = AnimSpeedType.FramesPerSecond
        };
        _spriteDict.Add(sprite.AssetIndex, sprite);

        return sprite.AssetIndex;
    }

    public static bool sprite_delete(int index)
    {
        // only used with sprite_create_from_surface

        // remove the sprite
        if (_spriteDict.Remove(index, out var sprite))
        {
            // remove the copied texture
            PageManager.DeleteTexture(sprite.Textures[0].Page);
            return true;
        }

        return false;
    }
}
