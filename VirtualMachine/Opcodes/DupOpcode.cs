using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DELTARUNITYStandalone.VirtualMachine;

public static partial class VMExecutor
{
	// see https://github.com/UnderminersTeam/Underanalyzer/blob/main/Underanalyzer/Decompiler/AST/BlockSimulator.cs#L104
	public static (ExecutionResult, object?) DoDup(VMScriptInstruction instruction)
	{
		var dupType = instruction.TypeOne;
		var dupTypeSize = VMTypeToSize(dupType);
		var dupSize = instruction.IntData;
		var dupSwapSize = instruction.SecondIntData;

		if (dupSwapSize != 0)
		{
			throw new NotImplementedException();
		}
		else
		{
			// Normal duplication mode
			var size = (dupSize + 1) * dupTypeSize;
			List<(object? value, VMType type)> toDuplicate = new();

			while (size > 0)
			{
#pragma warning disable CS0618
				var curr = Ctx.Stack.Pop(); // i think this is the only time its okay to use untyped pop
#pragma warning restore CS0618
				toDuplicate.Add(curr);
				size -= VMTypeToSize(curr.type);
			}

			// Ensure we didn't read too much data accidentally
			if (size < 0)
			{
				throw new NotImplementedException();
			}

			// Push data back to the stack twice (duplicating it, while maintaining internal order)
			for (int i = 0; i < 2; i++)
			{
				for (int j = toDuplicate.Count - 1; j >= 0; j--)
				{
#pragma warning disable CS0618
					Ctx.Stack.Push(toDuplicate[j]);
#pragma warning restore CS0618
				}
			}
		}

		return (ExecutionResult.Success, null);
	}
}
