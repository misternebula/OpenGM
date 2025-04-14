using OpenGM.SerializedFiles;
using Newtonsoft.Json.Linq;
using OpenTK.Audio.OpenAL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System.Collections;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using OpenTK.Graphics.OpenGL;
using OpenGM.Rendering;
using OpenGM.IO;
using OpenGM.Loading;
using StbImageSharp;
using StbVorbisSharp;
using static UndertaleModLib.Models.UndertaleRoom;
using static UndertaleModLib.Models.UndertaleBackground;

namespace OpenGM.VirtualMachine;
public static partial class ScriptResolver
{
	public static Dictionary<string, VMScript> Scripts = new();
	public static List<VMCode?> GlobalInit = new();

	public static Dictionary<string, (VMCode script, int index)> ScriptFunctions = new Dictionary<string, (VMCode script, int index)>();

	public static Dictionary<string, Func<object?[], object?>> BuiltInFunctions = new()
	{
		#region Game
		//{ "move_random", move_random },
		//{ "place_free", place_free },
		//{ "place_empty", place_empty },
		{ "place_meeting", place_meeting },
		//{ "place_snapped", place_snapped },
		//{ "move_snap", move_snap },
		{ "move_towards_point", move_towards_point },
		//{ "move_contact", move_contact },
		//{ "move_contact_solid", move_contact_solid },
		//{ "move_contact_all", move_contact_all },
		//{ "move_outside_solid", move_outside_solid },
		//{ "move_outside_all", move_outside_all },
		//{ "move_bounce", move_bounce },
		//{ "move_bounce_solid", move_bounce_solid },
		//{ "move_bounce_all", move_bounce_all },
		//{ "move_wrap", move_wrap },
		//{ "motion_set", motion_set },
		{ "motion_add", motion_add },
		{ "distance_to_point", distance_to_point },
		{ "distance_to_object", distance_to_object },
		{ "path_start", path_start },
		{ "path_end", path_end },
		//{ "mp_linear_step", mp_linear_step },
		//{ "mp_linear_path", mp_linear_path },
		//{ "mp_linear_step_object", mp_linear_step_object },
		//{ "mp_linear_path_object", mp_linear_path_object },
		//{ "mp_potential_settings", mp_potential_settings },
		//{ "mp_potential_step", mp_potential_step },
		//{ "mp_potential_path", mp_potential_path },
		//{ "mp_potential_step_object", mp_potential_step_object },
		//{ "mp_potential_path_object", mp_potential_path_object },
		//{ "mp_grid_create", mp_grid_create },
		//{ "mp_grid_destroy", mp_grid_destroy },
		//{ "mp_grid_clear_all", mp_grid_clear_all },
		//{ "mp_grid_clear_cell", mp_grid_clear_cell },
		//{ "mp_grid_clear_rectangle", mp_grid_clear_rectangle },
		//{ "mp_grid_add_cell", mp_grid_add_cell },
		//{ "mp_grid_get_cell", mp_grid_get_cell },
		//{ "mp_grid_add_rectangle", mp_grid_add_rectangle },
		//{ "mp_grid_add_instances", mp_grid_add_instances },
		//{ "mp_grid_path", mp_grid_path },
		//{ "mp_grid_draw", mp_grid_draw },
		//{ "mp_grid_to_ds_grid", mp_grid_to_ds_grid },
		{ "collision_point", collision_point },
		//{ "collision_point_list", collision_point_list },
		{ "collision_rectangle", collision_rectangle },
		//{ "collision_rectangle_list", collision_rectangle_list },
		//{ "collision_circle", collision_circle },
		//{ "collision_circle_list", collision_circle_list },
		//{ "collision_ellipse", collision_ellipse },
		//{ "collision_ellipse_list", collision_ellipse_list },
		{ "collision_line", collision_line },
		//{ "collision_line_list", collision_line_list },
		{ "instance_find", instance_find },
		{ "instance_exists", instance_exists },
		{ "instance_number", instance_number },
		//{ "instance_position", instance_position },
		//{ "instance_position_list", instance_position_list },
		{ "instance_nearest", instance_nearest },
		//{ "instance_furthest", instance_furthest },
		{ "instance_place", instance_place },
		//{ "instance_place_list", instance_place_list },
		{ "instance_create_depth", instance_create_depth },
		//{ "instance_create_layer", instance_create_layer },
		//{ "instance_copy", instance_copy },
		//{ "instance_change", instance_change },
		{ "instance_destroy", instance_destroy },
		//{ "instance_sprite", instance_sprite },
		//{ "position_empty", position_empty },
		//{ "position_meeting", position_meeting },
		//{ "position_destroy", position_destroy },
		//{ "position_change", position_change },
		//{ "instance_id_get", instance_id_get },
		//{ "instance_deactivate_all", instance_deactivate_all },
		{ "instance_deactivate_object", instance_deactivate_object },
		//{ "instance_deactivate_region", instance_deactivate_region },
		//{ "instance_activate_all", instance_activate_all },
		{ "instance_activate_object", instance_activate_object },
		//{ "instance_activate_region", instance_activate_region },
		//{ "instance_deactivate_region_special", instance_deactivate_region_special },
		{ "room_goto", room_goto },
		{ "room_goto_previous", room_goto_previous },
		{ "room_goto_next", room_goto_next },
		{ "room_previous", room_previous },
		{ "room_next", room_next },
		//{ "room_restart", room_restart },
		//{ "game_end", game_end },
		{ "game_restart", game_restart },
		//{ "game_load", game_load },
		//{ "game_save", game_save },
		//{ "game_save_buffer", game_save_buffer },
		//{ "game_load_buffer", game_load_buffer },
		//{ "transition_define", transition_define },
		//{ "transition_exists", transition_exists },
		//{ "sleep", sleep },
		{ "point_in_rectangle", point_in_rectangle },
		//{ "point_in_circle", point_in_circle },
		//{ "rectangle_in_rectangle", rectangle_in_rectangle },
		//{ "rectangle_in_triangle", rectangle_in_triangle },
		//{ "rectangle_in_circle", rectangle_in_circle },
		#endregion

		#region Math
		//{ "is_bool", is_bool },
		{ "is_real", is_real },
		{ "is_string", is_string },
		//{ "is_array", is_array },
		{ "is_undefined", is_undefined},
		//{ "is_int32", is_int32 },
		//{ "is_int64", is_int64 },
		//{ "is_ptr", is_ptr },
		//{ "is_vec3", is_vec3 },
		//{ "is_vec4", is_vec4 },
		//{ "is_matrix", is_matrix },
		//{ "typeof", typeof },
		{ "array_length_1d", array_length_1d },
		{ "array_length_2d", array_length_2d },
		{ "array_height_2d", array_height_2d },
		//{ "array_get", array_get },
		//{ "array_set", array_set },
		//{ "array_set_pre", array_set_pre },
		//{ "array_set_post", array_set_post },
		//{ "array_get_2D", array_get_2D },
		//{ "array_set_2D", array_set_2D },
		//{ "array_set_2D_pre", array_set_2D_pre },
		//{ "array_set_2D_post", array_set_2D_post },
		//{ "array_equals", array_equals },
		//{ "array_create", array_create },
		//{ "array_copy", array_copy },
		{ "random", random },
		{ "random_range", random_range },
		{ "irandom", irandom },
		{ "irandom_range", irandom_range },
		//{ "random_use_old_version", random_use_old_version },
		//{ "random_set_seed", random_set_seed },
		//{ "random_get_seed", random_get_seed },
		{ "randomize", randomize},
		{ "randomise", randomize},
		//
		{ "round", round },
		{ "floor", floor },
		{ "ceil", ceil },
		{ "sign", sign},
		//{ "frac", frac },
		{ "sqrt", sqrt },
		// 
		//
		//{ "ln", ln },
		//{ "log2", log2 },
		//{ "log10", log10 },
		{ "sin", sin },
		{ "cos", cos },
		//{ "tan", tan },
		{ "arcsin", arcsin},
		{ "arccos", arccos},
		//{ "arctan", arctan },
		//{ "arctan2", arctan2 },
		{ "dsin", dsin},
		{ "dcos", dcos},
		//{ "dtan", dtan },
		//{ "darcsin", darcsin },
		//{ "darccos", darccos },
		//{ "darctan", darctan },
		//{ "darctan2", darctan2 },
		{ "degtorad", degtorad },
		{ "radtodeg", radtodeg},
		{ "power", power},
		//{ "logn", logn },
		{ "min", min },
		{ "max", max },
		//{ "min3", min3 },
		//{ "max3", max3 },
		//{ "mean", mean },
		{ "median", median },
		{ "choose", choose },
		{ "clamp", clamp },
		{ "lerp", lerp },
		{ "real", real },
		//{ "bool", bool },
		{ "string", @string },
		//{ "int64", int64 },
		// 
		//{ "string_format", string_format },
		// 
		//{ "ansi_char", ansi_char },
		{ "ord", ord},
		{ "string_length", string_length },
		{ "string_pos", string_pos },
		{ "string_copy", string_copy },
		{ "string_char_at", string_char_at },
		//{ "string_ord_at", string_ord_at },
		//{ "string_byte_length", string_byte_length },
		//{ "string_byte_at", string_byte_at },
		//{ "string_set_byte_at", string_set_byte_at },
		{ "string_delete", string_delete },
		{ "string_insert", string_insert },
		{ "string_lower", string_lower },
		{ "string_upper", string_upper},
		//{ "string_repeat", string_repeat },
		//{ "string_letters", string_letters },
		{ "string_digits", string_digits },
		//{ "string_lettersdigits", string_lettersdigits },
		//{ "string_replace", string_replace },
		{ "string_replace_all", string_replace_all },
		//{ "string_string_count", string_string_count },
		{ "string_hash_to_newline", string_hash_to_newline },
		{ "point_distance", point_distance },
		{ "point_direction", point_direction },
		{ "lengthdir_x", lengthdir_x },
		{ "lengthdir_y", lengthdir_y },
		//{ "point_distance_3d", point_distance_3d },
		//{ "dot_product", dot_product },
		//{ "dot_product_normalised", dot_product_normalised },
		//{ "dot_product_normalized", dot_product_normalized },
		//{ "dot_product_3d", dot_product_3d },
		//{ "dot_product_3d_normalised", dot_product_3d_normalised },
		//{ "dot_product_3d_normalized", dot_product_3d_normalized },
		//{ "math_set_epsilon", math_set_epsilon },
		//{ "math_get_epsilon", math_get_epsilon },
		{ "angle_difference", angle_difference },

		{ "abs", abs },
		
		#endregion

		#region Graphic
		{ "display_get_width", display_get_width },
		{ "display_get_height", display_get_height },
		{ "window_set_fullscreen", window_set_fullscreen },
		{ "window_get_fullscreen", window_get_fullscreen },
		{ "window_set_caption", window_set_caption },
		{ "window_get_caption", window_get_caption },
		{ "window_set_size", window_set_size },
		{ "window_center", window_center },
		{ "draw_set_colour", draw_set_colour },
		{ "draw_set_color", draw_set_colour }, // mfw
		{ "draw_set_alpha", draw_set_alpha },
		{ "draw_get_alpha", draw_get_alpha },
		{ "draw_get_colour", draw_get_colour },
		{ "draw_get_color", draw_get_colour },
		{ "merge_colour", merge_colour },
		{ "merge_color", merge_colour },
		{ "draw_line_width", draw_line_width },
		{ "draw_rectangle", draw_rectangle },
		{ "draw_set_font", draw_set_font },
		{ "draw_set_halign", draw_set_halign },
		{ "draw_set_valign", draw_set_valign },
		{ "draw_get_halign", draw_get_halign },
		{ "draw_get_valign", draw_get_valign },
		{ "string_width", string_width },
		{ "string_height", string_height },
		{ "draw_text", draw_text },
		{ "draw_text_transformed", draw_text_transformed },
		{ "draw_text_color", draw_text_colour },
		{ "draw_text_colour", draw_text_colour },
		{ "draw_self", draw_self },
		{ "draw_sprite", draw_sprite },
		{ "draw_sprite_ext", draw_sprite_ext },
		{ "draw_sprite_stretched", draw_sprite_stretched },
		{ "draw_sprite_stretched_ext", draw_sprite_stretched_ext },
		{ "draw_sprite_part", draw_sprite_part },
		{ "draw_sprite_part_ext", draw_sprite_part_ext },
		{ "draw_sprite_tiled_ext", draw_sprite_tiled_ext },
		#endregion

		#region File
		{ "file_text_open_read", file_text_open_read },
		{ "file_text_open_write", file_text_open_write },
		{ "file_text_close", file_text_close },
		{ "file_text_read_string", file_text_read_string },
		{ "file_text_read_real", file_text_read_real },
		{ "file_text_readln", file_text_readln },
		{ "file_text_eof", file_text_eof },
		{ "file_text_write_string", file_text_write_string },
		{ "file_text_write_real", file_text_write_real },
		{ "file_text_writeln", file_text_writeln },
		{ "file_exists", file_exists },
		{ "file_delete", file_delete },
		{ "file_copy", file_copy },
		{ "ini_open", ini_open },
		{ "ini_close", ini_close },
		{ "ini_read_string", ini_read_string },
		{ "ini_read_real", ini_read_real },
		{ "ini_write_string", ini_write_string },
		{ "ini_write_real", ini_write_real },
		{ "json_decode", json_decode },
		#endregion

		#region Resource
		{ "sprite_get_number", sprite_get_number },
		{ "font_add_sprite_ext", font_add_sprite_ext },
		{ "script_execute", script_execute },
		{ "asset_get_index", asset_get_index },
		#endregion

		#region Interaction
		{ "keyboard_check", keyboard_check },
		{ "keyboard_check_pressed", keyboard_check_pressed },
		#endregion

		#region 3D
		{ "gpu_set_fog", gpu_set_fog },
		{ "gpu_set_colorwriteenable", gpu_set_colourwriteenable},
		{ "gpu_set_colourwriteenable", gpu_set_colourwriteenable},
		{ "gpu_set_alphatestenable", gpu_set_alphatestenable},
		{ "gpu_set_alphatestref", gpu_set_alphatestref},
		#endregion

		#region Misc
		{ "event_inherited", event_inherited },
		{ "event_user", event_user },
		{ "show_debug_message", show_debug_message },
		{ "variable_global_exists", variable_global_exists },
		{ "variable_instance_exists", variable_instance_exists},
		{ "variable_instance_get", variable_instance_get},
		{ "variable_instance_set", variable_instance_set},
		#endregion

		#region DS
		{ "ds_list_create", ds_list_create },
		{ "ds_list_destroy", ds_list_destroy },
		{ "ds_list_add", ds_list_add },
		{ "ds_map_create", ds_map_create },
		{ "ds_map_destroy", ds_map_destroy },
		// ds_map_clear
		// ds_map_copy
		{ "ds_map_size", ds_map_size },
		// ds_map_empty
		{ "ds_map_add", ds_map_add },
		// ds_map_set
		// ds_map_set_pre
		// ds_map_set_post
		// ds_map_add_list
		// ds_map_add_map
		// ds_map_replace
		// ds_map_replace_list
		// ds_map_replace_map
		// ds_map_delete
		{ "ds_map_exists", ds_map_exists},
		{ "ds_map_find_value", ds_map_find_value },
		#endregion

		#region Sound
		{ "audio_play_sound", audio_play_sound },
		{ "audio_stop_sound", audio_stop_sound },
		{ "audio_pause_sound", audio_pause_sound },
		{ "audio_resume_sound", audio_resume_sound },
		{ "audio_is_playing", audio_is_playing },
		{ "audio_sound_gain", audio_sound_gain },
		{ "audio_sound_pitch", audio_sound_pitch},
		{ "audio_stop_all", audio_stop_all },
		{ "audio_set_master_gain", audio_set_master_gain },
		{ "audio_sound_set_track_position", audio_sound_set_track_position },
		{ "audio_group_load", audio_group_load },
		{ "audio_group_is_loaded", audio_group_is_loaded },
		{ "audio_group_set_gain", audio_group_set_gain },
		{ "audio_create_stream", audio_create_stream },
		{ "audio_destroy_stream", audio_destroy_stream },
		#endregion

		#region Gamepad
		{ "gamepad_is_connected", gamepad_is_connected },
		{ "gamepad_button_check", gamepad_button_check },
		{ "gamepad_axis_value", gamepad_axis_value },
		#endregion

		#region Buffer

		{ "buffer_create", buffer_create },
		{ "buffer_delete", buffer_delete},
		//{ "buffer_write", buffer_write },
		{ "buffer_read", buffer_read },
		//{ "buffer_poke", buffer_poke },
		//{ "buffer_peek", buffer_peek },
		//{ "buffer_seek", buffer_seek },
		//{ "buffer_save", buffer_save },
		//{ "buffer_save_ext", buffer_save_ext },
		{ "buffer_load", buffer_load },
		//{ "buffer_load_ext", buffer_load_ext },
		//{ "buffer_load_partial", buffer_load_partial },
		//{ "buffer_save_async", buffer_save_async },
		//{ "buffer_load_async", buffer_load_async },
		//{ "buffer_async_group_begin", buffer_async_group_begin },
		//{ "buffer_async_group_end", buffer_async_group_end },
		//{ "buffer_async_group_option", buffer_async_group_option },
		//{ "buffer_copy", buffer_copy },
		//{ "buffer_exists", buffer_exists },
		//{ "buffer_get_type", buffer_get_type },
		//{ "buffer_get_alignment", buffer_get_alignment },
		//{ "buffer_fill", buffer_fill },
		//{ "buffer_resize", buffer_resize },
		//{ "buffer_md5", buffer_md5 },
		//{ "buffer_sha1", buffer_sha1 },
		//{ "buffer_base64_encode", buffer_base64_encode },
		//{ "buffer_base64_decode", buffer_base64_decode },
		//{ "buffer_base64_decode_ext", buffer_base64_decode_ext },
		//{ "buffer_sizeof", buffer_sizeof },
		//{ "buffer_get_address", buffer_get_address },
		//{ "buffer_get_surface", buffer_get_surface },
		//{ "buffer_set_surface", buffer_set_surface },
		//{ "buffer_create_from_vertex_buffer", buffer_create_from_vertex_buffer },
		//{ "buffer_create_from_vertex_buffer_ext", buffer_create_from_vertex_buffer_ext },
		//{ "buffer_copy_from_vertex_buffer", buffer_copy_from_vertex_buffer },
		//{ "buffer_compress", buffer_compress },
		//{ "buffer_decompress", buffer_decompress },

		#endregion

		#region YoYo
		{ "@@NewGMLArray@@", NewGMLArray },
		{ "@@NewGMLObject@@", NewGMLObject },
		{ "@@This@@", This },
		{ "@@Other@@", Other },
		{ "@@try_hook@@", try_hook },
		{ "@@try_unhook@@", try_unhook },
		#endregion

		#region Layer
		{ "layer_get_id", layer_get_id },
		{ "layer_get_id_at_depth", layer_get_id_at_depth},
		{ "layer_get_depth", layer_get_depth },
		{ "layer_create", layer_create },
		// layer_destroy
		// layer_destroy_instances
		// layer_add_instance
		// layer_has_instance
		{ "layer_set_visible", layer_set_visible},
		{ "layer_get_visible", layer_get_visible},
		// layer_exists
		{ "layer_x", layer_x },
		{ "layer_y", layer_y },
		{ "layer_get_x", layer_get_x },
		{ "layer_get_y", layer_get_y },
		{ "layer_hspeed", layer_hspeed },
		{ "layer_vspeed", layer_vspeed },
		{ "layer_get_hspeed", layer_get_hspeed },
		{ "layer_get_vspeed", layer_get_vspeed },
		// layer_script_begin
		// layer_script_end
		// layer_shader
		// layer_get_script_begin
		// layer_get_script_end
		// layer_get_shader
		// layer_set_target_room
		// layer_get_target_room
		// layer_reset_target_room
		{ "layer_get_all", layer_get_all },
		{ "layer_get_all_elements", layer_get_all_elements },
		{ "layer_get_name", layer_get_name },
		{ "layer_depth", layer_depth },
		// layer_get_element_layer
		{ "layer_get_element_type", layer_get_element_type },
		// layer_element_move
		{ "layer_force_draw_depth", layer_force_draw_depth },
		// layer_is_draw_depth_forced
		// layer_get_forced_depth
		// layer_background_get_id
		// layer_background_exists
		{ "layer_background_create", layer_background_create},
		// layer_background_destroy
		{ "layer_background_visible", layer_background_visible},
		{ "layer_background_htiled", layer_background_htiled},
		{ "layer_background_vtiled", layer_background_vtiled},
		{ "layer_background_xscale", layer_background_xscale},
		{ "layer_background_yscale", layer_background_yscale},
		{ "layer_background_stretch", layer_background_stretch},
		//{ "layer_background_blend", layer_background_blend},
		// layer_background_alpha
		// layer_background_index
		// layer_background_speed
		// layer_background_sprite
		// layer_background_change
		// layer_background_get_visible
		// layer_background_get_sprite
		// layer_background_get_htiled
		// layer_background_get_vtiled
		// layer_background_get_xscale
		// layer_background_get_yscale
		// layer_background_get_stretch
		// layer_background_get_blend
		// layer_background_get_alpha
		// layer_background_get_index
		// layer_background_get_speed

		{ "layer_tilemap_get_id", layer_tilemap_get_id },
		{ "layer_tile_alpha", layer_tile_alpha },
		#endregion

		#region Camera
		{ "camera_set_view_pos", camera_set_view_pos },
		{ "camera_set_view_target", camera_set_view_target },
		{ "camera_get_view_x", camera_get_view_x },
		{ "camera_get_view_y", camera_get_view_y },
		{ "camera_get_view_width", camera_get_view_width },
		{ "camera_get_view_height", camera_get_view_height },
		{ "camera_get_view_target", camera_get_view_target },
		{ "view_get_camera", view_get_camera },
		#endregion

		
		{ "object_get_sprite", object_get_sprite },

		{ "draw_arrow", draw_arrow },

		{ "make_color_hsv", make_color_hsv },
		{ "make_colour_hsv", make_color_hsv },

		{ "gpu_set_blendmode", gpu_set_blendmode},

		{ "draw_circle", draw_circle },
		{ "draw_triangle", draw_triangle },

		{ "texturegroup_get_textures", texturegroup_get_textures},
		{ "array_length", array_length},
		{ "texture_prefetch", texture_prefetch},
		{ "window_get_width", window_get_width},
		{ "window_get_height", window_get_height},
		{ "surface_get_width", surface_get_width},
		{ "surface_get_height", surface_get_height},
		{ "os_get_language", os_get_language},
		{ "surface_resize", surface_resize },
		{ "surface_exists", surface_exists },

		{ "gamepad_button_check_pressed", gamepad_button_check_pressed},
		{ "sprite_create_from_surface", sprite_create_from_surface},
		{ "sprite_set_offset", sprite_set_offset },
		{ "gamepad_get_device_count", gamepad_get_device_count},
		{ "draw_text_ext", draw_text_ext },

		{ "texture_flush", texture_flush},
		{ "room_get_name", room_get_name},


		{ "sprite_get_xoffset", sprite_get_xoffset },
		{ "sprite_get_yoffset", sprite_get_yoffset },

		{ "sprite_get_width", sprite_get_width},
		{ "sprite_get_height", sprite_get_height},

		{ "chr", chr},

		{ "date_current_datetime", date_current_datetime},
		{ "date_get_year", date_get_year},
		{ "date_get_month", date_get_month},
		{ "date_get_day", date_get_day},
		{ "date_get_weekday", date_get_weekday},
		{ "date_get_week", date_get_week},
		{ "date_get_hour", date_get_hour},
		{ "date_get_minute", date_get_minute},
		{ "date_get_second", date_get_second},

		{ "mouse_check_button_pressed", mouse_check_button_pressed},

		{ "draw_line_width_color", draw_line_width_color},
		{ "surface_create", surface_create },
		{ "surface_set_target", surface_set_target },
		{ "draw_clear_alpha", draw_clear_alpha },


		{ "draw_tilemap", draw_tilemap},
		{ "surface_reset_target", surface_reset_target },
		{ "surface_free", surface_free },
		{ "draw_rectangle_colour", draw_rectangle_colour },
		{ "draw_rectangle_color", draw_rectangle_colour },
		{ "draw_ellipse", draw_ellipse },

		{ "game_get_speed", game_get_speed},

		{ "audio_sound_get_track_position", audio_sound_get_track_position},
		{ "draw_surface", draw_surface},
		{ "gpu_set_blendmode_ext", gpu_set_blendmode_ext},
		{ "draw_triangle_color", draw_triangle_color},
		{ "path_add", path_add},
		{ "path_set_closed", path_set_closed},
		{ "path_set_precision", path_set_precision},
		{ "path_add_point", path_add_point},
		{ "draw_path", draw_path},
		{ "draw_sprite_pos", draw_sprite_pos },
		{ "ds_list_shuffle", ds_list_shuffle},
		{ "os_get_region",os_get_region},

		{ "ds_map_set", ds_map_set},
		{ "sprite_prefetch", sprite_prefetch},

		{ "steam_initialised", steam_initialised},
		{ "audio_channel_num", audio_channel_num},

		{ "object_get_name", object_get_name},
		{ "audio_sound_length", audio_sound_length},
		{ "draw_get_font", draw_get_font },

		{ "make_colour_rgb", make_color_rgb},
		{ "make_color_rgb", make_color_rgb},
		{ "draw_text_ext_transformed", draw_text_ext_transformed},
		{ "draw_surface_ext", draw_surface_ext},

		{ "sprite_exists", sprite_exists},
		{ "event_perform", event_perform},
		{ "gpu_set_blendenable", gpu_set_blendenable},

		{ "sqr", sqr},

		{ "action_move_to", action_move_to},
		{ "action_kill_object", action_kill_object},
		{ "instance_create", instance_create},
		{ "joystick_exists", joystick_exists},
		{ "keyboard_check_released", keyboard_check_released},
		{ "ini_section_exists", ini_section_exists},
		{ "keyboard_check_direct", keyboard_check_direct},

		{ "action_move", action_move},
		{ "action_set_alarm", action_set_alarm},
		{ "action_set_friction", action_set_friction},
		{ "sprite_delete", sprite_delete},

		{ "window_enable_borderless_fullscreen", window_enable_borderless_fullscreen},
		{ "parameter_count", parameter_count},
		{ "game_change", game_change},
		{ "parameter_string", parameter_string},
		{ "string_split", string_split},
		{ "ds_list_size", ds_list_size},
		{ "ds_list_find_value", ds_list_find_value},
		{ "array_push", array_push},
		{ "object_is_ancestor", object_is_ancestor},
		{ "tilemap_get_x", tilemap_get_x},
		{ "tilemap_x", tilemap_x},
		{ "tilemap_get_y", tilemap_get_y},
		{ "tilemap_y", tilemap_y},
		{ "audio_sound_get_pitch", audio_sound_get_pitch},
		{ "shader_set", shader_set},
		{ "sprite_get_texture", sprite_get_texture},
		{ "sprite_get_uvs", sprite_get_uvs},
		{ "texture_set_stage", texture_set_stage},
		{ "texture_get_texel_width", texture_get_texel_width},
		{ "texture_get_texel_height", texture_get_texel_height},
		{ "shader_set_uniform_f", shader_set_uniform_f},
		{ "surface_get_texture", surface_get_texture},
		{ "sprite_get_bbox_left", sprite_get_bbox_left},
		{ "sprite_get_bbox_top", sprite_get_bbox_top},
		{ "sprite_get_bbox_right", sprite_get_bbox_right},
		{ "sprite_get_bbox_bottom", sprite_get_bbox_bottom},
		{ "shader_get_uniform", shader_get_uniform},
		{ "shader_get_sampler_index", shader_get_sampler_index},
		{ "shader_reset", shader_reset},
		{ "draw_line", draw_line },
		{ "draw_healthbar", draw_healthbar},
		{ "path_set_kind", path_set_kind},
		{ "path_exists", path_exists},
		{ "audio_sound_get_gain", audio_sound_get_gain},
		{ "layer_sprite_get_sprite", layer_sprite_get_sprite},
		{ "layer_sprite_get_x", layer_sprite_get_x},
		{ "layer_sprite_get_y", layer_sprite_get_y},
		{ "layer_sprite_get_xscale", layer_sprite_get_xscale},
		{ "layer_sprite_get_yscale", layer_sprite_get_yscale},
		{ "layer_sprite_get_speed", layer_sprite_get_speed},
		{ "layer_sprite_get_index", layer_sprite_get_index},
		{ "layer_sprite_destroy", layer_sprite_destroy},
		{ "draw_clear", draw_clear},
		{ "method", method}, // seems to only be for global scripts (using global self instance) or struct constructors (using null self instance)
		{ "@@NullObject@@", NullObject},
		{ "draw_background", draw_background},
		{ "room_set_persistent", room_set_persistent},
		{ "path_get_x", path_get_x },
		{ "path_get_y", path_get_y },

		{ "matrix_get", matrix_get },
		{ "matrix_set", matrix_set },
		{ "matrix_build", matrix_build },
		{ "matrix_multiply", matrix_multiply },
		{ "matrix_build_identity", matrix_build_identity },
		{ "matrix_build_lookat", matrix_build_lookat },
		{ "matrix_build_projection_ortho", matrix_build_projection_ortho },
		{ "matrix_build_projection_perspective", matrix_build_projection_perspective },
		{ "matrix_build_projection_perspective_fov", matrix_build_projection_perspective_fov },
		{ "matrix_transform_vertex", matrix_transform_vertex },
	};

