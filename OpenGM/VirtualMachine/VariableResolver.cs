using OpenGM.IO;
using OpenGM.Rendering;
using OpenGM.VirtualMachine.BuiltInFunctions;
using System.Collections;
using System.Diagnostics;

namespace OpenGM.VirtualMachine;

public static class VariableResolver
{
    /// <summary>
    /// general form of gamemaker array index setting logic 
    /// </summary>
    /// <param name="index">the index to set</param>
    /// <param name="value">the value to set</param>
    /// <param name="getter">gets the variable that might exist and might contain the array</param>
    /// <param name="setter">sets the variable to the array, if it's not already</param>
    /// <param name="onlyGrow">instead of setting index to value, just grow the array using the default value</param>
    public static void ArraySet(int index, object? value,
        Func<object?> getter, Action<IList>? setter = null,
        bool onlyGrow = false)
    {
        if (getter() is not IList array)
        {
            array = new List<object?>();
            if (setter != null)
            {
                setter(array);
            }
            else
            {
                throw new ArgumentException("getter returned non-array or null, and there's no setter. what?");
            }
        }

        if (index >= array.Count)
        {
            if (array.IsFixedSize)
            {
                throw new ArgumentException("tried to grow fixed size array. use List<T> instead of T[]");
            }
            
            var numToAdd = index - array.Count + 1;
            for (var i = 0; i < numToAdd; i++)
            {
                // html uses undefined
                // cpp seems to set length, so 0 rvalue = real value 0
                // TODO: make gamemaker test program...
                array.Add(value switch
                {
                    int or short or long or float or double => 0,
                    bool => false,
                    string => "",
                    IList => new List<object?>(), // new list per element so this switch is in for loop
                    GMLObject => null, // for storing structs
                    Method => null,
                    null => null,
                    _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
                });
            }
        }
        
        // this used to call setter after to trigger side-effects (with builtins mainly)
        // that broke PT, so it was removed

        if (onlyGrow) return; // TODO: should we call the setter here too just in case?

        var arrayType = array.GetType();

        Type elementType;
        if (arrayType.IsGenericType) // List<T>
        {
            elementType = arrayType.GenericTypeArguments[0];
        }
        else if (arrayType.IsArray) // T[]
        {
            elementType = arrayType.GetElementType()!;
        }
        else
        {
            throw new UnreachableException($"array is not list or array (is {arrayType}");
        }

        if (elementType != typeof(object)) // typed array
        {
            array[index] = value.Conv(elementType);
        }
        else // untyped array
        {
            array[index] = value;
        }
    }

    public static readonly Dictionary<string, object?> GlobalVariables = new();

