using Newtonsoft.Json;
using OpenTK.Mathematics;
using UndertaleModLib.Models;

namespace DELTARUNITYStandalone.SerializedFiles;

[Serializable]
public class SpriteData
{
	public int AssetIndex;
	public string Name = null!;
	public int Width;
	public int Height;
	public int MarginLeft;
	public int MarginRight;
	public int MarginBottom;
	public int MarginTop;
	public int BBoxMode;
	public UndertaleSprite.SepMaskType SepMasks;
	public int OriginX;
	public int OriginY;
	public List<SpritePageItem> Textures = null!;
	public List<byte[]> CollisionMasks = new();
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
	public string Page = null!;
}