	public static object? room_set_persistent(object?[] args)
	{
		var roomIndex = args[0].Conv<int>();
		var persistent = args[1].Conv<bool>();

		var room = RoomManager.RoomList[roomIndex];
		room.Persistent = persistent;

		return null;
	}

	public static object? draw_background(object?[] args)
	{
		var index = args[0].Conv<int>();
		var x = args[1].Conv<double>();
		var y = args[2].Conv<double>();

		var background = GameLoader.Backgrounds[index];

		// TODO : handle tiling
		// TODO : handle strech
		// TODO : handle foreground
		// TODO : handle speed

		var sprite = background.Texture;

		if (sprite == null)
		{
			return null;
		}

		CustomWindow.Draw(new GMSpriteJob()
		{
			texture = sprite,
			screenPos = new Vector2d(x, y),
			blend = Color4.White
		});

		return null;
	}

	public static object? NullObject(object?[] args)
	{
		return null;
	}

	public static object? method(object?[] args)
	{
		// TODO: seems to always be self or null. need to resolve to instance (https://github.com/YoYoGames/GameMaker-HTML5/blob/develop/scripts/yyVariable.js#L279)
		var struct_ref_or_instance_id = args[0];
		var func = args[1].Conv<int>(); // BUG: this should be a script index, but it is sometimes a code index because push.i does that

		var method = new Method()
		{
			struct_ref_or_instance_id = struct_ref_or_instance_id,
			func = func
		};

		return method;
	}

	public static object? draw_clear(object?[] args)
	{
		var col = args[0].Conv<int>();
		var colour = col.ABGRToCol4(0);
		GL.ClearColor(colour);
		GL.Clear(ClearBufferMask.ColorBufferBit);
		GL.ClearColor(0, 0, 0, 0);
		return null;
	}

	public static object? audio_sound_get_gain(object?[] args)
	{
		var index = args[0].Conv<int>();

		if (index >= GMConstants.FIRST_INSTANCE_ID)
		{
			var instance = AudioManager.GetAudioInstance(index);

			if (instance == null)
			{
				return 0;
			}

			AL.GetSource(instance.SoundInstanceId, ALSourcef.Gain, out var gain);
			return gain;
		}
		else
		{
			var asset = AudioManager.GetAudioAsset(index);
			return asset.Gain;
		}
	}

	public static object? draw_line(object?[] args)
	{
		var x1 = args[0].Conv<double>();
		var y1 = args[1].Conv<double>();
		var x2 = args[2].Conv<double>();
		var y2 = args[3].Conv<double>();

		var col = SpriteManager.DrawColor.ABGRToCol4(SpriteManager.DrawAlpha);

		CustomWindow.Draw(new GMLineJob()
		{
			col1 = col,
			col2 = col,
			width = 1,
			x1 = (float)x1,
			y1 = (float)y1,
			x2 = (float)x2,
			y2 = (float)y2
		});

		return null;
	}

	// TODO : Implement these ughhhhhh

	public static object? shader_set(object?[] args)
	{
		var shaderId = args[0].Conv<int>();
		DebugLog.LogWarning("shader_set not implemented.");
		return null;
	}

	public static object? sprite_get_texture(object?[] args)
	{
		var spr = args[0].Conv<int>();
		var subimg = args[0].Conv<int>();
		DebugLog.LogWarning("sprite_get_texture not implemented.");
		return 0;
	}

	public static object? sprite_get_uvs(object?[] args)
	{
		var spr = args[0].Conv<int>();
		var subimg = args[0].Conv<int>();
		DebugLog.LogWarning("sprite_get_uvs not implemented.");
		return new int[8];
	}

	public static object? texture_set_stage(object?[] args)
	{
		DebugLog.LogWarning("texture_set_stage not implemented.");
		return null;
	}

	public static object? texture_get_texel_width(object?[] args)
	{
		DebugLog.LogWarning("texture_get_texel_width not implemented.");
		return 0;
	}

	public static object? texture_get_texel_height(object?[] args)
	{
		DebugLog.LogWarning("texture_get_texel_height not implemented.");
		return 0;
	}

	public static object? shader_set_uniform_f(object?[] args)
	{
		DebugLog.LogWarning("shader_set_uniform_f not implemented.");
		return null;
	}

	public static object? shader_get_uniform(object?[] args)
	{
		DebugLog.LogWarning("shader_get_uniform not implemented.");
		return null;
	}

	public static object? surface_get_texture(object?[] args)
	{
		DebugLog.LogWarning("surface_get_texture not implemented.");
		return -1;
	}

	public static object? shader_get_sampler_index(object?[] args)
	{
		DebugLog.LogWarning("shader_get_sampler_index not implemented.");
		return -1;
	}

	public static object? shader_reset(object?[] args)
	{
		DebugLog.LogWarning("shader_reset not implemented.");
		return null;
	}

	private static object? layer_force_draw_depth(object?[] args)
	{
		var force = args[0].Conv<bool>();
		var depth = args[1].Conv<int>();
		//Debug.Log($"layer_force_draw_depth force:{force} depth:{depth}");

		// not implementing yet because uhhhhhhhhhhhhhhhhhhh

		DebugLog.LogWarning("layer_force_draw_depth not implemented.");

		return null;
	}

	public static object? draw_set_colour(object?[] args)
	{
		var color = args[0].Conv<int>();
		SpriteManager.DrawColor = color;
		return null;
	}

