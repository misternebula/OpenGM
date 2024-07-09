using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DELTARUNITYStandalone.VirtualMachine;

// TODO: merge this into RValue and delete it here
public class ArrayReference
{
	public List<object> Array = null!;
	public string ArrayName = null!;
	public bool IsGlobal;
	public GamemakerObject Instance = null!;
}
