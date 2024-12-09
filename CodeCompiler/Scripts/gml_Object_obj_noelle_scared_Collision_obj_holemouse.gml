show_debug_message("holemouse collided with noelle scared")
if (other.nointeract == false)
{
	show_debug_message(" - nointeract is false. con:" + con + " graceperiod:" + graceperiod + " other.con:" + other.con)
	if (con == 0 && graceperiod <= 0 && other.con == 1)
	{
		alarm[0] = 1
	}
}