	public static object draw_get_colour(object?[] args)
	{
		return SpriteManager.DrawColor;
	}

	public static object? draw_set_alpha(object?[] args)
	{
		var alpha = args[0].Conv<double>();
		SpriteManager.DrawAlpha = alpha;
		return null;
	}

	public static object? draw_get_alpha(object?[] args)
	{
		return SpriteManager.DrawAlpha;
	}

	public static object NewGMLArray(object?[] args)
	{
		return args.ToList(); // needs to be resizeable, e.g. initializing __objectID2Depth
	}

	public static object asset_get_index(object?[] args)
	{
		var name = args[0].Conv<string>();
		return AssetIndexManager.GetIndex(name);
	}

	public static object? event_inherited(object?[] args)
	{
		if (VMExecutor.Self.ObjectDefinition?.parent == null)
		{
			return null;
		}

		GamemakerObject.ExecuteEvent(VMExecutor.Self.GMSelf, VMExecutor.Self.ObjectDefinition.parent, VMExecutor.Self.EventType, VMExecutor.Self.EventIndex);
		return null;
	}

	private static IniFile? _iniFile;

	public static object? ini_open(object?[] args)
	{
		var name = args[0].Conv<string>();

		if (_iniFile != null)
		{
			// Docs say this throws an error.
			// C++ and HTML runners just save the old ini file and open the new one, with no error.
			// I love Gamemaker.

			ini_close(new object[0]);
		}

		var filepath = Path.Combine(Entry.DataWinFolder, name);

		if (!File.Exists(filepath))
		{
			_iniFile = new IniFile { Name = name };
			return null;
		}

		var lines = File.ReadAllLines(filepath);

		KeyValuePair<string, string> ParseKeyValue(string line)
		{
			var lineByEquals = line.Split('=');
			var key = lineByEquals[0].Trim();
			var value = lineByEquals[1].Trim();
			value = value.Trim('"');
			return new KeyValuePair<string, string>(key, value);
		}

		_iniFile = new IniFile { Name = name };
		IniSection? currentSection = null;

		for (var i = 0; i < lines.Length; i++)
		{
			var currentLine = lines[i];
			if (currentLine.StartsWith('[') && currentLine.EndsWith("]"))
			{
				currentSection = new IniSection(currentLine.TrimStart('[').TrimEnd(']'));
				_iniFile.Sections.Add(currentSection);
				continue;
			}

			if (string.IsNullOrEmpty(currentLine))
			{
				continue;
			}

			var keyvalue = ParseKeyValue(currentLine);
			currentSection?.Dict.Add(keyvalue.Key, keyvalue.Value);
		}

		return null;
	}

	public static object ini_read_string(object?[] args)
	{
		var section = args[0].Conv<string>();
		var key = args[1].Conv<string>();
		var value = args[2].Conv<string>();

		var sectionClass = _iniFile!.Sections.FirstOrDefault(x => x.Name == section);

		if (sectionClass == null)
		{
			return value;
		}

		if (!sectionClass.Dict.ContainsKey(key))
		{
			return value;
		}

		return sectionClass.Dict[key];
	}

	public static object? ini_write_string(object?[] args)
	{
		var section = args[0].Conv<string>();
		var key = args[1].Conv<string>();
		var value = args[2].Conv<string>();

		var sectionClass = _iniFile!.Sections.FirstOrDefault(x => x.Name == section);

		if (sectionClass == null)
		{
			sectionClass = new IniSection(section);
			_iniFile.Sections.Add(sectionClass);
		}

		sectionClass.Dict[key] = value;

		return null;
	}

	public static object? ini_read_real(object?[] args)
	{
		var section = args[0].Conv<string>();
		var key = args[1].Conv<string>();
		var value = args[2].Conv<double>();

		var sectionClass = _iniFile!.Sections.FirstOrDefault(x => x.Name == section);

		if (sectionClass == null)
		{
			return value;
		}

		if (!sectionClass.Dict.ContainsKey(key))
		{
			return value;
		}

		if (!double.TryParse(sectionClass.Dict[key], out var _res))
		{
			// TODO : check what it does here. maybe it only parses up to an invalid character?
			return value;
		}

		return _res;
	}

	public static object? ini_write_real(object?[] args)
	{
		var section = args[0].Conv<string>();
		var key = args[1].Conv<string>();
		var value = args[2].Conv<double>();

		var sectionClass = _iniFile!.Sections.FirstOrDefault(x => x.Name == section);

		if (sectionClass == null)
		{
			sectionClass = new IniSection(section);
			_iniFile.Sections.Add(sectionClass);
		}

		sectionClass.Dict[key] = value.ToString();

		return null;
	}

	public static object? ini_close(object?[] args)
	{
		var filepath = Path.Combine(Entry.DataWinFolder, _iniFile!.Name);
		File.Delete(filepath);
		var fileStream = new FileStream(filepath, FileMode.Append, FileAccess.Write);
		var streamWriter = new StreamWriter(fileStream);

		foreach (var section in _iniFile.Sections)
		{
			streamWriter.WriteLine($"[{section.Name}]");
			foreach (var kv in section.Dict)
			{
				streamWriter.WriteLine($"{kv.Key}=\"{kv.Value}\"");
			}
		}

		var text = streamWriter.ToString();

		streamWriter.Close();
		streamWriter.Dispose();
		_iniFile = null;

		return text; // BUG: this does NOT return the written text
	}

	public static object audio_group_is_loaded(object?[] args)
	{
		// TODO : actually implement this properly? DELTARUNITY doesnt use audio groups or any GM storage files (yet?)
		return true;
	}

	private static readonly Dictionary<int, FileHandle> _fileHandles = new(32);

	public static object? file_text_open_read(object?[] args)
	{
		var fname = args[0].Conv<string>();
		var filepath = Path.Combine(Entry.DataWinFolder, fname);

		DebugLog.Log($"file_text_open_read {filepath}");

		if (!File.Exists(filepath))
		{
			return -1;
		}

		var fileStream = new FileStream(filepath, FileMode.Open, FileAccess.Read);

		if (_fileHandles.Count == 32)
		{
			fileStream.Close();
			return -1;
		}

		var highestIndex = -1;
		if (_fileHandles.Count > 0)
		{
			highestIndex = _fileHandles.Keys.Max();
		}

		var handle = new FileHandle
		{
			Reader = new StreamReader(fileStream)
		};

		_fileHandles.Add(highestIndex + 1, handle);
		return highestIndex + 1;
	}

	public static object? file_text_open_write(object?[] args)
	{
		if (_fileHandles.Count == 32)
		{
			return -1;
		}

		var fname = args[0].Conv<string>();
		var filepath = Path.Combine(Entry.DataWinFolder, fname);

		File.Delete(filepath);
		var fileStream = new FileStream(filepath, FileMode.Create, FileAccess.Write);

		var highestIndex = -1;
		if (_fileHandles.Count > 0)
		{
			highestIndex = _fileHandles.Keys.Max();
		}

		var handle = new FileHandle
		{
			Writer = new StreamWriter(fileStream)
		};

		_fileHandles.Add(highestIndex + 1, handle);
		return highestIndex + 1;
	}

	public static object? file_text_close(object?[] args)
	{
		var index = args[0].Conv<int>();

		if (_fileHandles.ContainsKey(index))
		{
			if (_fileHandles[index].Reader != null)
			{
				_fileHandles[index].Reader!.Close();
				_fileHandles[index].Reader!.Dispose();
			}

			if (_fileHandles[index].Writer != null)
			{
				_fileHandles[index].Writer!.Close();
				_fileHandles[index].Writer!.Dispose();
			}

			_fileHandles.Remove(index);
		}

		return null;
	}

	public static object file_text_eof(object?[] args)
	{
		var fileid = args[0].Conv<int>();
		var reader = _fileHandles[fileid].Reader!;
		return reader.EndOfStream;
	}

	public static object file_exists(object?[] args)
	{
		var fname = args[0].Conv<string>();
		var filepath = Path.Combine(Entry.DataWinFolder, fname);
		return File.Exists(filepath);
	}

	public static object? file_text_readln(object?[] args)
	{
		var fileid = args[0].Conv<int>();
		var reader = _fileHandles[fileid].Reader!;
		return reader.ReadLine(); // BUG: returns null if eof
	}

	public static object? file_text_writeln(object?[] args)
	{
		var fileid = args[0].Conv<int>();
		var writer = _fileHandles[fileid].Writer!;
		writer.WriteLine();
		return null;
	}

	public static object file_text_read_string(object?[] args)
	{
		var fileid = args[0].Conv<int>();
		var reader = _fileHandles[fileid].Reader!;

		var result = "";
		while (reader.Peek() != 0x0D && reader.Peek() >= 0)
		{
			result += (char)reader.Read();
		}

		return result;
	}

	public static object? file_text_write_string(object?[] args)
	{
		var fileid = args[0].Conv<int>();
		var str = args[1].Conv<string>();
		var writer = _fileHandles[fileid].Writer!;
		writer.Write(str);
		return null;
	}

	public static object file_text_read_real(object?[] args)
	{
		var fileid = args[0].Conv<int>();
		var reader = _fileHandles[fileid].Reader!;

		var result = "";
		while (reader.Peek() != 0x0D && reader.Peek() >= 0)
		{
			result += (char)reader.Read();
		}

		return double.Parse(result);
	}

	public static object? file_text_write_real(object?[] args)
	{
		var fileid = args[0].Conv<int>();
		var val = args[1];
		var writer = _fileHandles[fileid].Writer!;

		if (val is not int and not double and not float and not long and not short)
		{
			DebugLog.LogError($"file_text_write_real got {val} ({val!.GetType()}) instead of a real!");
			// i have no fucking idea
			writer.Write(0);
			return null;
		}

		writer.Write(val.Conv<double>());
		return null;
	}

	public static object file_delete(object?[] args)
	{
		var fname = args[0].Conv<string>();
		var filepath = Path.Combine(Entry.DataWinFolder, fname);
		File.Delete(filepath);
		return true; // TODO : this should return false if this fails.
	}

	public static object? file_copy(object?[] args)
	{
		var fname = args[0].Conv<string>();
		var newname = args[1].Conv<string>();

		fname = Path.Combine(Entry.DataWinFolder, fname);
		newname = Path.Combine(Entry.DataWinFolder, newname);

		if (File.Exists(newname))
		{
			throw new Exception("File already exists.");
		}

		File.Copy(fname, newname);

		return null;
	}

	public static object variable_global_exists(object?[] args)
	{
		var name = args[0].Conv<string>();
		return VariableResolver.GlobalVariables.ContainsKey(name);
	}

	private static Dictionary<int, Dictionary<object, object>> _dsMapDict = new();

	public static object ds_map_create(params object?[] args)
	{
		var highestIndex = -1;
		if (_dsMapDict.Count > 0)
		{
			highestIndex = _dsMapDict.Keys.Max();
		}

		_dsMapDict.Add(highestIndex + 1, new());
		return highestIndex + 1;
	}

	public static object? ds_map_destroy(object?[] args)
	{
		var index = args[0].Conv<int>();
		_dsMapDict.Remove(index);
		return null;
	}

	public static object ds_map_add(params object?[] args)
	{
		var id = args[0].Conv<int>();
		var key = args[1]!;
		var value = args[2]!;

		if (!_dsMapDict.ContainsKey(id))
		{
			return false;
		}

		var dict = _dsMapDict[id];
		if (dict.ContainsKey(key))
		{
			return false;
		}

		dict.Add(key, value);
		return true;
	}

	public static object ds_map_size(object?[] args)
	{
		var id = args[0].Conv<int>();
		return _dsMapDict[id].Count;
	}

	private static Dictionary<int, List<object>> _dsListDict = new();

	public static object ds_list_create(params object?[] args)
	{
		var highestIndex = -1;
		if (_dsListDict.Count > 0)
		{
			highestIndex = _dsListDict.Keys.Max();
		}

		_dsListDict.Add(highestIndex + 1, new());
		return highestIndex + 1;
	}

	public static object? ds_list_destroy(object?[] args)
	{
		var index = args[0].Conv<int>();
		_dsListDict.Remove(index);
		return null;
	}

	public static object? ds_list_add(params object?[] args)
	{
		var id = args[0].Conv<int>();
		var values = args[1..];

		if (!_dsListDict.ContainsKey(id))
		{
			return null;
		}

		var list = _dsListDict[id];
		list.AddRange(values!);
		return null;
	}

	public static object? ds_list_shuffle(params object?[] args)
	{
		// TODO : make this use GMRandom

		var id = args[0].Conv<int>();

		if (!_dsListDict.ContainsKey(id))
		{
			return null;
		}

		var list = _dsListDict[id];

		list.Shuffle();

		return null;
	}

	public static object? ds_list_find_value(params object?[] args)
	{
		var id = args[0].Conv<int>();
		var pos = args[1].Conv<int>();

		/*
		 * TODO - this shit:
		 * Note that if you give a position that is outside of the given list size (i.e.: position 11 in a 10 value list)
		 * then the function may return undefined or 0.
		 * This is because when you create the list, internally the first few entries in the list
		 * are set to 0 to minimize performance issues when initially adding items to the list
		 * (although the ds_list_size() function will still return 0 on a newly created list).
		 */

		if (!_dsListDict.ContainsKey(id))
		{
			return null;
		}

		var list = _dsListDict[id];

		if (id >= list.Count)
		{
			return null;
		}

		return list[id];
	}

	public static object? ds_map_find_value(object?[] args)
	{
		var id = args[0].Conv<int>();
		var key = args[1]!;

		if (!_dsMapDict.ContainsKey(id))
		{
			return null;
		}

		var dict = _dsMapDict[id];
		if (!dict.ContainsKey(key))
		{
			return null;
		}

		return dict[key];
	}

	public static object? show_debug_message(object?[] args)
	{
		DebugLog.Log(args[0]?.ToString() ?? "undefined");
		return null;
	}

