using OpenGM.IO;
using OpenGM.SerializedFiles;
using OpenGM.VirtualMachine;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using UndertaleModLib.Models;
using EventType = OpenGM.VirtualMachine.EventType;

namespace OpenGM.Rendering;
public static class DrawManager
{
    public static int CirclePrecision = 24;

    public static List<DrawWithDepth> _drawObjects = new();

    public static bool DebugBBoxes = false;

    public static void Register(DrawWithDepth obj)
    {
        if (_drawObjects.Contains(obj))
        {
            return;
        }

        _drawObjects.Add(obj);
    }

    public static void Unregister(DrawWithDepth obj)
    {
        if (!_drawObjects.Contains(obj))
        {
            return;
        }

        _drawObjects.Remove(obj);
    }

    private static bool RunStepScript(IOrderedEnumerable<DrawWithDepth> items, EventSubtypeStep stepType)
    {
        foreach (var item in items)
        {
            if (item is GamemakerObject gm && gm._createRan && RoomManager.RoomLoaded)
            {
                GamemakerObject.ExecuteEvent(gm, gm.Definition, EventType.Step, (int)stepType);
            }
        }

        if (RoomManager.New_Room != -1)
        {
            RoomManager.ChangeToWaitingRoom();
            return true;
        }

        return false;
    }

    private static bool RunDrawScript(IOrderedEnumerable<DrawWithDepth> items, EventSubtypeDraw drawType)
    {
        foreach (var item in items)
        {
            if (item is GamemakerObject gm)
            {
                if (!gm._createRan || !RoomManager.RoomLoaded || !gm.visible || !gm.Active)
                {
                    continue;
                }

                if (drawType == EventSubtypeDraw.Draw)
                {
                    var hasDrawScript = gm.Definition.DrawScript.ContainsKey(EventSubtypeDraw.Draw);

                    var currentParent = gm.Definition.parent;
                    while (currentParent != null)
                    {
                        if (currentParent.DrawScript.ContainsKey(EventSubtypeDraw.Draw))
                        {
                            hasDrawScript = true;
                            break;
                        }

                        currentParent = currentParent.parent;
                    }

                    if (!hasDrawScript)
                    {
                        SpriteManager.DrawSelf(gm);
                        continue;
                    }
                }

                GamemakerObject.ExecuteEvent(gm, gm.Definition, EventType.Draw, (int)drawType);
            }
            else if (drawType == EventSubtypeDraw.Draw)
            {
                item.Draw();
            }
        }


        if (DebugBBoxes)
        {
            foreach (var item in items)
            {
                if (item is GamemakerObject gm && gm.Active)
                {
                    var color = new OpenTK.Mathematics.Color4(1.0f, 0.0f, 0.0f, 1.0f);
                    var fill = new OpenTK.Mathematics.Color4(1.0f, 0.0f, 0.0f, 0.05f);
                    var camX = ViewportManager.CurrentRenderingView!.ViewPosition.X;
                    var camY = ViewportManager.CurrentRenderingView!.ViewPosition.Y;

                    var vertices = new OpenTK.Mathematics.Vector2d[] {
                        new(gm.bbox.left - camX, gm.bbox.top - camY),
                        new(gm.bbox.right - camX, gm.bbox.top - camY),
                        new(gm.bbox.right - camX, gm.bbox.bottom - camY),
                        new(gm.bbox.left - camX, gm.bbox.bottom - camY)
                    };

                    CustomWindow.Draw(new GMPolygonJob()
                    {
                        Colors = [color, color, color, color],
                        Vertices = vertices,
                        Outline = true
                    });

                    CustomWindow.Draw(new GMPolygonJob()
                    {
                        Colors = [fill, fill, fill, fill],
                        Vertices = vertices,
                        Outline = false
                    });
                }
            }
        }

        if (RoomManager.New_Room != -1)
        {
            RoomManager.ChangeToWaitingRoom();
            return true;
        }

        return false;
    }

