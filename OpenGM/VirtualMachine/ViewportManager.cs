using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenGM.VirtualMachine;
public static class ViewportManager
{
	public static int[] view_wport = Enumerable.Repeat(640, 8).ToArray();
	public static int[] view_hport = Enumerable.Repeat(480, 8).ToArray();

	public static int[] view_xview = Enumerable.Repeat(0, 8).ToArray();
	public static int[] view_yview = Enumerable.Repeat(0, 8).ToArray();
	public static int[] view_wview = Enumerable.Repeat(640, 8).ToArray();
	public static int[] view_hview = Enumerable.Repeat(480, 8).ToArray();
}