	public static object? json_decode(object?[] args)
	{
		// is recursive weeeeeeeeeeee
		static object Parse(JToken jToken)
		{
			switch (jToken)
			{
				case JValue jValue:
					return jValue.Value!;
				case JArray jArray:
				{
					var dsList = (int)ds_list_create();
					foreach (var item in jArray)
					{
						// TODO: make and call the proper function for maps and lists
						ds_list_add(dsList, Parse(item));
					}
					return dsList;
				}
				case JObject jObject:
				{
					var dsMap = (int)ds_map_create();
					foreach (var (name, value) in jObject)
					{
						// TODO: make and call the proper function for maps and lists
						ds_map_add(dsMap, name, Parse(value!));
					}
					return dsMap;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		var @string = args[0].Conv<string>();
		var jToken = JToken.Parse(@string);

		switch (jToken)
		{
			case JValue jValue:
			{
				var dsMap = (int)ds_map_create();
				ds_map_add(dsMap, "default", Parse(jValue));
				return dsMap;
			}
			case JArray jArray:
			{
				var dsMap = (int)ds_map_create();
				ds_map_add(dsMap, "default", Parse(jArray));
				return dsMap;
			}
			case JObject jObject:
			{
				return Parse(jObject);
			}
			default:
				throw new ArgumentOutOfRangeException();
		}
	}

	public static object font_add_sprite_ext(object?[] args)
	{
		var spriteAssetIndex = args[0].Conv<int>();
		var string_map = args[1].Conv<string>();
		var prop = args[2].Conv<bool>();
		var sep = args[3].Conv<int>();

		var spriteAsset = SpriteManager.GetSpriteAsset(spriteAssetIndex)!;

		var index = AssetIndexManager.Register(AssetType.fonts, $"fnt_{spriteAsset.Name}");

		var newFont = new FontAsset
		{
			AssetIndex = index,
			name = $"fnt_{spriteAsset.Name}",
			spriteIndex = spriteAssetIndex,
			sep = sep,
			Size = spriteAsset.Width
		};

		for (var i = 0; i < string_map.Length; i++)
		{
			var fontAssetEntry = new Glyph
			{
				characterIndex = string_map[i]
			};

			newFont.entries.Add(fontAssetEntry);
		}

		TextManager.FontAssets.Add(newFont);

		return newFont.AssetIndex;
	}

	public static object? draw_rectangle(params object?[] args)
	{
		var x1 = args[0].Conv<double>();
		var y1 = args[1].Conv<double>();
		var x2 = args[2].Conv<double>();
		var y2 = args[3].Conv<double>();
		var outline = args[4].Conv<bool>();

		x2 += 1;
		y2 += 1;

		CustomWindow.Draw(new GMPolygonJob()
		{
			blend = SpriteManager.DrawColor.ABGRToCol4(SpriteManager.DrawAlpha),
			Vertices = new Vector2d[]
			{
				new(x1, y1),
				new(x2, y1),
				new(x2, y2),
				new(x1, y2)
			},
			Outline = outline
		});
		return null;
	}

	public static object? draw_set_font(object?[] args)
	{
		var font = args[0].Conv<int>();

		var library = TextManager.FontAssets;
		var fontAsset = library.First(x => x.AssetIndex == font);
		TextManager.fontAsset = fontAsset;
		return null;
	}

	public static object? draw_text(object?[] args)
	{
		var x = args[0].Conv<double>();
		var y = args[1].Conv<double>();
		var str = args[2].Conv<string>();
		TextManager.DrawText(x, y, str);
		return null;
	}

	public static object merge_colour(object?[] args)
	{
		var col1 = args[0].Conv<int>();
		var col2 = args[1].Conv<int>();
		var amount = args[2].Conv<double>();

		/*
		 * GameMaker stores colors in 3 bytes - BGR
		 * RED		: 255		: 00 00 FF
		 * ORANGE	: 4235519	: 40 A0 FF
		 * Alpha (or "blend") is not stored in colors.
		 */

		var oneBytes = BitConverter.GetBytes(col1);
		var twoBytes = BitConverter.GetBytes(col2);
		amount = Math.Clamp(amount, 0, 1);
		var mr = oneBytes[0] + (twoBytes[0] - oneBytes[0]) * amount;
		var mg = oneBytes[1] + (twoBytes[1] - oneBytes[1]) * amount;
		var mb = oneBytes[2] + (twoBytes[2] - oneBytes[2]) * amount;

		return BitConverter.ToInt32(new[] { (byte)mr, (byte)mg, (byte)mb, (byte)0 }, 0);
	}

	public static object? draw_set_halign(object?[] args)
	{
		var halign = args[0].Conv<int>();
		TextManager.halign = (HAlign)halign;
		return null;
	}

	public static object? draw_set_valign(object?[] args)
	{
		var valign = args[0].Conv<int>();
		TextManager.valign = (VAlign)valign;
		return null;
	}

	public static object? draw_get_halign(object?[] args)
	{
		return (int)TextManager.halign;
	}

	public static object? draw_get_valign(object?[] args)
	{
		return (int)TextManager.valign;
	}

	public static object? draw_sprite(object?[] args)
	{
		var sprite = args[0].Conv<int>();
		var subimg = args[1].Conv<int>();
		var x = args[2].Conv<double>();
		var y = args[3].Conv<double>();

		SpriteManager.DrawSprite(sprite, subimg, x, y);
		return null;
	}

	public static object? window_set_caption(object?[] args)
	{
		var caption = args[0].Conv<string>();
		DebugLog.LogInfo($"window_set_caption : {caption}");
		CustomWindow.Instance.Title = caption;
		return null;
	}

	public static object? window_get_caption(object?[] args)
	{
		return CustomWindow.Instance.Title;
	}

	public static object audio_group_load(object?[] args)
	{
		// TODO : actually implement this properly? DELTARUNITY doesnt use audio groups or any GM storage files (yet?)
		return true;
	}

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

	public static object display_get_width(object?[] args)
	{
		return Monitors.GetPrimaryMonitor().HorizontalResolution;
	}

	public static object display_get_height(object?[] args)
	{
		return Monitors.GetPrimaryMonitor().VerticalResolution;
	}

	public static object? window_set_size(object?[] args)
	{
		var w = args[0].Conv<int>();
		var h = args[1].Conv<int>();

		DebugLog.Log($"window_set_size {w} {h}");

		CustomWindow.Instance.ClientSize = new Vector2i(w, h);

		return null;
	}

	public static object? window_center(object?[] args)
	{
		CustomWindow.Instance.CenterWindow();

		return null;
	}

	public static object window_get_fullscreen(object?[] args)
	{
		return CustomWindow.Instance.IsFullscreen;
	}

	public static object? window_set_fullscreen(object?[] args)
	{
		var full = args[0].Conv<bool>();
		CustomWindow.Instance.WindowState = full ? WindowState.Fullscreen : WindowState.Normal;
		// BUG: this fucks resolution
		return null;
	}

	public static object gamepad_button_check(object?[] args)
	{
		// TODO : implement?
		return false;
	}

	public static object gamepad_axis_value(object?[] args)
	{
		// TODO : implement?
		return 0;
	}

	public static object gamepad_is_connected(object?[] args)
	{
		var device = args[0].Conv<int>();
		return false; // TODO : implement
	}

	public static object? draw_sprite_ext(object?[] args)
	{
		var sprite = args[0].Conv<int>();
		var subimg = args[1].Conv<int>();
		var x = args[2].Conv<double>();
		var y = args[3].Conv<double>();
		var xscale = args[4].Conv<double>();
		var yscale = args[5].Conv<double>();
		var rot = args[6].Conv<double>();
		var colour = args[7].Conv<int>();
		var alpha = args[8].Conv<double>();

		SpriteManager.DrawSpriteExt(sprite, subimg, x, y, xscale, yscale, rot, colour, alpha);
		return null;
	}

	public static object? draw_text_transformed(object?[] args)
	{
		var x = args[0].Conv<double>();
		var y = args[1].Conv<double>();
		var str = args[2].Conv<string>();
		var xscale = args[3].Conv<double>();
		var yscale = args[4].Conv<double>();
		var angle = args[5].Conv<double>();
		TextManager.DrawTextTransformed(x, y, str, xscale, yscale, angle);
		return null;
	}

	public static object? draw_sprite_part_ext(object?[] args)
	{
		var sprite = args[0].Conv<int>();
		var subimg = args[1].Conv<int>();
		var left = args[2].Conv<int>();
		var top = args[3].Conv<int>();
		var width = args[4].Conv<int>();
		var height = args[5].Conv<int>();
		var x = args[6].Conv<double>();
		var y = args[7].Conv<double>();
		var xscale = args[8].Conv<double>();
		var yscale = args[9].Conv<double>();
		var colour = args[10].Conv<int>();
		var alpha = args[11].Conv<double>();

		SpriteManager.DrawSpritePartExt(sprite, subimg, left, top, width, height, x, y, xscale, yscale, colour, alpha);

		return null;
	}

	public static object? draw_sprite_part(object?[] args)
	{
		var sprite = args[0].Conv<int>();
		var subimg = args[1].Conv<int>();
		var left = args[2].Conv<int>();
		var top = args[3].Conv<int>();
		var width = args[4].Conv<int>();
		var height = args[5].Conv<int>();
		var x = args[6].Conv<double>();
		var y = args[7].Conv<double>();

		SpriteManager.DrawSpritePart(sprite, subimg, left, top, width, height, x, y);

		return null;
	}

	public static object? draw_self(object?[] args)
	{
		SpriteManager.DrawSelf(VMExecutor.Self.GMSelf);
		return null;
	}

	public static object? draw_sprite_stretched(object?[] args)
	{
		var sprite = args[0].Conv<int>();
		var subimg = args[1].Conv<int>();
		var x = args[2].Conv<double>();
		var y = args[3].Conv<double>();
		var w = args[4].Conv<double>();
		var h = args[5].Conv<double>();

		SpriteManager.draw_sprite_stretched(sprite, subimg, x, y, w, h, 0x00FFFFFF, 1);
		return null;
	}

	public static object? draw_sprite_stretched_ext(object?[] args)
	{
		var sprite = args[0].Conv<int>();
		var subimg = args[1].Conv<int>();
		var x = args[2].Conv<double>();
		var y = args[3].Conv<double>();
		var w = args[4].Conv<double>();
		var h = args[5].Conv<double>();
		var colour = args[6].Conv<int>();
		var alpha = args[7].Conv<double>();

		SpriteManager.draw_sprite_stretched(sprite, subimg, x, y, w, h, colour, alpha);
		return null;
	}

	public static object? draw_text_colour(object?[] args)
	{
		var x = args[0].Conv<double>();
		var y = args[1].Conv<double>();
		var str = args[2].Conv<string>();
		var c1 = args[3].Conv<int>();
		var c2 = args[4].Conv<int>();
		var c3 = args[5].Conv<int>();
		var c4 = args[6].Conv<int>();
		var alpha = args[7].Conv<double>();

		TextManager.DrawTextColor(x, y, str, c1, c2, c3, c4, alpha);
		
		return null;
	}

	public static object? draw_sprite_tiled_ext(object?[] args)
	{
		var sprite = args[0].Conv<int>();
		var subimg = args[1].Conv<int>();
		var x = args[2].Conv<double>();
		var y = args[3].Conv<double>();
		var xscale = args[4].Conv<double>();
		var yscale = args[5].Conv<double>();
		var colour = args[6].Conv<int>();
		var alpha = args[7].Conv<double>();

		var spriteTex = SpriteManager.GetSpritePage(sprite, subimg);

		var sizeWidth = spriteTex.TargetSizeX;
		var sizeHeight = spriteTex.TargetSizeY;

		var tempX = x;
		var tempY = y;

		var viewTopLeftX = CustomWindow.Instance.X;
		var viewTopLeftY = CustomWindow.Instance.Y;

		while (tempX > viewTopLeftX)
		{
			tempX -= sizeWidth;
		}

		while (tempY > viewTopLeftY)
		{
			tempY -= sizeHeight;
		}

		// tempX and tempY are now the topleft-most co-ords that are offscreen

		var xOffscreenValue = viewTopLeftX - tempX;
		var yOffscreenValue = viewTopLeftY - tempY;

		var countToDrawHoriz = CustomMath.CeilToInt((RoomManager.CurrentRoom.CameraWidth + (float)xOffscreenValue) / sizeWidth);
		var countToDrawVert = CustomMath.CeilToInt((RoomManager.CurrentRoom.CameraHeight + (float)yOffscreenValue) / sizeHeight);

		for (var i = 0; i < countToDrawVert; i++)
		{
			for (var j = 0; j < countToDrawHoriz; j++)
			{
				SpriteManager.DrawSpriteExt(sprite, subimg, tempX + (j * sizeWidth), tempY + (i * sizeHeight), xscale, yscale, 0, colour, alpha);
			}
		}

		return null;
	}

	public static object? audio_group_set_gain(object?[] args)
	{
		// TODO : actually implement this properly? DELTARUNITY doesnt use audio groups or any GM storage files (yet?)
		return null;
	}

	public static object? audio_play_sound(object?[] args)
	{
		var index = args[0].Conv<int>();
		var priority = args[1].Conv<double>();
		var loop = args[2].Conv<bool>();
		var asset = AudioManager.GetAudioAsset(index);
		var gain = asset.Gain;
		var offset = asset.Offset;
		var pitch = asset.Pitch;
		var listener_mask = 0; // TODO : work out what the hell this is for
		if (args.Length > 3)
		{
			gain = args[3].Conv<double>();
		}

		if (args.Length > 4)
		{
			offset = args[4].Conv<double>();
		}

		if (args.Length > 5)
		{
			pitch = args[5].Conv<double>();
		}

		if (args.Length > 6)
		{
			listener_mask = args[6].Conv<int>();
		}

		var ret = AudioManager.audio_play_sound(index, priority, loop, gain, offset, pitch);
		return ret;
	}

	public static object? audio_set_master_gain(object?[] args)
	{
		var listenerIndex = args[0].Conv<double>(); // deltarune doesnt use other listeners rn so i dont care
		var gain = args[1].Conv<double>();
		AL.Listener(ALListenerf.Gain, (float)gain);
		return null;
	}

	public static object view_get_camera(object?[] args)
	{
		// TODO : ughhh implement multiple cameras
		return 0;
	}

	public static object camera_get_view_x(object?[] args)
	{
		var camera_id = args[0].Conv<int>();

		if (camera_id > 0)
		{
			// TODO : ughhh implement multiple cameras
			throw new NotImplementedException();
		}

		return CustomWindow.Instance.X;
	}

	public static object camera_get_view_y(object?[] args)
	{
		var camera_id = args[0].Conv<int>();

		if (camera_id > 0)
		{
			// TODO : ughhh implement multiple cameras
			throw new NotImplementedException();
		}

		return CustomWindow.Instance.Y;
	}

	public static object camera_get_view_width(object?[] args)
	{
		var camera_id = args[0].Conv<int>();

		if (camera_id > 0)
		{
			// TODO : ughhh implement multiple cameras
			throw new NotImplementedException();
		}

		return RoomManager.CurrentRoom.CameraWidth;
	}

	public static object camera_get_view_height(object?[] args)
	{
		var camera_id = args[0].Conv<int>();

		if (camera_id > 0)
		{
			// TODO : ughhh implement multiple cameras
			throw new NotImplementedException();
		}

		return RoomManager.CurrentRoom.CameraHeight;
	}

	public static object? camera_set_view_target(object?[] args)
	{
		var camera_id = args[0].Conv<int>();

		if (camera_id > 0)
		{
			// TODO : ughhh implement multiple cameras
			throw new NotImplementedException();
		}

		var id = args[1].Conv<int>();

		GamemakerObject? instance = null;

		if (id == GMConstants.noone)
		{
			// set view target to no one i guess
		}
		else if (id < GMConstants.FIRST_INSTANCE_ID)
		{
			instance = InstanceManager.FindByAssetId(id).FirstOrDefault();
		}
		else
		{
			instance = InstanceManager.FindByInstanceId(id);
		}

		CustomWindow.Instance.FollowInstance = instance;

		return null;
	}

	public static object? camera_get_view_target(object?[] args)
	{
		var camera_id = args[0].Conv<int>();

		if (camera_id > 0)
		{
			// TODO : ughhh implement multiple cameras
			throw new NotImplementedException();
		}

		// TODO : this can apparently return either an instance id or object index????
		return CustomWindow.Instance.FollowInstance == null ? -1 : CustomWindow.Instance.FollowInstance.instanceId;
	}

	public static object? camera_set_view_pos(object?[] args)
	{
		var camera_id = args[0].Conv<int>();

		if (camera_id > 0)
		{
			// TODO : ughhh implement multiple cameras
			throw new NotImplementedException();
		}

		var x = args[1].Conv<double>();
		var y = args[2].Conv<double>();

		CustomWindow.Instance.SetPosition(x, y);

		return null;
	}

	public static object? audio_create_stream(object?[] args)
	{
		var filename = args[0].Conv<string>();
		filename = Path.Combine(Entry.DataWinFolder, filename);

		var assetName = Path.GetFileNameWithoutExtension(filename);
		var existingIndex = AssetIndexManager.GetIndex(AssetType.sounds, assetName);
		if (existingIndex != -1)
		{
			// happens in deltarune on battle.ogg
			DebugLog.LogWarning($"audio_create_stream on {filename} already registered with index {existingIndex}");
			return existingIndex;
		}

		// this should probably be put in AudioManager
		using var vorbis = Vorbis.FromMemory(File.ReadAllBytes(filename));
		var data = new float[vorbis.StbVorbis.total_samples * vorbis.Channels];
		unsafe
		{
			fixed (float* ptr = data)
			{
				var realLength = StbVorbis.stb_vorbis_get_samples_float_interleaved(vorbis.StbVorbis, vorbis.Channels, ptr, data.Length);
				realLength *= vorbis.Channels;
				if (realLength != data.Length)
				{
					DebugLog.LogWarning($"{filename} length {realLength} != {data.Length}");
				}
			}
		}
		var stereo = vorbis.Channels == 2;
		var freq = vorbis.SampleRate;

		var buffer = AL.GenBuffer();
		AudioManager.CheckALError();
		AL.BufferData(buffer, stereo ? ALFormat.StereoFloat32Ext : ALFormat.MonoFloat32Ext, data, freq);
		AudioManager.CheckALError();

		return AudioManager.RegisterAudioClip(new()
		{
			// RegisterAudioClip sets AssetIndex
			Name = assetName,
			Clip = buffer,
			Gain = 1,
			Pitch = 1,
			Offset = 0,
		});
	}

	public static object? audio_destroy_stream(object?[] args)
	{
		var index = args[0].Conv<int>();
		AudioManager.UnregisterAudio(index);
		return null;
	}

	public static object? audio_sound_gain(object?[] args)
	{
		var index = args[0].Conv<int>();
		var volume = args[1].Conv<double>();
		var time = args[2].Conv<double>();

		if (index < 0)
		{
			return null;
		}

		if (index >= GMConstants.FIRST_INSTANCE_ID)
		{
			// instance id
			var soundAsset = AudioManager.GetAudioInstance(index);
			if (soundAsset == null)
			{
				return null;
			}

			AudioManager.ChangeGain(soundAsset.Source, volume, time);
		}
		else
		{
			// sound asset index
			AudioManager.SetAssetGain(index, volume);

			foreach (var item in AudioManager.GetAudioInstances(index))
			{
				AudioManager.ChangeGain(item.Source, volume, time);
			}
		}

		return null;
	}

	public static object? audio_sound_pitch(object?[] args)
	{
		var index = args[0].Conv<int>();
		var pitch = args[1].Conv<double>();

		pitch = Math.Clamp(pitch, 1.0 / 256.0, 256.0);

		if (index >= GMConstants.FIRST_INSTANCE_ID)
		{
			// instance id
			var soundAsset = AudioManager.GetAudioInstance(index);
			if (soundAsset == null)
			{
				return null;
			}
			
			AL.Source(soundAsset.Source, ALSourcef.Pitch, (float)pitch);
			AudioManager.CheckALError();
		}
		else
		{
			// sound asset index
			AudioManager.SetAssetPitch(index, pitch);

			foreach (var item in AudioManager.GetAudioInstances(index))
			{
				AL.Source(item.Source, ALSourcef.Pitch, (float)pitch);
				AudioManager.CheckALError();
			}
		}

		return null;
	}

	public static object? audio_stop_all(object?[] args)
	{
		AudioManager.StopAllAudio();
		return null;
	}

	public static object? audio_stop_sound(object?[] args)
	{
		var id = args[0].Conv<int>();

		if (id < GMConstants.FIRST_INSTANCE_ID)
		{
			foreach (var item in AudioManager.GetAudioInstances(id))
			{
				AL.SourceStop(item.Source);
				AudioManager.CheckALError();
			}
		}
		else
		{
			var soundAsset = AudioManager.GetAudioInstance(id);
			if (soundAsset == null)
			{
				//DebugLog.LogWarning($"trying to stop sound {id} which does not exist.\n" +
				//	$"it was probably either done playing or already stopped");
				return null;
			}
			AL.SourceStop(soundAsset.Source);
			AudioManager.CheckALError();
		}
		AudioManager.Update(); // hack: deletes the sources. maybe make official stop and delete function
		
		return null;
	}

	public static object? audio_pause_sound(object?[] args)
	{
		var index = args[0].Conv<int>();

		if (index < GMConstants.FIRST_INSTANCE_ID)
		{
			foreach (var item in AudioManager.GetAudioInstances(index))
			{
				AL.SourcePause(item.Source);
				AudioManager.CheckALError();
			}
		}
		else
		{
			var instance = AudioManager.GetAudioInstance(index);
			if (instance != null)
			{
				AL.SourcePause(instance.Source);
				AudioManager.CheckALError();
			}
				
		}
		
		return null;
	}

	public static object? audio_resume_sound(object?[] args)
	{
		var index = args[0].Conv<int>();

		if (index < GMConstants.FIRST_INSTANCE_ID)
		{
			foreach (var item in AudioManager.GetAudioInstances(index))
			{
				AL.SourcePlay(item.Source);
				AudioManager.CheckALError();
			}
		}
		else
		{
			var instance = AudioManager.GetAudioInstance(index);
			if (instance != null)
			{
				AL.SourcePlay(instance.Source);
				AudioManager.CheckALError();
			}
		}

		return null;
	}

	public static object? audio_sound_set_track_position(object?[] args)
	{
		var index = args[0].Conv<int>();
		var time = args[1].Conv<double>();

		if (index < GMConstants.FIRST_INSTANCE_ID)
		{
			AudioManager.SetAssetOffset(index, time);
			
			// unlike gain and pitch, this doesnt change currently playing instances
		}
		else
		{
			var instance = AudioManager.GetAudioInstance(index);
			if (instance != null)
			{
				AL.Source(instance.Source, ALSourcef.SecOffset, (float)time);
				AudioManager.CheckALError();
			}
		}
		
		return null;
	}

	public static object? audio_is_playing(object?[] args)
	{
		var index = args[0].Conv<int>();

		if (index < GMConstants.FIRST_INSTANCE_ID)
		{
			// playing = exists for us, so anything in here means something is playing
			foreach (var item in AudioManager.GetAudioInstances(index))
			{
				if (item != null)
				{
					return true;
				}
			}

			return false;
		}
		else
		{
			var instance = AudioManager.GetAudioInstance(index);
			return instance != null;
		}
	}

	public static object string_width(object?[] args)
	{
		var str = args[0].Conv<string>();

		return TextManager.StringWidth(str);
	}

	public static object string_height(object?[] args)
	{
		var str = args[0].Conv<string>();

		if (TextManager.fontAsset == null)
		{
			return 1;
		}

		var lines = TextManager.SplitText(str, -1, TextManager.fontAsset);
		var textHeight = TextManager.TextHeight(str);
		if (lines == null)
		{
			return textHeight;
		}
		else
		{
			return textHeight * lines.Count;
		}
	}

	public static object? event_user(object?[] args)
	{
		var numb = args[0].Conv<int>();
		GamemakerObject.ExecuteEvent(VMExecutor.Self.GMSelf, VMExecutor.Self.ObjectDefinition, EventType.Other, (int)EventSubtypeOther.User0 + numb);
		return null;
	}

	public static object? script_execute(object?[] args)
	{
		var scriptAssetId = args[0].Conv<int>(); // BUG: this should be a script index, but it is sometimes a code index because push.i does that
		var scriptArgs = args[1..];

		var script = Scripts.FirstOrDefault(x => x.Value.AssetIndex == scriptAssetId).Value;

		if (script == default)
		{
			// BUG: wrong. should be script id, which is done above, idk if this is used
			(var code, var index) = ScriptFunctions[ScriptFunctions.Keys.ToList()[scriptAssetId]];

			return VMExecutor.ExecuteCode(code, VMExecutor.Self.GMSelf, VMExecutor.Self.ObjectDefinition, args: scriptArgs, startingIndex: index);
		}

		return VMExecutor.ExecuteCode(script.GetCode(), VMExecutor.Self.GMSelf, VMExecutor.Self.ObjectDefinition, args: scriptArgs);
	}

	public static object? draw_line_width(object?[] args)
	{
		var x1 = args[0].Conv<double>();
		var y1 = args[1].Conv<double>();
		var x2 = args[2].Conv<double>();
		var y2 = args[3].Conv<double>();
		var w = args[4].Conv<int>();
			
		CustomWindow.Draw(new GMLineJob()
		{
			col1 = SpriteManager.DrawColor.ABGRToCol4(SpriteManager.DrawAlpha),
			col2 = SpriteManager.DrawColor.ABGRToCol4(SpriteManager.DrawAlpha),
			x1 = (float)x1,
			y1 = (float)y1,
			x2 = (float)x2,
			y2 = (float)y2,
			width = w
		});

		return null;
	}

	public static object? gpu_set_fog(object?[] args)
	{
		var enable = args[0].Conv<bool>();
		var colour = args[1].Conv<int>();
		var start = args[2].Conv<double>();
		var end = args[3].Conv<double>();

		if ((start != 0 && start != 1) || (end != 0 && end != 1))
		{
			throw new NotImplementedException("actual fog");
		}

		SpriteManager.FogEnabled = enable;
		SpriteManager.FogColor = colour;

		return null;
	}

	public static object sprite_get_number(object?[] args)
	{
		var index = args[0].Conv<int>();
		return SpriteManager.GetNumberOfFrames(index);
	}

	

	public static object object_get_sprite(object?[] args)
	{
		var obj = args[0].Conv<int>();
		return InstanceManager.ObjectDefinitions[obj].sprite;
	}

	public static object layer_get_all(object?[] args)
	{
		return RoomManager.CurrentRoom.Layers.Values.Select(x => x.ID).ToList();
	}

	public static object layer_get_name(object?[] args)
	{
		var layer_id = args[0].Conv<int>();
		return RoomManager.CurrentRoom.Layers[layer_id].Name;
	}

	private static object layer_get_depth(object?[] args)
	{
		var layer_id = args[0];
		var layer = RoomManager.CurrentRoom.GetLayer(layer_id);
		return layer.Depth;
	}

	private static object? layer_x(object?[] args)
	{
		var layer_id = args[0];
		var x = args[1].Conv<double>();

		var layer = RoomManager.CurrentRoom.GetLayer(layer_id);
		layer.X = (float)x;
		return null;
	}

	private static object? layer_y(object?[] args)
	{
		var layer_id = args[0];
		var y = args[1].Conv<double>();

		var layer = RoomManager.CurrentRoom.GetLayer(layer_id);
		layer.Y = (float)y;
		return null;
	}

	private static object layer_get_x(object?[] args)
	{
		var layer_id = args[0];

		var layer = RoomManager.CurrentRoom.GetLayer(layer_id);
		return layer.X;
	}

	private static object layer_get_y(object?[] args)
	{
		var layer_id = args[0];

		var layer = RoomManager.CurrentRoom.GetLayer(layer_id);
		return layer.Y;
	}

	private static object? layer_hspeed(object?[] args)
	{
		var layer_id = args[0];
		var hspd = args[1].Conv<double>();

		var layer = RoomManager.CurrentRoom.GetLayer(layer_id);
		layer.HSpeed = (float)hspd;
		return null;
	}

	private static object? layer_vspeed(object?[] args)
	{
		var layer_id = args[0];
		var vspd = args[1].Conv<double>();

		var layer = RoomManager.CurrentRoom.GetLayer(layer_id);
		layer.VSpeed = (float)vspd;
		return null;
	}

	private static object layer_get_vspeed(object?[] args)
	{
		var layer_id = args[0];

		var layer = RoomManager.CurrentRoom.GetLayer(layer_id);
		return layer.VSpeed;
	}

	private static object layer_get_hspeed(object?[] args)
	{
		var layer_id = args[0];

		var layer = RoomManager.CurrentRoom.GetLayer(layer_id);
		return layer.HSpeed;
	}

	private static object? layer_depth(object?[] args)
	{
		var layer_id = args[0];
		var depth = args[1].Conv<int>();

		var layer = RoomManager.CurrentRoom.GetLayer(layer_id);
		layer.Depth = depth;
		return null;
	}

	private static object layer_get_all_elements(object?[] args)
	{
		var layer_id = args[0].Conv<int>();
		var layer = RoomManager.CurrentRoom.Layers[layer_id];
		return layer.ElementsToDraw.Select(x => x.instanceId).ToList();
	}

	private static object layer_get_element_type(object?[] args)
	{
		var element_id = args[0].Conv<int>();

		CLayerElementBase baseElement = null!;
		foreach (var layer in RoomManager.CurrentRoom.Layers)
		{
			foreach (var element in layer.Value.LayerAsset.Elements)
			{
				if (element.Id == element_id)
				{
					baseElement = element;
					break;
				}
			}

			if (baseElement != null)
			{
				break;
			}
		}

		if (baseElement == null)
		{
			return (int)ElementType.Undefined;
		}

		return (int)baseElement.Type;
	}

	private static object? layer_tile_alpha(object?[] args)
	{
		var __index = args[0].Conv<int>();
		var __alpha = args[1].Conv<double>();

		foreach (var layer in RoomManager.CurrentRoom.Layers.Values)
		{
			foreach (var element in layer.ElementsToDraw)
			{
				if (element is GMTile tile && tile.instanceId == __index)
				{
					var col = tile.Color.ABGRToCol4();
					col.A = (float)__alpha;
					tile.Color = col.Col4ToABGR();
				}
			}
		}

		return null;
	}

	private static object? layer_create(object?[] args)
	{
		var depth = args[0].Conv<int>();
		var name = "";
		if (args.Length > 1)
		{
			name = args[1].Conv<string>();
		}

		var newLayerId = RoomManager.CurrentRoom.Layers.Keys.Max() + 1;

		if (string.IsNullOrEmpty(name))
		{
			name = $"_layer_{Guid.NewGuid()}";
		}

		var layerContainer = new LayerContainer(new SerializedFiles.Layer() { LayerName = name, LayerDepth = depth, LayerID = newLayerId });

		RoomManager.CurrentRoom.Layers.Add(newLayerId, layerContainer);

		return newLayerId;
	}

	

	private static object? draw_arrow(object?[] args)
	{
		var x1 = args[0].Conv<double>();
		var y1 = args[1].Conv<double>();
		var x2 = args[2].Conv<double>();
		var y2 = args[3].Conv<double>();
		var size = args[4].Conv<double>();

		// todo : name all these variables better and refactor this

		var height = y2 - y1;
		var length = x2 - x1;

		var magnitude = Math.Sqrt((height * height) + (length * length));

		if (magnitude != 0)
		{
			// draw body of arrow

			var col = SpriteManager.DrawColor.ABGRToCol4(SpriteManager.DrawAlpha);

			CustomWindow.Draw(new GMLineJob()
			{
				col1 = col,
				col2 = col,
				width = 1,
				x1 = (float)x1,
				y1 = (float)y1,
				x2 = (float)x2,
				y2 = (float)y2
			});

			// draw head of arrow

			var headSize = magnitude;
			if (size <= magnitude)
			{
				headSize = size;
			}

			var headLength = (length * headSize) / magnitude;
			var headHeight = (height * headSize) / magnitude;

			var a = (x2 - headLength) - headHeight / 3;
			var b = (headLength / 3) + (y2 - headHeight);

			var c = (headHeight / 3) + (x2 - headLength);
			var d = (y2 - headHeight) - headLength / 3;

			CustomWindow.Draw(new GMPolygonJob()
			{
				blend = SpriteManager.DrawColor.ABGRToCol4(SpriteManager.DrawAlpha),
				Vertices = new[] { new Vector2d(x2, y2), new Vector2d(a, b), new Vector2d(c, d) }
			});
		}

		return null;
	}

	public static object? make_color_hsv(object?[] args)
	{
		var hue = args[0].Conv<double>();
		var sat = args[1].Conv<double>() / 255;
		var val = args[2].Conv<double>() / 255;

		var hueDegree = (hue / 255) * 360;

		if (hueDegree >= 360)
		{
			hueDegree -= 360;
		}

		var chroma = val * sat;

		var hPrime = hueDegree / 60;

		var x = chroma * (1 - Math.Abs((hPrime % 2) - 1));

		var r = 0.0;
		var g = 0.0;
		var b = 0.0;

		switch (hPrime)
		{
			case >= 0 and < 1:
				r = chroma;
				g = x;
				b = 0;
				break;
			case >= 1 and < 2:
				r = x;
				g = chroma;
				b = 0;
				break;
			case >= 2 and < 3:
				r = 0;
				g = chroma;
				b = x;
				break;
			case >= 3 and < 4:
				r = 0;
				g = x;
				b = chroma;
				break;
			case >= 4 and < 5:
				r = x;
				g = 0;
				b = chroma;
				break;
			case >= 5 and < 6:
				r = chroma;
				g = 0;
				b = x;
				break;
		}

		var m = val - chroma;
		r += m;
		g += m;
		b += m;

		var rByte = (byte)(r * 255);
		var gByte = (byte)(g * 255);
		var bByte = (byte)(b * 255);

		return (bByte << 16) + (gByte << 8) + rByte;
	}

	public static object? gpu_set_blendmode(object?[] args)
	{
		var mode = args[0].Conv<int>();

		switch (mode)
		{
			case 0:
				// bm_normal
				GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
				GL.BlendEquation(BlendEquationMode.FuncAdd);
				break;
			case 1:
				// bm_add
				GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
				GL.BlendEquation(BlendEquationMode.FuncAdd);
				break;
			case 2:
				// bm_subtract
				GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
				GL.BlendEquation(BlendEquationMode.FuncSubtract);
				break;
			case 3:
				// bm_reverse_subtract
				GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
				GL.BlendEquation(BlendEquationMode.FuncReverseSubtract);
				break;
			case 4:
				// bm_min
				GL.BlendFunc(BlendingFactor.One, BlendingFactor.One);
				GL.BlendEquation(BlendEquationMode.Min);
				break;
			case 5:
				// bm_max
				GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcColor);
				GL.BlendEquation(BlendEquationMode.FuncAdd);
				break;
		}

		return null;
	}