    public static Dictionary<string, object?> CustomBuiltInVariableValues = new();
    // InitGlobalVariables
    public static Dictionary<string, (Func<object?> getter, Action<object?>? setter)> BuiltInVariables = new()
    {
        // argument_relative
        { "argument", (get_argument, null) },
        { "argument0", (get_argument_0, (val) => VMExecutor.PopToArgument(0, val)) },
        { "argument1", (get_argument_1, (val) => VMExecutor.PopToArgument(1, val)) },
        { "argument2", (get_argument_2, (val) => VMExecutor.PopToArgument(2, val)) },
        { "argument3", (get_argument_3, (val) => VMExecutor.PopToArgument(3, val)) },
        { "argument4", (get_argument_4, (val) => VMExecutor.PopToArgument(4, val)) },
        { "argument5", (get_argument_5, (val) => VMExecutor.PopToArgument(5, val)) },
        { "argument6", (get_argument_6, (val) => VMExecutor.PopToArgument(6, val)) },
        { "argument7", (get_argument_7, (val) => VMExecutor.PopToArgument(7, val)) },
        { "argument8", (get_argument_8, (val) => VMExecutor.PopToArgument(8, val)) },
        { "argument9", (get_argument_9, (val) => VMExecutor.PopToArgument(9, val)) },
        { "argument10", (get_argument_10, (val) => VMExecutor.PopToArgument(10, val)) },
        { "argument11", (get_argument_11, (val) => VMExecutor.PopToArgument(11, val)) },
        { "argument12", (get_argument_12, (val) => VMExecutor.PopToArgument(12, val)) },
        { "argument13", (get_argument_13, (val) => VMExecutor.PopToArgument(13, val)) },
        { "argument14", (get_argument_14, (val) => VMExecutor.PopToArgument(14, val)) },
        { "argument15", (get_argument_15, (val) => VMExecutor.PopToArgument(15, val)) },
        { "argument_count", (get_argument_count, null) },
        { "debug_mode", (get_debug_mode, null)},
        // pointer_invalid
        { "pointer_null", (get_pointer_null, null)},
        { "undefined", (get_undefined, null) },
        // NaN
        // infinity
        { "room", (get_room, set_room) },
        // room_first
        // room_last
        // transition_kind
        // transition_steps
        // score
        // lives
        // health
        // game_id
        // game_display_name
        // game_project_name
        // game_save_id
        { "working_directory", (get_working_directory, null) },
        // temp_directory
        // program_directory
        { "instance_count", (get_instance_count, null)},
        // instance_id
        { "room_width", (get_room_width, null) },
        { "room_height", (get_room_height, null) },
        // room_caption
        { "room_speed", (get_room_speed, set_room_speed) },
        { "room_persistent", (get_room_persistent, set_room_persistent)},
        { "background_color", (get_background_color, set_background_color)},
        // background_showcolor
        // background_colour
        // background_showcolour
        // background_visible
        // background_foreground
        // background_index
        // background_x
        // background_y
        // background_width
        // background_height
        // background_htiled
        // background_vtiled
        // background_xscale
        // background_yscale
        // background_hspeed
        // background_vspeed
        // background_blend
        // background_alpha
        // view_enabled
        { "view_current", (get_view_current, null)},
        { "view_visible", (get_view_visible, set_view_visible)},
        { "view_xview", (get_view_xview, set_view_xview)},
        { "view_yview", (get_view_yview, set_view_yview)},
        { "view_wview", (get_view_wview, set_view_wview)},
        { "view_hview", (get_view_hview, set_view_hview)},
        // view_xport
        // view_yport
        { "view_wport", (get_view_wport, set_view_wport)},
        { "view_hport", (get_view_hport, set_view_hport)},
        // view_angle
        // view_hborder
        // view_vborder
        // view_hspeed
        // view_vspeed
        // view_object
        // view_surface_id
        { "view_camera", (get_view_camera, set_view_camera)},
        { "mouse_x", (get_mouse_x, null)},
        { "mouse_y", (get_mouse_y, null)},
        // mouse_button
        // mouse_lastbutton
        // keyboard_key
        // keyboard_lastkey
        // keyboard_lastchar
        // keyboard_string
        // cursor_sprite
        // show_score
        // show_lives
        // show_health
        // caption_score
        // caption_lives
        // caption_health
        { "fps", (get_fps, null) },
        // fps_real
        { "delta_time", (get_delta_time, null) },
        { "current_time", (get_current_time, null)},
        // current_year
        { "current_month", (get_current_month, null)},
        // current_day
        // current_weekday
        // current_hour
        // current_minute
        { "current_second", (get_current_second, null)},
        // event_type
        // event_number
        // event_object
        // event_action
        // error_occurred
        // error_last
        // gamemaker_registered
        // gamemaker_pro
        { "application_surface", (get_application_surface, null) },
        // font_texture_page_size

        { "async_load", (get_async_load, null) },

        // InitYoYoBuiltInVariables
        { "os_type", (get_os_type, null) },
        // os_device
        // os_version
        // os_browser
        { "browser_width", (get_browser_width, null) },
        { "browser_height", (get_browser_height, null) },
    };

