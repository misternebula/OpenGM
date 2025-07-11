using OpenTK.Mathematics;
using UndertaleModLib.Models;

namespace OpenGM.SerializedFiles;

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
    public List<GameObject> LooseObjects = new();
    public List<Tile> Tiles = new();
    public List<OldBackground> OldBackgrounds = new();
}

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

public class Tile
{
    public int X;
    public int Y;
    public int Definition;
    public required bool SpriteMode;
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

public class OldBackground
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

public abstract partial class CLayerElementBase
{
    public ElementType Type;
    public int Id;
    public string Name = null!;

    public LayerContainer Layer = null!;
}

public class CLayerTilemapElement : CLayerElementBase
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

    public TileBlob[,] TilesData = null!;
}

public class CLayerBackgroundElement : CLayerElementBase
{
    public bool Visible;
    public bool Foreground;
    public int Index;
    public bool HTiled;
    public bool VTiled;
    public double XScale;
    public double YScale;
    public bool Stretch;
    public uint Color;
    public int FirstFrame;
    public double AnimationSpeed;
    public double Alpha;
    public AnimationSpeedType AnimationSpeedType;
}

public class CLayerTileElement : CLayerElementBase
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
    public uint Color;
    public bool SpriteMode;
}

public class CLayerSpriteElement : CLayerElementBase
{
    public int Definition;
    public int X;
    public int Y;
    public double ScaleX;
    public double ScaleY;
    public uint Color;
    public double AnimationSpeed;
    public AnimationSpeedType AnimationSpeedType;
    public double FrameIndex;
    public double Rotation;
}

public class CLayerParticleElement : CLayerElementBase
{
    public int SystemID;
}

// not serialized
public class TileBlob
{
    public int TileIndex; // bits 0-18
    public bool Mirror; // bit 28
    public bool Flip; // bit 29
    public bool Rotate; // bit 30
}