	public static object? gpu_set_blendmode_ext(object?[] args)
	{
		var src = args[0].Conv<int>();
		var dst = args[0].Conv<int>();

		BlendingFactor GetBlend(int arg)
		{
			switch (arg)
			{
				case 0:
					return BlendingFactor.Zero;
				case 1:
					return BlendingFactor.One;
				case 2:
					return BlendingFactor.SrcColor;
				case 3:
					return BlendingFactor.OneMinusSrcColor;
				case 4:
					return BlendingFactor.SrcAlpha;
				case 5:
					return BlendingFactor.OneMinusSrcAlpha;
				case 6:
					return BlendingFactor.DstAlpha;
				case 7:
					return BlendingFactor.OneMinusDstAlpha;
				case 8:
					return BlendingFactor.DstColor;
				case 9:
					return BlendingFactor.OneMinusDstColor;
				case 10:
					return BlendingFactor.SrcAlphaSaturate;
				default:
					throw new ArgumentException();
			}
		}

		GL.BlendFunc(GetBlend(src), GetBlend(dst));
		return null;
	}

	public static object? draw_circle(object?[] args)
	{
		var x = args[0].Conv<double>();
		var y = args[1].Conv<double>();
		var r = args[2].Conv<double>();
		var outline = args[3].Conv<bool>();

		var angle = 360 / DrawManager.CirclePrecision;

		var points = new Vector2d[DrawManager.CirclePrecision];
		for (var i = 0; i < DrawManager.CirclePrecision; i++)
		{
			points[i] = new Vector2d(x + (r * Math.Sin(angle * i * CustomMath.Deg2Rad)), y + (r * Math.Cos(angle * i * CustomMath.Deg2Rad)));
		}

		CustomWindow.Draw(new GMPolygonJob()
		{
			blend = SpriteManager.DrawColor.ABGRToCol4(SpriteManager.DrawAlpha),
			Vertices = points,
			Outline = outline
		});

		return null;
	}

	public static object? draw_triangle(object?[] args)
	{
		var x1 = args[0].Conv<double>();
		var y1 = args[1].Conv<double>();
		var x2 = args[2].Conv<double>();
		var y2 = args[3].Conv<double>();
		var x3 = args[4].Conv<double>();
		var y3 = args[5].Conv<double>();
		var outline = args[6].Conv<bool>();

		CustomWindow.Draw(new GMPolygonJob()
		{
			blend = SpriteManager.DrawColor.ABGRToCol4(SpriteManager.DrawAlpha),
			Vertices = new Vector2d[]
			{
				new Vector2d(x1, y1),
				new Vector2d(x2, y2),
				new Vector2d(x3, y3)
			},
			Outline = outline
		});

		return null;
	}

	

	

	public static object texturegroup_get_textures(object?[] args)
	{
		var tex_id = args[0].Conv<string>();

		if (!GameLoader.TexGroups.TryGetValue(tex_id, out var texGroup))
		{
			return Array.Empty<string>();
		}

		return texGroup.TexturePages;
	}

	public static object? texture_prefetch(object?[] args)
	{
		var tex_id = args[0].Conv<string>();
		// TODO : Implement? Or not?
		return null;
	}

	public static object? texture_flush(object?[] args)
	{
		var tex_id = args[0].Conv<string>();
		// TODO : Implement? Or not?
		return null;
	}

	public static object window_get_width(object?[] args)
	{
		return CustomWindow.Instance.ClientSize.X;
	}

	public static object window_get_height(object?[] args)
	{
		return CustomWindow.Instance.ClientSize.Y;
	}

