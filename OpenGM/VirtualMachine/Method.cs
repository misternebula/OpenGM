using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenGM.Loading;

namespace OpenGM.VirtualMachine;
internal class Method
{
	public object? struct_ref_or_instance_id;
	public int func;

	public VMCode code => GameLoader.Codes[func]!;
}
