if (place_meeting(x, y, obj_mainchara) && pressed == 0 && global.plot < 72)
{
    pressed = 1
    snd_play(snd_noise)
    if (attempt >= 1)
    {
        if instance_exists(obj_holemouse_generator)
        {
            obj_holemouse_generator.mousecreate = 1
            obj_holemouse_generator.gentimer = 15
            global.interact = 1
        }
    }
    timer = 0
    attempt++
}

if (place_meeting(x, y, obj_mainchara) && pressed == 0 && global.plot >= 72)
{
    pressed = 1
    snd_play(snd_noise)
}

if (place_meeting(x, y, obj_caterpillarchara) && pressed == 0 && (!instance_exists(obj_npc_facing)) && (!instance_exists(obj_noelle_scared)) && obj_controller_dw_city_mice.con > 0)
{
    pressed = 1
    snd_play(snd_noise)
}

if (place_meeting(x, y, obj_actor) && pressed == 0 && global.plot < 72)
{
    pressed = 1
    snd_play(snd_noise)
    if instance_exists(obj_holemouse_generator)
    {
        obj_holemouse_generator.mousecreate = 1
        obj_holemouse_generator.gentimer = 15
        global.interact = 1
    }
    timer = 0
}

if (pressed == 1)
    timer++

if ((!(place_meeting(x, y, obj_mainchara))) && (!(place_meeting(x, y, obj_caterpillarchara))) && (!(place_meeting(x, y, obj_actor))) && pressed == 1)
    pressed = 0

image_index = pressed
if (room == room_dw_city_mice && attempt == 1 && instance_exists(obj_controller_dw_city_mice) && obj_controller_dw_city_mice.victory == 0)
{
    tIntroCon = 1
    global.interact = 1
    attempt++
}

if (introCon == 0 || introCon == 1 || introCon == 2 || introCon == 3 || introCon == 4)
{
    timer = 0
    global.interact = 1
}

if (tIntroCon == 1)
{
    show_debug_message("tIntroCon is " + tIntroCon)
    cutscene_master = scr_cutscene_make()
    scr_maincharacters_actors()
    tIntroCon = 2
    fakemouse = scr_dark_marker(220, 180, spr_holemouse_emerge)
    fakemouse.visible = 0
    fakedoor = scr_dark_marker(obj_holemouse_generator.x, obj_holemouse_generator.y, spr_mouseHole)
    fakedoor.image_index = 5
    realdoor = instance_find(obj_holemouse_generator, 0)
    cutscene_master.save_object[0] = fakemouse
    cutscene_master.save_object[1] = fakedoor
    cutscene_master.save_object[2] = realdoor
}

if (tIntroCon == 2)
{
	show_debug_message("tIntroCon is " + tIntroCon)
    tIntroCon = 999
    c_sel(kr)
    c_walkdirect(160, 270, 15)
    c_sel(no)
    c_walkdirect(196, 260, 15)
    c_wait(30)
    c_sel(no)
    c_facing("u")
    c_var_instance(fakedoor, "image_index", 0)
    c_soundplay(snd_locker)
    c_wait(30)
    c_var_instance(fakemouse, "visible", 1)
    c_soundplay(snd_mouse)
    c_wait(30)
    c_sel(no)
    c_sprite(spr_noelle_shocked_dw)
    c_speaker("noelle")
    c_msgsetloc(0, "\\E2* A m-m-m-m-m.../", "obj_mouseSpawnSwitch_slash_Step_0_gml_123_0")
    c_msgnextloc("\\EE* Mouse!?/%", "obj_mouseSpawnSwitch_slash_Step_0_gml_124_0")
    c_talk_wait()
    c_var_instance(realdoor, "desIndex", 0)
    c_var_instance(realdoor, "mousecreate", 1)
    c_var_instance(realdoor, "gentimer", 15)
    c_var_instance(fakemouse, "visible", 0)
    c_var_instance(fakedoor, "visible", 0)
    c_var_instance(id, "tIntroCon", 5)
    c_waitcustom()
    c_wait(1)
}

