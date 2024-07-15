using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenGM.VirtualMachine;

internal class GMLObject : IStackContextSelf
{
	public Dictionary<string, object?> SelfVariables { get; } = new();
}
