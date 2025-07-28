if (!setupComplete)
{
    puzzleSurfState = surface_create(puzzleSize, puzzleSize);
    surface_set_target(puzzleSurfState);
    draw_clear_alpha(c_black, 0);
    draw_sprite_ext(sprite_index, 1, 0, 0, image_xscale, image_yscale, image_angle, c_white, 1);
    var _buffer = buffer_create(puzzleSize * puzzleSize * 4, buffer_fixed, 1);
    buffer_get_surface(_buffer, puzzleSurfState, 0);
    bufferGoal = buffer_md5(_buffer, 0, buffer_get_size(_buffer));
    show_debug_message("bufferGoal is " + bufferGoal);
    buffer_delete(_buffer);
    draw_clear_alpha(c_black, 0);
    surface_reset_target();
    setupComplete = true;
    show_debug_message("setup complete");
}

if (insideOnly && setupComplete && goalCheck && room == room_dw_mansion_darkbulb_3)
{
    with (obj_shapepuzzlepiece)
    {
        if (image_index == 6)
        {
            var _sideways = (image_angle % 180) != 0;
            var __x = x;
            var __y = y;
            var __yy = y;
            var __xx = x;
            
            if (_sideways)
            {
                __x = x + lengthdir_x(20, image_angle - 90);
                __yy = y + lengthdir_y(108, image_angle);
                __xx = x + lengthdir_x(106, image_angle - 90);
            }
            else
            {
                __y = y + lengthdir_y(20, image_angle - 90);
                __xx = x + lengthdir_x(108, image_angle);
                __yy = y + lengthdir_y(106, image_angle - 90);
            }
            
            if (min(__xx, __x) < other.x || max(__xx, __x) > (other.x + other.puzzleSize) || min(__yy, __y) < other.y || max(__yy, __y) > (other.y + other.puzzleSize))
                other.goalCheck = false;
        }
        else if (x < other.x || x > (other.x + other.puzzleSize) || y < other.y || y > (other.y + other.puzzleSize))
        {
            other.goalCheck = false;
        }
    }
}

if (setupComplete && goalCheck)
{
    if (!surface_exists(puzzleSurfState))
        puzzleSurfState = surface_create(puzzleSize, puzzleSize);
    
    var _xx = x;
    var _yy = y;
    surface_set_target(puzzleSurfState);
    draw_clear_alpha(c_black, 0);
    gpu_set_colorwriteenable(false, false, false, true);
    draw_sprite_ext(sprite_index, 1, 0, 0, image_xscale, image_yscale, image_angle, c_white, 1);
    gpu_set_colorwriteenable(true, true, true, true);
    gpu_set_blendmode_ext(bm_dest_alpha, bm_inv_dest_alpha);
    gpu_set_alphatestenable(true);
    
    with (obj_shapepuzzlepiece)
        draw_sprite_ext(sprite_index, image_index, x - _xx, y - _yy, image_xscale, image_yscale, image_angle, c_white, 1);
    
    surface_reset_target();
    gpu_set_alphatestenable(false);
    gpu_set_blendmode(bm_normal);

    draw_surface(puzzleSurfState, 0, 0);

    var _buffer = buffer_create(puzzleSize * puzzleSize * 4, buffer_fixed, 1);
    buffer_get_surface(_buffer, puzzleSurfState, 0);
    var _currentState = buffer_md5(_buffer, 0, buffer_get_size(_buffer));
    show_debug_message("current state is " + _currentState);
    
    if (_currentState == bufferGoal)
    {
        snd_play(snd_shadowpendant);
        goalHit = true;
        goalCheck = false;
        
        with (obj_shapepuzzlepiece)
            slowOff = true;
        
        if (room == room_dw_mansion_darkbulb_1)
        {
            with (obj_ch2_scene19a)
                con = 20;
        }
        
        if (room == room_dw_mansion_darkbulb_2)
        {
            with (obj_ch2_scene19b)
                con = 20;
        }
        
        if (room == room_dw_mansion_darkbulb_3)
        {
            with (obj_ch2_scene19c)
                con = 20;
        }
    }
    
    buffer_delete(_buffer);
}