    // InitLocalVariables
    public static Dictionary<string, (Func<GamemakerObject, object> getter, Action<GamemakerObject, object?>? setter)> BuiltInSelfVariables = new()
    {
        { "x", (get_x, set_x) },
        { "y", (get_y, set_y) },
        { "xprevious", (get_xprevious, set_xprevious)},
        { "yprevious", (get_yprevious, set_yprevious)},
        { "xstart", (get_xstart, set_xstart) },
        { "ystart", (get_ystart, set_ystart) },
        { "hspeed", (get_hspeed, set_hspeed) },
        { "vspeed", (get_vspeed, set_vspeed) },
        { "direction", (get_direction, set_direction) },
        { "speed", (get_speed, set_speed) },
        { "friction", (get_friction, set_friction) },
        { "gravity", (get_gravity, set_gravity) },
        { "gravity_direction", (get_gravity_direction, set_gravity_direction) },
        // in_collision_tree
        { "object_index", (get_object_index, null) },
        { "id", (get_id, null) },
        { "alarm", (get_alarm, set_alarm) },
        // solid
        { "visible", (get_visible, set_visible) },
        { "persistent", (get_persistent, set_persistent) },
        { "depth", (get_depth, set_depth) },
        { "bbox_left", (get_bbox_left, null) },
        { "bbox_right", (get_bbox_right, null) },
        { "bbox_top", (get_bbox_top, null) },
        { "bbox_bottom", (get_bbox_bottom, null) },
        { "sprite_index", (get_sprite_index, set_sprite_index) },
        { "image_index", (get_image_index, set_image_index) },
        // image_single
        { "image_number", (get_image_number, null) },
        { "sprite_width", (get_sprite_width, null) },
        { "sprite_height", (get_sprite_height, null) },
        { "sprite_xoffset", (get_sprite_xoffset, null)},
        { "sprite_yoffset", (get_sprite_yoffset, null)},
        { "image_xscale", (get_image_xscale, set_image_xscale) },
        { "image_yscale", (get_image_yscale, set_image_yscale) },
        { "image_angle", (get_image_angle, set_image_angle) },
        { "image_alpha", (get_image_alpha, set_image_alpha) },
        { "image_blend", (get_image_blend, set_image_blend) },
        { "image_speed", (get_image_speed, set_image_speed) },
        { "mask_index", (get_mask_index, set_mask_index)},
        { "path_index", (get_path_index, null)},
        { "path_position", (get_path_position, set_path_position)},
        // path_positionprevious
        { "path_speed", (get_path_speed, set_path_speed)},
        { "path_scale", (get_path_scale, set_path_scale)},
        // path_orientation
        // path_endaction
        // ...
        { "layer", (get_layer, set_layer)}
    };

    public static object get_working_directory()
    {
        return Entry.DataWinFolder + Path.DirectorySeparatorChar;
    }

    public static object get_fps()
    {
        return Entry.GameSpeed; // TODO : this shouldnt be the desired fps, but the current fps (fluctuating)
    }

    public static object get_delta_time()
    {
        return CustomWindow.Instance.DeltaTime;
    }

    public static object get_x(GamemakerObject instance) => instance.x;
    public static void set_x(GamemakerObject instance, object? value) => instance.x = value.Conv<double>();

    public static object get_y(GamemakerObject instance) => instance.y;
    public static void set_y(GamemakerObject instance, object? value) => instance.y = value.Conv<double>();

    public static object get_room_width() => (double)RoomManager.CurrentRoom.SizeX;
    public static object get_room_height() => (double)RoomManager.CurrentRoom.SizeY;

    public static object get_image_index(GamemakerObject instance) => instance.image_index;
    public static void set_image_index(GamemakerObject instance, object? value) => instance.image_index = value.Conv<double>();

    public static object get_mask_index(GamemakerObject instance) => instance.mask_index;
    public static void set_mask_index(GamemakerObject instance, object? value) => instance.mask_index = value.Conv<int>();

    public static object get_sprite_index(GamemakerObject instance) => instance.sprite_index;
    public static void set_sprite_index(GamemakerObject instance, object? value) => instance.sprite_index = value.Conv<int>();

    public static object get_sprite_width(GamemakerObject instance) => instance.sprite_width;
    public static object get_sprite_height(GamemakerObject instance) => instance.sprite_height;

