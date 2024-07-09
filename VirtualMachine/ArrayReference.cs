using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DELTARUNITYStandalone.VirtualMachine;

// TODO: merge this into RValue and delete it here
public class ArrayReference
{
	public IList Array = null!;
	public string ArrayName = null!;
	public bool IsGlobal;
	public GamemakerObject Instance = null!;

	public override string ToString()
	{
		return $"ArrayReference(ArrayName:{ArrayName}, IsGlobal:{IsGlobal}, Instance:{(Instance == null ? "NULL" : $"{Instance.Definition.Name} ({Instance.instanceId})")}";
	}
}
