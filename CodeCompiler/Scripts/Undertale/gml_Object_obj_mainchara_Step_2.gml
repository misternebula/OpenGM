if (obj_time.left == 0)
{
    if (obj_time.right == 0)
    {
        if (obj_time.down == 0)
        {
            if (obj_time.up == 0)
                moving = 0;
        }
    }
}

if (global.interact > 0)
{
    moving = 0;
    movement = 0;
}
else
{
    movement = 1;
}

if (abs(xprevious - x) > 0.01 || abs(yprevious - y) > 0.01)
    moving = 1;

if (moving == 0)
{
    image_speed = 0;
    image_index = 0;
}

if (global.interact == 0)
{
    if (moving == 1)
        global.encounter += 1;
}

if (cutscene == 0)
{
    if (instance_exists(obj_shaker) == 0)
    {
        view_xview[0] = round((x - (view_wview[0] / 2)) + 10);
        view_yview[0] = round((y - (view_hview[0] / 2)) + 10);
        show_debug_message("x: " + x + " y: " + y);
        show_debug_message("wview: " + view_wview[0] + " hview: " + view_hview[0]);
        show_debug_message("(x - (view_wview[0] / 2)) + 10 ----> " + ((x - (view_wview[0] / 2)) + 10))
        show_debug_message("xview: " + view_xview[0] + " yview: " + view_yview[0]);
    }
}

with (obj_backgrounder_parent)
    event_user(0);
