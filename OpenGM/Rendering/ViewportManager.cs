namespace OpenGM.Rendering;
public static class ViewportManager
{
    public static int[] view_wport = new int[8];
    public static int[] view_hport = new int[8];

    public static int[] view_xview = new int[8];
    public static int[] view_yview = new int[8];
    public static int[] view_wview = new int[8];
    public static int[] view_hview = new int[8];

    public static int[] view_camera = new int[8];

    public static RuntimeView? CurrentRenderingView;

    public static void UpdateViews()
    {
        for (var i = 0; i < 8; i++)
        {
            var view = RoomManager.CurrentRoom.Views[i];

            view_wport[i] = view.PortSize.X;
            view_hport[i] = view.PortSize.Y;

            view_xview[i] = view.ViewPosition.X;
            view_yview[i] = view.ViewPosition.Y;
            view_wview[i] = view.ViewSize.X;
            view_hview[i] = view.ViewSize.Y;

            view_camera[i] = view.Camera?.ID ?? -1;
        }
    }
}
