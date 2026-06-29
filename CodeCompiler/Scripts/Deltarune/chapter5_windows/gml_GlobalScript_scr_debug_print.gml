function scr_debug_print()
{
    if (!scr_debug())
        exit;

    show_debug_message("DEBUG: " + argument0);
}

function scr_debug_clear_all()
{
}
