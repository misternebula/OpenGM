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

    public static readonly GMLObject GlobalVariables = new();

    public static Dictionary<string, object?> CustomBuiltInVariableValues = new();
    // InitGlobalVariables
    public static Dictionary<string, (Func<int, object?>? getter, Action<object?, int>? setter)> BuiltInVariables = new()
    {

        { "argument_relative", (null, null)},
        { "argument", (get_argument, null) },
        { "argument0", ((_) => get_argument(0), (val, _) => VMExecutor.PopToArgument(0, val)) },
        { "argument1", ((_) => get_argument(1), (val, _) => VMExecutor.PopToArgument(1, val)) },
        { "argument2", ((_) => get_argument(2), (val, _) => VMExecutor.PopToArgument(2, val)) },
        { "argument3", ((_) => get_argument(3), (val, _) => VMExecutor.PopToArgument(3, val)) },
        { "argument4", ((_) => get_argument(4), (val, _) => VMExecutor.PopToArgument(4, val)) },
        { "argument5", ((_) => get_argument(5), (val, _) => VMExecutor.PopToArgument(5, val)) },
        { "argument6", ((_) => get_argument(6), (val, _) => VMExecutor.PopToArgument(6, val)) },
        { "argument7", ((_) => get_argument(7), (val, _) => VMExecutor.PopToArgument(7, val)) },
        { "argument8", ((_) => get_argument(8), (val, _) => VMExecutor.PopToArgument(8, val)) },
        { "argument9", ((_) => get_argument(9), (val, _) => VMExecutor.PopToArgument(9, val)) },
        { "argument10", ((_) => get_argument(10), (val, _) => VMExecutor.PopToArgument(10, val)) },
        { "argument11", ((_) => get_argument(11), (val, _) => VMExecutor.PopToArgument(11, val)) },
        { "argument12", ((_) => get_argument(12), (val, _) => VMExecutor.PopToArgument(12, val)) },
        { "argument13", ((_) => get_argument(13), (val, _) => VMExecutor.PopToArgument(13, val)) },
        { "argument14", ((_) => get_argument(14), (val, _) => VMExecutor.PopToArgument(14, val)) },
        { "argument15", ((_) => get_argument(15), (val, _) => VMExecutor.PopToArgument(15, val)) },
        { "argument_count", (get_argument_count, null) },
        { "debug_mode", (get_debug_mode, null)},
        { "pointer_invalid", (null, null)},
        { "pointer_null", (get_pointer_null, null)},
        { "undefined", (get_undefined, null) },
        { "NaN", (get_nan, null) },
        { "infinity", (get_infinity, null) },
        { "room", (get_room, set_room) },
        { "room_first", (null, null)},
        { "room_last", (null, null)},
        { "transition_kind", (null, null)},
        { "transition_steps", (null, null)},
        { "score", (null, null)},
        { "lives", (null, null)},
        { "health", (null, null)},
        { "game_id", (null, null)},
        { "game_display_name", (null, null)},
        { "game_project_name", (null, null)},
        { "game_save_id", (get_game_save_id, null)},
        { "working_directory", (get_working_directory, null) },
        { "temp_directory", (null, null)},
        { "program_directory", (null, null)},
        { "instance_count", (get_instance_count, null)},
        { "instance_id", (null, null)},
        { "room_width", (get_room_width, null) },
        { "room_height", (get_room_height, null) },
        { "room_caption", (null, null)},
        { "room_speed", (get_room_speed, set_room_speed) },
        { "room_persistent", (get_room_persistent, set_room_persistent)},
        { "background_color", (null, null)},
        { "background_showcolor", (null, null)},
        { "background_colour", (null, null)},
        { "background_showcolour", (null, null)},
        { "background_visible", (null, null)},
        { "background_foreground", (null, null)},
        { "background_index", (null, null)},
        { "background_x", (null, null)},
        { "background_y", (null, null)},
        { "background_width", (null, null)},
        { "background_height", (null, null)},
        { "background_htiled", (null, null)},
        { "background_vtiled", (null, null)},
        { "background_xscale", (get_background_xscale, set_background_xscale)},
        { "background_yscale", (get_background_yscale, set_background_yscale)},
        { "background_hspeed", (null, null)},
        { "background_vspeed", (null, null)},
        { "background_blend", (null, null)},
        { "background_alpha", (null, null)},
        { "view_enabled", (null, null)},
        { "view_current", (get_view_current, null)},
        { "view_visible", (get_view_visible, set_view_visible)},
        { "view_xview", (get_view_xview, set_view_xview)},
        { "view_yview", (get_view_yview, set_view_yview)},
        { "view_wview", (get_view_wview, set_view_wview)},
        { "view_hview", (get_view_hview, set_view_hview)},
        { "view_xport", (get_view_xport, set_view_xport)},
        { "view_yport", (get_view_yport, set_view_yport)},
        { "view_wport", (get_view_wport, set_view_wport)},
        { "view_hport", (get_view_hport, set_view_hport)},
        { "view_angle", (null, null)},
        { "view_hborder", (null, null)},
        { "view_vborder", (null, null)},
        { "view_hspeed", (null, null)},
        { "view_vspeed", (null, null)},
        { "view_object", (null, null)},
        { "view_surface_id", (null, null)},
        { "view_camera", (get_view_camera, set_view_camera)},
        { "mouse_x", (get_mouse_x, null)},
        { "mouse_y", (get_mouse_y, null)},
        // mouse_button
        // mouse_lastbutton
        // keyboard_key
        // keyboard_lastkey
        // keyboard_lastchar
        { "keyboard_string", (get_keyboard_string, set_keyboard_string)},
        // cursor_sprite
        // show_score
        // show_lives
        // show_health
        // caption_score
        // caption_lives
        // caption_health
        { "fps", (get_fps, null) },
        { "fps_real", (get_fps_real, null) },
        { "current_time", (get_current_time, null)},
        // current_year
        { "current_month", (get_current_month, null)},
        // current_day
        { "current_weekday", (get_current_weekday, null)},
        // current_hour
        // current_minute
        { "current_second", (get_current_second, null)},
        { "event_type", (get_event_type, null)},
        { "event_number", (get_event_number, null)},
        // event_object
        // event_action
        // error_occurred
        // error_last
        // gamemaker_registered
        // gamemaker_pro
        { "application_surface", (get_application_surface, null) },
        // font_texture_page_size

        // InitYoYoBuiltInVariables
        { "os_type", (get_os_type, null) },
        // os_device
        // os_version
        // os_browser
        { "browser_width", (get_browser_width, null) },
        { "browser_height", (get_browser_height, null) },

        /* TODO: this is a hack fix! delta_time is a self variable,
         * but can be called by constructors which don't have an associated GamemakerObject!
         */
        { "delta_time", ((_) => TimingManager.DeltaTime, null)} 
    };

    // InitLocalVariables
    public static Dictionary<string, (Func<GamemakerObject, object>? getter, Action<GamemakerObject, object?>? setter)> BuiltInSelfVariables = new()
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
        { "in_collision_tree", (null, null)},
        { "object_index", (get_object_index, null) },
        { "id", (get_id, null) },
        { "alarm", (get_alarm, set_alarm) },
        { "solid", (null, null)},
        { "visible", (get_visible, set_visible) },
        { "persistent", (get_persistent, set_persistent) },
        { "depth", (get_depth, set_depth) },
        { "bbox_left", (get_bbox_left, null) },
        { "bbox_right", (get_bbox_right, null) },
        { "bbox_top", (get_bbox_top, null) },
        { "bbox_bottom", (get_bbox_bottom, null) },
        { "sprite_index", (get_sprite_index, set_sprite_index) },
        { "image_index", (get_image_index, set_image_index) },
        { "image_single", (null, null)},
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
        { "path_positionprevious", (null, null)},
        { "path_speed", (get_path_speed, set_path_speed)},
        { "path_scale", (get_path_scale, set_path_scale)},
        { "path_orientation", (null, null)},
        { "path_endaction", (null, null)},
        
        // .. timeline stuff

        { "async_load", (get_async_load, null) },
        // event_data
        // iap_data

        // .. physics stuff

        // display_aa
        { "delta_time", (get_delta_time, null) },
        // webgl_enabled
        { "layer", (get_layer, set_layer)}
        // in_sequence
        // sequence_instance
    };

    public static object get_working_directory(int index)
    {
        return Entry.DataWinFolder + Path.DirectorySeparatorChar;
    }

    public static object get_fps(int index) => TimingManager.FPS;

    public static object get_fps_real(int index) => TimingManager.FPSReal;

    public static object get_delta_time(GamemakerObject instance)
    {
        // why is this a self variable? who knows!
        return TimingManager.DeltaTime;
    }

    public static object get_x(GamemakerObject instance) => instance.x;
    public static void set_x(GamemakerObject instance, object? value) => instance.x = value.Conv<double>();

    public static object get_y(GamemakerObject instance) => instance.y;
    public static void set_y(GamemakerObject instance, object? value) => instance.y = value.Conv<double>();

    public static object get_room_width(int index) => (double)RoomManager.CurrentRoom.SizeX;
    public static object get_room_height(int index) => (double)RoomManager.CurrentRoom.SizeY;

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

    public static object get_room(int index) => RoomManager.CurrentRoom.AssetId;
    public static void set_room(object? value, int index) => RoomManager.New_Room = value.Conv<int>();

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
    public static void set_vspeed(GamemakerObject instance, object? value) => instance.vspeed = value.Conv<double>();

    public static object get_direction(GamemakerObject instance) => instance.direction;
    public static void set_direction(GamemakerObject instance, object? value) => instance.direction = value.Conv<double>();

    public static object get_view_current(int index) => DrawManager.ViewCurrent;

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

    public static object get_room_persistent(int index) => RoomManager.CurrentRoom.Persistent;
    public static void set_room_persistent(object? value, int index)
    {
        var val = value.Conv<bool>();
        DebugLog.Log($"room_persistent = {val}");
        RoomManager.CurrentRoom.Persistent = val;
    }

    public static object get_room_speed(int index) => Entry.GameSpeed;
    public static void set_room_speed(object? value, int index) => Entry.SetGameSpeed(value.Conv<int>());

    public static object get_async_load(GamemakerObject instance) => AsyncManager.AsyncLoadDsIndex;

    public static object get_os_type(int index) => 0;

    public static object get_browser_width(int index) => CustomWindow.Instance.ClientSize.X;
    public static object get_browser_height(int index) => CustomWindow.Instance.ClientSize.Y;

    public static object get_application_surface(int index) => SurfaceManager.application_surface;

    public static object get_alarm(GamemakerObject instance) => instance.alarm;
    public static void set_alarm(GamemakerObject instance, object? value) => instance.alarm = value.Conv<IList>().ConvAll<int>().ToArray();

    public static object get_argument_count(int index) => VMExecutor.Call.Locals["arguments"].Conv<IList>().Count;

    private static object? get_argument(int index)
    {
        var arguments = VMExecutor.Call.Locals["arguments"].Conv<IList>();
        if (arguments.Count <= index)
        {
            // Reading arguments past the given ones gives undefined.
            return null;
        }

        return arguments[index];
    }

    private static void SetArgumentIndex(int index, object? value)
    {
        var args = VMExecutor.Call.Locals["arguments"].Conv<IList>();
        args[index] = value;
        VMExecutor.Call.Locals["arguments"] = args;
    }

    public static object? get_undefined(int index) => null;

    public static object? get_nan(int index) => double.NaN;
    public static object? get_infinity(int index) => double.PositiveInfinity;

    public static object get_view_xport(int index) => RoomManager.CurrentRoom.Views[index].PortPosition.X;
    public static void set_view_xport(object? value, int index) => RoomManager.CurrentRoom.Views[index].PortPosition.X = value.Conv<int>();

    public static object get_view_yport(int index) => RoomManager.CurrentRoom.Views[index].PortPosition.Y;
    public static void set_view_yport(object? value, int index) => RoomManager.CurrentRoom.Views[index].PortPosition.Y = value.Conv<int>();

    public static object get_view_wport(int index) => RoomManager.CurrentRoom.Views[index].PortSize.X;
    public static void set_view_wport(object? value, int index) => RoomManager.CurrentRoom.Views[index].PortSize.X = value.Conv<int>();

    public static object get_view_hport(int index) => RoomManager.CurrentRoom.Views[index].PortSize.Y;
    public static void set_view_hport(object? value, int index) => RoomManager.CurrentRoom.Views[index].PortSize.Y = value.Conv<int>();

    public static object get_view_xview(int index) => RoomManager.CurrentRoom.Views[index].ViewX;
    public static void set_view_xview(object? value, int index) => RoomManager.CurrentRoom.Views[index].ViewX = value.Conv<float>();

    public static object get_view_yview(int index) => RoomManager.CurrentRoom.Views[index].ViewY;
    public static void set_view_yview(object? value, int index) => RoomManager.CurrentRoom.Views[index].ViewY = value.Conv<float>();

    public static object get_view_wview(int index) => RoomManager.CurrentRoom.Views[index].ViewW;
    public static void set_view_wview(object? value, int index) => RoomManager.CurrentRoom.Views[index].ViewW = value.Conv<float>();

    public static object get_view_hview(int index) => RoomManager.CurrentRoom.Views[index].ViewH;
    public static void set_view_hview(object? value, int index) => RoomManager.CurrentRoom.Views[index].ViewH = value.Conv<float>();

    public static object get_view_camera(int index) => RoomManager.CurrentRoom.Views[index].Camera.ID;
    public static void set_view_camera(object? value, int index) => RoomManager.CurrentRoom.Views[index].Camera = CameraManager.GetCamera(value.Conv<int>())!;

    public static object get_view_visible(int index) => RoomManager.CurrentRoom.Views[index].Visible;
    public static void set_view_visible(object? value, int index) => RoomManager.CurrentRoom.Views[index].Visible = value.Conv<bool>();

    public static object get_mouse_x(int index) => GraphicFunctions.window_views_mouse_get_x([]).Conv<double>();
    public static object get_mouse_y(int index) => GraphicFunctions.window_views_mouse_get_y([]).Conv<double>();

    public static object get_keyboard_string(int index) => InputHandler.KeyboardString;
    public static void set_keyboard_string(object? value, int index) => InputHandler.KeyboardString = value.Conv<string>();

    public static object? get_pointer_null(int index) => null;

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

    public static object get_instance_count(int index) => InstanceManager.instances.Count; // TODO : this should only count instances at the START of the step

    public static object get_current_time(int index) => (DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime()).TotalMilliseconds; // TODO : do this in a better way

    public static object get_current_month(int index) => DateTime.Now.Month;

    public static object get_current_weekday(int index) => DateTime.Now.DayOfWeek;

    public static object get_current_second(int index) => DateTime.Now.Second;

    public static object get_debug_mode(int index) => false;

    public static object get_layer(GamemakerObject instance) => instance.Layer;
    public static void set_layer(GamemakerObject instance, object? value) => instance.Layer = value.Conv<int>();

    public static object? get_game_save_id(int index) => LoadSave.GetSavePrePend();

    public static object? get_event_type(int index) => (int)DrawManager.EventType - 1;
    public static object? get_event_number(int index) => DrawManager.EventNumber;

    public static object? get_background_xscale(int index)
    {
        DebugLog.LogWarning("get_background_xscale not implemented.");
        return 0;
    }

    public static void set_background_xscale(object? value, int index)
    {
        DebugLog.LogWarning("set_background_xscale not implemented.");
    }

    public static object? get_background_yscale(int index)
    {
        DebugLog.LogWarning("get_background_yscale not implemented.");
        return 0;
    }

    public static void set_background_yscale(object? value, int index)
    {
        DebugLog.LogWarning("set_background_yscale not implemented.");
    }
}