	public static object surface_get_width(object?[] args)
	{
		var surface_id = args[0].Conv<int>();
		return SurfaceManager.GetSurfaceWidth(surface_id);
	}

	public static object surface_get_height(object?[] args)
	{
		var surface_id = args[0].Conv<int>();
		return SurfaceManager.GetSurfaceHeight(surface_id);
	}

	public static object os_get_language(object?[] args)
	{
		return "en"; // TODO : actually implement
	}

	public static object? surface_resize(object?[] args)
	{
		var surface_id = args[0].Conv<int>();
		var w = args[1].Conv<int>();
		var h = args[2].Conv<int>();

		if (w < 1 || h < 1 || w > 8192 || h > 8192)
		{
			throw new NotImplementedException("Invalid surface dimensions");
		}

		if (surface_id == SurfaceManager.application_surface)
		{
			SurfaceManager.NewApplicationSize = true;
			SurfaceManager.NewApplicationWidth = w;
			SurfaceManager.NewApplicationHeight = h;
			return null;
		}

		SurfaceManager.ResizeSurface(surface_id, w, h);
		return null;
	}

	

	

	public static object gamepad_button_check_pressed(object?[] args)
	{
		// TODO : implement
		return false;
	}

	public static object sprite_create_from_surface(object?[] args)
	{
		var index = args[0].Conv<int>();
		var x = args[1].Conv<double>(); // unused by html5. probably sourcepos but deltarune doesnt use it so idc to find out
		var y = args[2].Conv<double>();
		var w = args[3].Conv<int>();
		var h = args[4].Conv<int>();
		var removeback = args[5].Conv<bool>(); // TODO: implement
		var smooth = args[6].Conv<bool>();
		var xorig = args[7].Conv<int>();
		var yorig = args[8].Conv<int>();

		return SpriteManager.sprite_create_from_surface(index, x, y, w, h, removeback, smooth, xorig, yorig);
	}

	public static object? sprite_set_offset(object?[] args)
	{
		var ind = args[0].Conv<int>();
		var xoff = args[1].Conv<int>();
		var yoff = args[2].Conv<int>();

		var data = SpriteManager._spriteDict[ind];
		data.OriginX = xoff;
		data.OriginY = yoff;
		return null;
	}

	public static object gamepad_get_device_count(object?[] args)
	{
		// TODO : implement
		return 0;
	}

	public static object? draw_text_ext(object?[] args)
	{
		var x = args[0].Conv<double>();
		var y = args[1].Conv<double>();
		var str = args[2].Conv<string>();
		var sep = args[3].Conv<int>();
		var w = args[4].Conv<double>();

		CustomWindow.Draw(new GMTextJob()
		{
			blend = SpriteManager.DrawColor.ABGRToCol4(SpriteManager.DrawAlpha),
			angle = 0,
			asset = TextManager.fontAsset,
			halign = TextManager.halign,
			valign = TextManager.valign,
			sep = sep,
			text = str,
			screenPos = new Vector2d(x, y),
			scale = Vector2d.One
		});

		return null;
	}

	

	public static object room_get_name(object?[] args)
	{
		var index = args[0].Conv<int>();

		return RoomManager.RoomList[index].Name;
	}

	public static object buffer_create(object?[] args)
	{
		var size = args[0].Conv<int>();
		var type = (BufferType)args[1].Conv<int>();
		var alignment = args[2].Conv<int>();

		return BufferManager.CreateBuffer(size, type, alignment);
	}

	public static object buffer_load(object?[] args)
	{
		var filename = args[0].Conv<string>();
		return BufferManager.LoadBuffer(filename);
	}

	public static object buffer_read(object?[] args)
	{
		var bufferIndex = args[0].Conv<int>();
		var type = args[1].Conv<int>();
		return BufferManager.ReadBuffer(bufferIndex, (BufferDataType)type);
	}

	public static object? buffer_delete(object?[] args)
	{
		var bufferIndex = args[0].Conv<int>();

		var buffer = BufferManager.Buffers[bufferIndex];
		buffer.Data = null!; // why

		BufferManager.Buffers.Remove(bufferIndex);

		return null;
	}

	

	

	

	public static object sprite_get_xoffset(object?[] args)
	{
		var index = args[0].Conv<int>();
		var sprite = SpriteManager._spriteDict[index];
		return sprite.OriginX;
	}

	public static object sprite_get_yoffset(object?[] args)
	{
		var index = args[0].Conv<int>();
		var sprite = SpriteManager._spriteDict[index];
		return sprite.OriginY;
	}

	

	

	public static object sprite_get_width(object?[] args)
	{
		var index = args[0].Conv<int>();
		var sprite = SpriteManager._spriteDict[index];
		return sprite.Width;
	}

	public static object sprite_get_height(object?[] args)
	{
		var index = args[0].Conv<int>();
		var sprite = SpriteManager._spriteDict[index];
		return sprite.Height;
	}

	public static object? variable_instance_set(object?[] args)
	{
		var instanceId = args[0].Conv<int>();
		var name = args[1].Conv<string>();
		var value = args[2];

		var instance = InstanceManager.FindByInstanceId(instanceId)!;

		if (VariableResolver.BuiltInSelfVariables.TryGetValue(name, out var getset))
		{
			var (getter, setter) = getset;
			setter?.Invoke(instance, value);

			return null;
		}

		instance.SelfVariables[name] = value;
		return null;
	}

	public static object chr(object?[] args)
	{
		var val = args[0].Conv<int>();

		// TODO : "This character depends on the current drawing fonts character set code page and if no font is set, it will use the default code page for the machine."
		// what the fuck does this mean

		/*var currentFont = TextManager.fontAsset;

		if (currentFont.entriesDict.ContainsKey(val))
		{
			return Convert.ToChar(val).ToString();
		}
		else
		{
			throw new NotImplementedException();
		}*/

		return Convert.ToChar(val).ToString();
	}

	// basically copied from https://github.com/YoYoGames/GameMaker-HTML5/blob/965f410a6553dd8e2418006ebeda5a86bd55dba2/scripts/functions/Function_Date.js

