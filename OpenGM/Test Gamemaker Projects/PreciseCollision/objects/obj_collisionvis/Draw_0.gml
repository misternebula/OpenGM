var size = 5;

var collides = collision_rectangle(x, y, x + size, y + size, obj_collider, true, true);

if (collides)
{
	draw_set_color(c_lime);
	draw_rectangle(x, y, x + size, y + size, false);
}