    public static void DoAStep()
    {
        // g_pBuiltIn.delta_time = (g_CurrentTime - g_pBuiltIn.last_time)*1000;
        // g_pBuiltIn.last_time = g_CurrentTime;
        // ResetSpriteMessageEvents();
        // g_pIOManager.StartStep();
        // HandleOSEvents();
        // g_pGamepadManager.Update();

        InstanceManager.RememberOldPositions();
        InstanceManager.UpdateImages();

        // UpdateActiveLists();
        if (RoomManager.New_Room != -1)
        {
            RoomManager.ChangeToWaitingRoom();
            return;
        }

        // g_pLayerManager.UpdateLayers();
        // g_pSequenceManager.PerformInstanceEvents(g_RunRoom, EVENT_STEP_BEGIN);

        var stepList = _drawObjects.OrderBy(x => x.instanceId);
        if (RunStepScript(stepList, EventSubtypeStep.BeginStep))
        {
            return;
        }

        // resize event

        // g_pASyncManager.Process();

        // UpdateActiveLists();
        if (RoomManager.New_Room != -1)
        {
            RoomManager.ChangeToWaitingRoom();
            return;
        }

        // HandleTimeLine();

        // UpdateActiveLists();
        if (RoomManager.New_Room != -1)
        {
            RoomManager.ChangeToWaitingRoom();
            return;
        }

        // HandleTimeSources();

        // UpdateActiveLists();
        if (RoomManager.New_Room != -1)
        {
            RoomManager.ChangeToWaitingRoom();
            return;
        }

        HandleAlarm(stepList);

        // UpdateActiveLists();
        if (RoomManager.New_Room != -1)
        {
            RoomManager.ChangeToWaitingRoom();
            return;
        }

        HandleKeyboard();

        // UpdateActiveLists();
        if (RoomManager.New_Room != -1)
        {
            RoomManager.ChangeToWaitingRoom();
            return;
        }

        // HandleMouse();

        // UpdateActiveLists();
        if (RoomManager.New_Room != -1)
        {
            RoomManager.ChangeToWaitingRoom();
            return;
        }

        // g_pEffectsManager.StepEffectsForRoom(g_RunRoom);

        // g_pSequenceManager.UpdateInstancesForRoom(g_RunRoom);
        // g_pSequenceManager.PerformInstanceEvents(g_RunRoom, EVENT_STEP_NORMAL);

        if (RunStepScript(stepList, EventSubtypeStep.Step))
        {
            return;
        }

        // ProcessSpriteMessageEvents();
        InstanceManager.UpdatePositions(); // UpdateInstancePositions

        HandleOther();

        // UpdateActiveLists();
        if (RoomManager.New_Room != -1)
        {
            RoomManager.ChangeToWaitingRoom();
            return;
        }

        // YYPushEventsDispatch();

        // UpdateActiveLists();
        if (RoomManager.New_Room != -1)
        {
            RoomManager.ChangeToWaitingRoom();
            return;
        }

        UpdateCollisions(stepList);

        // UpdateActiveLists();
        if (RoomManager.New_Room != -1)
        {
            RoomManager.ChangeToWaitingRoom();
            return;
        }

        // g_pSequenceManager.PerformInstanceEvents(g_RunRoom, EVENT_STEP_END);

        if (RunStepScript(stepList, EventSubtypeStep.EndStep))
        {
            return;
        }

        // ParticleSystem_UpdateAll();

        /*
        if (g_RunRoom!=null) 
        {
            g_RunRoom.RemoveMarked();
            if (Draw_Automatic) 
            {
                g_RunRoom.Draw();
                UpdateActiveLists();
            }
        }
        */
    }

    public static void HandleAlarm(IOrderedEnumerable<DrawWithDepth> stepList)
    {
        foreach (var item in stepList)
        {
            if (item is GamemakerObject gm)
            {
                gm.UpdateAlarms();
            }
        }
    }

    public static void UpdateCollisions(IOrderedEnumerable<DrawWithDepth> stepList)
    {
        foreach (var item in stepList)
        {
            if (item is GamemakerObject gmo)
            {
                foreach (var id in GetCollisionScripts(gmo.Definition).Keys)
                {
                    //var collide = CollisionManager.instance_place_assetid(gmo.x, gmo.y, id, gmo);
                    var instanceId = CollisionManager.Command_InstancePlace(gmo, gmo.x, gmo.y, id);

                    if (instanceId == GMConstants.noone)
                    {
                        continue;
                    }

                    var collide = InstanceManager.instances[instanceId];

                    if (collide != null)
                    {
                        // makes it so `other` is the collided thing
                        VMExecutor.EnvStack.Push(new VMEnvFrame { Self = collide, ObjectDefinition = collide.Definition });
                        GamemakerObject.ExecuteEvent(gmo, gmo.Definition, EventType.Collision, id);
                        VMExecutor.EnvStack.Pop();
                    }
                }
            }
        }
    }

