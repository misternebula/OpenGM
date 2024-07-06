using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DELTARUNITYStandalone.VirtualMachine;

public static partial class VMExecutor
{
	public static (ExecutionResult, object) DoPop(VMScriptInstruction instruction)
	{
		object dataPopped = null;
		switch (instruction.TypeTwo)
		{
			case VMType.i:
				dataPopped = (int)Ctx.Stack.Pop();
				break;
			case VMType.v:
				break;
			case VMType.b:
				dataPopped = (bool)Ctx.Stack.Pop();
				break;
			case VMType.d:
				dataPopped = (double)Ctx.Stack.Pop();
				break;
			case VMType.e:
				dataPopped = (int)Ctx.Stack.Pop();
				break;
			case VMType.s:
				dataPopped = (string)Ctx.Stack.Pop();
				break;
			case VMType.l:
				dataPopped = (long)Ctx.Stack.Pop();
				break;
		}

		if (instruction.TypeOne == VMType.e)
		{
			// weird swap thingy
			throw new NotImplementedException();
		}

		return (ExecutionResult.Failed, $"Don't know how to pop {instruction.Raw}");
	}
}
