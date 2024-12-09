graceperiod--
if (con == 1 && graceperiod < 0)
{
    show_debug_message("con = 1")
    if (jumppuzzle == 1)
        hastarget = 0

    if (autocomplete == 0)
    {
        sprite_index = spr_noelleb_battleintro_l
        image_index = 3
    }
    else
    {
        sprite_index = spr_noelle_walk_right_smile_dw
        image_index = 1
        image_speed = 0
    }

    jumping = 1
    if (jumpstyle == 1)
        jumpyspeed = -30
    else
        jumpyspeed = -20

    maxjumpy = -200
    xadjust = 0
    yadjust = 0
    step = 0
    while (xadjust == 0)
    {
        step++
        yadjust = 0
        met = 0
        while (yadjust == 0)
        {
            mety = 0
            if (mety == 0)
                yadjust = 1
        }
        if (met == 0)
            xadjust = 1
        if (step >= 10)
        {
            targetx = x
            targety = y
            xadjust = 1
            yadjust = 1
        }
    }
    timer = 0
    con = 2
}

if (jumpstyle == 0 && con == 2)
{
	show_debug_message("con = 2, jumpstyle = 0")
    timer++
    image_index += 0.5
    if (image_index >= 5)
        image_index = 3
    x = lerp(x, targetx, 0.2)
    y = lerp(y, targety, 0.2)
    jumpyspeed += 2
    jumpy += jumpyspeed
    if (jumpy >= jumpyspeed && timer >= 4)
    {
        image_index = 2
        x = targetx
        y = targety
        jumping = 0
        jumpy = 0
        jumpyspeed = 0
        timer = 0
        con = 4
    }
}

if (con == 2 && jumpstyle == 1)
{
	show_debug_message("con = 2, jumpstyle = 1")
    image_index += 0.5
    if (image_index >= 5)
        image_index = 3
    if ((y + jumpy) > ((__view_get((1 << 0), 0)) - 80))
        jumpy += jumpyspeed
    else
    {
        x += 10
        if (x >= targetx)
        {
            con = 3
            jumpyspeed = 10
        }
    }
}

if (con == 3 && jumpstyle == 1)
{
	show_debug_message("con = 3, jumpstyle = 1")
    image_index += 0.5
    if (image_index >= 5)
        image_index = 3
    jumpyspeed += 2
    jumpy += jumpyspeed
    if (jumpy >= jumpyspeed)
    {
        image_index = 2
        jumping = 0
        jumpy = 0
        jumpyspeed = 0
        con = 4
    }
}

if (con == 4)
{
	show_debug_message("con = 4")
    if (jumppuzzle == 1)
    {
        hastarget = 1
        if (fallen == 0)
            target = obj_masterPlatformController.plat[obj_masterPlatformController.currentPlatform]
        else
            target = obj_masterPlatformController.plat[(obj_masterPlatformController.currentPlatform - 1)]
    }
    image_index -= 0.5
    if (image_index <= 0.5)
    {
        image_index = 0
        con = 0
        graceperiod = 30
        if (jumppuzzle == 1)
        {
            if (instance_exists(obj_masterPlatformController) && fallen == 0)
                obj_masterPlatformController.makenewplatform = 1
            else
                fallen = 0
        }
    }
}

if (jostlecon == 1)
{
    sprite_index = spr_noelleb_battleintro
    image_index = 4
    jostlesiner = 0
    crowdx = crowdid.x - 10
    crowdy = crowdid.y - 60
    jumpy = lerp(jumpy, -60, 0.2)
    jumpyfactor = jumpy
    x = lerp(x, crowdx, 0.2)
    y = lerp(y, crowdy, 0.2)
    if (crowdx < x)
        x -= 2
    if (crowdx > x)
        x += 2
    if (crowdy > y)
        y += 2
    if (crowdy < y)
        y -= 2
    if (point_distance(x, y, crowdx, crowdy) < 6)
        jostlecon = 2
}

if (jostlecon == 2)
{
    jostlesiner++
    x = crowdid.x - 10 + sin(jostlesiner) * 3
    y = crowdid.y - 60
    jumpy = (-(abs(sin(jostlesiner / 2)))) * 16 + random(2) + jumpyfactor
    if (jumpyfactor > -60)
        jumpyfactor -= 5
    if (jostlesiner >= 180)
    {
        jostlecon = 0
        con = 1
        targetx = x + 240
        jumpstyle = 0
    }
}

if (fallen == 0 && y > room_height)
{
    failureCount++
    fallen = 1
}

if (fallen == 1)
{
    x = 114
    y = room_height + 20
    tower = instance_create(x, y, obj_tower_o_mice)
    scr_move_to_point_over_time(115, 285, 60)
    fallen = 2
    timer = 0
}

if (fallen == 2)
{
    timer++
    if (timer == 60)
    {
        alarm[0] = true
        tower.con++
        fallen = 3
    }
}

if (room != room_dw_city_mice3)
    scr_depth()

if (con == 0 && jostlecon == 0 && extflag == "mice1Scare")
{
    noelleFacing = instance_create(x, y, obj_npc_facing)
    noelleFacing.sprite_index = sprite_index
    noelleFacing.image_xscale = 2
    noelleFacing.image_yscale = 2
    noelleFacing.y = noelleFacing.ystart
    with (noelleFacing)
        scr_set_facing_sprites("noelledark")
    instance_destroy()
}

if (con == 0 && jostlecon == 0)
    jostletimer++
else
    jostletimer = 0

if instance_exists(obj_controller_city_mice3)
{
    if (obj_controller_city_mice3.micescore >= obj_controller_city_mice3.micetarget)
        image_speed = 0
}

scr_depth()
