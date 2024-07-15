using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenGM.VirtualMachine;
public interface IStackContextSelf
{
	public Dictionary<string, object?> SelfVariables { get; }
}
