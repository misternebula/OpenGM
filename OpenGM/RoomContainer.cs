using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenGM.SerializedFiles;

namespace OpenGM;
public class RoomContainer
{
	public RoomContainer(Room room)
	{
		RoomAsset = room;
	}

	public Room RoomAsset;

	public int AssetId => RoomAsset.AssetId;
	public int CameraWidth => RoomAsset.CameraWidth;
	public int CameraHeight => RoomAsset.CameraHeight;
	public bool Persistent => RoomAsset.Persistent;
	public int SizeX => RoomAsset.SizeX;
	public int SizeY => RoomAsset.SizeY;

	public Dictionary<int, LayerContainer> Layers = new();
}

public class LayerContainer
{
	public LayerContainer(Layer layer)
	{
		LayerAsset = layer;
		X = layer.XOffset;
		Y = layer.YOffset;
		VSpeed = layer.VSpeed;
		HSpeed = layer.HSpeed;
		Depth = layer.LayerDepth;
	}

	public Layer LayerAsset;

	public List<DrawWithDepth> Elements = new();

	public int ID => LayerAsset.LayerID;
	public string Name => LayerAsset.LayerName;
	public float X;
	public float Y;
	public float VSpeed;
	public float HSpeed;
	public int Depth;
}
