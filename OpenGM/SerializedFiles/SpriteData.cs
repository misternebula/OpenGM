using OpenTK.Mathematics;
using UndertaleModLib.Models;

namespace OpenGM.SerializedFiles;

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

    public Vector2i Origin => new(OriginX, OriginY);

    public Vector4i Margins => new(MarginLeft, MarginRight, MarginBottom, MarginTop);
}

public class SpritePageItem
{
    /// <summary>
    /// YYTPageEntry.x
    /// </summary>
    public int SourceX;
    /// <summary>
    /// YYTPageEntry.y
    /// </summary>
    public int SourceY;
    /// <summary>
    /// YYTPageEntry.w
    /// </summary>
    public int SourceWidth;
    /// <summary>
    /// YYTPageEntry.h
    /// </summary>
    public int SourceHeight;

    /// <summary>
    /// YYTPageEntry.XOffset
    /// </summary>
    public int TargetX;

    /// <summary>
    /// YYTPageEntry.YOffset
    /// </summary>
    public int TargetY;

    /// <summary>
    /// YYTPageEntry.CropWidth
    /// </summary>
    public int TargetWidth;

    /// <summary>
    /// YYTPageEntry.CropHeight
    /// </summary>
    public int TargetHeight;

    /// <summary>
    /// YYTPageEntry.ow
    /// </summary>
    public int BoundingWidth;

    /// <summary>
    /// YYTPageEntry.oh
    /// </summary>
    public int BoundingHeight;

    /// <summary>
    /// YYTPageEntry.tp
    /// </summary>
    public string Page = null!;
}
