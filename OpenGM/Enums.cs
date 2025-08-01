namespace OpenGM
{
    public enum HAlign
    {
        fa_left,
        fa_center,
        fa_right
    }

    public enum VAlign
    {
        fa_top,
        fa_middle,
        fa_bottom
    }

    public enum FalloffModel
    {
        NONE,
        INVERSE_DISTANCE,
        INVERSE_DISTANCE_CLAMPED,
        LINEAR_DISTANCE,
        LINEAR_DISTANCE_CLAMPED,
        EXPONENT_DISTANCE,
        EXPONENT_DISTANCE_CLAMPED,
        INVERSE_DISTANCE_SCALED,
        EXPONENT_DISTANCE_SCALED
    }

    public enum CurveType // eAnimCurveChannelType
    {
        LINEAR,
        CATMULLROM_CENTRIPETAL,
        BEZIER_2D
    }
}
