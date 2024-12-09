if (room == room_dw_city_big_2 && instance_exists(obj_controller_dw_city_big_2))
{
    if (obj_controller_dw_city_big_2.con != 0)
        caterpillarable = false
}
if (keeptoggled == 0)
{
    if (obj_controller_dw_city_big_2.bothToggled == 1)
    {
        show_debug_message("both toggled!");
        global.flag[383] = 1
        keepdown = 1
    }

    if (keepdown == 0)
    {
        if (place_meeting(x, y, obj_mainchara) || (place_meeting(x, y, obj_marker) && x > 480) || (place_meeting(x, y, obj_marker) && global.flag[915] == 3 && global.flag[916] == 0) || (place_meeting(x, y, obj_caterpillarchara) && caterpillarable == true))
        {
            if (toggle == 0)
            {
                snd_play(snd_noise)
                show_debug_message("toggle ON");
            }
                
            toggle = 1
        }
        else
        {
            if (toggle == 1)
            {
                show_debug_message("toggle OFF");
                snd_play(snd_dtrans_flip)
            }
                
            toggle = 0
        }
    }

    if (keepdown == 1)
        toggle = 1

    image_index = toggle
}
if (keeptoggled == 1)
    image_index = 1
