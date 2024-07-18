using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenGM.VirtualMachine;
public static class ViewportManager
{
	public static int[] view_wport = Enumerable.Repeat(0, 8).ToArray();
	public static int[] view_hport = Enumerable.Repeat(0, 8).ToArray();
}
