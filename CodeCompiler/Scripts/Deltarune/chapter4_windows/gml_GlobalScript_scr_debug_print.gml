function scr_debug_print(arg0)
{
    show_debug_message("scr_debug_print: " + string(arg0));
}

function print_message(arg0)
{
    show_debug_message("print_message: " + string(arg0));
}

function debug_print(arg0)
{
    show_debug_message("debug_print: " + string(arg0));
}

function scr_debug_clear_all()
{
    scr_debug_clear_persistent();
}