if (tIntroCon == 5)
{
	show_debug_message("tIntroCon is " + tIntroCon)
    if (instance_exists(obj_caterpillarchara) && (!instance_exists(obj_noelle_scared)))
    {
        no_actor.visible = 0
        obj_caterpillarchara.x = no_actor.x
        obj_caterpillarchara.y = no_actor.y
        with (obj_caterpillarchara)
        {
            visible = 1
            fun = 1
            sprite_index = spr_noelle_shocked_dw
        }
        tIntroCon = 6
        mousetimer = 0
    }
}

if (tIntroCon == 6)
{
	show_debug_message("tIntroCon is " + tIntroCon)
    mousetimer++
    if (obj_controller_dw_city_mice.victory == 1 && (!instance_exists(obj_holemouse)))
        mousetimer = 90
    if (mousetimer >= 90)
    {
        tIntroCon = 10
        if instance_exists(obj_noelle_scared)
        {
            if (obj_controller_dw_city_mice.victory == 0)
            {
                c_waitcustom_end()
                scr_speaker("noelle")
                c_msgsetloc(0, "\\E3* S..^1. sorry^1, Kris^1, could you..^1. d-deal with them somehow?/", "obj_mouseSpawnSwitch_slash_Step_0_gml_171_0")
                c_msgnextloc("\\E4* Maybe if you can get them in the other mouse hole?/%", "obj_mouseSpawnSwitch_slash_Step_0_gml_172_0")
                c_talk_wait()
                c_var_instance(id, "introCon", 4)
                c_var_instance(id, "introFinished", 1)
                c_actortokris()
                c_terminatekillactors()
            }
            else
            {
                earlywin = 1
                c_waitcustom_end()
                c_var_instance(id, "introCon", 4)
                c_var_instance(id, "introFinished", 1)
                c_actortokris()
                c_terminatekillactors()
            }
        }
        else
        {
            c_waitcustom_end()
            c_sel(no)
            c_facing("u")
            c_speaker("noelle")
            c_msgsetloc(0, "\\E2* H-huh^1?&* Nothing happened.../%", "obj_mouseSpawnSwitch_slash_Step_0_gml_189_0")
            c_talk_wait()
            c_actortokris()
            c_actortocaterpillar()
            c_terminatekillactors()
            tIntroCon = 200
        }
        with (obj_caterpillarchara)
        {
            fun = 0
            sprite_index = spr_noelle_walk_left_dw
        }
    }
}

if (introCon == 0)
{
	show_debug_message("introCon is " + introCon)
    if instance_exists(obj_caterpillarchara)
    {
        with (obj_caterpillarchara)
        {
            fun = 1
            sprite_index = spr_noelle_shocked_dw
        }
    }
    if instance_exists(obj_noelle_scared)
    {
        if (obj_noelle_scared.x == obj_noelle_scared.targetx && obj_noelle_scared.y == obj_noelle_scared.targety)
            introCon++
    }
}

if (introCon == 1)
{
	show_debug_message("introCon is " + introCon)
    obj_noelle_scared.sprite_index = spr_noelleb_battleintro_l
    obj_noelle_scared.image_index = 3
    introconTimer++
    if (introconTimer == 30)
        introCon++
}

if (introCon == 2)
{
	show_debug_message("introCon is " + introCon)
    fakenoelle = scr_dark_marker(obj_noelle_scared.x, obj_noelle_scared.y, spr_noelleb_battleintro_l)
    with (obj_noelle_scared)
        instance_destroy()
    noelleWaitTimer = 0
    introCon++
}

if (introCon == 3)
{
	show_debug_message("introCon is " + introCon)
    if (fakenoelle.image_index >= 11 || tIntroCon >= 10)
    {
        noelleWaitTimer++
        if (noelleWaitTimer >= 5)
        {
            scr_speaker("noelle")
            msgsetloc(0, "\\EE* M-m-m-m-mouse!!/", "obj_mouseSpawnSwitch_slash_Step_0_gml_275_0")
            msgnextloc("\\E2* S..^1. sorry^1, Kris^1, could you..^1. d-deal with them somehow?/", "obj_mouseSpawnSwitch_slash_Step_0_gml_276_0")
            msgnextloc("\\E4* Maybe^1, make a mouseless path I can walk through...?/%", "obj_mouseSpawnSwitch_slash_Step_0_gml_277_0")
            d_make()
            introCon = 4
        }
    }
    else
        fakenoelle.image_index += 0.5
}

