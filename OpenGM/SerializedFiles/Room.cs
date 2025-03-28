using MemoryPack;
using Newtonsoft.Json;
using OpenGM.Rendering;
using OpenTK.Mathematics;
using UndertaleModLib.Models;

namespace OpenGM.SerializedFiles;

[MemoryPackable]
public partial class Room
{
	public int AssetId;
	public string Name = null!;
	public int SizeX;
	public int SizeY;
	public bool Persistent;
	public int CreationCodeId;
	public float GravityX;
	public float GravityY;

	public int CameraWidth;
	public int CameraHeight;
	public int FollowsObject;

	public List<Layer> Layers = new();
	public List<GameObject> LooseObjects = new();
	public List<Tile> Tiles = new();
	public List<OldBackground> OldBackgrounds = new();
}

[MemoryPackable]
public partial class Layer
{
	public int LayerID;
	public int LayerDepth;
	public float XOffset;
	public float YOffset;
	public float HSpeed;
	public float VSpeed;
	public bool IsVisible;
	public string LayerName = null!;
	public List<CLayerElementBase> Elements = new();
}

[MemoryPackable]
public partial class Tile
{
	public int X;
	public int Y;
	public int Definition;
	public int SourceLeft;
	public int SourceTop;
	public int SourceWidth;
	public int SourceHeight;
	public int Depth;
	public int InstanceID;
	public float ScaleX;
	public float ScaleY;
	public uint Color;
}

[MemoryPackable]
public partial class OldBackground
{
	public bool Enabled;
	public bool Foreground;
	public int Definition;
	public Vector2i Position;
	public bool TilingX;
	public bool TilingY;
	public Vector2i Speed;
	public bool Stretch;
}

public enum ElementType
{
	Undefined,
	Background,
	Instance,
	OldTilemap,
	Sprite,
	Tilemap,
	ParticleSystem,
	Tile,
	Sequence,
	Text
}

[MemoryPackable]
[MemoryPackUnion(0, typeof(GameObject))]
[MemoryPackUnion(1, typeof(CLayerTilemapElement))]
[MemoryPackUnion(2, typeof(CLayerBackgroundElement))]
[MemoryPackUnion(3, typeof(CLayerTileElement))]
[MemoryPackUnion(4, typeof(CLayerSpriteElement))]
public abstract partial class CLayerElementBase
{
	public ElementType Type;
	public int Id;
	public string Name = null!;

	[MemoryPackIgnore]
	public LayerContainer Layer = null!;
}

[MemoryPackable]
public partial class CLayerTilemapElement : CLayerElementBase
{
	public int BackgroundIndex;
	public double x;
	public double y;
	public int Width;
	public int Height;

	/// <summary>
	/// Just for compressed storage, don't actually use this for displaying tiles.
	/// </summary>
	public uint[][] Tiles = null!;

	[MemoryPackIgnore]
	public TileBlob[,] TilesData = null!;
}

[MemoryPackable]
public partial class CLayerBackgroundElement : CLayerElementBase
{
	public bool Visible;
	public bool Foreground;
	public int Index;
	public bool HTiled;
	public bool VTiled;
	public double XScale;
	public double YScale;
	public bool Stretch;
	public int Color;
	public double Alpha;
	public int FirstFrame;
	public double AnimationSpeed;
	public AnimationSpeedType AnimationSpeedType;
}

[MemoryPackable]
public partial class CLayerTileElement : CLayerElementBase
{
	public bool Visible;
	public int X;
	public int Y;
	public int Definition;
	public int SourceLeft;
	public int SourceTop;
	public int SourceWidth;
	public int SourceHeight;
	//public int Depth;
	//public int InstanceID;
	public float ScaleX;
	public float ScaleY;
	public int Color;
}

[MemoryPackable]
public partial class CLayerSpriteElement : CLayerElementBase
{
	public int Definition;
	public int X;
	public int Y;
	public double ScaleX;
	public double ScaleY;
	public int Color;
	public double AnimationSpeed;
	public AnimationSpeedType AnimationSpeedType;
	public double FrameIndex;
	public double Rotation;
}

// not serialized
public class TileBlob
{
	public int TileIndex; // bits 0-18
	public bool Mirror; // bit 28
	public bool Flip; // bit 29
	public bool Rotate; // bit 30
}
