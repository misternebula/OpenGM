if (_fade_in)
{
    _alpha = lerp(_alpha, 1, 0.06);
    
    for (var i = 0; i < array_length(_choices); i++)
    {
        var choice = _choices[i];
        choice.set_alpha(_alpha);
        choice.y = lerp(choice.y, choice.ystart, 0.14);
    }
    
    if (_alpha >= 1)
        _fade_in = false;
}

if (!_input_enabled)
    exit;

if (up_p())
{
    show_debug_message("up pressed");
    audio_play_sound(snd_menumove, 50, 0);
    _parent.trigger_event("scroll_footer_up");
}
else if (down_p())
{
    show_debug_message("down pressed");
    audio_play_sound(snd_menumove, 50, 0);
    _parent.trigger_event("scroll_footer_down");
}

if (array_length(_choices) == 1)
    exit;

if (left_p())
{
    audio_play_sound(snd_menumove, 50, 0);
    _choice_index = scr_wrap(_choice_index - 1, 0, array_length(_choices) - 1);
    highlight();
}
else if (right_p())
{
    audio_play_sound(snd_menumove, 50, 0);
    _choice_index = scr_wrap(_choice_index + 1, 0, array_length(_choices) - 1);
    highlight();
}
