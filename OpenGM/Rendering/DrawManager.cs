using System.Diagnostics;
using OpenGM.IO;
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
            if (item is GamemakerObject gm && gm._createRan && RoomManager.RoomLoaded && gm.visible)
            {
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

        if (RoomManager.New_Room != -1)
        {
            RoomManager.ChangeToWaitingRoom();
            return true;
        }

        return false;
    }

    public static void FixedUpdate()
    {
        /*
         * https://github.com/YoYoGames/GameMaker-HTML5/blob/develop/scripts/_GameMaker.js#L1716
         * GameMaker_DoAStep
         */
        
		//VariableResolver.GlobalVariables["debug"] = true;

		var stepList = _drawObjects.OrderBy(x => x.instanceId);

        InstanceManager.RememberOldPositions();
        InstanceManager.UpdateImages();

		// g_pLayerManager.UpdateLayers();

		if (RunStepScript(stepList, EventSubtypeStep.BeginStep))
        {
            return;
        }

		// do resize event

		// g_pASyncManager.Process();

		// HandleTimeLine();

		// HandleTimeSources();

		// HandleAlarm()
		foreach (var item in stepList)
        {
            if (item is GamemakerObject gm)
            {
                gm.UpdateAlarms();
            }
        }

        HandleKeyboard();

		// HandleMouse();

		if (RoomManager.New_Room != -1)
        {
            RoomManager.ChangeToWaitingRoom();
            return;
        }

		if (RunStepScript(stepList, EventSubtypeStep.Step))
		{
			return;
		}

		//  ProcessSpriteMessageEvents();

        InstanceManager.UpdatePositions(); // UpdateInstancePositions

		// HandleOther();

		// YYPushEventsDispatch();


		// UpdateCollisions();	
		foreach (var item in stepList)
        {
            if (item is GamemakerObject gmo)
            {
                foreach (var id in gmo.Definition.CollisionScript.Keys)
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

        if (RunStepScript(stepList, EventSubtypeStep.EndStep))
        {
            return;
        }

        var destroyedList = new List<int>();
        foreach (var (instanceId, instance) in InstanceManager.instances)
        {
			if (!instance.Marked)
	        {
                continue;
	        }

			//DebugLog.Log($"DESTROY {instance.Definition.Name}");

			destroyedList.Add(instanceId);
			instance.Destroy();
		}

        foreach (var id in destroyedList)
        {
	        InstanceManager.instances.Remove(id);
        }
        
        /*
         * https://github.com/YoYoGames/GameMaker-HTML5/blob/develop/scripts/yyRoom.js#L4168
         * yyRoom.prototype.Draw
         */

		var drawList = _drawObjects.OrderByDescending(x => x.depth).ThenBy(x => x.instanceId);

        if (CustomWindow.Instance != null) // only null in tests
        {
	        GL.Clear(ClearBufferMask.ColorBufferBit);
		}

        /*
         * PreDraw
         */
		if (RunDrawScript(drawList, EventSubtypeDraw.PreDraw))
        {
            return;
        }
        
        GL.Uniform1(VertexManager.u_flipY, 0); // dont flip when not drawing to backbuffer

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
		// TODO: at some point this must be replaced by drawing each view

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

        if (SurfaceManager.UsingAppSurface)
        {
            SurfaceManager.surface_reset_target();
        }
        if (SurfaceManager.SurfaceStack.Count != 0)
        {
            DebugLog.LogError("Unbalanced surface stack. You MUST use surface_reset_target() for each set.");
            // BUG: one new game in ch2, this becomes unbalanced. i have no idea why.
            // i dont feel like actually fixing this right now
            Debugger.Break();
            while (SurfaceManager.SurfaceStack.Count != 0)
            {
                SurfaceManager.surface_reset_target();
            }
            return;
        }
        
        GL.Uniform1(VertexManager.u_flipY, 1); // flip when drawing to backbuffer

        /*
         * PostDraw
         */
        if (RunDrawScript(drawList, EventSubtypeDraw.PostDraw))
        {
            return;
        }

        /*
         * DrawApplicationSurface 
         */
        if (SurfaceManager.UsingAppSurface)
        {
            GL.Disable(EnableCap.Blend);
            SurfaceManager.draw_surface_stretched(SurfaceManager.application_surface,
                0, 0, CustomWindow.Instance!.FramebufferSize.X, CustomWindow.Instance.FramebufferSize.Y);
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

        //GamemakerCamera.Instance.GetComponent<Camera>().Render();
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
