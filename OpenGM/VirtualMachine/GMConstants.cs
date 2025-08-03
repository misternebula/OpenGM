namespace OpenGM.VirtualMachine;

public static class GMConstants
{
    public const int FIRST_INSTANCE_ID = 100000;

    public const int self = -1;
    public const int other = -2;
    public const int all = -3;
    public const int noone = -4;
    public const int global = -5;
    public const int builtin = -6;
    public const int local = -7;
    // fuck you -8 doesnt exist
    public const int stacktop = -9;
    // fuck you -10 to -14 doesnt exist either!!!
    public const int argument = -15;
    public const int @static = -16;

    public const int ROOM_ENDOFGAME = -100;
    public const int ROOM_RESTARTGAME = -200;
    public const int ROOM_LOADGAME = -300;
    public const int ROOM_ABORTGAME = -400;

    // These are used in HTML, but not C++ ...
    public const int BUFFER_GENERALERROR = -1;
    public const int BUFFER_OUTOFSPACE = -2;
    public const int BUFFER_OUTOFBOUNDS = -3;
    public const int BUFFER_INVALIDTYPE = -4;
    public const int BUFFER_UNKNOWNBUFFER = -5;
}
