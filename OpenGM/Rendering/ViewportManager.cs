using OpenTK.Mathematics;

namespace OpenGM.Rendering;
public static class ViewportManager
{
    public static int[] view_xport = new int[8];
    public static int[] view_yport = new int[8];
    public static int[] view_wport = new int[8];
    public static int[] view_hport = new int[8];

    public static float[] view_xview = new float[8];
    public static float[] view_yview = new float[8];
    public static float[] view_wview = new float[8];
    public static float[] view_hview = new float[8];

    public static int[] view_camera = new int[8];
    public static bool[] view_visible = new bool[8];

    public static RuntimeView? CurrentRenderingView;

    public static void UpdateViews()
    {
        for (var i = 0; i < 8; i++)
        {
            var view = RoomManager.CurrentRoom.Views[i];

            view_xport[i] = view.PortPosition.X;
            view_yport[i] = view.PortPosition.Y;
            view_wport[i] = view.PortSize.X;
            view_hport[i] = view.PortSize.Y;

            view_xview[i] = view.ViewPosition.X;
            view_yview[i] = view.ViewPosition.Y;
            view_wview[i] = view.ViewSize.X;
            view_hview[i] = view.ViewSize.Y;

            view_camera[i] = view.Camera?.ID ?? -1;
            view_visible[i] = view.Visible;
        }
    }

    public static void UpdateFromArrays()
    {
        for (var i = 0; i < 8; i++)
        {
            var view = RoomManager.CurrentRoom.Views[i];

            view.PortPosition = new(view_xport[i], view_yport[i]);
            view.PortSize = new(view_wport[i], view_hport[i]);

            view.ViewPosition = new(view_xview[i], view_yview[i]);
            view.ViewSize = new(view_wview[i], view_hview[i]);

            view.Visible = view_visible[i];

            // todo: view_camera???
        }
    }
}
