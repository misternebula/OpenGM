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

    public View[] Views = new View[8];

    public List<Layer> Layers = new();
    public List<GameObject> LooseObjects = new();
    public List<Tile> Tiles = new();
    public List<OldBackground> OldBackgrounds = new();
    
    public bool EnableViews;

    // show color
    // view clear screen? what is this?
    // clear display buffer
}

[MemoryPackable]
public partial class View
{
    public bool Enabled = false;
    public int PositionX = 0;
    public int PositionY = 0;
    public int SizeX;
    public int SizeY;
    public int PortPositionX = 0;
    public int PortPositionY = 0;
    public int PortSizeX;
    public int PortSizeY;
    public int BorderX = 32;
    public int BorderY = 32;
    public int SpeedX = -1;
    public int SpeedY = -1;
    public int FollowsObject = -1;
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
    public bool Rotate; // bit 30 (clockwise)

    public TileBlob(uint blobData)
    {
        TileIndex = (int)blobData & 0x7FFFF;
        Mirror = (blobData & (1 << 28)) != 0;
        Flip = (blobData & (1 << 29)) != 0;
        Rotate = (blobData & (1 << 30)) != 0;
    }

    public int ToNumber() 
    {
        var result = 0;
        result |= TileIndex;
        result |= (Mirror ? 1 : 0) << 28;
        result |= (Flip ? 1 : 0) << 29;
        result |= (Rotate ? 1 : 0) << 30;
        return result;
    }
}
