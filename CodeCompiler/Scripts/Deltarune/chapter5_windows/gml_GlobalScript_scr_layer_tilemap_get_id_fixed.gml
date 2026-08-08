function scr_layer_tilemap_get_id_fixed(arg0)
{
    show_debug_message("scr_layer_tilemap_get_id_fixed arg0: " + string(arg0));

    var els = layer_get_all_elements(arg0);
    var n = array_length_1d(els);

    show_debug_message("- element count: " + string(n));
    
    for (var i = 0; i < n; i++)
    {
        var el = els[i];
        show_debug_message("-- " + string(el));
        
        if (layer_get_element_type(el) == 5)
            return el;
    }
    
    return -1;
}
