using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DELTARUNITYStandalone.SerializedFiles;

namespace DELTARUNITYStandalone;
public class RoomContainer
{
	public RoomContainer(Room room)
	{
		RoomAsset = room;
	}

	public Room RoomAsset;

	public int AssetId => RoomAsset.AssetId;
	public uint CameraWidth => RoomAsset.CameraWidth;
	public uint CameraHeight => RoomAsset.CameraHeight;
	public bool Persistent => RoomAsset.Persistent;
	public uint SizeX => RoomAsset.SizeX;
	public uint SizeY => RoomAsset.SizeY;

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

	public uint ID => LayerAsset.LayerID;
	public string Name => LayerAsset.LayerName;
	public float X;
	public float Y;
	public float VSpeed;
	public float HSpeed;
	public int Depth;
}