	const double MILLISECONDS_IN_A_DAY = 86400000.0;
	const double DAYS_SINCE_1900 = 25569;
	private static readonly int[] monthlen = new[] { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

	private static int DayOfYear(Date d)
	{
		var day = 0;
		if (_useLocalTime)
		{
			var monthlens = GetMonthLengths(d.GetFullYear());
			for (var i = 0; i < d.GetMonth(); i++)
				day += monthlens[i];
			day += d.GetDate();
		}
		else
		{
			var monthlens = GetMonthLengths(d.GetUTCFullYear());
			for (var i = 0; i < d.GetUTCMonth(); i++)
				day += monthlens[i];
			day += d.GetUTCDate();
		}

		return day;
	}

	private static int[] GetMonthLengths(int year)
	{
		var monthLengths = monthlen.ToArray(); // copy array
		if (IsLeapYear(year))
		{
			monthLengths[1] = 29;
		}
		return monthLengths;
	}

	private static bool IsLeapYear(int year)
	{
		return year % 400 == 0 || (year % 100 != 0 && year % 4 == 0);
	}

	private static double FromGMDateTime(double dateTime) => dateTime < DAYS_SINCE_1900
			? dateTime * MILLISECONDS_IN_A_DAY
			: (dateTime - DAYS_SINCE_1900) * MILLISECONDS_IN_A_DAY;

	private static bool _useLocalTime = false;

	public static object date_current_datetime(object?[] args)
	{
		var dt = new Date();
		var mm = dt.GetMilliseconds();
		var t = dt.GetTime() - mm;
		return (t / MILLISECONDS_IN_A_DAY) + DAYS_SINCE_1900;
	}

	public static object date_get_year(object?[] args)
	{
		var time = args[0].Conv<double>();
		var d = new Date();
		d.SetTime(FromGMDateTime(time));

		return _useLocalTime ? d.GetFullYear() : d.GetUTCFullYear();
	}

	public static object date_get_month(object?[] args)
	{
		var time = args[0].Conv<double>();
		var d = new Date();
		d.SetTime(FromGMDateTime(time));

		return _useLocalTime ? d.GetMonth() + 1 : d.GetUTCMonth() + 1;
	}

	public static object date_get_day(object?[] args)
	{
		var time = args[0].Conv<double>();
		var d = new Date();
		d.SetTime(FromGMDateTime(time));

		return _useLocalTime ? d.GetDate() : d.GetUTCDate();
	}

	public static object date_get_weekday(object?[] args)
	{
		var time = args[0].Conv<double>();
		var d = new Date();
		d.SetTime(FromGMDateTime(time));

		return _useLocalTime ? d.GetDay() : d.GetUTCDay();
	}

	public static object date_get_week(object?[] args)
	{
		var time = args[0].Conv<double>();
		var d = new Date();
		d.SetTime(FromGMDateTime(time));

		var w = DayOfYear(d);
		return CustomMath.FloorToInt(w / 7.0);
	}

	public static object date_get_hour(object?[] args)
	{
		var time = args[0].Conv<double>();
		var d = new Date();
		d.SetTime(FromGMDateTime(time));

		return _useLocalTime ? d.GetHours() : d.GetUTCHours();
	}

	public static object date_get_minute(object?[] args)
	{
		var time = args[0].Conv<double>();
		var d = new Date();
		d.SetTime(FromGMDateTime(time));

		return _useLocalTime ? d.GetMinutes() : d.GetUTCMinutes();
	}

	public static object date_get_second(object?[] args)
	{
		var time = args[0].Conv<double>();
		var d = new Date();
		d.SetTime(FromGMDateTime(time));

		return _useLocalTime ? d.GetSeconds() : d.GetUTCSeconds();
	}

	public static object? NewGMLObject(object?[] args)
	{
		var constructorIndex = args[0].Conv<int>(); // BUG: this should be a script index, but it is sometimes a code index because push.i does that
		var values = args[1..];
		var obj = new GMLObject();

		var ret = VMExecutor.ExecuteCode(GameLoader.Codes[constructorIndex], obj, args: values);

		return obj;
	}

	public static object mouse_check_button_pressed(object?[] args)
	{
		var numb = args[0].Conv<int>();
		return KeyboardHandler.MousePressed[numb];
	}

	public static object? layer_set_visible(object?[] args)
	{
		var layer_value = args[0];
		LayerContainer layer;
		if (layer_value is string s)
		{
			layer = RoomManager.CurrentRoom.Layers.FirstOrDefault(x => x.Value.Name == s).Value;
			if (layer == null)
			{
				DebugLog.Log($"layer_set_visible() - could not find specified layer in current room");
				return null;
			}
		}
		else
		{
			layer = RoomManager.CurrentRoom.Layers[args[0].Conv<int>()];
		}

		var visible = args[1].Conv<bool>();

		layer.Visible = visible;
		return null;
	}

	public static object? layer_get_visible(object?[] args)
	{
		var layer_value = args[0];

		if (layer_value is string s)
		{
			return RoomManager.CurrentRoom.Layers.FirstOrDefault(x => x.Value.Name == s).Value.Visible;
		}
		else
		{
			return RoomManager.CurrentRoom.Layers[args[0].Conv<int>()].Visible;
		}
	}

	public static object? draw_line_width_color(object?[] args)
	{
		var x1 = args[0].Conv<double>();
		var y1 = args[1].Conv<double>();
		var x2 = args[2].Conv<double>();
		var y2 = args[3].Conv<double>();
		var width = args[4].Conv<double>();
		var col1 = args[5].Conv<int>();
		var col2 = args[6].Conv<int>();

		CustomWindow.Draw(new GMLineJob()
		{
			x1 = (float)x1,
			y1 = (float)y1,
			x2 = (float)x2,
			y2 = (float)y2,
			width = (float)width,
			col1 = col1.ABGRToCol4(SpriteManager.DrawAlpha),
			col2 = col2.ABGRToCol4(SpriteManager.DrawAlpha)
		});

		return null;
	}

	public static object surface_create(object?[] args)
	{
		var w = args[0].Conv<int>();
		var h = args[1].Conv<int>();

		var format = 0; // TODO : work out if this actually is surface_rgba8unorm

		if (args.Length == 3)
		{
			format = args[2].Conv<int>();
		}

		return SurfaceManager.CreateSurface(w, h, format);
	}

	public static object surface_set_target(object?[] args)
	{
		var id = args[0].Conv<int>();

		if (args.Length == 2)
		{
			throw new NotImplementedException("depth surface passed uh oh");
		}

		return SurfaceManager.surface_set_target(id);
	}

	public static object? draw_clear_alpha(object?[] args)
	{
		var col = args[0].Conv<int>();
		var alpha = args[1].Conv<double>();

		// this is wrong. it makes the start of cyber dw white. what
		// var realCol = col.ABGRToCol4();
		// realCol.A = (float)alpha;
		// GL.ClearColor(realCol);

		return null;
	}

	public static object layer_get_id(object?[] args)
	{
		var layer_name = args[0].Conv<string>();

		var layer = RoomManager.CurrentRoom.Layers.Values.FirstOrDefault(x => x.Name == layer_name);
		return layer == default ? -1 : layer.ID;
	}

	public static object layer_tilemap_get_id(object?[] args)
	{
		var layer_id = args[0].Conv<int>();

		if (!RoomManager.CurrentRoom.Layers.ContainsKey(layer_id))
		{
			DebugLog.Log($"layer_tilemap_get_id() - specified tilemap not found");
			return -1;
		}

		var layer = RoomManager.CurrentRoom.Layers[layer_id];

		var layerElements = layer.LayerAsset.Elements;
		var element = layerElements.FirstOrDefault(x => x is CLayerTilemapElement);
		if (element == default)
		{
			return -1;
		}
		else
		{
			return element.Id;
		}
	}

	public static object tilemap_get_x(object?[] args)
	{
		var tilemap_element_id = args[0].Conv<int>();

		foreach (var layer in RoomManager.CurrentRoom.Layers)
		{
			foreach (var element in layer.Value.ElementsToDraw)
			{
				if (element is GMTilesLayer tilemap && tilemap.Element.Id == tilemap_element_id)
				{
					return tilemap.Element.x;
				}
			}
		}

		return 0;
	}

	public static object? tilemap_x(object?[] args)
	{
		var tilemap_element_id = args[0].Conv<int>();
		var x = args[1].Conv<double>();

		foreach (var layer in RoomManager.CurrentRoom.Layers)
		{
			foreach (var element in layer.Value.ElementsToDraw)
			{
				if (element is GMTilesLayer tilemap && tilemap.Element.Id == tilemap_element_id)
				{
					tilemap.Element.x = x;
				}
			}
		}

		return null;
	}

	public static object tilemap_get_y(object?[] args)
	{
		var tilemap_element_id = args[0].Conv<int>();

		foreach (var layer in RoomManager.CurrentRoom.Layers)
		{
			foreach (var element in layer.Value.ElementsToDraw)
			{
				if (element is GMTilesLayer tilemap && tilemap.Element.Id == tilemap_element_id)
				{
					return tilemap.Element.x;
				}
			}
		}

		return 0;
	}

	public static object? tilemap_y(object?[] args)
	{
		var tilemap_element_id = args[0].Conv<int>();
		var y = args[1].Conv<double>();

		foreach (var layer in RoomManager.CurrentRoom.Layers)
		{
			foreach (var element in layer.Value.ElementsToDraw)
			{
				if (element is GMTilesLayer tilemap && tilemap.Element.Id == tilemap_element_id)
				{
					tilemap.Element.y = y;
				}
			}
		}

		return null;
	}

	public static object? draw_tilemap(object?[] args)
	{
		var element_id = args[0].Conv<int>();
		var x = args[1].Conv<double>();
		var y = args[1].Conv<double>();

		foreach (var layer in RoomManager.CurrentRoom.Layers.Values)
		{
			foreach (var element in layer.ElementsToDraw)
			{
				if (element is GMTilesLayer tilemap && tilemap.Element.Id == element_id)
				{
					var oldDepth = tilemap.depth;
					var oldX = tilemap.Element.x;
					var oldY = tilemap.Element.y;
					var wasVisible = tilemap.Element.Layer.Visible;

					// TODO - whats the point of setting depth? isn't that just for drawing ordering?
					tilemap.depth = VMExecutor.Self.GMSelf.depth;
					tilemap.Element.x = x;
					tilemap.Element.y = y;
					tilemap.Element.Layer.Visible = true;

					tilemap.Draw();

					tilemap.depth = oldDepth;
					tilemap.Element.x = oldX;
					tilemap.Element.y = oldY;
					tilemap.Element.Layer.Visible = wasVisible;
				}
			}
		}

		return null;
	}

	public static object surface_reset_target(object?[] args)
	{
		return SurfaceManager.surface_reset_target();
	}

	public static object? surface_free(object?[] args)
	{
		var surface = args[0].Conv<int>();
		SurfaceManager.FreeSurface(surface, false);
		return null;
	}

	public static object? draw_rectangle_colour(object?[] args)
	{
		var x1 = args[0].Conv<double>();
		var y1 = args[1].Conv<double>();
		var x2 = args[2].Conv<double>();
		var y2 = args[3].Conv<double>();
		var col1 = args[4].Conv<int>();
		var col2 = args[5].Conv<int>();
		var col3 = args[6].Conv<int>();
		var col4 = args[7].Conv<int>();
		var outline = args[8].Conv<bool>();

		x2 += 1;
		y2 += 1;

		CustomWindow.Draw(new GMPolygonJob()
		{
			Outline = outline,
			Vertices = new[]
			{
				new Vector2d(x1, y1),
				new Vector2d(x2, y1),
				new Vector2d(x2, y2),
				new Vector2d(x1, y2)
			},
			Colors = new []
			{
				col1.ABGRToCol4(SpriteManager.DrawAlpha),
				col2.ABGRToCol4(SpriteManager.DrawAlpha),
				col3.ABGRToCol4(SpriteManager.DrawAlpha),
				col4.ABGRToCol4(SpriteManager.DrawAlpha)
			}
		});

		return null;
	}



	public static object? draw_ellipse(object?[] args)
	{
		var x1 = args[0].Conv<double>();
		var y1 = args[1].Conv<double>();
		var x2 = args[2].Conv<double>();
		var y2 = args[3].Conv<double>();
		var outline = args[4].Conv<bool>();

		var midpointX = (x1 + x2) / 2;
		var midpointY = (y1 + y2) / 2;

		var xRadius = Math.Abs((x1 - x2) / 2);
		var yRadius = Math.Abs((y1 - y2) / 2);

		var angle = 360 / DrawManager.CirclePrecision;

		var points = new Vector2d[DrawManager.CirclePrecision];
		for (var i = 0; i < DrawManager.CirclePrecision; i++)
		{
			points[i] = new Vector2d(
				midpointX + (xRadius * Math.Sin(angle * i * CustomMath.Deg2Rad)),
				midpointY + (yRadius * Math.Cos(angle * i * CustomMath.Deg2Rad)));
		}

		CustomWindow.Draw(new GMPolygonJob()
		{
			blend = SpriteManager.DrawColor.ABGRToCol4(SpriteManager.DrawAlpha),
			Vertices = points,
			Outline = outline
		});

		return null;
	}

	public static object variable_instance_exists(object?[] args)
	{
		var instance_id = args[0].Conv<int>();
		var name = args[1].Conv<string>();

		var instance = InstanceManager.FindByInstanceId(instance_id);

		if (instance == null)
		{
			return false;
		}

		if (instance.SelfVariables.ContainsKey(name))
		{
			return true;
		}

		return false;
	}

	public static object game_get_speed(object?[] args)
	{
		var type = args[0].Conv<int>();

		if (type == 0)
		{
			// FPS
			return Entry.GameSpeed;
		}
		else
		{
			// microseconds per frame
			return CustomMath.FloorToInt(1000000.0 / Entry.GameSpeed);
		}
	}

	

	public static object audio_sound_get_track_position(object?[] args)
	{
		var index = args[0].Conv<int>();

		if (index < GMConstants.FIRST_INSTANCE_ID)
		{
			return AudioManager.GetAssetOffset(index);

			// unlike gain and pitch, this doesnt change currently playing instances
		}
		else
		{
			var instance = AudioManager.GetAudioInstance(index);
			if (instance != null)
			{
				var offset = AL.GetSource(instance!.Source, ALSourcef.SecOffset);
				AudioManager.CheckALError();
				return offset;
			}
			return 0;
		}
	}

	private static object This(object?[] args) => VMExecutor.Self.GMSelf.instanceId;

	private static object Other(object?[] args) => VMExecutor.Other.GMSelf.instanceId;

	// TODO what the fuck are these? apparently its JS stuff? see F_JSTryHook etc
	// these are to do with breakpoints. probably not important. - neb
	private static object? try_hook(object?[] args) => null;
	private static object? try_unhook(object?[] args) => null;

	private static object surface_exists(object?[] args)
	{
		var surface = args[0].Conv<int>();
		return SurfaceManager.surface_exists(surface);
	}

	private static object? draw_surface(object?[] args)
	{
		var id = args[0].Conv<int>();
		var x = args[1].Conv<double>();
		var y = args[2].Conv<double>();
		
		SurfaceManager.draw_surface(id, x, y);
		
		return null;
	}

	public static object? draw_triangle_color(object?[] args)
	{
		var x1 = args[0].Conv<double>();
		var y1 = args[1].Conv<double>();
		var x2 = args[2].Conv<double>();
		var y2 = args[3].Conv<double>();
		var x3 = args[4].Conv<double>();
		var y3 = args[5].Conv<double>();
		var col1 = args[6].Conv<int>();
		var col2 = args[7].Conv<int>();
		var col3 = args[8].Conv<int>();
		var outline = args[9].Conv<bool>();

		CustomWindow.Draw(new GMPolygonJob()
		{
			Outline =  outline,
			Vertices = new[]
			{
				new Vector2d(x1, y1),
				new Vector2d(x2, y2),
				new Vector2d(x3, y3)
			},
			Colors = new[] { 
				col1.ABGRToCol4(SpriteManager.DrawAlpha),
				col2.ABGRToCol4(SpriteManager.DrawAlpha),
				col3.ABGRToCol4(SpriteManager.DrawAlpha) }
		});

		return null;
	}

	public static object? draw_sprite_pos(object?[] args)
	{
		DebugLog.LogWarning("draw_sprite_pos not implemented.");
		return null;
	}

	public static object path_add(object?[] args)
	{
		return PathManager.PathAdd();
	}

	public static object? path_set_closed(object?[] args)
	{
		var id = args[0].Conv<int>();
		var value = args[1].Conv<bool>();

		PathManager.Paths[id].closed = value;
		PathManager.ComputeInternal(PathManager.Paths[id]);
		return null;
	}

	public static object? path_set_precision(object?[] args)
	{
		var id = args[0].Conv<int>();
		var value = args[1].Conv<int>();

		PathManager.Paths[id].precision = value;
		PathManager.ComputeInternal(PathManager.Paths[id]);
		return null;
	}

	public static object? path_add_point(object?[] args)
	{
		var id = args[0].Conv<int>();
		var x = args[1].Conv<float>();
		var y = args[2].Conv<float>();
		var speed = args[3].Conv<float>();

		var path = PathManager.Paths[id];
		PathManager.AddPoint(path, x, y, speed);

		return null;
	}

	public static object? draw_path(object?[] args)
	{
		var id = args[0].Conv<int>();
		var x = args[1].Conv<float>();
		var y = args[2].Conv<float>();
		var absolute = args[3].Conv<bool>();

		var path = PathManager.Paths[id];

		if (absolute)
		{
			PathManager.DrawPath(path, 0, 0, absolute);
		}
		else
		{
			PathManager.DrawPath(path, x, y, absolute);
		}
		
		return null;
	}

	public static object os_get_region(object?[] args)
	{
		// TODO : implement
		return "GB";
	}

	

	public static object? ds_map_set(object?[] args)
	{
		var id = args[0].Conv<int>();
		var key = args[1]!;
		var value = args[2]!;

		if (!_dsMapDict.ContainsKey(id))
		{
			return false;
		}

		var dict = _dsMapDict[id];
		dict[key] = value;

		return null;
	}

	public static object? sprite_prefetch(object?[] args)
	{
		// TODO : implement?
		return 0;
	}

	

	public static object? steam_initialised(object?[] args)
	{
		// todo : implement
		return false;
	}

	public static object? audio_channel_num(object?[] args)
	{
		var num = args[0].Conv<int>();

		AudioManager.StopAllAudio();
		AudioManager.AudioChannelNum = num;
		return null;
	}

	

	

	public static object object_get_name(object?[] args)
	{
		var obj = args[0].Conv<int>();
		return InstanceManager.ObjectDefinitions[obj].Name;
	}

	public static object audio_sound_length(object?[] args)
	{
		var index = args[0].Conv<int>();

		if (index < GMConstants.FIRST_INSTANCE_ID)
		{
			var asset = AudioManager.GetAudioAsset(index);
			return AudioManager.GetClipLength(asset);
		}
		else
		{
			var instance = AudioManager.GetAudioInstance(index);
			if (instance != null)
			{
				return AudioManager.GetClipLength(instance.Asset);
			}
			return -1;
		}
	}

	public static object draw_get_font(object?[] args)
	{
		if (TextManager.fontAsset == null)
		{
			return -1;
		}

		return TextManager.fontAsset.AssetIndex;
	}

	

	

	public static object make_color_rgb(object?[] args)
	{
		var r = args[0].Conv<int>();
		var g = args[1].Conv<int>();
		var b = args[2].Conv<int>();

		return r | g << 8 | b << 16;
	}

	public static object? draw_text_ext_transformed(object?[] args)
	{
		var x = args[0].Conv<double>();
		var y = args[1].Conv<double>();
		var str = args[2].Conv<string>();
		var sep = args[3].Conv<int>();
		var w = args[4].Conv<int>(); // TODO : implement
		var xscale = args[5].Conv<double>();
		var yscale = args[6].Conv<double>();
		var angle = args[7].Conv<double>();

		CustomWindow.Draw(new GMTextJob()
		{
			screenPos = new Vector2d(x, y),
			asset = TextManager.fontAsset,
			angle = angle,
			blend = SpriteManager.DrawColor.ABGRToCol4(SpriteManager.DrawAlpha),
			halign = TextManager.halign,
			valign = TextManager.valign,
			scale = new Vector2d(xscale, yscale),
			sep = sep,
			text = str
		});

		return null;
	}

	public static object? draw_surface_ext(object?[] args)
	{
		DebugLog.LogWarning("draw_surface_ext not implemented.");
		return null;
	}

	public static object sprite_exists(object?[] args)
	{
		var index = args[0].Conv<int>();
		return SpriteManager._spriteDict.ContainsKey(index);
	}

	public static object? event_perform(object?[] args)
	{
		var type = args[0].Conv<int>();
		var numb = args[0].Conv<int>();

		GamemakerObject.ExecuteEvent(VMExecutor.Self.GMSelf, VMExecutor.Self.ObjectDefinition, (EventType)type + 1, numb);
		return null;
	}

	public static object? gpu_set_blendenable(object?[] args)
	{
		var enable = args[0].Conv<bool>();

		// TODO : is this right?

		if (enable)
		{
			GL.Enable(EnableCap.Blend);
		}
		else
		{
			GL.Disable(EnableCap.Blend);
		}

		return null;
	}

	public static object sqr(object?[] args)
	{
		var val = args[0].Conv<double>();
		return val * val;
	}

	

	public static bool Action_Relative = false;

	public static object? action_move_to(object?[] args)
	{
		var x = args[0].Conv<double>();
		var y = args[1].Conv<double>();

		if (Action_Relative)
		{
			x += VMExecutor.Self.GMSelf.x;
			y += VMExecutor.Self.GMSelf.y;
		}

		VMExecutor.Self.GMSelf.x = x;
		VMExecutor.Self.GMSelf.y = y;
		return null;
	}

	public static object? action_kill_object(object?[] args)
	{
		return instance_destroy(args);
	}

	public static object? instance_create(object?[] args)
	{
		var x = args[0].Conv<double>();
		var y = args[1].Conv<double>();
		var obj = args[2].Conv<int>();

		return InstanceManager.instance_create(x, y, obj);
	}

	public static object? joystick_exists(object?[] args) => false; // TODO : implement

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

	public static object? ini_section_exists(object?[] args)
	{
		var section = args[0].Conv<string>();
		return _iniFile!.Sections.Any(x => x.Name == section);
	}

	public static object? keyboard_check_direct(object?[] args)
	{
		var key = args[0].Conv<int>();
		return KeyboardHandler.KeyboardCheckDirect(key);
	}

	

	public static object? action_move(object?[] args)
	{
		var dirString = args[0].Conv<string>();
		var speed = args[1].Conv<double>();

		/*
		 * This function is weird.
		 * dirString must be 9 characters long, and each character is a 1 or a 0.
		 * Each character represents a direction. If multiple directions are set, a random one is picked.
		 * Directions are as followed:
		 * 0 - 225		Down-Left
		 * 1 - 270		Down
		 * 2 - 315		Down-Right
		 * 3 - 180		Left
		 * 4 - 0		Stop
		 * 5 - 0		Right
		 * 6 - 135		Up-Left
		 * 7 - 90		Up
		 * 8 - 45		Up-Right
		 */

		if (dirString.Length != 9)
		{
			throw new InvalidOperationException("dirString must be 9 characters long");
		}

		if (Action_Relative)
		{
			speed = VMExecutor.Self.GMSelf.speed + speed;
		}

		VMExecutor.Self.GMSelf.speed = speed;

		int dir;
		do
		{
			dir = (int)GMRandom.YYRandom(9);
		} while (dirString[dir] != '1');

		switch (dir)
		{
			case 0:
				VMExecutor.Self.GMSelf.direction = 255;
				break;
			case 1:
				VMExecutor.Self.GMSelf.direction = 270;
				break;
			case 2:
				VMExecutor.Self.GMSelf.direction = 315;
				break;
			case 3:
				VMExecutor.Self.GMSelf.direction = 180;
				break;
			case 4:
				VMExecutor.Self.GMSelf.direction = 0;
				VMExecutor.Self.GMSelf.speed = 0;
				break;
			case 5:
				VMExecutor.Self.GMSelf.direction = 0;
				break;
			case 6:
				VMExecutor.Self.GMSelf.direction = 135;
				break;
			case 7:
				VMExecutor.Self.GMSelf.direction = 90;
				break;
			case 8:
				VMExecutor.Self.GMSelf.direction = 56;
				break;
		}

		return null;
	}

	public static object? action_set_alarm(object?[] args)
	{
		var value = args[0].Conv<int>();
		var index = args[1].Conv<int>();

		if (Action_Relative)
		{
			var curValue = VMExecutor.Self.GMSelf.alarm[index].Conv<int>();
			if (curValue > -1)
			{
				VMExecutor.Self.GMSelf.alarm[index] = curValue + value;
				return null;
			}
		}

		VMExecutor.Self.GMSelf.alarm[index] = value;
		return null;
	}

	public static object? action_set_friction(object?[] args)
	{
		var friction = args[0].Conv<double>();

		if (Action_Relative)
		{
			friction += VMExecutor.Self.GMSelf.friction;
		}

		VMExecutor.Self.GMSelf.friction = friction;
		return null;
	}

	public static object? layer_get_id_at_depth(object?[] args)
	{
		var depth = args[0].Conv<int>();

		var retList = new List<int>();

		foreach (var layer in RoomManager.CurrentRoom.Layers.Values)
		{
			if (layer.Depth == depth)
			{
				retList.Add(layer.ID);
			}
		}

		if (retList.Count == 0)
		{
			retList.Add(-1);
		}

		return retList;
	}

	public static object? gpu_set_colourwriteenable(object?[] args)
	{
		bool r;
		bool g;
		bool b;
		bool a;

		if (args.Length == 4)
		{
			r = args[0].Conv<bool>();
			g = args[1].Conv<bool>();
			b = args[2].Conv<bool>();
			a = args[3].Conv<bool>();
		}
		else
		{
			var array = args[0].Conv<IList>();
			r = array[0].Conv<bool>();
			g = array[1].Conv<bool>();
			b = array[2].Conv<bool>();
			a = array[3].Conv<bool>();
		}

		GL.ColorMask(r, g, b, a);
		return null;
	}

	private static float AlphaRef = 0;

	public static object? gpu_set_alphatestenable(object?[] args)
	{
		var enabled = args[0].Conv<bool>();

		if (enabled)
		{
			GL.AlphaFunc(AlphaFunction.Greater, AlphaRef);
		}
		else
		{
			GL.AlphaFunc(AlphaFunction.Always, AlphaRef);
		}

		return null;
	}

	public static object? gpu_set_alphatestref(object?[] args)
	{
		var alphaRef = args[0].Conv<int>();
		AlphaRef = alphaRef / 255f;
		return null;
	}

	public static object? variable_instance_get(object?[] args)
	{
		var instanceId = args[0].Conv<int>();
		var name = args[1].Conv<string>();

		var instance = InstanceManager.FindByInstanceId(instanceId);

		if (instance == null)
		{
			return null;
		}

		if (VariableResolver.BuiltInSelfVariables.ContainsKey(name))
		{
			var (getter, setter) = VariableResolver.BuiltInSelfVariables[name];
			return getter(instance);
		}

		return instance.SelfVariables.TryGetValue(name, out var value) ? value : null;
	}

	public static object? ds_map_exists(object?[] args)
	{
		var id = args[0].Conv<int>();
		var key = args[1].Conv<string>();

		if (!_dsMapDict.ContainsKey(id))
		{
			return false;
		}

		var dict = _dsMapDict[id];
		return dict.ContainsKey(key);
	}

	public static object? sprite_delete(object?[] args)
	{
		var index = args[0].Conv<int>();
		
		return SpriteManager.sprite_delete(index);
	}

	public static object? window_enable_borderless_fullscreen(object?[] args)
	{
		// todo : implement
		return null;
	}

	public static object? parameter_count(object?[] args)
	{
		return Entry.LaunchParameters.Length;
	}

	public static object? game_change(object?[] args)
	{
		var working_directory = args[0].Conv<string>();
		var launch_parameters = args[1].Conv<string>();

		var winLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "game" + working_directory, "data.win");
		DebugLog.LogInfo($"game_change path:{winLocation} launch_parameters:{launch_parameters}");

		Entry.LoadGame(winLocation, launch_parameters.Split(" "));

		return null;
	}

	public static object? parameter_string(object?[] args)
	{
		var n = args[0].Conv<int>();
		return Entry.LaunchParameters[n - 1];
	}

	public static object? string_split(object?[] args)
	{
		var str = args[0].Conv<string>();
		var delimiter = args[1].Conv<string>();

		var remove_empty = false;
		if (args.Length > 2)
		{
			remove_empty = args[2].Conv<bool>();
		}

		var max_splits = -1;
		if (args.Length > 3)
		{
			max_splits = args[3].Conv<int>();
		}

		var option = remove_empty ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None;

