using UndertaleModLib.Models;

namespace DELTARUNITYStandalone;
public static class DrawManager
{
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

	private static void RunStepScript(IOrderedEnumerable<DrawWithDepth> items, EventSubtypeStep stepType)
	{
		foreach (var item in items)
		{
			if (item is GamemakerObject gm && gm._createRan/* && Room.RoomLoaded*/)
			{
				GamemakerObject.ExecuteScript(gm, gm.Definition, EventType.Step, (uint)stepType);
			}
		}
	}

	private static void RunDrawScript(IOrderedEnumerable<DrawWithDepth> items, EventSubtypeDraw drawType)
	{
		foreach (var item in items)
		{
			if (item is GamemakerObject gm && gm._createRan/* && Room.RoomLoaded*/)
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

				GamemakerObject.ExecuteScript(gm, gm.Definition, EventType.Draw, (uint)drawType);
			}
			else if (drawType == EventSubtypeDraw.Draw)
			{
				item.Draw();
			}
		}
	}

	public static void FixedUpdate()
	{
		var stepList = _drawObjects.OrderByDescending(x => x.instanceId);

		RunStepScript(stepList, EventSubtypeStep.BeginStep);

		foreach (var item in stepList)
		{
			if (item is GamemakerObject gm)
			{
				gm.UpdateAlarms();
			}
		}

		foreach (var item in stepList)
		{
			if (item is GamemakerObject gmo)
			{
				foreach (var id in gmo.Definition.CollisionScript.Keys)
				{
					/*var collide = CollisionManager.instance_place_assetid(gmo.x, gmo.y, id, gmo);
					if (collide != null)
					{
						VMExecutor.EnvironmentStack.Push(new VMScriptExecutionContext() { Self = collide, ObjectDefinition = collide.Definition, Stack = new() });
						GamemakerObject.ExecuteScript(gmo, gmo.Definition, EventType.Collision, id);
						VMExecutor.EnvironmentStack.Pop();
					}*/
				}
			}
		}

		RunStepScript(stepList, EventSubtypeStep.Step);
		RunStepScript(stepList, EventSubtypeStep.EndStep);

		var drawList = _drawObjects.OrderByDescending(x => x.depth).ThenByDescending(x => x.instanceId);

		RunDrawScript(drawList, EventSubtypeDraw.PreDraw);
		RunDrawScript(drawList, EventSubtypeDraw.DrawBegin);
		RunDrawScript(drawList, EventSubtypeDraw.Draw);
		RunDrawScript(drawList, EventSubtypeDraw.DrawEnd);
		RunDrawScript(drawList, EventSubtypeDraw.PostDraw);
		RunDrawScript(drawList, EventSubtypeDraw.DrawGUIBegin);
		RunDrawScript(drawList, EventSubtypeDraw.DrawGUI);
		RunDrawScript(drawList, EventSubtypeDraw.DrawGUIEnd);

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
