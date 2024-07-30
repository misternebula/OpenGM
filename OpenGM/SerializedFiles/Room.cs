using Newtonsoft.Json;
using UndertaleModLib.Models;

namespace OpenGM.SerializedFiles;

[Serializable]
public class Room
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
}

[Serializable]
public class Layer
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

[Serializable]
public class CLayerElementBase
{
	public ElementType Type;
	public int Id;
	public string Name = null!;

	[JsonIgnore]
	public LayerContainer Layer = null!;
}

[Serializable]
public class CLayerTilemapElement : CLayerElementBase
{
	public int BackgroundIndex;
	public float x;
	public float y;
	public int Width;
	public int Height;

	/// <summary>
	/// Just for compressed storage, don't actually use this for displaying tiles.
	/// </summary>
	public uint[][] Tiles = null!;

	[JsonIgnore]
	public TileBlob[,] TilesData = null!;
}

[Serializable]
public class CLayerBackgroundElement : CLayerElementBase
{
	public bool Visible;
	public bool Foreground;
	public int Index;
	public bool HTiled;
	public bool VTiled;
	public bool Stretch;
	public int Color;
	public double Alpha;
	public int FirstFrame;
	public double AnimationSpeed;
	public AnimationSpeedType AnimationSpeedType;
}

public class GamemakerTile
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
	public int Color;
}

public class TileBlob
{
	public int TileIndex; // bits 0-18
	public bool Mirror; // bit 28
	public bool Flip; // bit 29
	public bool Rotate; // bit 30
}
