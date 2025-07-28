function scr_board_objname()
{
    if (scr_debug())
    {
        //if (global.chemg_show_room)
        //{
            var __cx = board_tilex(12) - 2;
            var __cy = board_tiley(0);
            
            if (argument_count >= 1)
                __cx = argument0;
            
            if (argument_count >= 2)
                __cy = argument1;
            
            draw_set_halign(fa_right);
            draw_set_font(fnt_main);
            draw_set_color(c_aqua);
            draw_text_outline(__cx, __cy, string_copy(object_get_name(object_index), 5, 99));
            draw_set_font(fnt_small);
            draw_set_halign(fa_left);
            draw_set_color(c_white);
        //}
    }
}
