function findsprite(arg0, arg1 = "all", arg2 = -99999, arg3 = -99999, arg4 = -99999, arg5 = -99999, arg6 = -99999)
{
    show_debug_message("findsprite arg0:" + string(arg0) + ", arg1:" + string(arg1) + ", arg2:" + string(arg2) + ", arg3:" + string(arg3) + ", arg4:" + string(arg4) + ", arg5:" + string(arg5) + ", arg6:" + string(arg6));

    var layerstocheck = [];
    var spritestocheck = [];
    
    if (arg1 == "all")
        layerstocheck = layer_get_all();
    else
        layerstocheck = [arg1];
    
    for (var i = 0; i < array_length(layerstocheck); i++)
    {
        var elements = layer_get_all_elements(layerstocheck[i]);
        
        for (var j = 0; j < array_length(elements); j++)
        {
            if (layer_get_element_type(elements[j]) == 4)
            {
                if (layer_sprite_get_sprite(elements[j]) == arg0)
                    array_push(spritestocheck, elements[j]);
            }
        }
    }
    
    for (var i = 0; i < array_length(spritestocheck); i++)
    {
        var isitme = true;
        
        if (arg2 != -99999)
        {
            if (layer_sprite_get_blend(spritestocheck[i]) != arg2)
                isitme = false;
        }
        
        if (arg3 != -99999)
        {
            if (layer_sprite_get_xscale(spritestocheck[i]) != arg3)
                isitme = false;
        }
        
        if (arg4 != -99999)
        {
            if (layer_sprite_get_yscale(spritestocheck[i]) != arg4)
                isitme = false;
        }
        
        if (sprite_get_speed_type(arg0) == 0)
            show_debug_message_concat("Warning, sprite: ", sprite_get_name(arg0), " is set to Frames Per Second instead of Frames Per Game Frame");
        
        if (arg5 != -99999)
        {
            if (layer_sprite_get_index(spritestocheck[i]) != arg5)
                isitme = false;
        }
        
        if (arg6 != -99999)
        {
            if (layer_sprite_get_speed(spritestocheck[i]) != arg6)
                isitme = false;
        }
        
        if (isitme)
            return spritestocheck[i];
    }
    
    show_debug_message_concat("findsprite: asset not found:", sprite_get_name(arg0), "| ", arg1);
    return -4;
}

function findsprite_all(arg0, arg1 = "all", arg2 = -99999, arg3 = -99999, arg4 = -99999, arg5 = -99999, arg6 = -99999)
{
    show_debug_message("findsprite_all arg0:" + string(arg0) + ", arg1:" + string(arg1) + ", arg2:" + string(arg2) + ", arg3:" + string(arg3) + ", arg4:" + string(arg4) + ", arg5:" + string(arg5) + ", arg6:" + string(arg6));

    var layerstocheck = [];
    var spritestocheck = [];
    
    if (arg1 == "all")
        layerstocheck = layer_get_all();
    else
        layerstocheck = [arg1];
    
    for (var i = 0; i < array_length(layerstocheck); i++)
    {
        var elements = layer_get_all_elements(layerstocheck[i]);
        
        for (var j = 0; j < array_length(elements); j++)
        {
            if (layer_get_element_type(elements[j]) == 4)
            {
                if (layer_sprite_get_sprite(elements[j]) == arg0)
                    array_push(spritestocheck, elements[j]);
            }
        }
    }
    
    var matchingsprites = [];
    
    for (var i = 0; i < array_length(spritestocheck); i++)
    {
        var isitme = true;
        
        if (arg2 != -99999)
        {
            if (layer_sprite_get_blend(spritestocheck[i]) != arg2)
                isitme = false;
        }
        
        if (arg3 != -99999)
        {
            if (layer_sprite_get_xscale(spritestocheck[i]) != arg3)
                isitme = false;
        }
        
        if (arg4 != -99999)
        {
            if (layer_sprite_get_yscale(spritestocheck[i]) != arg4)
                isitme = false;
        }
        
        if (arg5 != -99999)
        {
            if (layer_sprite_get_index(spritestocheck[i]) != arg5)
                isitme = false;
        }
        
        if (arg6 != -99999)
        {
            if (layer_sprite_get_speed(spritestocheck[i]) != arg6)
                isitme = false;
        }
        
        if (isitme)
            array_push(matchingsprites, spritestocheck[i]);
    }
    
    if (array_length(matchingsprites) > 0)
    {
        return matchingsprites;
    }
    else
    {
        show_debug_message_concat("findsprite_all: asset not found:", sprite_get_name(arg0), "| ", arg1);
        return [];
    }
}

