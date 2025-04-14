using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenGM.Loading;

namespace OpenGM.VirtualMachine;
internal class Method
{
	public IStackContextSelf? inst;
	public VMScript func = null!;
}