    public static object get_xstart(GamemakerObject instance) => instance.xstart;
    public static void set_xstart(GamemakerObject instance, object? value) => instance.xstart = value.Conv<double>();
    public static object get_ystart(GamemakerObject instance) => instance.ystart;
    public static void set_ystart(GamemakerObject instance, object? value) => instance.ystart = value.Conv<double>();

    public static object get_object_index(GamemakerObject instance) => instance.object_index;

    public static object get_image_blend(GamemakerObject instance) => instance.image_blend;
    public static void set_image_blend(GamemakerObject instance, object? value) => instance.image_blend = value.Conv<int>();

    public static object get_depth(GamemakerObject instance) => instance.depth;
    public static void set_depth(GamemakerObject instance, object? value) => instance.depth = value.Conv<double>();

    public static object get_room() => RoomManager.CurrentRoom.AssetId;
    public static void set_room(object? value) => RoomManager.New_Room = value.Conv<int>();

    public static object get_bbox_bottom(GamemakerObject instance) => (instance.sprite_index == -1 && instance.mask_index == -1) ? instance.y : instance.bbox_bottom;
    public static object get_bbox_top(GamemakerObject instance) => (instance.sprite_index == -1 && instance.mask_index == -1) ? instance.y : instance.bbox_top;
    public static object get_bbox_left(GamemakerObject instance) => (instance.sprite_index == -1 && instance.mask_index == -1) ? instance.x : instance.bbox_left;
    public static object get_bbox_right(GamemakerObject instance) => (instance.sprite_index == -1 && instance.mask_index == -1) ? instance.x : instance.bbox_right;

    public static object get_image_yscale(GamemakerObject instance) => instance.image_yscale;
    public static void set_image_yscale(GamemakerObject instance, object? value) => instance.image_yscale = value.Conv<double>();

    public static object get_image_xscale(GamemakerObject instance) => instance.image_xscale;
    public static void set_image_xscale(GamemakerObject instance, object? value) => instance.image_xscale = value.Conv<double>();

    public static object get_image_speed(GamemakerObject instance) => instance.image_speed;
    public static void set_image_speed(GamemakerObject instance, object? value) => instance.image_speed = value.Conv<double>();

    public static object get_visible(GamemakerObject instance) => instance.visible;
    public static void set_visible(GamemakerObject instance, object? value) => instance.visible = value.Conv<bool>();

    public static object get_image_alpha(GamemakerObject instance) => instance.image_alpha;
    public static void set_image_alpha(GamemakerObject instance, object? value) => instance.image_alpha = value.Conv<double>();

    public static object get_image_angle(GamemakerObject instance) => instance.image_angle;
    public static void set_image_angle(GamemakerObject instance, object? value) => instance.image_angle = value.Conv<double>();

    public static object get_speed(GamemakerObject instance) => instance.speed;
    public static void set_speed(GamemakerObject instance, object? value) => instance.speed = value.Conv<double>();

    public static object get_hspeed(GamemakerObject instance) => instance.hspeed;
    public static void set_hspeed(GamemakerObject instance, object? value) => instance.hspeed = value.Conv<double>();

    public static object get_vspeed(GamemakerObject instance) => instance.vspeed;
    public static void set_vspeed(GamemakerObject instance, object? value)
    {
        //DebugLog.Log($"{instance.Definition.Name} set vspeed to {value}");
        instance.vspeed = value.Conv<double>();
    }

    public static object get_direction(GamemakerObject instance) => instance.direction;
    public static void set_direction(GamemakerObject instance, object? value) => instance.direction = value.Conv<double>();

    public static object get_view_current() => 0; // TODO : aghhhhh viewports aghhh

    public static object get_persistent(GamemakerObject instance) => instance.persistent;
    public static void set_persistent(GamemakerObject instance, object? value) => instance.persistent = value.Conv<bool>();

    public static object get_id(GamemakerObject instance) => instance.instanceId;

    public static object get_gravity(GamemakerObject instance) => instance.gravity;
    public static void set_gravity(GamemakerObject instance, object? value) => instance.gravity = value.Conv<double>();