    // TODO : this is really bad, this needs to be cached somewhere
    public static Dictionary<int, VMCode> GetCollisionScripts(ObjectDefinition obj)
    {
        var ret = new Dictionary<int, VMCode>();

        void AddScripts(ObjectDefinition obj)
        {
            foreach (var item in obj.CollisionScript)
            {
                if (ret.ContainsKey(item.Key))
                {
                    continue;
                }

                ret.Add(item.Key, item.Value);
            }
        }

        var parent = obj;
        while (parent != null)
        {
            AddScripts(parent);
            parent = parent.parent;
        }

        return ret;
    }

    public static void HandleOther()
    {
        // create copy since events can create new instances
        var instances = InstanceManager.instances.Values.ToList();
        foreach (var instance in instances)
        {
            if (!instance.Marked)
            {
                if (instance.HasEvent(EventType.Other, (int)EventSubtypeOther.OutsideRoom))
                {
                    var outside = false;

                    if (SpriteManager.SpriteExists(instance.sprite_index) || SpriteManager.SpriteExists(instance.mask_index))
                    {
                        var bbox = instance.bbox;
                        outside = ((bbox.right < 0) || (bbox.left > RoomManager.CurrentRoom.SizeX) || (bbox.bottom < 0) || (bbox.top > RoomManager.CurrentRoom.SizeY));
                    }
                    else
                    {
                        outside = ((instance.x < 0) || (instance.x > RoomManager.CurrentRoom.SizeX) || (instance.y < 0) || (instance.y > RoomManager.CurrentRoom.SizeY));
                    }

                    if (outside)
                    {
                        if (!instance.IsOutsideRoom)
                        {
                            GamemakerObject.ExecuteEvent(instance, instance.Definition, EventType.Other, (int)EventSubtypeOther.OutsideRoom);
                        }
                    }
                    instance.IsOutsideRoom = outside;
                }

                // boundary events

                // outside/boundary view events
            }
        }
    }

