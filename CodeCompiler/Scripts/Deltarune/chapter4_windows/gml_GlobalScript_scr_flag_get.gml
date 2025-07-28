function scr_flag_get(arg0)
{
    var flag_value = global.flag[arg0];
    return flag_value;
}

function scr_flag_name_get(arg0)
{
    return "*unknown flag*";
}

function scr_getflag(arg0)
{
    return scr_flag_get(arg0);
}