		var splits = str.Split(delimiter, option);

		if (max_splits != -1)
		{
			throw new NotImplementedException();
		}

		return splits;
	}

	public static object? ds_list_size(object?[] args)
	{
		var id = args[0].Conv<int>();

		if (!_dsListDict.ContainsKey(id))
		{
			DebugLog.LogError($"Data structure with index {id} does not exist.");
			return 0;
		}

		return _dsListDict[id].Count;
	}

	public static object instance_place(object?[] args)
	{
		var x = args[0].Conv<double>();
		var y = args[1].Conv<double>();
		var obj = args[2].Conv<int>(); // TODO : this can be an array, or "all" or "other", or tile map stuff

		return CollisionManager.Command_InstancePlace(VMExecutor.Self.GMSelf, x, y, obj);
	}

	public static object? array_push(object?[] args)
	{
		var variable = args[0].Conv<IList>();
		for (var i = 1; i < args.Length; i++)
		{
			// TODO : is variable actually a reference to the stored array, or is it just a value copy?
			variable.Add(args[i]);
		}

		return null;
	}

	public static object object_is_ancestor(object?[] args)
	{
		var obj = args[0].Conv<int>();
		var par = args[1].Conv<int>();

		var objDef = InstanceManager.ObjectDefinitions[obj];
		var parDef = InstanceManager.ObjectDefinitions[par];

		var currentParent = objDef;
		while (currentParent != null)
		{
			if (currentParent.AssetId == parDef.AssetId)
			{
				return true;
			}

			currentParent = currentParent.parent;
		}

		return false;
	}

	public static object? path_start(object?[] args)
	{
		var path = args[0].Conv<int>();
		var speed = args[1].Conv<double>();
		var endaction = (PathEndAction)args[2].Conv<int>();
		var absolute = args[3].Conv<bool>();

		VMExecutor.Self.GMSelf.AssignPath(path, speed, 1, 0, absolute, endaction);

		return null;
	}

	public static object sqrt(object?[] args)
	{
		var val = args[0].Conv<double>();

		// TODO : Docs say that values [-epsilon,0) are set to 0, probably added in newer GM versions

		return Math.Sqrt(val);
	}

	public static object layer_background_create(object?[] args)
	{
		var layer_id = args[0];
		var sprite = args[1].Conv<int>();

		LayerContainer layer;
		if (layer_id is string s)
		{
			layer = RoomManager.CurrentRoom.Layers.FirstOrDefault(x => x.Value.Name == s).Value;
		}
		else
		{
			var id = layer_id.Conv<int>();
			layer = RoomManager.CurrentRoom.Layers[id];
		}

		var item = new CLayerBackgroundElement();
		item.Index = sprite;
		item.Visible = true;
		item.Color = 0xFFFFFFFF;
		item.Layer = layer;

		var background = new GMBackground(item)
		{
			depth = layer.Depth
		};

		layer.ElementsToDraw.Add(background);

		return item.Id;
	}

	public static object? layer_background_htiled(object?[] args)
	{
		var background_element_id = args[0].Conv<int>();
		var htiled = args[1].Conv<bool>();

		foreach (var layer in RoomManager.CurrentRoom.Layers)
		{
			foreach (var element in layer.Value.ElementsToDraw)
			{
				if (element is GMBackground tilemap && tilemap.Element.Id == background_element_id)
				{
					tilemap.Element.HTiled = htiled;
				}
			}
		}

		return null;
	}

	public static object? layer_background_vtiled(object?[] args)
	{
		var background_element_id = args[0].Conv<int>();
		var vtiled = args[1].Conv<bool>();

		foreach (var layer in RoomManager.CurrentRoom.Layers)
		{
			foreach (var element in layer.Value.ElementsToDraw)
			{
				if (element is GMBackground tilemap && tilemap.Element.Id == background_element_id)
				{
					tilemap.Element.VTiled = vtiled;
				}
			}
		}

		return null;
	}

	public static object? layer_background_visible(object?[] args)
	{
		var background_element_id = args[0].Conv<int>();
		var visible = args[1].Conv<bool>();

		foreach (var layer in RoomManager.CurrentRoom.Layers)
		{
			foreach (var element in layer.Value.ElementsToDraw)
			{
				if (element is GMBackground tilemap && tilemap.Element.Id == background_element_id)
				{
					tilemap.Element.Visible = visible;
				}
			}
		}

		return null;
	}

	public static object? layer_background_xscale(object?[] args)
	{
		var background_element_id = args[0].Conv<int>();
		var xscale = args[1].Conv<double>();

		foreach (var layer in RoomManager.CurrentRoom.Layers)
		{
			foreach (var element in layer.Value.ElementsToDraw)
			{
				if (element is GMBackground tilemap && tilemap.Element.Id == background_element_id)
				{
					tilemap.Element.XScale = xscale;
				}
			}
		}

		return null;
	}

	public static object? layer_background_yscale(object?[] args)
	{
		var background_element_id = args[0].Conv<int>();
		var yscale = args[1].Conv<double>();

		foreach (var layer in RoomManager.CurrentRoom.Layers)
		{
			foreach (var element in layer.Value.ElementsToDraw)
			{
				if (element is GMBackground tilemap && tilemap.Element.Id == background_element_id)
				{
					tilemap.Element.YScale = yscale;
				}
			}
		}

		return null;
	}

	public static object? layer_background_stretch(object?[] args)
	{
		var background_element_id = args[0].Conv<int>();
		var stretch = args[1].Conv<bool>();

		foreach (var layer in RoomManager.CurrentRoom.Layers)
		{
			foreach (var element in layer.Value.ElementsToDraw)
			{
				if (element is GMBackground tilemap && tilemap.Element.Id == background_element_id)
				{
					tilemap.Element.Stretch = stretch;
				}
			}
		}

		return null;
	}

	public static object? instance_activate_object(object?[] args)
	{
		var obj = args[0].Conv<int>();

		if (obj < GMConstants.FIRST_INSTANCE_ID)
		{
			// asset id
			var instances = InstanceManager.FindByAssetId(obj);
			foreach (var instance in instances)
			{
				instance.Active = true;
			}
		}
		else
		{
			// instance id
			var instance = InstanceManager.FindByInstanceId(obj)!;
			instance.Active = true;
		}

		return null;
	}

	public static object? instance_deactivate_object(object?[] args)
	{
		var obj = args[0].Conv<int>();

		if (obj < GMConstants.FIRST_INSTANCE_ID)
		{
			// asset id
			var instances = InstanceManager.FindByAssetId(obj);
			foreach (var instance in instances)
			{
				instance.Active = false;
			}
		}
		else
		{
			// instance id
			var instance = InstanceManager.FindByInstanceId(obj)!;
			instance.Active = false;
		}

		return null;
	}

	public static object? audio_sound_get_pitch(object?[] args)
	{
		var index = args[0].Conv<int>();

		if (index >= GMConstants.FIRST_INSTANCE_ID)
		{
			// instance id
			var soundAsset = AudioManager.GetAudioInstance(index);
			if (soundAsset == null)
			{
				return null;
			}

			return (double)AL.GetSource(soundAsset.Source, ALSourcef.Pitch);
		}
		else
		{
			// sound asset index
			return AudioManager.GetAssetPitch(index);
		}
	}

	public static object sprite_get_bbox_left(object?[] args)
	{
		var ind = args[0].Conv<int>();
		var sprite = SpriteManager._spriteDict[ind];
		return sprite.MarginLeft;
	}

	public static object sprite_get_bbox_top(object?[] args)
	{
		var ind = args[0].Conv<int>();
		var sprite = SpriteManager._spriteDict[ind];
		return sprite.MarginTop;
	}

	public static object sprite_get_bbox_right(object?[] args)
	{
		var ind = args[0].Conv<int>();
		var sprite = SpriteManager._spriteDict[ind];
		return sprite.MarginRight;
	}

	public static object sprite_get_bbox_bottom(object?[] args)
	{
		var ind = args[0].Conv<int>();
		var sprite = SpriteManager._spriteDict[ind];
		return sprite.MarginBottom;
	}

	public static object string_digits(object?[] args)
	{
		var str = args[0].Conv<string>();

		var result = "";

		foreach (var c in str)
		{
			if (char.IsAsciiDigit(c))
			{
				result += c;
			}
		}

		return result;
	}

	public static object median(object?[] args)
	{
		if (args.Length == 0)
		{
			return 0;
		}

		var realValues = new double[args.Length];
		for (var i = 0; i < args.Length; i++)
		{
			realValues[i] = args.Conv<double>();
		}

		Array.Sort(realValues);

		return realValues[CustomMath.FloorToInt(args.Length / 2f)];
	}

	public static object? draw_healthbar(object?[] args)
	{
		var x1 = args[0].Conv<double>();
		var y1 = args[1].Conv<double>();
		var x2 = args[2].Conv<double>();
		var y2 = args[3].Conv<double>();
		var amount = args[4].Conv<double>();
		var backcol = args[5].Conv<int>();
		var mincol = args[6].Conv<int>();
		var maxcol = args[7].Conv<int>();
		var direction = args[8].Conv<int>();
		var showback = args[9].Conv<bool>();
		var showborder = args[10].Conv<bool>();

		var midcol = merge_colour(new object[] { mincol, maxcol, 0.5 });

		if (showback)
		{
			draw_rectangle_colour(new object?[] { x1, y1, x2, y2, backcol, backcol, backcol, backcol, false });

			if (showborder)
			{
				draw_rectangle_colour(new object?[] { x1, y1, x2, y2, 0, 0, 0, 0, true });
			}
		}

		amount = Math.Clamp(amount, 0, 100);

		var fraction = amount / 100;

		var barx1 = 0d;
		var bary1 = 0d;
		var barx2 = 0d;
		var bary2 = 0d;

		switch (direction)
		{
			case 0:
				barx1 = x1;
				bary1 = y1;
				barx2 = x1 + fraction * (x2 - x1);
				bary2 = y2;
				break;
			case 1:
				barx1 = x2 - fraction * (x2 - x1);
				bary1 = y1;
				barx2 = x2;
				bary2 = y2;
				break;
			case 2:
				barx1 = x1;
				bary1 = y1;
				barx2 = x2;
				bary2 = y1 + fraction * (y2 - y1);
				break;
			case 3:
				barx1 = x1;
				bary1 = y2 - fraction * (y2 - y1);
				barx2 = x2;
				bary2 = y2;
				break;
		}

		var col = 0;
		if (amount > 50)
		{
			col = merge_colour(new object[] { midcol, maxcol, (amount - 50) / 50 }).Conv<int>();
		}
		else
		{
			col = merge_colour(new object[] { mincol, midcol, amount / 50 }).Conv<int>();
		}

		draw_rectangle_colour(new object?[] { barx1, bary1, barx2, bary2, col, col, col, col, false });
		if (showborder)
		{
			draw_rectangle_colour(new object?[] { x1, y1, x2, y2, 0, 0, 0, 0, true });
		}

		return null;
	}

	public static object? path_set_kind(object?[] args)
	{
		var index = args[0].Conv<int>();
		var val = args[1].Conv<int>();

		var path = PathManager.Paths[index];
		path.kind = val;
		return null;
	}

	public static object? motion_add(object?[] args)
	{
		double ClampFloat(double value)
		{
			return Math.Floor(value * 1000000) / 1000000.0;
		}

		var dir = args[0].Conv<double>();
		var speed = args[1].Conv<double>();

		var self = VMExecutor.Self.GMSelf;

		self.hspeed += speed * ClampFloat(Math.Cos(dir * CustomMath.Deg2Rad));
		self.vspeed -= speed * ClampFloat(Math.Sin(dir * CustomMath.Deg2Rad));

		return null;
	}

	public static object? path_exists(object?[] args)
	{
		var index = args[0].Conv<int>();

		return PathManager.Paths.ContainsKey(index);
	}

	public static object? game_restart(object?[] args)
	{
		RoomManager.New_Room = GMConstants.ROOM_RESTARTGAME;
		return null;
	}

	public static object? layer_sprite_get_sprite(object?[] args)
	{
		var element_id = args[0].Conv<int>();

		foreach (var layer in RoomManager.CurrentRoom.Layers)
		{
			foreach (var element in layer.Value.ElementsToDraw)
			{
				if (element is GMSprite sprite && sprite.Element.Id == element_id)
				{
					return sprite.Definition;
				}
			}
		}

		return -1;
	}

	public static object? layer_sprite_get_x(object?[] args)
	{
		var element_id = args[0].Conv<int>();

		foreach (var layer in RoomManager.CurrentRoom.Layers)
		{
			foreach (var element in layer.Value.ElementsToDraw)
			{
				if (element is GMSprite sprite && sprite.Element.Id == element_id)
				{
					return sprite.X;
				}
			}
		}

		return 0;
	}

	public static object? layer_sprite_get_y(object?[] args)
	{
		var element_id = args[0].Conv<int>();

		foreach (var layer in RoomManager.CurrentRoom.Layers)
		{
			foreach (var element in layer.Value.ElementsToDraw)
			{
				if (element is GMSprite sprite && sprite.Element.Id == element_id)
				{
					return sprite.Y;
				}
			}
		}

		return 0;
	}

	public static object? layer_sprite_get_xscale(object?[] args)
	{
		var element_id = args[0].Conv<int>();

		foreach (var layer in RoomManager.CurrentRoom.Layers)
		{
			foreach (var element in layer.Value.ElementsToDraw)
			{
				if (element is GMSprite sprite && sprite.Element.Id == element_id)
				{
					return sprite.XScale;
				}
			}
		}

		return 0;
	}

	public static object? layer_sprite_get_yscale(object?[] args)
	{
		var element_id = args[0].Conv<int>();

		foreach (var layer in RoomManager.CurrentRoom.Layers)
		{
			foreach (var element in layer.Value.ElementsToDraw)
			{
				if (element is GMSprite sprite && sprite.Element.Id == element_id)
				{
					return sprite.YScale;
				}
			}
		}

		return 0;
	}

	public static object? layer_sprite_get_index(object?[] args)
	{
		var element_id = args[0].Conv<int>();

		foreach (var layer in RoomManager.CurrentRoom.Layers)
		{
			foreach (var element in layer.Value.ElementsToDraw)
			{
				if (element is GMSprite sprite && sprite.Element.Id == element_id)
				{
					return sprite.FrameIndex;
				}
			}
		}

		return -1;
	}

	public static object? layer_sprite_get_speed(object?[] args)
	{
		var element_id = args[0].Conv<int>();

		foreach (var layer in RoomManager.CurrentRoom.Layers)
		{
			foreach (var element in layer.Value.ElementsToDraw)
			{
				if (element is GMSprite sprite && sprite.Element.Id == element_id)
				{
					return sprite.AnimationSpeed;
				}
			}
		}

		return -1;
	}

	public static object? layer_sprite_destroy(object?[] args)
	{
		var element_id = args[0].Conv<int>();

		foreach (var layer in RoomManager.CurrentRoom.Layers)
		{
			DrawWithDepth? elementToDestroy = null;

			foreach (var element in layer.Value.ElementsToDraw)
			{
				if (element is GMSprite sprite && sprite.Element.Id == element_id)
				{
					elementToDestroy = element;
				}
			}

			if (elementToDestroy == null)
			{
				continue;
			}

			layer.Value.ElementsToDraw.Remove(elementToDestroy);
			elementToDestroy.Destroy();
		}

		return null;
	}

	public static object? path_get_x(object?[] args)
	{
		var index = args[0].Conv<int>();
		var pos = args[0].Conv<double>();

		if (!PathManager.Paths.TryGetValue(index, out var path))
		{
			return -1; // this isn't documented anywhere, smh gamemaker
		}

		return path.XPosition(pos);
	}

	public static object? path_get_y(object?[] args)
	{
		var index = args[0].Conv<int>();
		var pos = args[0].Conv<double>();

		if (!PathManager.Paths.TryGetValue(index, out var path))
		{
			return -1; // this isn't documented anywhere, smh gamemaker
		}

		return path.YPosition(pos);
	}
}

public class FileHandle
{
	public StreamReader? Reader;
	public StreamWriter? Writer;
}

// wrapper around c# datetime stuff that emulates JS Date stuff because im really lazy
// TODO : get rid of this
public class Date
{
	private DateTime _utcDate = DateTime.Now;

	public int GetMilliseconds() => _utcDate.ToLocalTime().Millisecond;

	public long GetTime() => new DateTimeOffset(_utcDate).ToUnixTimeMilliseconds();

	public void SetTime(double ms)
	{
		_utcDate = DateTime.UnixEpoch;
		_utcDate = _utcDate.AddMilliseconds(ms);
	}

	public int GetFullYear() => _utcDate.ToLocalTime().Year;
	public int GetUTCFullYear() => _utcDate.Year;

	public int GetMonth() => _utcDate.ToLocalTime().Month - 1;
	public int GetUTCMonth() => _utcDate.Month - 1;

	public int GetDate() => _utcDate.ToLocalTime().Day;
	public int GetUTCDate() => _utcDate.Day;

	public int GetDay() => (int)_utcDate.ToLocalTime().DayOfWeek;
	public int GetUTCDay() => (int)_utcDate.DayOfWeek;

	public int GetHours() => _utcDate.ToLocalTime().Hour;
	public int GetUTCHours() => _utcDate.Hour;

	public int GetMinutes() => _utcDate.ToLocalTime().Minute;
	public int GetUTCMinutes() => _utcDate.Minute;

	public int GetSeconds() => _utcDate.ToLocalTime().Second;
	public int GetUTCSeconds() => _utcDate.Second;
}