    public static void FixedUpdate()
    {
        VariableResolver.GlobalVariables["debug"] = true;

        var itemsToRemove = new List<DrawWithDepth>();
        foreach (var item in _drawObjects)
        {
            if (item is GamemakerObject gm)
            {
                if (gm.Destroyed || gm.Marked)
                {
                    DebugLog.LogWarning($"{gm.Definition.Name} ({gm.instanceId}) in _drawObjects at start of frame when destroyed/marked!");
                    itemsToRemove.Add(item);
                }

                if (!InstanceManager.instance_exists_instanceid(gm.instanceId))
                {
                    // TODO : this really shouldnt happen!! instance wasn't destroyed properly??
                    DebugLog.LogWarning($"{gm.Definition.Name} ({gm.instanceId}) in _drawObjects at start of frame when not in Instance list!");
                    gm.Destroyed = true;
                    gm.Marked = true;
                    itemsToRemove.Add(item);
                }
            }
        }
        _drawObjects.RemoveAll(x => itemsToRemove.Contains(x));

        DoAStep();
        
        /*
         * https://github.com/YoYoGames/GameMaker-HTML5/blob/develop/scripts/yyRoom.js#L4168
         * yyRoom.prototype.Draw
         */

        var drawList = _drawObjects.OrderByDescending(x => x.depth).ThenByDescending(x => x.instanceId);

        /*
         * PreDraw
         */
        var fbsize = CustomWindow.Instance.FramebufferSize;
        GraphicsManager.SetViewPort(0, 0, fbsize.X, fbsize.Y);
        GraphicsManager.SetViewArea(0, 0, fbsize.X, fbsize.Y);
        
        if (CustomWindow.Instance != null) // only null in tests
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);
        }

        if (RunDrawScript(drawList, EventSubtypeDraw.PreDraw))
        {
            return;
        }
        
        SurfaceManager.SetApplicationSurface();

        if (SurfaceManager.UsingAppSurface)
        {
            if (CustomWindow.Instance != null) // only null in tests
            {
                GL.Clear(ClearBufferMask.ColorBufferBit);
            }
        }

        // ROOM BACKGROUNDS
        // this is for undertale, this is definitely in the wrong place. just putting it here to get it drawing.
        foreach (var item in RoomManager.CurrentRoom.OldBackgrounds)
        {
            if (item == null)
            {
                continue;
            }

            item.Draw();
        }

        /*
         * DrawViews
         */
        
        /*
         * UpdateViews
         */
        UpdateViews();
        

        if (RoomManager.CurrentRoom.RoomAsset.EnableViews)
        {
            for (var i = 0; i < 8; i++)
            {
                ViewportManager.CurrentRenderingView = RoomManager.CurrentRoom.Views[i];

                if (!ViewportManager.CurrentRenderingView.Visible)
                {
                    continue;
                }

                if (ViewportManager.CurrentRenderingView.SurfaceId != -1)
                {
                    SurfaceManager.surface_set_target(ViewportManager.CurrentRenderingView.SurfaceId);

                    // idk what the deal with scaled port stuff is. happens in both html5 and cpp

                    // ignore viewport, use entire surface. idk why
                }
                else
                {
                    // BUG: this stretches. idk why
                    /*
                    GraphicsManager.SetViewPort(
                        ViewportManager.CurrentRenderingView.PortPosition.X,// * displayScaleX,
                        ViewportManager.CurrentRenderingView.PortPosition.Y,// * displayScaleY,
                        ViewportManager.CurrentRenderingView.PortSize.X,// * displayScaleX,
                        ViewportManager.CurrentRenderingView.PortSize.Y// * displayScaleY
                    );
                    */
                }

                GraphicsManager.SetViewArea(
                    ViewportManager.CurrentRenderingView.ViewPosition.X,
                    ViewportManager.CurrentRenderingView.ViewPosition.Y,
                    ViewportManager.CurrentRenderingView.ViewSize.X,
                    ViewportManager.CurrentRenderingView.ViewSize.Y
                );

                /*
                 * DrawTheRoom
                 */
                if (RunDrawScript(drawList, EventSubtypeDraw.DrawBegin))
                {
                    break;
                }

                if (RunDrawScript(drawList, EventSubtypeDraw.Draw))
                {
                    break;
                }

                if (RunDrawScript(drawList, EventSubtypeDraw.DrawEnd))
                {
                    break;
                }

                if (ViewportManager.CurrentRenderingView.SurfaceId != -1)
                {
                    SurfaceManager.surface_reset_target();
                }
            }
        }
        else
        {
            GraphicsManager.SetViewPort(0, 0, SurfaceManager.ApplicationWidth, SurfaceManager.ApplicationHeight);
            GraphicsManager.SetViewArea(0, 0, RoomManager.CurrentRoom.SizeX, RoomManager.CurrentRoom.SizeY);

            // dummy view for full room rendering
            ViewportManager.CurrentRenderingView = new()
            {
                ViewPosition = Vector2.Zero,
                ViewSize = new(RoomManager.CurrentRoom.SizeX, RoomManager.CurrentRoom.SizeY),
                PortSize = new(SurfaceManager.ApplicationWidth, SurfaceManager.ApplicationHeight)
            };

            /*
             * DrawTheRoom
             */
            if (RunDrawScript(drawList, EventSubtypeDraw.DrawBegin))
            {
                return;
            }

            if (RunDrawScript(drawList, EventSubtypeDraw.Draw))
            {
                return;
            }

            if (RunDrawScript(drawList, EventSubtypeDraw.DrawEnd))
            {
                return;
            }
        }

        if (SurfaceManager.UsingAppSurface)
        {
            SurfaceManager.surface_reset_target();
        }

        if (SurfaceManager.SurfaceStack.Count != 0)
        {
            DebugLog.LogError("Unbalanced surface stack. You MUST use surface_reset_target() for each set.");
            // BUG: room transitions become unbalanced. probably because of early returns above
            while (SurfaceManager.SurfaceStack.Count != 0)
            {
                SurfaceManager.surface_reset_target();
            }
            return;
        }

        ViewportManager.CurrentRenderingView = null;

        /*
         * PostDraw
         */
        GraphicsManager.SetViewPort(0, 0, fbsize.X, fbsize.Y);
        GraphicsManager.SetViewArea(0, 0, fbsize.X, fbsize.Y);
        
        if (RunDrawScript(drawList, EventSubtypeDraw.PostDraw))
        {
            return;
        }

        /*
         * DrawApplicationSurface
         */
        if (SurfaceManager.UsingAppSurface)
        {
            // gamemaker actually uses alpha test enable here, and saves/restores the state. just change it to that when this breaks
            GL.Disable(EnableCap.Blend);
            SurfaceManager.draw_surface_stretched(SurfaceManager.application_surface, 0, 0, fbsize.X, fbsize.Y);
            GL.Enable(EnableCap.Blend);
        }

        /*
         * DrawGUI
         */
        if (RunDrawScript(drawList, EventSubtypeDraw.DrawGUIBegin))
        {
            return;
        }

        if (RunDrawScript(drawList, EventSubtypeDraw.DrawGUI))
        {
            return;
        }

        if (RunDrawScript(drawList, EventSubtypeDraw.DrawGUIEnd))
        {
            return;
        }

        if (RoomManager.CurrentRoom != null)
        {
            RoomManager.CurrentRoom.RemoveMarked();
        }

        //GamemakerCamera.Instance.GetComponent<Camera>().Render();
    }

    public static void UpdateViews()
    {
        if (!RoomManager.CurrentRoom.RoomAsset.EnableViews)
        {
            return;
        }

        for (var i = 0; i < 8; i++)
        {
            var view = RoomManager.CurrentRoom.Views[i];

            if (!view.Visible)
            {
                continue;
            }

            if (view.Camera == null)
            {
                continue;
            }

            view.Camera.Update();
        }

        /*
        var left = 999999;
        var right = -999999;
        var top  = 999999;
        var bottom = -999999;

        for (var i = 0; i < 8; i++)
        {
            var view = RoomManager.CurrentRoom.Views[i];

            if (view.Visible )// && pView.surface_id==-1)
            {
                if( left>view.PortPosition.X) left = view.PortPosition.X;
                if( right<(view.PortPosition.X+view.PortSize.X) ) right= view.PortPosition.X+view.PortSize.X;
                if( top>view.PortPosition.Y) top = view.PortPosition.Y;
                if( bottom<(view.PortPosition.Y+view.PortSize.Y) ) bottom = view.PortPosition.Y+view.PortSize.Y;
            }
        }

        var displayScaleX = SurfaceManager.ApplicationWidth /  (right-left);
        var displayScaleY = SurfaceManager.ApplicationHeight /  (bottom-top);
        */
    }

    public static void HandleKeyboard()
    {
        KeyDown();
        KeyPressed();
        KeyReleased();
    }

    public static void KeyDown()
    {
        var keyDown = 0;
        for (var i = 0; i < 256; i++)
        {
            if (KeyboardHandler.KeyDown[i])
            {
                keyDown = 1;
                Handle(i, EventType.Keyboard);
            }
        }

        // either 0 (no key) or 1 (any key)
        Handle(keyDown, EventType.Keyboard);
    }

    public static void KeyPressed()
    {
        var keyPressed = 0;
        for (var i = 0; i < 256; i++)
        {
            if (KeyboardHandler.KeyPressed[i])
            {
                keyPressed = 1;
                Handle(i, EventType.KeyPress);
            }
        }

        // either 0 (no key) or 1 (any key)
        Handle(keyPressed, EventType.KeyPress);
    }

    public static void KeyReleased()
    {
        var keyReleased = 0;
        for (var i = 0; i < 256; i++)
        {
            if (KeyboardHandler.KeyReleased[i])
            {
                keyReleased = 1;
                Handle(i, EventType.KeyRelease);
            }
        }

        // either 0 (no key) or 1 (any key)
        Handle(keyReleased, EventType.KeyRelease);
    }

    public static void Handle(int key, EventType type)
    {
        var objects = InstanceManager.instances.Values.ToList();
        foreach (var obj in objects)
        {
            GamemakerObject.ExecuteEvent(obj, obj.Definition, type, key);
        }
    }
}
