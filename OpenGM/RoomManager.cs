using OpenGM.IO;
using OpenGM.Loading;
using OpenGM.Rendering;
using OpenGM.SerializedFiles;
using OpenGM.VirtualMachine;
using OpenTK.Mathematics;
using UndertaleModLib.Models;
using EventType = OpenGM.VirtualMachine.EventType;

namespace OpenGM;

public static class RoomManager
{
    /// <summary>
    /// The room to change to.
    /// </summary>
    //public static Room? RoomToChangeTo = null;
    public static int New_Room;
    public static RoomContainer CurrentRoom = null!; // its set to room on start

    public static Dictionary<int, Room> RoomList = new();
    public static Dictionary<int, PersistentRoom> PersistentRooms = new();
    public static bool RoomLoaded = false;
    public static bool FirstRoom = false;

    public static void EndGame() // this doesnt clear all the lists, but thats okay. we're closing the game or starting a new one, which clears stuff
    {
        foreach (var (instanceId, instance) in InstanceManager.instances)
        {
            instance.Destroy();
        }

        InstanceManager.instances.Clear();

        CustomWindow.Instance.Close();
    }

    public static void StartGame()
    {
        FirstRoom = true;
        New_Room = 0;
        ChangeToWaitingRoom();
    }

    private static void SavePersistentRoom()
    {
        DebugLog.LogInfo("SAVING PERSISTENT ROOM");

        var instancesToSave = new List<GamemakerObject>();

        // events could destroy other objects, cant modify during iteration
        var instanceList = new List<GamemakerObject>(InstanceManager.instances.Values);
        foreach (var instance in instanceList)
        {
            if (instance == null)
            {
                continue;
            }

            if (instance.persistent) // dont bother saving thing that is already going to be saved
            {
                continue;
            }

            // TODO : should we call RoomEnd on marked objects? will any marked objects still exist at this point?
            if (instance.Marked || instance.Destroyed)
            {
                continue;
            }

            // TODO : if RoomEnd event creates objects, should they be saved??
            GamemakerObject.ExecuteEvent(instance, instance.Definition, EventType.Other, (int)EventSubtypeOther.RoomEnd);

            instancesToSave.Add(instance);
            DrawManager.Unregister(instance);
        }

        // todo: this seems dumb and slow. do it in a not dumb way
        //InstanceManager.instances = InstanceManager.instances.Where(x => !instancesToSave.Contains(x.Value)).ToDictionary();
        InstanceManager.ClearInstances(instancesToSave);

        foreach (var item in CurrentRoom.Tiles)
        {
            DrawManager.Unregister(item);
        }

        foreach (var layer in CurrentRoom.Layers)
        {
            foreach (var element in layer.Value.ElementsToDraw)
            {
                if (element is GMTilesLayer tileLayer)
                {
                    DrawManager.Unregister(tileLayer);
                }
                else if (element is GMBackground background)
                {
                    DrawManager.Unregister(background);
                }
                else if (element is GMSprite sprite)
                {
                    DrawManager.Unregister(sprite);
                }
            }
        }

        var persistentRoom = new PersistentRoom()
        {
            RoomAssetId = CurrentRoom.AssetId,
            Container = CurrentRoom,
            Instances = instancesToSave
        };
        PersistentRooms.Add(CurrentRoom.AssetId, persistentRoom);
    }

    private static void LoadPersistentRoom(Room room, PersistentRoom value)
    {
        DebugLog.LogInfo($"LOADING PERSISTENT ROOM AssetID:{room.AssetId} ({value.Container.RoomAsset.AssetId}) Name:{value.Container.RoomAsset.Name}");
        CurrentRoom = value.Container;

        // TODO : does instance creation order affect this?
        DebugLog.Log("Instances in persistent room:");
        foreach (var instance in value.Instances)
        {
            DebugLog.Log($"{instance.instanceId} - {instance.Definition.Name} Active:{instance.Active} Marked:{instance.Marked} Destroyed:{instance.Destroyed}");
            InstanceManager.instances.Add(instance.instanceId, instance);
            InstanceManager.ObjectMap[instance.Definition.AssetId].Instances.Add(instance);
            GamemakerObject.ExecuteEvent(instance, instance.Definition, EventType.Other, (int)EventSubtypeOther.RoomStart);
            DrawManager.Register(instance);
        }

        foreach (var tile in CurrentRoom.Tiles)
        {
            DrawManager.Register(tile);
        }

        foreach (var layer in CurrentRoom.Layers)
        {
            foreach (var element in layer.Value.ElementsToDraw)
            {
                if (element is GMTilesLayer tileLayer)
                {
                    DrawManager.Register(tileLayer);
                }
                else if (element is GMBackground background)
                {
                    DrawManager.Register(background);
                }
                else if (element is GMSprite sprite)
                {
                    DrawManager.Register(sprite);
                }
                else if (element is GMTile tile)
                {
                    DrawManager.Register(tile);
                }
            }
        }

        PersistentRooms.Remove(room.AssetId);
    }

