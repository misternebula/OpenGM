if (nointeract == false && con == 1 && other.name == "noelle" && other.visible == true)
{
	show_debug_message("COLLIDED WITH NOELLE")
	other.visible = false
	scaredNoelle = instance_create(other.x, other.y, obj_noelle_scared)
	if (room == room_dw_city_mice)
	{
		scaredNoelle.targetx = 485
		scaredNoelle.targety = 225
	}
	if (room == room_dw_city_mice2)
		scaredNoelle.alarm[0] = 1
}