using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace DELTARUNITYStandalone;
public class KeyboardHandler
{
	public static bool[] KeyDown = new bool[256];
	public static bool[] KeyPressed = new bool[256];
	public static bool[] KeyReleased = new bool[256];

	public static void UpdateKeyboardState(KeyboardState state)
	{
		for (var i = 0; i < 256; i++)
		{
			var isDown = IsKeyDown(i);
			var wasDown = KeyDown[i];

			KeyPressed[i] = isDown && !wasDown;
			KeyReleased[i] = !isDown && wasDown;
			KeyDown[i] = isDown;
		}
	}

	private static bool IsKeyDown(int key) => ((ushort)GetKeyState(key) & 0x8000) != 0;

	[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
	private static extern short GetKeyState(int keyCode);
}
