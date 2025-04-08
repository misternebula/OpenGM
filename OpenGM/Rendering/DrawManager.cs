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
        //VariableResolver.GlobalVariables["debug"] = true;

        var stepList = _drawObjects.OrderBy(x => x.instanceId);

        foreach (var item in stepList)
        {
	        if (item is not GamemakerObject gm)
	        {
                continue;
	        }

	        gm.xprevious = gm.x;
	        gm.yprevious = gm.y;
        }

        if (RunStepScript(stepList, EventSubtypeStep.BeginStep))
        {
            return;
        }

        foreach (var item in stepList)
        {
            if (item is GamemakerObject gm)
            {
                gm.UpdateAlarms();
            }
        }

        HandleKeyboard();

		if (RoomManager.New_Room != -1)
        {
            RoomManager.ChangeToWaitingRoom();
            return;
        }

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
                        VMExecutor.EnvironmentStack.Push(new VMScriptExecutionContext() { Self = collide, ObjectDefinition = collide.Definition, Stack = new() });
                        GamemakerObject.ExecuteEvent(gmo, gmo.Definition, EventType.Collision, id);
                        VMExecutor.EnvironmentStack.Pop();
                    }
                }
            }
        }

        if (RunStepScript(stepList, EventSubtypeStep.Step))
        {
            return;
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

		var drawList = _drawObjects.OrderByDescending(x => x.depth).ThenBy(x => x.instanceId);

        // reference for this surface code is here: https://github.com/YoYoGames/GameMaker-HTML5/blob/develop/scripts/yyRoom.js#L4168

        if (CustomWindow.Instance != null) // only null in tests
        {
	        GL.Clear(ClearBufferMask.ColorBufferBit);
		}

        // ROOM BACKGROUNDS
        // BUG: uhhh this happens before application surface? idk where this is in html5 code
        //      and old backgrounds dont register themselves in the regular draw events? why?
        //      or maybe this intentionally draws directly to the display buffer like pre/postdraw
        foreach (var item in RoomManager.CurrentRoom.OldBackgrounds)
        {
	        if (item == null)
	        {
		        continue;
	        }

	        item.Draw();
        }

		if (RunDrawScript(drawList, EventSubtypeDraw.PreDraw))
        {
            return;
        }

        /*
        SurfaceManager.surface_set_target(SurfaceManager.application_surface);
        
        if (CustomWindow.Instance != null) // only null in tests
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);
        }
        */

        // TODO: at some point this must be replaced by drawing each view
        
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

        // SurfaceManager.surface_reset_target();
        if (SurfaceManager.SurfaceStack.Count != 0)
        {
            DebugLog.LogError("Unbalanced surface stack. You MUST use surface_reset_target() for each set.");
            return;
        }

        if (RunDrawScript(drawList, EventSubtypeDraw.PostDraw))
        {
            return;
        }

        /*
        SurfaceManager.draw_surface_stretched(SurfaceManager.application_surface, 
            0, 0, CustomWindow.Instance!.FramebufferSize.X, CustomWindow.Instance.FramebufferSize.Y);
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

        foreach (var item in drawList)
        {
            if (item is GamemakerObject)
            {
                item.Draw();
            }
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
