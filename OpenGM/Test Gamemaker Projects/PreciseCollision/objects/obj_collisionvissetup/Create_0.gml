var size = 5;

for (var i = 0; i < 1000; i += size)
{
	for (var j = 0; j < 1000; j += size)
	{
		instance_create_depth(i, j, 0, obj_collisionvis);
	}
}