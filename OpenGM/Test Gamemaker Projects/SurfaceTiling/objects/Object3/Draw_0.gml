var test_surface = surface_create(128, 128);
surface_copy_part(test_surface, 0, 0, application_surface, 0, 0, 128, 128);
draw_surface_tiled_ext(test_surface, current_time / 10, current_time / 10, 0.5, 1, c_blue, 0.5);