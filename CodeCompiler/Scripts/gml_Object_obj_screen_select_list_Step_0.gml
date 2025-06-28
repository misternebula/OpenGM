if (!_input_enabled)
    exit;

_input_time--;

if (_input_time > 0)
    exit;

if (up_p())
{
    _input_time = _input_buffer;
    audio_play_sound(snd_menumove, 50, 0);
    var target_index = _chapter_index - 1;
    
    if (target_index < 0)
    {
        show_debug_message("scroll_list_up");
        _parent.trigger_event("scroll_list_up");
        reset();
    }
    else
    {
        _chapter_index = target_index;
        highlight();
    }
}
else if (down_p())
{
    _input_time = _input_buffer;
    audio_play_sound(snd_menumove, 50, 0);
    var target_index = _chapter_index + 1;
    
    if (target_index >= array_length(_chapters))
    {
        show_debug_message("scroll_list_down");
        _parent.trigger_event("scroll_list_down");
        reset();
    }
    else
    {
        _chapter_index = target_index;
        highlight();
    }
}
