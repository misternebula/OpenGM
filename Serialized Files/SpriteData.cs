using Newtonsoft.Json;
using OpenTK.Mathematics;
using UndertaleModLib.Models;

namespace DELTARUNITYStandalone;

[Serializable]
public class SpriteData
{
	public int AssetIndex;
	public string Name;
	public uint Width;
	public uint Height;
	public int MarginLeft;
	public int MarginRight;
	public int MarginBottom;
	public int MarginTop;
	public uint BBoxMode;
	public UndertaleSprite.SepMaskType SepMasks;
	public int OriginX;
	public int OriginY;
	public List<SpritePageItem> Textures;
	// collision masks
	public float PlaybackSpeed;
	public AnimSpeedType PlaybackSpeedType;

	[JsonIgnore] public Vector2i Origin => new(OriginX, OriginY);

	[JsonIgnore] public Vector4i Margins => new(MarginLeft, MarginRight, MarginBottom, MarginTop);
}

[Serializable]
public class SpritePageItem
{
	public int SourcePosX;
	public int SourcePosY;
	public int SourceSizeX;
	public int SourceSizeY;
	public int TargetPosX;
	public int TargetPosY;
	public int TargetSizeX;
	public int TargetSizeY;
	public int BSizeX;
	public int BSizeY;
	public string Page;
}