function findsprite_layer(arg0)
{
    show_debug_message("findsprite_layer arg0:" + string(arg0));

    if (layer_exists(arg0))
    {
        var layerList = [];
        var elements = layer_get_all_elements(arg0);
        
        for (var j = 0; j < array_length(elements); j++)
        {
            if (layer_get_element_type(elements[j]) == 4)
                array_push(layerList, elements[j]);
        }
        
        return layerList;
    }
    else
    {
        scr_debug_print("Asset Layer Not Found: findsprite_layer(" + arg0 + ")");
        return [];
    }
}

function findspriteinfo_layer(arg0)
{
    show_debug_message("findspriteinfo_layer arg0:" + string(arg0));

    if (layer_exists(arg0))
    {
        var layerList = [];
        var elements = layer_get_all_elements(arg0);
        
        for (var j = 0; j < array_length(elements); j++)
        {
            if (layer_get_element_type(elements[j]) == 4)
            {
                var spr = elements[j];
                var thisAsset = 
                {
                    sprite_index: layer_sprite_get_sprite(spr),
                    mask_index: layer_sprite_get_sprite(spr),
                    image_index: layer_sprite_get_index(spr),
                    image_speed: layer_sprite_get_speed(spr),
                    image_xscale: layer_sprite_get_xscale(spr),
                    image_yscale: layer_sprite_get_yscale(spr),
                    image_angle: layer_sprite_get_angle(spr),
                    image_blend: layer_sprite_get_blend(spr),
                    image_alpha: layer_sprite_get_alpha(spr),
                    sprite_width: sprite_get_width(layer_sprite_get_sprite(spr)) * layer_sprite_get_xscale(spr),
                    sprite_height: sprite_get_height(layer_sprite_get_sprite(spr)) * layer_sprite_get_yscale(spr),
                    x: layer_sprite_get_x(spr),
                    y: layer_sprite_get_y(spr),
                    bbox_left: layer_sprite_get_x(spr) + sprite_get_bbox_left(layer_sprite_get_sprite(spr)),
                    bbox_top: layer_sprite_get_y(spr) + sprite_get_bbox_top(layer_sprite_get_sprite(spr)),
                    bbox_right: layer_sprite_get_x(spr) + (sprite_get_bbox_right(layer_sprite_get_sprite(spr)) * layer_sprite_get_xscale(spr)),
                    bbox_bottom: layer_sprite_get_y(spr) + (sprite_get_bbox_bottom(layer_sprite_get_sprite(spr)) * layer_sprite_get_yscale(spr)),
                    depth: 0
                };
                array_push(layerList, thisAsset);
            }
        }
        
        return layerList;
    }
    else
    {
        scr_debug_print("Asset Layer Not Found: findsprite_layer(" + arg0 + ")");
        return [];
    }
}

