using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenGM.Rendering;

namespace OpenGM.VirtualMachine;
public static class ViewportManager
{
	public static int[] view_wport = Enumerable.Repeat(640, 8).ToArray();
	public static int[] view_hport = Enumerable.Repeat(480, 8).ToArray();

	public static double[] view_xview = Enumerable.Repeat(0d, 8).ToArray();
	public static double[] view_yview = Enumerable.Repeat(0d, 8).ToArray();
	public static int[] view_wview = Enumerable.Repeat(640, 8).ToArray();
	public static int[] view_hview = Enumerable.Repeat(480, 8).ToArray();

	public static int[] view_camera = Enumerable.Repeat(0, 8).ToArray();

	public static void UpdateViews()
	{
		view_xview[0] = CustomWindow.Instance.X;
		view_yview[0] = CustomWindow.Instance.Y;
	}
}
