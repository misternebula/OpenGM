using OpenGM.IO;
using OpenGM.SerializedFiles;
using OpenGM.VirtualMachine;
using OpenGM.VirtualMachine.BuiltInFunctions;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using UndertaleModLib.Models;
using EventType = OpenGM.VirtualMachine.EventType;

namespace OpenGM.Rendering;
public static class DrawManager
{
    public static int CirclePrecision = 24;

    public static List<DrawWithDepth> _drawObjects = new();

    /// <summary>
    /// this sets view area during gui events. im told this is not how gamemaker does it, but it seems to work
    /// </summary>
    public static Vector2i? GuiSize = null;

    public static bool DebugBBoxes = false;
    public static bool ShouldDrawGui = true;

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
            if (GraphicsManager.ForceDepth)
            {
                GraphicsManager.GR_Depth = CustomMath.Min(16000, CustomMath.Max(-16000, GraphicsManager.ForcedDepth));
            }
            else
            {
                GraphicsManager.GR_Depth = CustomMath.Min(16000, CustomMath.Max(-16000, item.depth));
            }

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
                        GraphicsManager.PushMessage($"{drawType} (DRAW SELF) {gm.instanceId} ({AssetIndexManager.GetName(AssetType.objects, gm.object_index)})");
                        SpriteManager.DrawSelf(gm);
                        GraphicsManager.PopMessage();
                        continue;
                    }
                }

                GraphicsManager.PushMessage($"{drawType} {gm.instanceId} ({AssetIndexManager.GetName(AssetType.objects, gm.object_index)})");
                GamemakerObject.ExecuteEvent(gm, gm.Definition, EventType.Draw, (int)drawType);
                GraphicsManager.PopMessage();
            }
            else if (drawType == EventSubtypeDraw.Draw)
            {
                GraphicsManager.PushMessage($"{drawType} {item.instanceId} ({item.GetType().Name})");
                item.Draw();
                GraphicsManager.PopMessage();
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
        if (RoomManager.CheckAndChangeRoom())
        {
            return;
        }

        // g_pLayerManager.UpdateLayers();
        // g_pSequenceManager.PerformInstanceEvents(g_RunRoom, EVENT_STEP_BEGIN);

        var stepList = _drawObjects.Where(x => x is not GamemakerObject obj || obj.Active).OrderBy(x => x.instanceId);
        if (RunStepScript(stepList, EventSubtypeStep.BeginStep))
        {
            return;
        }

        // resize event

        AsyncManager.HandleAsyncQueue();
        if (RoomManager.CheckAndChangeRoom())
        {
            return;
        }

        // HandleTimeLine();
        if (RoomManager.CheckAndChangeRoom())
        {
            return;
        }

        // HandleTimeSources();
        if (RoomManager.CheckAndChangeRoom())
        {
            return;
        }

        HandleAlarm(stepList);
        if (RoomManager.CheckAndChangeRoom())
        {
            return;
        }

        HandleKeyboard();
        if (RoomManager.CheckAndChangeRoom())
        {
            return;
        }

        HandleMouse();
        if (RoomManager.CheckAndChangeRoom())
        {
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
        if (RoomManager.CheckAndChangeRoom())
        {
            return;
        }

        // YYPushEventsDispatch();
        if (RoomManager.CheckAndChangeRoom())
        {
            return;
        }

        UpdateCollisions(stepList);
        if (RoomManager.CheckAndChangeRoom())
        {
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
        VariableResolver.GlobalVariables["chemg_show_room"] = true;
        VariableResolver.GlobalVariables["chemg_show_val"] = true;

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

        GraphicsManager.PushMessage("DoAStep");
        DoAStep();
        GraphicsManager.PopMessage();
        
        /*
         * https://github.com/YoYoGames/GameMaker-HTML5/blob/develop/scripts/yyRoom.js#L4168
         * yyRoom.prototype.Draw
         */

        var drawList = CompatFlags.DepthSortingReverseInstanceIds
            ? _drawObjects.OrderByDescending(x => x.depth).ThenByDescending(x => x.instanceId)
            : _drawObjects.OrderByDescending(x => x.depth).ThenBy(x => x.instanceId);

        /*
         * PreDraw
         */
        GraphicsManager.PushMessage("PreDraw");
        var fbsize = CustomWindow.Instance.FramebufferSize;
        GraphicsManager.SetViewPort(0, 0, fbsize.X, fbsize.Y);
        GraphicsManager.SetViewArea(0, 0, fbsize.X, fbsize.Y, 0);
        
        if (CustomWindow.Instance != null) // only null in tests
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);
        }

        if (RunDrawScript(drawList, EventSubtypeDraw.PreDraw))
        {
            return;
        }
        GraphicsManager.PopMessage();

        SurfaceManager.SetApplicationSurface();

        if (SurfaceManager.UsingAppSurface)
        {
            if (CustomWindow.Instance != null) // only null in tests
            {
                GL.Clear(ClearBufferMask.ColorBufferBit);
            }
        }

        /*
         * DrawViews
         */
        GraphicsManager.PushMessage("DrawViews");
        
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

                GraphicsManager.PushMessage($"draw view {i}");
                
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
                    ViewportManager.CurrentRenderingView.ViewSize.Y,
                    0
                );

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
                 * DrawTheRoom
                 */
                GraphicsManager.PushMessage("DrawTheRoom");
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
                GraphicsManager.PopMessage();

                if (DebugBBoxes)
                {
                    foreach (var item in _drawObjects)
                    {
                        if (item is GamemakerObject gm && gm.Active)
                        {
                            var color = new Color4(1.0f, 0.0f, 0.0f, 1.0f);
                            var fill = new Color4(1.0f, 0.0f, 0.0f, 0.05f);

                            if (gm.mouse_over)
                            {
                                color.R = fill.R = 0.0f;
                                color.G = fill.G = 1.0f;
                                color.B = fill.B = 0.0f;
                            }

                            var vertices = new Vector3d[] {
                                new(gm.bbox.left, gm.bbox.top, GraphicsManager.GR_Depth),
                                new(gm.bbox.right, gm.bbox.top, GraphicsManager.GR_Depth),
                                new(gm.bbox.right, gm.bbox.bottom, GraphicsManager.GR_Depth),
                                new(gm.bbox.left, gm.bbox.bottom, GraphicsManager.GR_Depth)
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

                if (ViewportManager.CurrentRenderingView.SurfaceId != -1)
                {
                    SurfaceManager.surface_reset_target();
                }
                
                GraphicsManager.PopMessage(); // draw view
            }
        }
        else // views not enabled
        {
            GraphicsManager.PushMessage("draw viewless");
            
            GraphicsManager.SetViewPort(0, 0, SurfaceManager.ApplicationWidth, SurfaceManager.ApplicationHeight);
            GraphicsManager.SetViewArea(0, 0, RoomManager.CurrentRoom.SizeX, RoomManager.CurrentRoom.SizeY, 0);

            // dummy view for full room rendering
            // i think this is mostly for tiled rendering, which should switch to using room extents
            ViewportManager.CurrentRenderingView = new()
            {
                ViewPosition = Vector2.Zero,
                ViewSize = new(RoomManager.CurrentRoom.SizeX, RoomManager.CurrentRoom.SizeY),
                PortSize = new(SurfaceManager.ApplicationWidth, SurfaceManager.ApplicationHeight)
            };

            /*
             * DrawTheRoom
             */
            GraphicsManager.PushMessage("DrawTheRoom");
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
            GraphicsManager.PopMessage();
            GraphicsManager.PopMessage();
        }
        
        GraphicsManager.PopMessage(); // draw views

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
        GraphicsManager.PushMessage("PostDraw");
        GraphicsManager.SetViewPort(0, 0, fbsize.X, fbsize.Y);
        GraphicsManager.SetViewArea(0, 0, fbsize.X, fbsize.Y, 0);
        
        if (RunDrawScript(drawList, EventSubtypeDraw.PostDraw))
        {
            return;
        }
        GraphicsManager.PopMessage();

        /*
         * DrawApplicationSurface
         */
        GraphicsManager.PushMessage("DrawApplicationSurface");
        if (SurfaceManager.UsingAppSurface)
        {
            // gamemaker actually uses alpha test enable here, and saves/restores the state. just change it to that when this breaks
            GL.Disable(EnableCap.Blend);
            SurfaceManager.draw_surface_stretched(SurfaceManager.application_surface, 0, 0, fbsize.X, fbsize.Y);
            GL.Enable(EnableCap.Blend);
        }
        GraphicsManager.PopMessage();
        
        // TODO: calc gui transform? and port stuff?? idk what this is lol
        // and update room extents to that i guess

        /*
         * DrawGUI
         */
        if (ShouldDrawGui)
        {
            GraphicsManager.PushMessage("DrawGUI");
            if (GuiSize is Vector2i vec)
            {
                GraphicsManager.SetViewArea(0, 0, vec.X, vec.Y, 0);
            }

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

            GraphicsManager.SetViewArea(0, 0, fbsize.X, fbsize.Y, 0);
            GraphicsManager.PopMessage();
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
            if (InputHandler.KeyDown[i])
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
            if (InputHandler.KeyPressed[i])
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
            if (InputHandler.KeyReleased[i])
            {
                keyReleased = 1;
                Handle(i, EventType.KeyRelease);
            }
        }

        // either 0 (no key) or 1 (any key)
        Handle(keyReleased, EventType.KeyRelease);
    }

    public static void HandleMouse()
    {
        var keys = InstanceManager.instances.Keys;
        for (var i = 0; i < InstanceManager.instances.Count; i++)
        {
            var inst = InstanceManager.instances[keys.ElementAt(i)];

            if (inst.Definition.MouseScripts.Count == 0)
            {
                continue;
            }

            var mouseX = GraphicFunctions.window_views_mouse_get_x([]).Conv<double>();
            var mouseY = GraphicFunctions.window_views_mouse_get_y([]).Conv<double>();
            if (CollisionManager.Collision_Point(inst, mouseX, mouseY, false))
            {
                MouseDown(inst);
                MousePressed(inst);
                MouseReleased(inst);

                if (!inst.mouse_over)
                {
                    inst.mouse_over = true;
                    GamemakerObject.ExecuteEvent(inst, inst.Definition, EventType.Mouse, (int)EventSubtypeMouse.MouseEnter);
                }
            }
            else
            {
                if (inst.mouse_over)
                {
                    inst.mouse_over = false;
                    GamemakerObject.ExecuteEvent(inst, inst.Definition, EventType.Mouse, (int)EventSubtypeMouse.MouseLeave);
                }
            }
        }

        MouseDown();
        MousePressed();
        MouseReleased();

        // TODO: mouse wheel
    }

    public static void MouseDown(GamemakerObject? inst = null)
    {
        for (var i = 0; i < 3; i++)
        {
            if (!InputHandler.MouseDown[i])
            {
                continue;
            }

            if (inst is not null)
            {
                GamemakerObject.ExecuteEvent(inst, inst.Definition, EventType.Mouse, (int)EventSubtypeMouse.LeftButton + i);
            }
            else
            {
                Handle((int)EventSubtypeMouse.GlobLeftButton + i, EventType.Mouse);
            }
        }
    }

    public static void MousePressed(GamemakerObject? inst = null)
    {
        for (var i = 0; i < 3; i++)
        {
            if (!InputHandler.MousePressed[i])
            {
                continue;
            }

            if (inst is not null)
            {
                GamemakerObject.ExecuteEvent(inst, inst.Definition, EventType.Mouse, (int)EventSubtypeMouse.LeftPressed + i);
            }
            else
            {
                Handle((int)EventSubtypeMouse.GlobLeftPressed + i, EventType.Mouse);
            }
        }
    }

    public static void MouseReleased(GamemakerObject? inst = null)
    {
        for (var i = 0; i < 3; i++)
        {
            if (!InputHandler.MouseReleased[i])
            {
                continue;
            }

            if (inst is not null)
            {
                GamemakerObject.ExecuteEvent(inst, inst.Definition, EventType.Mouse, (int)EventSubtypeMouse.LeftReleased + i);
            }
            else
            {
                Handle((int)EventSubtypeMouse.GlobLeftReleased + i, EventType.Mouse);
            }
        }
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