function findspriteinfo(arg0, arg1 = "all", arg2 = -99999, arg3 = -99999, arg4 = -99999, arg5 = -99999, arg6 = -99999)
{
    show_debug_message("findspriteinfo arg0:" + string(arg0) + ", arg1:" + string(arg1) + ", arg2:" + string(arg2) + ", arg3:" + string(arg3) + ", arg4:" + string(arg4) + ", arg5:" + string(arg5) + ", arg6:" + string(arg6));

    var spr = findsprite(arg0, arg1, arg2, arg3, arg4, arg5, arg6);
    
    if (spr == -4)
        show_debug_message_concat("findspriteinfo: asset not found:", sprite_get_name(arg0), "| ", arg1);
    
    var myreturn = 
    {
        sprite_index: layer_sprite_get_sprite(spr),
        mask_index: layer_sprite_get_sprite(spr),
        image_index: layer_sprite_get_index(spr),
        image_speed: layer_sprite_get_speed(spr),
        image_xscale: layer_sprite_get_xscale(spr),
        image_yscale: layer_sprite_get_yscale(spr),
        image_angle: layer_sprite_get_angle(spr),
        image_blend: layer_sprite_get_blend(spr),
        image_alpha: layer_sprite_get_alpha(spr),
        sprite_width: sprite_get_width(layer_sprite_get_sprite(spr)) * layer_sprite_get_xscale(spr),
        sprite_height: sprite_get_height(layer_sprite_get_sprite(spr)) * layer_sprite_get_yscale(spr),
        x: layer_sprite_get_x(spr),
        y: layer_sprite_get_y(spr),
        bbox_left: layer_sprite_get_x(spr) + sprite_get_bbox_left(layer_sprite_get_sprite(spr)),
        bbox_top: layer_sprite_get_y(spr) + sprite_get_bbox_top(layer_sprite_get_sprite(spr)),
        bbox_right: layer_sprite_get_x(spr) + (sprite_get_bbox_right(layer_sprite_get_sprite(spr)) * layer_sprite_get_xscale(spr)),
        bbox_bottom: layer_sprite_get_y(spr) + (sprite_get_bbox_bottom(layer_sprite_get_sprite(spr)) * layer_sprite_get_yscale(spr)),
        depth: 0
    };
    return myreturn;
}

function findspriteinfo_all(arg0, arg1 = "all", arg2 = -99999, arg3 = -99999, arg4 = -99999, arg5 = -99999, arg6 = -99999)
{
    show_debug_message("findspriteinfo_all arg0:" + string(arg0) + ", arg1:" + string(arg1) + ", arg2:" + string(arg2) + ", arg3:" + string(arg3) + ", arg4:" + string(arg4) + ", arg5:" + string(arg5) + ", arg6:" + string(arg6));

    var spr = findsprite_all(arg0, arg1, arg2, arg3, arg4, arg5, arg6);
    var info_all = [];
    
    for (var i = 0; i < array_length(spr); i++)
    {
        var myreturn = 
        {
            sprite_index: layer_sprite_get_sprite(spr[i]),
            mask_index: layer_sprite_get_sprite(spr[i]),
            image_index: layer_sprite_get_index(spr[i]),
            image_speed: layer_sprite_get_speed(spr[i]),
            image_xscale: layer_sprite_get_xscale(spr[i]),
            image_yscale: layer_sprite_get_yscale(spr[i]),
            image_angle: layer_sprite_get_angle(spr[i]),
            image_blend: layer_sprite_get_blend(spr[i]),
            image_alpha: layer_sprite_get_alpha(spr[i]),
            sprite_width: sprite_get_width(layer_sprite_get_sprite(spr[i])) * layer_sprite_get_xscale(spr[i]),
            sprite_height: sprite_get_height(layer_sprite_get_sprite(spr[i])) * layer_sprite_get_yscale(spr[i]),
            x: layer_sprite_get_x(spr[i]),
            y: layer_sprite_get_y(spr[i]),
            bbox_left: layer_sprite_get_x(spr[i]) + sprite_get_bbox_left(layer_sprite_get_sprite(spr[i])),
            bbox_top: layer_sprite_get_y(spr[i]) + sprite_get_bbox_top(layer_sprite_get_sprite(spr[i])),
            bbox_right: layer_sprite_get_x(spr[i]) + (sprite_get_bbox_right(layer_sprite_get_sprite(spr[i])) * layer_sprite_get_xscale(spr[i])),
            bbox_bottom: layer_sprite_get_y(spr[i]) + (sprite_get_bbox_bottom(layer_sprite_get_sprite(spr[i])) * layer_sprite_get_yscale(spr[i])),
            depth: 0
        };
        array_push(info_all, myreturn);
    }
    
    if (array_length(info_all) > 0)
    {
        return info_all;
    }
    else
    {
        show_debug_message_concat("findspriteinfo_all: asset not found:", sprite_get_name(arg0), "| ", arg1);
        return [];
    }
}
