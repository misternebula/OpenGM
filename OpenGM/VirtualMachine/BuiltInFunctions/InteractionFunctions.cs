using OpenGM.IO;

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
        // keyboard_key_press
        // keyboard_key_release
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
                        result = KeyboardHandler.KeyDown[i] != true && result;
                    }
                    return result;
                }
                case 1:
                {
                    var result = false;
                    for (var i = 0; i <= 255; ++i)
                    {
                        result = KeyboardHandler.KeyDown[i] || result;
                    }
                    return result;
                }
                case > 255:
                    return false;
                default:
                    return KeyboardHandler.KeyDown[key];
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
                        result = KeyboardHandler.KeyPressed[i] != true && result;
                    }
                    return result;
                }
                case 1:
                {
                    var result = false;
                    for (var i = 0; i <= 255; ++i)
                    {
                        result = KeyboardHandler.KeyPressed[i] || result;
                    }
                    return result;
                }
                case > 255:
                    return false;
                default:
                    return KeyboardHandler.KeyPressed[key];
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
                        result = KeyboardHandler.KeyReleased[i] != true && result;
                    }
                    return result;
                }
                case 1:
                {
                    var result = false;
                    for (var i = 0; i <= 255; ++i)
                    {
                        result = KeyboardHandler.KeyReleased[i] || result;
                    }
                    return result;
                }
                case > 255:
                    return false;
                default:
                    return KeyboardHandler.KeyReleased[key];
            }
        }

        [GMLFunction("keyboard_check_direct")]
        public static object? keyboard_check_direct(object?[] args)
        {
            var key = args[0].Conv<int>();
            return KeyboardHandler.KeyboardCheckDirect(key);
        }

        // mouse_check_button

        [GMLFunction("mouse_check_button_pressed")]
        public static object mouse_check_button_pressed(object?[] args)
        {
            var numb = args[0].Conv<int>();
            return KeyboardHandler.MousePressed[numb];
        }

        // mouse_check_button_released
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

            KeyboardHandler.KeyDown = new bool[256];
            KeyboardHandler.KeyPressed = new bool[256];
            KeyboardHandler.KeyReleased = new bool[256];

            KeyboardHandler.MouseDown = new bool[5];
            KeyboardHandler.KeyPressed = new bool[5];
            KeyboardHandler.KeyReleased = new bool[5];
            return null;
        }

        [GMLFunction("device_mouse_x_to_gui", GMLFunctionFlags.Stub)]
        public static object? device_mouse_x_to_gui(object?[] args)
        {
            return 0;
        }

        [GMLFunction("device_mouse_y_to_gui", GMLFunctionFlags.Stub)]
        public static object? device_mouse_y_to_gui(object?[] args)
        {
            return 0;
        }

        // device_mouse_dbclick_enable
        // browser_input_capture

        // gesture stuff
    }
}