    public static object get_friction(GamemakerObject instance) => instance.friction;
    public static void set_friction(GamemakerObject instance, object? value) => instance.friction = value.Conv<double>();

    public static object get_gravity_direction(GamemakerObject instance) => instance.gravity_direction;
    public static void set_gravity_direction(GamemakerObject instance, object? value) => instance.gravity_direction = value.Conv<double>();

    public static object get_image_number(GamemakerObject instance) => SpriteManager.GetNumberOfFrames(instance.sprite_index);

    public static object get_room_persistent() => RoomManager.CurrentRoom.Persistent;

    public static void set_room_persistent(object? value)
    {
        var val = value.Conv<bool>();
        DebugLog.Log($"room_persistent = {val}");
        RoomManager.CurrentRoom.Persistent = val;
    }

    public static object get_room_speed() => Entry.GameSpeed;
    public static void set_room_speed(object? value) => Entry.SetGameSpeed(value.Conv<int>());

    public static object get_async_load() => AsyncManager.AsyncLoadDsIndex;

    public static object get_os_type() => 0; // TODO : Check if this is actually os_windows

    public static object get_browser_width() => CustomWindow.Instance.ClientSize.X;
    public static object get_browser_height() => CustomWindow.Instance.ClientSize.Y;

    public static object get_application_surface() => SurfaceManager.application_surface;

    public static object get_alarm(GamemakerObject instance) => instance.alarm;
    public static void set_alarm(GamemakerObject instance, object? value) => instance.alarm = value.Conv<IList>().ConvAll<int>().ToArray();

    public static object get_argument_count() => VMExecutor.Call.Locals["arguments"].Conv<IList>().Count;
    public static object get_argument() => VMExecutor.Call.Locals["arguments"]!;
    public static object? get_argument_0() => VMExecutor.Call.Locals["arguments"].Conv<IList>()[0];
    public static object? get_argument_1() => VMExecutor.Call.Locals["arguments"].Conv<IList>()[1];
    public static object? get_argument_2() => VMExecutor.Call.Locals["arguments"].Conv<IList>()[2];
    public static object? get_argument_3() => VMExecutor.Call.Locals["arguments"].Conv<IList>()[3];
    public static object? get_argument_4() => VMExecutor.Call.Locals["arguments"].Conv<IList>()[4];
    public static object? get_argument_5() => VMExecutor.Call.Locals["arguments"].Conv<IList>()[5];
    public static object? get_argument_6() => VMExecutor.Call.Locals["arguments"].Conv<IList>()[6];
    public static object? get_argument_7() => VMExecutor.Call.Locals["arguments"].Conv<IList>()[7];
    public static object? get_argument_8() => VMExecutor.Call.Locals["arguments"].Conv<IList>()[8];
    public static object? get_argument_9() => VMExecutor.Call.Locals["arguments"].Conv<IList>()[9];
    public static object? get_argument_10() => VMExecutor.Call.Locals["arguments"].Conv<IList>()[10];
    public static object? get_argument_11() => VMExecutor.Call.Locals["arguments"].Conv<IList>()[11];
    public static object? get_argument_12() => VMExecutor.Call.Locals["arguments"].Conv<IList>()[12];
    public static object? get_argument_13() => VMExecutor.Call.Locals["arguments"].Conv<IList>()[13];
    public static object? get_argument_14() => VMExecutor.Call.Locals["arguments"].Conv<IList>()[14];
    public static object? get_argument_15() => VMExecutor.Call.Locals["arguments"].Conv<IList>()[15];

    private static void SetArgumentIndex(int index, object? value)
    {
        var args = VMExecutor.Call.Locals["arguments"].Conv<IList>();
        args[index] = value;
        VMExecutor.Call.Locals["arguments"] = args;
    }

    public static object? get_undefined() => null;

    public static object get_view_wport() => ViewportManager.view_wport;

    public static void set_view_wport(object? value)
    {
        ViewportManager.view_wport = value.Conv<IList>().ConvAll<int>().ToArray();
        ViewportManager.UpdateFromArrays();
    }

