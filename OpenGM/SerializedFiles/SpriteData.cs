using MemoryPack;
using Newtonsoft.Json;
using OpenTK.Mathematics;
using UndertaleModLib.Models;

namespace OpenGM.SerializedFiles;

[MemoryPackable]
public partial class SpriteData
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

	[MemoryPackIgnore] public Vector2i Origin => new(OriginX, OriginY);

	[MemoryPackIgnore] public Vector4i Margins => new(MarginLeft, MarginRight, MarginBottom, MarginTop);
}

[MemoryPackable]
public partial class SpritePageItem
{
	/// <summary>
	/// YYTPageEntry.x
	/// </summary>
	public int SourcePosX;
	/// <summary>
	/// YYTPageEntry.y
	/// </summary>
	public int SourcePosY;
	/// <summary>
	/// YYTPageEntry.w
	/// </summary>
	public int SourceSizeX;
	/// <summary>
	/// YYTPageEntry.h
	/// </summary>
	public int SourceSizeY;

	/// <summary>
	/// YYTPageEntry.XOffset
	/// </summary>
	public int TargetPosX;

	/// <summary>
	/// YYTPageEntry.YOffset
	/// </summary>
	public int TargetPosY;

	/// <summary>
	/// YYTPageEntry.CropWidth
	/// </summary>
	public int TargetSizeX;

	/// <summary>
	/// YYTPageEntry.CropHeight
	/// </summary>
	public int TargetSizeY;

	/// <summary>
	/// YYTPageEntry.ow
	/// </summary>
	public int BSizeX;

	/// <summary>
	/// YYTPageEntry.oh
	/// </summary>
	public int BSizeY;

	/// <summary>
	/// YYTPageEntry.tp
	/// </summary>
	public string Page = null!;
}
