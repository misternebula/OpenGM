if (wait)
{
    show_debug_message(string(id) + " : Waiting!");
    exit;
}

if (friction > 0 && speed <= end_speed)
{
    speed = end_speed;
    friction = 0;
}

if (friction == 0)
{
    sparkle_timer++;
    show_debug_message(string(id) + " : sparkle_timer is " + string(sparkle_timer));

    if (sparkle_timer == 8)
        image_speed = 1;
}

show_debug_message(string(id) + " : image_index is " + string(image_index) + ", image_number is " + string(image_number) + ", image_speed is " + string(image_speed));

if (image_index == (image_number - 1))
{
    show_debug_message(string(id) + " : Destroy!");
    instance_destroy();
}