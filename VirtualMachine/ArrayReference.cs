using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DELTARUNITYStandalone.VirtualMachine;

/// <summary>
/// used for for "af" operations
/// </summary>
public class ArrayReference
{
	public string Name = null!;
	public IList Value = null!;
	public bool IsGlobal;
	public bool IsLocal;
	/// <summary>
	/// do we need to store this? vs using Ctx.Self
	/// will this ever differ from Ctx.self? can the array reference escape to execution of another object???
	/// </summary>
	public GamemakerObject Instance = null!;

	public override string ToString()
	{
		return $"ArrayReference(Name:{Name}, IsGlobal:{IsGlobal}, IsLocal:{IsLocal}, Instance:{(Instance == null ? "NULL" : $"{Instance.Definition.Name} ({Instance.instanceId})")})";
	}
}
