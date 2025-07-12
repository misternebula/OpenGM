using OpenGM.Rendering;
using OpenGM.VirtualMachine;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace OpenGM.IO;

public class KeyboardHandler
{
    public enum State
    {
        NORMAL,
        PLAYBACK,
        RECORD
    }

    public static State HandlerState = State.NORMAL;
    public static FileStream? IOFilestream;

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

    /// <summary>
    /// https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
    /// most virtual keys map to opentk keys, but some dont, so we gotta do that here
    /// </summary>
    public static Keys[] Convert(int keyid)
    {
        return keyid switch
        {
            0x0D => [Keys.Enter],
            0x10 => [Keys.LeftShift, Keys.RightShift],
            0x11 => [Keys.LeftControl, Keys.RightControl],
            0x12 => [Keys.LeftAlt, Keys.RightAlt],
            0x1B => [Keys.Escape],

            0x21 => [Keys.PageUp],
            0x22 => [Keys.PageDown],
            0x23 => [Keys.End],
            0x24 => [Keys.Home],
            0x25 => [Keys.Left],
            0x26 => [Keys.Up],
            0x27 => [Keys.Right],
            0x28 => [Keys.Down],
            0x2D => [Keys.Insert],
            0x2E => [Keys.Delete],

            0x70 => [Keys.F1],
            0x71 => [Keys.F2],
            0x72 => [Keys.F3],
            0x73 => [Keys.F4],
            0x74 => [Keys.F5],
            0x75 => [Keys.F6],
            0x76 => [Keys.F7],
            0x77 => [Keys.F8],
            0x78 => [Keys.F9],
            0x79 => [Keys.F10],
            0x7A => [Keys.F11],
            0x7B => [Keys.F12],

            0xA0 => [Keys.LeftShift],
            0xA1 => [Keys.RightShift],

            _ => [(Keys)keyid]
        };
    }

    private static void RecordIOState()
    {
        if (IOFilestream == null)
        {
            throw new NullReferenceException("IOFilestream is null.");
        }

        IOFilestream.Write(new byte[4]); // IO_LastChar
        IOFilestream.Write(new byte[4100]); // IO_InputString
        IOFilestream.Write(new byte[4]); // IO_LastKey
        IOFilestream.Write(new byte[4]); // IO_CurrentKey

        for (var i = 0; i < 256; i++)
        {
            IOFilestream.WriteByte(KeyDown[i] ? (byte)1 : (byte)0);
        }

        for (var i = 0; i < 256; i++)
        {
            IOFilestream.WriteByte(KeyReleased[i] ? (byte)1 : (byte)0);
        }

        for (var i = 0; i < 256; i++)
        {
            IOFilestream.WriteByte(KeyPressed[i] ? (byte)1 : (byte)0);
        }

        IOFilestream.Write(new byte[40]); // IO_LastButton
        IOFilestream.Write(new byte[40]); // IO_CurrentButton

        IOFilestream.Write(new byte[50]); // IO_ButtonDown
        IOFilestream.Write(new byte[50]); // IO_ButtonReleased
        IOFilestream.Write(new byte[50]); // IO_ButtonPressed

        IOFilestream.Write(new byte[10]); // IO_WheelUp
        IOFilestream.Write(new byte[10]); // IO_WheelDown

        IOFilestream.Write(new byte[8]); // IO_MousePos
        IOFilestream.Write(new byte[4]); // IO_MouseX
        IOFilestream.Write(new byte[4]); // IO_MouseY
    }

    private static void PlaybackIOState()
    {
        if (IOFilestream == null)
        {
            throw new NullReferenceException("IOFilestream is null.");
        }

        IOFilestream.ReadExactly(new byte[4]); // IO_LastChar
        IOFilestream.ReadExactly(new byte[4100]); // IO_InputString
        IOFilestream.ReadExactly(new byte[4]); // IO_LastKey
        IOFilestream.ReadExactly(new byte[4]); // IO_CurrentKey

        for (var i = 0; i < 256; i++)
        {
            KeyDown[i] = IOFilestream.ReadByte() == 1;
        }

        for (var i = 0; i < 256; i++)
        {
            KeyReleased[i] = IOFilestream.ReadByte() == 1;
        }

        for (var i = 0; i < 256; i++)
        {
            KeyPressed[i] = IOFilestream.ReadByte() == 1;
        }

        IOFilestream.ReadExactly(new byte[40]); // IO_LastButton
        IOFilestream.ReadExactly(new byte[40]); // IO_CurrentButton

        IOFilestream.ReadExactly(new byte[50]); // IO_ButtonDown
        IOFilestream.ReadExactly(new byte[50]); // IO_ButtonReleased
        IOFilestream.ReadExactly(new byte[50]); // IO_ButtonPressed

        IOFilestream.ReadExactly(new byte[10]); // IO_WheelUp
        IOFilestream.ReadExactly(new byte[10]); // IO_WheelDown

        IOFilestream.ReadExactly(new byte[8]); // IO_MousePos
        IOFilestream.ReadExactly(new byte[4]); // IO_MouseX
        IOFilestream.ReadExactly(new byte[4]); // IO_MouseY

        if (IOFilestream.Position == IOFilestream.Length)
        {
            HandlerState = State.NORMAL;
        }
    }

    public static void UpdateKeyboardState(KeyboardState state)
    {
        if (HandlerState == State.PLAYBACK)
        {
            PlaybackIOState();
            return;
        }

        void CalculateKey(int vk, bool isDown)
        {
            var wasDown = KeyDown[vk];

            KeyPressed[vk] = isDown && !wasDown;
            KeyReleased[vk] = !isDown && wasDown;
            KeyDown[vk] = isDown;
        }

        CalculateKey(0, !state.IsAnyKeyDown); // vk_nokey
        CalculateKey(1, state.IsAnyKeyDown); // vk_anykey

        for (var i = 2; i < 256; i++)
        {
            CalculateKey(i, CustomWindow.Instance.IsFocused && Convert(i).Any(state.IsKeyDown));
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

        if (state.IsKeyPressed(Keys.KeyPad1))
        {
            DebugLog.Log("LAYERS :");
            foreach (var (id, layer) in RoomManager.CurrentRoom.Layers)
            {
                DebugLog.Log($" - Layer: {layer.Name} ({id})");
                foreach (var element in layer.ElementsToDraw)
                {
                    var str = $"     - {element.GetType().Name} ({element.instanceId})";
                    if (element is GMSprite sprite) 
                    {

                        str += $" [{sprite.X}, {sprite.Y}]";
                    }
                    else if (element is GMBackground bg) 
                    {

                        str += $" Index:{bg.Element.Index} Frame:{bg.FrameIndex}";
                    }
                    DebugLog.Log(str);
                }
            }
        }

        if (state.IsKeyPressed(Keys.KeyPad2))
        {
            DrawManager.DebugBBoxes = !DrawManager.DebugBBoxes;
        }

        if (HandlerState == State.RECORD)
        {
            RecordIOState();
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
}
