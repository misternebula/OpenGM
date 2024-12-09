if (autocomplete == 0)
{
	show_debug_message("autocomplete is 0")
	if (jumppuzzle == 0)
	{
		show_debug_message("jumppuzzle is 0, con is " + con)
		if (con == 0)
		{
			snd_play(snd_noelle_scared)
			jumpy = 20
			if i_ex(obj_controller_city_mice2)
			{
				targetx = 370
				targety = 166
			}
			if (instance_exists(obj_controller_city_mice2) && obj_controller_city_mice2.victory == 1 && obj_controller_city_mice2.con != -2)
			{
				targetx = 860
				targety = 238
			}
			if (extflag == "mice1Scare")
			{
				targetx = x
				targety = y
			}
			con = 1
		}
	}
	else
	{
		if instance_exists(obj_controller_city_mice3)
		{
			if (fallen == 0)
				obj_controller_city_mice3.exttrig = "success"
			else
				obj_controller_city_mice3.exttrig = "fell"
		}
		snd_play(snd_noelle_scared)
		if instance_exists(obj_masterPlatformController)
		{
			if (con == 0)
			{
				jumpy = 20
				if (fallen == 0)
				{
					targetx = obj_masterPlatformController.plat[obj_masterPlatformController.currentPlatform].x + 120
					targety = obj_masterPlatformController.plat[obj_masterPlatformController.currentPlatform].patidealy
				}
				else
				{
					targetx = (obj_masterPlatformController.plat[(obj_masterPlatformController.currentPlatform - 1)].x) + 120
					targety = obj_masterPlatformController.plat[(obj_masterPlatformController.currentPlatform - 1)].patidealy
				}
				con = 1
			}
		}
	}
}