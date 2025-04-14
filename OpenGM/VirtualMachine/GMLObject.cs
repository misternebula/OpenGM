using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenGM.VirtualMachine;

/// <summary>
/// a struct
/// </summary>
internal class GMLObject : IStackContextSelf
{
	public Dictionary<string, object?> SelfVariables { get; } = new();
}
