﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenGM.Rendering;
using OpenGM.SerializedFiles;
using OpenGM.VirtualMachine;

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
	public GamemakerObject? FollowObject => RoomAsset.FollowsObject == -1 ? null : InstanceManager.instances.FirstOrDefault(x => x.Value.Definition.AssetId == RoomAsset.FollowsObject).Value;

	public Dictionary<int, LayerContainer> Layers = new();
	public List<DrawWithDepth> Tiles = new();
	public List<GamemakerObject> LooseObjects = new();
	public List<GMOldBackground> OldBackgrounds = new();

	public LayerContainer GetLayer(object? layer_id)
	{
		if (layer_id is string s)
		{
			return Layers.FirstOrDefault(x => x.Value.Name == s).Value;
		}
		else
		{
			var id = layer_id.Conv<int>();
			return RoomManager.CurrentRoom.Layers[id];
		}
	}
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
		Visible = layer.IsVisible;
	}

	public Layer LayerAsset;

	public List<DrawWithDepth> ElementsToDraw = new();

	public int ID => LayerAsset.LayerID;
	public string Name => LayerAsset.LayerName;
	public float X;
	public float Y;
	public float VSpeed;
	public float HSpeed;
	public int Depth;

	public bool Visible;
}
