﻿using OpenGM.Rendering;
using OpenGM.SerializedFiles;
using OpenGM.VirtualMachine;

namespace OpenGM;
public class RoomContainer
{
    public RoomContainer(Room room)
    {
        RoomAsset = room;
        Persistent = room.Persistent;
    }

    public Room RoomAsset;

    public int AssetId => RoomAsset.AssetId;
    public bool Persistent;
    public int SizeX => RoomAsset.SizeX;
    public int SizeY => RoomAsset.SizeY;
    public RuntimeView[] Views = new RuntimeView[8];

    public Dictionary<int, LayerContainer> Layers = new();
    public List<DrawWithDepth> Tiles = new();
    public List<GamemakerObject> LooseObjects = new();
    public List<GMOldBackground> OldBackgrounds = new();

    public LayerContainer? GetLayer(object? layer_id)
    {
        if (layer_id is string s)
        {
            return Layers.FirstOrDefault(x => x.Value.Name == s).Value;
        }
        else
        {
            var id = layer_id.Conv<int>();
            return RoomManager.CurrentRoom.Layers.TryGetValue(id, out var value) ? value : null;
        }
    }

    public void RemoveMarked()
    {
        var destroyedList = new List<GamemakerObject>();

        foreach (var (_, instance) in InstanceManager.instances)
        {
            if (!instance.Marked)
            {
                continue;
            }

            destroyedList.Add(instance);
        }

        foreach (var instance in destroyedList)
        {
            DeleteInstance(instance);
        }
    }

    public void DeleteInstance(GamemakerObject obj)
    {
        // physics stuff

        // g_pLayerManager.RemoveInstance(this, pInst);
        InstanceManager.instances.Remove(obj.instanceId);
        InstanceManager.ObjectMap[obj.Definition.AssetId].Instances.Remove(obj);
        // this.m_Active.DeleteItem(pInst);
        // this.m_Deactive.DeleteItem(pInst);
        obj.Destroy();
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
