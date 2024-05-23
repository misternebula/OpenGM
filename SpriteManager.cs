using DELTARUNITYStandalone.SerializedFiles;
using System.Drawing;
using OpenTK.Mathematics;

namespace DELTARUNITYStandalone;

public static class SpriteManager
{
	public static int DrawColor = 16777215;
	public static double DrawAlpha = 1;
	public static int FogColor = 0;
	public static bool FogEnabled = false;

	public static Dictionary<int, SpriteData> _spriteDict = new();

	public static SpriteData GetSpriteAsset(int name)
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
		return subimages[CustomMath.FloorToInt((float)index)];
	}

	public static void DrawSprite(int name, double index, double x, double y)
		=> DrawSpriteExt(name, index, x, y, 1, 1, 0, 16777215, 1);

	public static void DrawSpriteExt(int name, double index, double x, double y, double xscale, double yscale, double rot, int blend, double alpha)
	{
		var sprite = GetSpritePage(name, index);
		var origin = GetSpriteOrigin(name);

		CustomWindow.RenderJobs.Add(new GMSpriteJob()
		{
			texture = sprite,
			screenPos = new Vector2((float)x, (float)y),
			angle = rot,
			scale = new Vector2((float)xscale, (float)yscale),
			blend = blend.BGRToColor(),
			alpha = alpha,
			origin = origin,
			fogEnabled = FogEnabled,
			fogColor = FogColor.BGRToColor()
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
		CustomWindow.RenderJobs.Add(new GMSpritePartJob()
		{
			texture = sprite,
			screenPos = new Vector2((float)x, (float)y),
			angle = 0,
			scale = new Vector2((float)xscale, (float)yscale),
			blend = blend.BGRToColor(),
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

		DrawSpriteExt(obj.sprite_index, obj.image_index, obj.x, obj.y, obj.image_xscale, obj.image_yscale, obj.image_angle, obj.image_blend, obj.image_alpha);
	}

	public static void draw_sprite_stretched(int name, int index, double x, double y, double w, double h)
	{
		var sprite = GetSpritePage(name, index);

		var spriteWidth = sprite.TargetSizeX;
		var spriteHeight = sprite.TargetSizeY;

		CustomWindow.RenderJobs.Add(new GMSpriteJob()
		{
			texture = sprite,
			screenPos = new Vector2((float)x, (float)y),
			angle = 0,
			scale = new Vector2((float)w / spriteWidth, (float)h / spriteHeight),
			blend = Color.White,
			alpha = 1,
			origin = Vector2.Zero
		});
	}

	public static int GetNumberOfFrames(int name)
	{
		return _spriteDict[name].Textures.Count;
	}
}