if (introCon == 4 && (!d_ex()) && (!instance_exists(obj_cutscene_master)))
{
	show_debug_message("introCon is " + introCon)
    if instance_exists(obj_noelle_scared)
    {
        noelleFacing = instance_create(obj_noelle_scared.x, obj_noelle_scared.y, obj_npc_facing)
        noelleFacing.sprite_index = spr_noelle_walk_down_dw
        noelleFacing.image_xscale = 2
        noelleFacing.image_yscale = 2
        noelleFacing.y = noelleFacing.ystart
        with (noelleFacing)
            scr_set_facing_sprites("noelledark")
        with (obj_noelle_scared)
            instance_destroy()
    }
    if i_ex(obj_cybercity_mousesign)
    {
        with (obj_cybercity_mousesign)
        {
            start = true
            fade_in = true
        }
    }
    timer = 29
    global.interact = 0
    introCon++
}

if (introCon == 4)
{
	show_debug_message("introCon is " + introCon)
    if i_ex(noelleFacing)
    {
        noelleFacing = instance_create(noelleFacing.x, noelleFacing.y, noelleFacing)
        noelleFacing.sprite_index = obj_noelle_scared.sprite_index
        noelleFacing.image_xscale = 2
        noelleFacing.image_yscale = 2
        with (noelleFacing)
            scr_set_facing_sprites("noelledark")
        noelleFacing.y = noelleFacing.ystart
    }
    with (noelleFacing)
        instance_destroy()
    if (!d_ex())
        introCon++
}

if (introCon == 5)
{
	show_debug_message("introCon is " + introCon)
    timer = 29
    global.interact = 0
    introCon++
}

if (introCon == 6)
{
	show_debug_message("introCon is " + introCon)
    if (global.plot >= 72)
        introCon = 99
    else if (obj_mainchara.y >= leaveTrigger)
        introCon = 7
    else if (obj_mainchara.y < 80)
        introCon = 9
    if (global.interact == 1 && timer > 60)
    {
        if ((!d_ex()) && (!i_ex(obj_holemouse)))
        {
            if (obj_controller_dw_city_mice.scaredAgain == 99 || obj_controller_dw_city_mice.scaredAgain == 3)
                global.interact = 0
        }
    }
}

if (introCon == 7)
{
	show_debug_message("introCon is " + introCon)
    global.interact = 1
    scr_speaker("noelle")
    if (leaveAttempt == 0)
    {
        msgsetloc(0, "\\E4* H-hey^1, Kris? You can't leave yet^1, y'know!/", "obj_mouseSpawnSwitch_slash_Step_0_gml_352_0")
        msgnextloc("\\E3* You need to deal with the mice^1, first./", "obj_mouseSpawnSwitch_slash_Step_0_gml_353_0")
        msgnextloc("\\E4* You can do it^1, Kris^1! I believe in you!/%", "obj_mouseSpawnSwitch_slash_Step_0_gml_354_0")
    }
    if (leaveAttempt == 1)
    {
        msgsetloc(0, "\\E3* Kris^1, you^1, um..^1. aren't gonna leave me here^1, are you?/", "obj_mouseSpawnSwitch_slash_Step_0_gml_358_0")
        msgnextloc("\\E2* Th-think about all the p-pencils I gave you!/", "obj_mouseSpawnSwitch_slash_Step_0_gml_359_0")
        msgnextloc("\\E2* The..^1. the peppermint cookies I shared with you!/%", "obj_mouseSpawnSwitch_slash_Step_0_gml_360_0")
    }
    if (leaveAttempt == 2)
    {
        msgsetloc(0, "\\E8* .../", "obj_mouseSpawnSwitch_slash_Step_0_gml_364_0")
        msgnextloc("\\E8* I'll do your homework for you./%", "obj_mouseSpawnSwitch_slash_Step_0_gml_365_0")
    }
    if (leaveAttempt == 3)
        introCon = 127
    if (leaveAttempt != 3)
        d = d_make()
    leaveAttempt++
    introCon++
}

