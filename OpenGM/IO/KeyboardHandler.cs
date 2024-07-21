﻿using OpenGM.VirtualMachine;
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
        if (state.IsKeyPressed(Keys.F1))
        {
            VMExecutor.VerboseStackLogs = !VMExecutor.VerboseStackLogs;
        }
        if (state.IsKeyDown(Keys.F2))
        {
            CustomWindow.Instance.UpdateFrequency = 0.0; // This means fastest
        }
        else
        {
            CustomWindow.Instance.UpdateFrequency = Entry.GameSpeed;
        }
    }

    private static bool IsKeyDown(int key) => ((ushort)GetKeyState(key) & 0x8000) != 0;

    [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
    private static extern short GetKeyState(int keyCode);
}