    public static void ChangeToWaitingRoom()
    {
        // TODO : What's the difference between ENDOFGAME and ABORTGAME?
        if (New_Room is GMConstants.ROOM_ENDOFGAME or GMConstants.ROOM_ABORTGAME)
        {
            EndGame();
            return;
        }
        else if (New_Room == GMConstants.ROOM_RESTARTGAME)
        {
            EndGame();
            StartGame();
            return;
        }
        else if (New_Room == GMConstants.ROOM_LOADGAME)
        {
            throw new NotImplementedException();
        }

        var room = RoomList[New_Room];
        New_Room = -1;

        DebugLog.LogInfo($"Changing to {room!.Name}");

        if (CurrentRoom != null && CurrentRoom.Persistent)
        {
            SavePersistentRoom();
        }
        else if (CurrentRoom != null)
        {
            // remove everything that isn't persistent

            // events could destroy other objects, cant modify during iteration
            var instanceList = new List<GamemakerObject>(InstanceManager.instances.Values);

            foreach (var instance in instanceList)
            {
                if (instance == null)
                {
                    continue;
                }

                if (instance.persistent)
                {
                    DebugLog.Log($"{instance.instanceId} - {instance.Definition.Name} is persistent!");
                    continue;
                }

                // TODO : if RoomEnd event creates objects, should they be destroyed??
                GamemakerObject.ExecuteEvent(instance, instance.Definition, EventType.Other, (int)EventSubtypeOther.RoomEnd);
                // no destroy event https://forum.gamemaker.io/index.php?threads/hold-up-room-change-doesnt-trigger-destroy-event-of-objects.43007/
                GamemakerObject.ExecuteEvent(instance, instance.Definition, EventType.CleanUp);

                instance.Destroy();
            }

            InstanceManager.ClearInactive();

            foreach (var item in CurrentRoom.Tiles)
            {
                item.Destroy();
            }
            //CurrentRoom.Tiles.Clear();

            //CurrentRoom.OldBackgrounds.Clear();

            foreach (var layer in CurrentRoom.Layers)
            {
                foreach (var item in layer.Value.ElementsToDraw)
                {
                    if (item is GamemakerObject)
                    {
                        continue;
                    }

                    item.Destroy();
                }
            }
        }

        if (PersistentRooms.TryGetValue(room.AssetId, out var value))
        {
            LoadPersistentRoom(room, value);
        }
        else
        {
            RoomLoaded = false;
            CurrentRoom = new RoomContainer(room);
            OnRoomChanged();
        }
    }

    /*public static Vector2i CalculateCanvasSize()
    {
        // https://manual.gamemaker.io/lts/en/GameMaker_Language/GML_Reference/Cameras_And_Display/Cameras_And_Viewports/Cameras_And_View_Ports.htm

        // TODO : check for view_enabled here too
        if (CurrentRoom.Views.All(x => !x.Visible))
        {
            // Use room size if all views are disabled.
            // https://manual.gamemaker.io/lts/en/GameMaker_Language/GML_Reference/Cameras_And_Display/Cameras_And_Viewports/view_enabled.htm
            return new(CurrentRoom.SizeX, CurrentRoom.SizeY);
        }
    }*/

