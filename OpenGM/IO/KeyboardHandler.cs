﻿using System.Diagnostics;
using OpenGM.VirtualMachine;
using System.Runtime.InteropServices;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenGM.Rendering;

namespace OpenGM.IO;

public class KeyboardHandler
{
    public static bool[] KeyDown = new bool[256];
    public static bool[] KeyPressed = new bool[256];
    public static bool[] KeyReleased = new bool[256];

    public static bool[] MouseDown = new bool[5];
    public static bool[] MousePressed = new bool[5];
    public static bool[] MouseReleased = new bool[5];

    public static void UpdateMouseState(MouseState state)
    {
        var mouseButtons = new[] { MouseButton.Left, MouseButton.Middle, MouseButton.Right, MouseButton.Button1, MouseButton.Button2 };

        for (var i = 0; i < 5; i++)
        {
            var isDown = state.IsButtonDown(mouseButtons[i]);
            var wasDown = MouseDown[i];

            MousePressed[i] = isDown && !wasDown;
            MouseReleased[i] = !isDown && wasDown;
            MouseDown[i] = isDown;
        }
    }

    public static void UpdateKeyboardState(KeyboardState state)
    {
        for (var i = 0; i < 256; i++)
        {
            var isDown = CustomWindow.Instance.IsFocused && IsKeyDown(i);
            var wasDown = KeyDown[i];

            KeyPressed[i] = isDown && !wasDown;
            KeyReleased[i] = !isDown && wasDown;
            KeyDown[i] = isDown;
        }

        // debug
        VMExecutor.VerboseStackLogs = state.IsKeyDown(Keys.F1);

		if (state.IsKeyDown(Keys.F2))
        {
            CustomWindow.Instance.UpdateFrequency = 0.0; // This means fastest
        }
        else if (state.IsKeyDown(Keys.F3))
        {
	        CustomWindow.Instance.UpdateFrequency = 2;
        }
        else
        {
            CustomWindow.Instance.UpdateFrequency = Entry.GameSpeed;
        }

        if (state.IsKeyPressed(Keys.F5))
        {
            VMExecutor.DebugMode = !VMExecutor.DebugMode;
            VariableResolver.GlobalVariables["debug"] = VMExecutor.DebugMode;
			DebugLog.LogInfo($"Debug mode : {VMExecutor.DebugMode}");
        }

        if (state.IsKeyPressed(Keys.KeyPad0))
        {
            DebugLog.Log("INSTANCES :");
            foreach (var instance in InstanceManager.instances.Values)
            {
                DebugLog.Log($" - {instance.Definition.Name} ({instance.instanceId}) Persistent:{instance.persistent} Active:{instance.Active} Marked:{instance.Marked} Destroyed:{instance.Destroyed}");
            }

            DebugLog.Log("DRAW OBJECTS :");
            foreach (var item in DrawManager._drawObjects)
            {
	            if (item is GamemakerObject gm)
	            {
		            DebugLog.Log($" - {gm.Definition.Name} ({gm.instanceId}) Persistent:{gm.persistent} Active:{gm.Active} Marked:{gm.Marked} Destroyed:{gm.Destroyed}");
				}
	            else
	            {
		            DebugLog.Log($" - ??? InstanceID:{item.instanceId} Depth:{item.depth}");
				}
            }
        }
    }

    public static bool KeyboardCheckDirect(int key)
    {
        // TODO : work out how this is different? tried using GetAsyncKeyState and it didnt work
        if (key < 256)
        {
            return KeyDown[key];
        }

        return false;
    }

    private static bool IsKeyDown(int key) => ((ushort)GetKeyState(key) & 0x8000) != 0;

    [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
    private static extern short GetKeyState(int keyCode);

    [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
    private static extern short GetAsyncKeyState(int keyCode);
}