draw_sprite_ext(sprite_index, 0, x, y, image_xscale, image_yscale, image_angle, image_blend, 0.25);

if (goalHit)
{
    finalGlow = lerp(finalGlow, 0.7, 0.2);
    
    if (finalGlow > 0.69)
        finalGlow = 0.7;
    
    gpu_set_blendmode(bm_add);
    draw_sprite_ext(sprite_index, 1, x, y, image_xscale, image_yscale, image_angle, c_white, finalGlow);
    gpu_set_blendmode(bm_normal);
    
    with (obj_shapepuzzlepiece)
    {
        if (!slowOff)
            visible = false;
    }
}

if (instructionLerp > 0 || inUse)
{
    if (goalHit)
        instructionLerp = 0;
    else if (instructionLerp < 1 && inUse)
        instructionLerp += 0.1;
    else if (!inUse)
        instructionLerp -= 0.1;
    
    var _easedLerp = scr_ease_out(instructionLerp, 3);
    scr_84_set_draw_font("mainbig");
    draw_set_halign(fa_left);
    draw_set_color(c_white);
    
    if (global.is_console || obj_gamecontroller.gamepad_active)
    {
        var sprite_pos_left = (camerax() - 80) + (_easedLerp * 100);
        draw_sprite_ext(scr_getbuttonsprite(global.input_g[3], false), 0, sprite_pos_left, cameray() + 400 + 2, 2, 2, 0, c_white, 1);
        draw_sprite_ext(scr_getbuttonsprite(global.input_g[0], false), 0, sprite_pos_left + 26, cameray() + 400 + 2, 2, 2, 0, c_white, 1);
        draw_sprite_ext(scr_getbuttonsprite(global.input_g[2], false), 0, sprite_pos_left + 52, cameray() + 400 + 2, 2, 2, 0, c_white, 1);
        draw_sprite_ext(scr_getbuttonsprite(global.input_g[1], false), 0, sprite_pos_left + 78, cameray() + 400 + 2, 2, 2, 0, c_white, 1);
        var move_text = stringsetloc("#Move", "obj_shapepuzzle_slash_Draw_0_gml_181_0");
        scr_84_draw_text_outline((camerax() - 80) + (_easedLerp * 100), cameray() + 400, string_hash_to_newline(move_text));
        var sprite_pos = (camerax() + 720) - (_easedLerp * 100) - 125;
        var _sprite = scr_getbuttonsprite(global.input_g[4], false);
        draw_sprite_ext(_sprite, 0, sprite_pos, cameray() + 400 + 2, 2, 2, 0, c_white, 1);
        _sprite = scr_getbuttonsprite(global.input_g[5], false);
        draw_sprite_ext(_sprite, 0, sprite_pos, cameray() + 400 + 32, 2, 2, 0, c_white, 1);
        draw_set_halign(fa_left);
        var rotate_text = stringsetloc("Rotate#Quit", "obj_shapepuzzle_slash_Draw_0_gml_192_0");
        scr_84_draw_text_outline(sprite_pos + 35, cameray() + 400, string_hash_to_newline(rotate_text));
    }
    else
    {
        scr_84_draw_text_outline((camerax() - 80) + (_easedLerp * 100), cameray() + 400, string_hash_to_newline(stringsetloc("[Arrows]#Move", "obj_shapepuzzle_slash_Draw_0_gml_171_0")));
        draw_set_halign(fa_right);
        scr_84_draw_text_outline((camerax() + 720) - (_easedLerp * 100), cameray() + 400, string_hash_to_newline(stringsetsubloc("~1 Rotate#~2 Quit", scr_get_input_name(4), scr_get_input_name(5), "obj_shapepuzzle_slash_Draw_0_gml_173_0")));
        draw_set_halign(fa_left);
    }
}
