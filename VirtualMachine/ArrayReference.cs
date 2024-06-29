using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DELTARUNITYStandalone.VirtualMachine;

public class ArrayReference
{
	public List<object> Array;
	public string ArrayName;
	public bool IsGlobal;
	public GamemakerObject Instance;
}
