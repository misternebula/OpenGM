using OpenGM.IO;
using OpenGM.VirtualMachine;
using OpenTK.Graphics.OpenGL;
using UndertaleModLib.Models;
using EventType = OpenGM.VirtualMachine.EventType;

namespace OpenGM.Rendering;
public static class DrawManager
{
    public static int CirclePrecision = 24;

    private static List<DrawWithDepth> _drawObjects = new();

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
                GamemakerObject.ExecuteScript(gm, gm.Definition, EventType.Step, (int)stepType);
            }
        }

        if (RoomManager.ChangeRoomAfterEventExecution)
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
            if (item is GamemakerObject gm && gm._createRan && RoomManager.RoomLoaded)
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

                GamemakerObject.ExecuteScript(gm, gm.Definition, EventType.Draw, (int)drawType);
            }
            else if (drawType == EventSubtypeDraw.Draw)
            {
                item.Draw();
            }
        }

        if (RoomManager.ChangeRoomAfterEventExecution)
        {
            RoomManager.ChangeToWaitingRoom();
            return true;
        }

        return false;
    }

    public static void FixedUpdate()
    {
        //VariableResolver.GlobalVariables["debug"] = true;

        var stepList = _drawObjects.OrderByDescending(x => x.instanceId);

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

        if (RoomManager.ChangeRoomAfterEventExecution)
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
                    var collide = CollisionManager.instance_place_assetid(gmo.x, gmo.y, id, gmo);
                    if (collide != null)
                    {
                        VMExecutor.EnvironmentStack.Push(new VMScriptExecutionContext() { Self = collide, ObjectDefinition = collide.Definition, Stack = new() });
                        GamemakerObject.ExecuteScript(gmo, gmo.Definition, EventType.Collision, id);
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

        var drawList = _drawObjects.OrderByDescending(x => x.depth).ThenBy(x => x.instanceId);

        // reference for this surface code is here: https://github.com/YoYoGames/GameMaker-HTML5/blob/develop/scripts/yyRoom.js#L3989
        
        GL.Clear(ClearBufferMask.ColorBufferBit);

        if (RunDrawScript(drawList, EventSubtypeDraw.PreDraw))
        {
            return;
        }

        // SurfaceManager.surface_set_target(SurfaceManager.application_surface);
        
        // GL.Clear(ClearBufferMask.ColorBufferBit);

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

        // SurfaceManager.draw_surface_stretched(SurfaceManager.application_surface, 
            // 0, 0, CustomWindow.Instance.ClientSize.X, CustomWindow.Instance.ClientSize.Y);

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
}