    public static object get_view_hport() => ViewportManager.view_hport;
    public static void set_view_hport(object? value)
    {
        ViewportManager.view_hport = value.Conv<IList>().ConvAll<int>().ToArray();
        ViewportManager.UpdateFromArrays();
    }

    public static object get_view_xview() => ViewportManager.view_xview;
    public static void set_view_xview(object? value)
    {
        ViewportManager.view_xview = value.Conv<IList>().ConvAll<float>().ToArray();
        ViewportManager.UpdateFromArrays();
    }

    public static object get_view_yview() => ViewportManager.view_yview;
    public static void set_view_yview(object? value)
    {
        ViewportManager.view_yview = value.Conv<IList>().ConvAll<float>().ToArray();
        ViewportManager.UpdateFromArrays();
    }

    public static object get_view_wview() => ViewportManager.view_wview;
    public static void set_view_wview(object? value)
    {
        ViewportManager.view_wview = value.Conv<IList>().ConvAll<float>().ToArray();
        ViewportManager.UpdateFromArrays();
    }

    public static object get_view_hview() => ViewportManager.view_hview;
    public static void set_view_hview(object? value)
    {
        ViewportManager.view_hview = value.Conv<IList>().ConvAll<float>().ToArray();
        ViewportManager.UpdateFromArrays();
    }

    public static object get_view_camera() => ViewportManager.view_camera;
    public static void set_view_camera(object? value)
    {
        ViewportManager.view_camera = value.Conv<IList>().ConvAll<int>().ToArray();
        ViewportManager.UpdateFromArrays();
    }

    public static object get_view_visible() => ViewportManager.view_visible;
    public static void set_view_visible(object? value)
    {
        ViewportManager.view_visible = value.Conv<IList>().ConvAll<bool>().ToArray();
        ViewportManager.UpdateFromArrays();
    }

    public static object get_mouse_x() => GraphicFunctions.window_views_mouse_get_x([]).Conv<double>();
    public static object get_mouse_y() => GraphicFunctions.window_views_mouse_get_y([]).Conv<double>();

    public static object? get_pointer_null() => null;

    public static object get_xprevious(GamemakerObject instance) => instance.xprevious;
    public static void set_xprevious(GamemakerObject instance, object? value) => instance.xprevious = value.Conv<double>();

    public static object get_yprevious(GamemakerObject instance) => instance.yprevious;
    public static void set_yprevious(GamemakerObject instance, object? value) => instance.yprevious = value.Conv<double>();

    public static object get_sprite_xoffset(GamemakerObject instance) => instance.sprite_xoffset;
    public static object get_sprite_yoffset(GamemakerObject instance) => instance.sprite_yoffset;

    public static object get_path_index(GamemakerObject instance) => instance.path_index;

    public static object get_path_position(GamemakerObject instance) => instance.path_position;
    public static void set_path_position(GamemakerObject instance, object? value) => instance.path_position = value.Conv<double>();

    public static object get_path_speed(GamemakerObject instance) => instance.path_speed;
    public static void set_path_speed(GamemakerObject instance, object? value) => instance.path_speed = value.Conv<double>();

    public static object get_path_scale(GamemakerObject instance) => instance.path_scale;
    public static void set_path_scale(GamemakerObject instance, object? value) => instance.path_scale = value.Conv<double>();

    public static object get_instance_count() => InstanceManager.instances.Count; // TODO : this should only count instances at the START of the step

    public static object get_current_time() => (DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime()).TotalMilliseconds; // TODO : do this in a better way

    public static object get_current_month() => DateTime.Now.Month;

    public static object get_current_second() => DateTime.Now.Second;

    public static object get_debug_mode() => false;

    public static object? get_background_color()
    {
        // TODO : Implement
        return null;
    }

    public static void set_background_color(object? value)
    {
        // TODO : Implement
    }

    public static object get_layer(GamemakerObject instance) => instance.Layer;
    public static void set_layer(GamemakerObject instance, object? value) => instance.Layer = value.Conv<int>();
}
