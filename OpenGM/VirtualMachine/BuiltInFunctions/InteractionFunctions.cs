using OpenGM.IO;
using OpenGM.Rendering;

namespace OpenGM.VirtualMachine.BuiltInFunctions
{
    public static class InteractionFunctions
    {
        // show_message
        // show_question
        // show_message_async
        // show_question_async
        // show_error
        // show_info
        // load_info
        // highscore_clear
        // highscore_add
        // highscore_value
        // highscore_name
        // draw_highscore
        // get_integer
        // get_integer_async
        // get_string
        // get_string_async
        // get_login_async
        // get_open_filename
        // get_save_filename
        // get_open_filename_ext
        // get_save_filename_ext
        // keyboard_get_numlock
        // keyboard_set_numlock

        [GMLFunction("keyboard_key_press")]
        public static object? keyboard_key_press(object?[] args)
        {
            var key = args[0].Conv<int>();

            if (InputHandler.KeyDown[key])
            {
                return null;
            }

            InputHandler.KeyDown[key] = true;
            InputHandler.KeyPressed[key] = true;

            return null;
        }

        [GMLFunction("keyboard_key_release")]
        public static object? keyboard_key_release(object?[] args)
        {
            var key = args[0].Conv<int>();

            InputHandler.KeyDown[key] = false;
            // oddly doesn't change KeyPressed in the HTML runner

            return null;
        }

        // keyboard_set_map
        // keyboard_get_map
        // keyboard_unset_map

        [GMLFunction("keyboard_check")]
        public static object? keyboard_check(object?[] args)
        {
            var key = args[0].Conv<int>();

            // from disassembly
            switch (key)
            {
                case 0:
                {
                    var result = true;
                    for (var i = 0; i <= 255; ++i)
                    {
                        result = InputHandler.KeyDown[i] != true && result;
                    }
                    return result;
                }
                case 1:
                {
                    var result = false;
                    for (var i = 0; i <= 255; ++i)
                    {
                        result = InputHandler.KeyDown[i] || result;
                    }
                    return result;
                }
                case > 255:
                    return false;
                default:
                    return InputHandler.KeyDown[key];
            }
        }

        [GMLFunction("keyboard_check_pressed")]
        public static object? keyboard_check_pressed(object?[] args)
        {
            var key = args[0].Conv<int>();

            // from disassembly
            switch (key)
            {
                case 0:
                {
                    var result = true;
                    for (var i = 0; i <= 255; ++i)
                    {
                        result = InputHandler.KeyPressed[i] != true && result;
                    }
                    return result;
                }
                case 1:
                {
                    var result = false;
                    for (var i = 0; i <= 255; ++i)
                    {
                        result = InputHandler.KeyPressed[i] || result;
                    }
                    return result;
                }
                case > 255:
                    return false;
                default:
                    return InputHandler.KeyPressed[key];
            }
        }

        [GMLFunction("keyboard_check_released")]
        public static object? keyboard_check_released(object?[] args)
        {
            var key = args[0].Conv<int>();

            // from disassembly
            switch (key)
            {
                case 0:
                {
                    var result = true;
                    for (var i = 0; i <= 255; ++i)
                    {
                        result = InputHandler.KeyReleased[i] != true && result;
                    }
                    return result;
                }
                case 1:
                {
                    var result = false;
                    for (var i = 0; i <= 255; ++i)
                    {
                        result = InputHandler.KeyReleased[i] || result;
                    }
                    return result;
                }
                case > 255:
                    return false;
                default:
                    return InputHandler.KeyReleased[key];
            }
        }

        [GMLFunction("keyboard_check_direct")]
        public static object? keyboard_check_direct(object?[] args)
        {
            var key = args[0].Conv<int>();
            return InputHandler.KeyboardCheckDirect(key);
        }

        [GMLFunction("mouse_check_button")]
        public static object mouse_check_button(object?[] args)
        {
            var numb = args[0].Conv<int>();

            return numb switch
            {
                -1 => InputHandler.MouseDown.Any(x => x),
                0 => InputHandler.MouseDown.All(x => !x),
                _ => InputHandler.MouseDown[numb - 1]
            };
        }

        [GMLFunction("mouse_check_button_pressed")]
        public static object mouse_check_button_pressed(object?[] args)
        {
            var numb = args[0].Conv<int>();
            return numb switch
            {
                -1 => InputHandler.MousePressed.Any(x => x),
                0 => InputHandler.MousePressed.All(x => !x),
                _ => InputHandler.MousePressed[numb - 1]
            };
        }

        [GMLFunction("mouse_check_button_released")]
        public static object mouse_check_button_released(object?[] args)
        {
            var numb = args[0].Conv<int>();
            return numb switch
            {
                -1 => InputHandler.MouseReleased.Any(x => x),
                0 => InputHandler.MouseReleased.All(x => !x),
                _ => InputHandler.MouseReleased[numb - 1]
            };
        }

        // mouse_wheel_up
        // mouse_wheel_down
        // keyboard_virtual_show
        // keyboard_virtual_hide
        // keyboard_virtual_status
        // keyboard_virtual_height
        // keyboard_clear
        // mouse_clear

        [GMLFunction("io_clear")]
        public static object? io_clear(object?[] args)
        {
            // TODO : clear other IO variables when we implement them

            InputHandler.KeyDown = new bool[256];
            InputHandler.KeyPressed = new bool[256];
            InputHandler.KeyReleased = new bool[256];

            InputHandler.MouseDown = new bool[5];
            InputHandler.MousePressed = new bool[5];
            InputHandler.MouseReleased = new bool[5];
            return null;
        }

        [GMLFunction("device_mouse_x_to_gui")]
        public static object? device_mouse_x_to_gui(object?[] args)
        {
            var dev = args[0].Conv<int>();

            var mouseX = InputHandler.MousePos.X;
            var windowWidth = GraphicFunctions.window_get_width([]).Conv<int>();
            var guiWidth = DrawManager.GuiSize?.X ?? windowWidth;
            return CustomMath.DoubleTilde(mouseX * (guiWidth / windowWidth));
        }

        [GMLFunction("device_mouse_y_to_gui")]
        public static object? device_mouse_y_to_gui(object?[] args)
        {
            var dev = args[0].Conv<int>();

            var mouseY = InputHandler.MousePos.Y;
            var windowHeight = GraphicFunctions.window_get_height([]).Conv<int>();
            var guiHeight = DrawManager.GuiSize?.Y ?? windowHeight;
            return CustomMath.DoubleTilde(mouseY * (guiHeight / windowHeight));
        }

        // device_mouse_dbclick_enable
        // browser_input_capture

        // gesture stuff
    }
}