if (introCon == 8 && (!d_ex()))
{
    global.facing = 2
    obj_mainchara.y = leaveTrigger - 1
    leaveTrigger += 8
    if (backeddown == 1)
    {
        leaveTrigger = 392
        leaveAttempt = 3
        backeddown = 0
    }
    global.interact = 0
    introCon = 6
}

if (introCon == 9)
{
    global.interact = 1
    with (obj_npc_facing)
        dfacing = 2
    scr_speaker("noelle")
    if (leaveUpAttempt == 0)
    {
        msgsetloc(0, "\\E4* Oh^1, merry Krismaaaas--!/", "obj_mouseSpawnSwitch_slash_Step_0_gml_395_0")
        msgnextloc("\\E8* That's where we came from!/%", "obj_mouseSpawnSwitch_slash_Step_0_gml_396_0")
    }
    else
        msgsetloc(0, "\\E4* Kris^1, are you afraid of mice^1, too? Fahahaha!/%", "obj_mouseSpawnSwitch_slash_Step_0_gml_400_0")
    d = d_make()
    leaveUpAttempt++
    introCon++
}

if (introCon == 10 && (!d_ex()))
{
    obj_mainchara.y = 81
    with (obj_npc_facing)
        dfacing = 0
    global.facing = 0
    global.interact = 0
    introCon = 6
}

if (introCon == 128)
{
    global.msc = 1202
    scr_text(global.msc)
    d_make()
    introCon++
}

if (introCon == 131 && (!d_ex()))
{
    global.interact = 1
    obj_caterpillarchara.x = obj_npc_facing.x
    obj_caterpillarchara.y = obj_npc_facing.y
    with (obj_npc_facing)
        instance_destroy()
    cutscene_master = scr_cutscene_make()
    scr_maincharacters_actors()
    hole = instance_find(obj_controller_dw_city_mice, 0)
    introCon++
}

if (introCon == 132)
{
    global.interact = 1
    c_sel(no)
    c_autowalk(0)
    c_sprite(spr_noelle_walk_down_dw)
    c_imagespeed(0.25)
    c_walkdirect(485, 250, 15)
    c_autowalk(1)
    c_facing("d")
    c_wait(15)
    c_wait(30)
    c_sprite(spr_noelle_cower_left)
    c_setxy(483, 256)
    c_halt()
    c_wait(15)
    c_autowalk(0)
    c_imagespeed(0.25)
    c_walkdirect_wait(150, 256, 25)
    c_halt()
    c_wait(65)
    c_speaker("noelle")
    c_msgsetloc(0, "\\E4* Ph..^1. phew.../", "obj_mouseSpawnSwitch_slash_Step_0_gml_454_0")
    c_msgnextloc("\\E8* (We'd better not have to do something like that again...)/%", "obj_mouseSpawnSwitch_slash_Step_0_gml_455_0")
    c_talk_wait()
    c_autowalk(1)
    c_walkdirect_wait(150, 330, 20)
    c_halt()
    c_speaker("noelle")
    c_msgsetloc(0, "\\E3* (Seriously, when did Kris get so bossy?)/%", "obj_mouseSpawnSwitch_slash_Step_0_gml_506_0")
    c_talk_wait()
    c_actortokris()
    c_actortocaterpillar()
    c_terminatekillactors()
    introCon = 133
}

if (introCon == 133 && (!d_ex()) && (!i_ex(cutscene_master)))
{
    if (global.plot < 72)
        global.plot = 72
    global.interact = 0
    introCon++
}

if (tIntroCon == 200 && (!instance_exists(obj_cutscene_master)))
{
    with (obj_caterpillarchara)
        scr_caterpillar_interpolate()
    global.interact = 0
    if (global.plot < 72)
        global.plot = 72
    tIntroCon = 201
}
