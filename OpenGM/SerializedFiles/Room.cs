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
	public string LayerName = null!;
	public int LayerID;
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
	public int Background_Color;
	public float Background_FirstFrame;
	public float Background_AnimationSpeed;
	public AnimationSpeedType Background_AnimationType;

	public List<GamemakerTile> Assets_LegacyTiles = new();

	public int Tiles_SizeX;
	public int Tiles_SizeY;
	public int Tiles_TileSet;
	public int[][] Tiles_TileData = null!;
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