    private static void OnRoomChanged()
    {
        for (var i = 0; i < 8; i++)
        {
            var view = CurrentRoom.RoomAsset.Views[i];

            var camera = CameraManager.CreateCamera();
            camera.ViewX = view.PositionX;
            camera.ViewY = view.PositionY;
            camera.ViewWidth = view.SizeX;
            camera.ViewHeight = view.SizeY;
            camera.SpeedX = view.SpeedX;
            camera.SpeedY = view.SpeedY;
            camera.BorderX = view.BorderX;
            camera.BorderY = view.BorderY;
            camera.ViewAngle = 0;
            camera.TargetInstance = view.FollowsObject;

            camera.Build2DView(camera.ViewX + (camera.ViewWidth / 2), camera.ViewY + (camera.ViewHeight / 2));

            var runtimeView = new RuntimeView
            {
                Visible = view.Enabled,
                PortPosition = new Vector2i(view.PortPositionX, view.PortPositionY),
                PortSize = new Vector2i(view.PortSizeX, view.PortSizeY),
                Camera = camera
            };

            CurrentRoom.Views[i] = runtimeView;
        }


        var createdObjects = new List<(GamemakerObject gm, GameObject go)>();

        void RunObjEvents(GamemakerObject obj, GameObject go)
        {
            GamemakerObject.ExecuteEvent(obj, obj.Definition, EventType.PreCreate);
            
            var preCreateCode = GetCodeFromCodeIndex(go.PreCreateCodeID);
            if (preCreateCode != null)
            {
                VMExecutor.ExecuteCode(preCreateCode, obj, obj.Definition);
            }

            GamemakerObject.ExecuteEvent(obj, obj.Definition, EventType.Create);
            obj._createRan = true;

            var createCode = GetCodeFromCodeIndex(go.CreationCodeID);
            if (createCode != null)
            {
                VMExecutor.ExecuteCode(createCode, obj, obj.Definition);
            }
        }

        foreach (var obj in CurrentRoom.RoomAsset.GameObjects)
        {
            var definition = InstanceManager.ObjectDefinitions[obj.DefinitionID];
            var newGM = new GamemakerObject(definition, obj.X, obj.Y, obj.DefinitionID, obj.InstanceID, definition.sprite, definition.visible, definition.persistent, definition.textureMaskId);

            newGM.image_xscale = obj.ScaleX;
            newGM.image_yscale = obj.ScaleY;
            newGM.image_blend = obj.Color;
            newGM.image_angle = obj.Rotation;
            newGM.image_index = obj.FrameIndex;
            newGM.image_speed = obj.ImageSpeed;

            createdObjects.Add((newGM, obj));
        }

        foreach (var layer in CurrentRoom.RoomAsset.Layers)
        {
            DebugLog.LogInfo($"Creating layer {layer.LayerName}...");

            var layerContainer = new LayerContainer(layer);

            foreach (var element in layer.Elements)
            {
                element.Layer = layerContainer;

                if (element.Type == ElementType.Instance)
                {
                    var item = (element as GameObject)!;

                    var obj = createdObjects.First(x => x.go.InstanceID == item.InstanceID);
                    obj.gm.depth = layer.LayerDepth;
                    layerContainer.ElementsToDraw.Add(obj.gm);
                }
                else if (element.Type == ElementType.Tilemap)
                {
                    var item = (element as CLayerTilemapElement)!;

                    if (item.BackgroundIndex == -1)
                    {
                        continue;
                    }

                    var tilesLayer = new GMTilesLayer(item)
                    {
                        depth = layer.LayerDepth
                    };

                    layerContainer.ElementsToDraw.Add(tilesLayer);
                }
                else if (element.Type == ElementType.Background)
                {
                    var item = (element as CLayerBackgroundElement)!;

                    var background = new GMBackground(item)
                    {
                        depth = layer.LayerDepth
                    };

                    layerContainer.ElementsToDraw.Add(background);
                }
                else if (element.Type == ElementType.Tile)
                {
                    var tile = (element as CLayerTileElement)!;

                    var newTile = new GMTile()
                    {
                        X = tile.X,
                        Y = tile.Y,
                        Definition = tile.Definition,
                        SpriteMode = tile.SpriteMode,
                        left = tile.SourceLeft,
                        top = tile.SourceTop,
                        width = tile.SourceWidth,
                        height = tile.SourceHeight,
                        depth = layer.LayerDepth,
                        instanceId = tile.Id, // todo : this is almost definitely very wrong
                        Visible = layer.IsVisible,
                        XScale = tile.ScaleX,
                        YScale = tile.ScaleY,
                        Color = tile.Color
                    };

                    layerContainer.ElementsToDraw.Add(newTile);
                }
                else if (element.Type == ElementType.Sprite)
                {
                    var sprite = (element as CLayerSpriteElement)!;

                    var c = sprite.Color;
                    var blend = (int)(c & 0x00FFFFFF);
                    var alpha = ((c & 0xFF000000) >> 6) / 255.0;

                    var newSprite = new GMSprite(sprite)
                    {
                        Definition = sprite.Definition,
                        X = sprite.X,
                        Y = sprite.Y,
                        XScale = sprite.ScaleX,
                        YScale = sprite.ScaleY,
                        Blend = blend,
                        Alpha = alpha,
                        AnimationSpeed = sprite.AnimationSpeed,
                        AnimationSpeedType = sprite.AnimationSpeedType,
                        FrameIndex = sprite.FrameIndex,
                        Rotation = sprite.Rotation,
                        instanceId = sprite.Id,
                        depth = layer.LayerDepth
                    };

                    layerContainer.ElementsToDraw.Add(newSprite);
                }
                else
                {
                    DebugLog.LogError($"Don't know how to load element type {element.Type}!");
                }
            }

            CurrentRoom.Layers.Add(layerContainer.ID, layerContainer);
        }

        // instance_exists will still return true for all objects even in Create of the first object.... ugh
        DebugLog.LogInfo($"Calling PreCreate/Create/CC...");
        foreach (var item in createdObjects)
        {
            RunObjEvents(item.gm, item.go);
        }

        DebugLog.LogInfo($"Creating loose tiles...");
        foreach (var item in CurrentRoom.RoomAsset.Tiles)
        {
            var newTile = new GMTile()
            {
                X = item.X,
                Y = item.Y,
                left = item.SourceLeft,
                top = item.SourceTop,
                width = item.SourceWidth,
                height = item.SourceHeight,
                depth = item.Depth,
                instanceId = item.InstanceID,
                XScale = item.ScaleX,
                YScale = item.ScaleY,
                Color = item.Color,
                Definition = item.Definition,
                SpriteMode = item.SpriteMode,
                Visible = true
            };

            CurrentRoom.Tiles.Add(newTile);
        }

        foreach (var item in CurrentRoom.RoomAsset.OldBackgrounds)
        {
            var oldBackground = new GMOldBackground()
            {
                Enabled = item.Enabled,
                Foreground = item.Foreground,
                Definition = item.Definition,
                Position = item.Position,
                TilingX = item.TilingX,
                TilingY = item.TilingY,
                Speed = item.Speed,
                Stretch = item.Stretch
            };

            CurrentRoom.OldBackgrounds.Add(oldBackground);
        }

        var currentInstances = InstanceManager.instances.Values.ToList();

        if (FirstRoom)
        {
            FirstRoom = false;
            DebugLog.LogInfo($"Calling GameStart...");
            foreach (var obj in currentInstances)
            {
                GamemakerObject.ExecuteEvent(obj, obj.Definition, EventType.Other, (int)EventSubtypeOther.GameStart);
            }
        }

        DebugLog.LogInfo($"Calling RoomCC...");
        // room creation code is called with a dummy object that gets de-referenced immediately after
        var createCode = GetCodeFromCodeIndex(CurrentRoom.RoomAsset.CreationCodeId);
        if (createCode != null)
        {
            var dummy = new GMLObject();
            VMExecutor.ExecuteCode(createCode, dummy);
            dummy = null;

            if (VMExecutor.EnvStack.Count != 0)
            {
                DebugLog.LogWarning("EnvStack not empty after room creation code!");
                foreach (var item in VMExecutor.EnvStack)
                {
                    if (item == null)
                    {
                        DebugLog.LogWarning(" - NULL");
                    }
                    else
                    {
                        if (item.Self is GamemakerObject)
                        {
                            DebugLog.LogWarning($" - {item.ObjectDefinition?.Name} ({item.GMSelf.instanceId})");
                        }
                        else
                        {
                            DebugLog.LogWarning($" - {item.GetType()}");
                        }
                    }
                }

                // HACK: why does the dummy object stay on envstack? no idea!!!
                VMExecutor.EnvStack.Clear();
            }
        }

        DebugLog.LogInfo($"Calling RoomStart...");
        foreach (var obj in currentInstances)
        {
            GamemakerObject.ExecuteEvent(obj, obj.Definition, EventType.Other, (int)EventSubtypeOther.RoomStart);
        }

        VMCode? GetCodeFromCodeIndex(int codeIndex)
        {
            if (codeIndex == -1)
            {
                return null;
            }

            return GameLoader.Codes[codeIndex];
        }
        
        GC.Collect(); // gc on load boundary

        DebugLog.LogInfo($"- Finished room change.");

        RoomLoaded = true;
    }

    public static void room_goto_next()
    {
        var order = GameLoader.GeneralInfo.RoomOrder;
        var idx = Array.IndexOf(order, CurrentRoom.AssetId);

        if (order.Length > idx + 1)
        {
            New_Room = order[idx + 1];
        }

    }

    public static void room_goto_previous()
    {
        var order = GameLoader.GeneralInfo.RoomOrder;
        var idx = Array.IndexOf(order, CurrentRoom.AssetId);

        if (idx > 0)
        {
            New_Room = order[idx - 1];
        }
    }

    public static int room_next(int numb)
    {
        var order = GameLoader.GeneralInfo.RoomOrder;
        var idx = Array.IndexOf(order, numb);

        if (order.Length > idx + 1)
        {
            return order[idx + 1];
        }

        return -1;
    }

    public static int room_previous(int numb)
    {
        var order = GameLoader.GeneralInfo.RoomOrder;
        var idx = Array.IndexOf(order, numb);

        if (idx <= 0)
        {
            return -1;
        }

        return order[idx - 1];
    }
}
