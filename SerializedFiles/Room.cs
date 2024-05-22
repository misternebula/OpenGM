using UndertaleModLib.Models;

namespace DELTARUNITYStandalone.SerializedFiles;

[Serializable]
public class Room
{
	public int AssetId;
	public string Name;
	public uint SizeX;
	public uint SizeY;
	public bool Persistent;
	public int CreationCodeId;
	public float GravityX;
	public float GravityY;

	public uint CameraWidth;
	public uint CameraHeight;

	public List<Layer> Layers = new();
}

[Serializable]
public class Layer
{
	public string LayerName;
	public uint LayerID;
	public int LayerDepth;
	public UndertaleRoom.LayerType LayerType;
	public float XOffset;
	public float YOffset;
	public float HSpeed;
	public float VSpeed;
	public bool IsVisible;

	public List<GameObject> Instances_Objects = new();

	public bool Background_Visible;
	public bool Background_Foreground;
	public int Background_SpriteID;
	public bool Background_TilingH;
	public bool Background_TilingV;
	public bool Background_Stretch;
	public uint Background_Color;
	public float Background_FirstFrame;
	public float Background_AnimationSpeed;
	public AnimationSpeedType Background_AnimationType;

	public List<GamemakerTile> Assets_LegacyTiles = new();
}

public class GamemakerTile
{
	public int X;
	public int Y;
	public int Definition;
	public uint SourceLeft;
	public uint SourceTop;
	public uint SourceWidth;
	public uint SourceHeight;
	public int Depth;
	public uint InstanceID;
	public float ScaleX;
	public float ScaleY;
	public uint Color;
}
