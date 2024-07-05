using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DELTARUNITYStandalone.VirtualMachine;

public static partial class VMExecutor
{
	public static (ExecutionResult, object) DoDup(VMScriptInstruction instruction)
	{
		var dupType = instruction.TypeOne;
		var dupTypeSize = VMTypeToSize(dupType);
		var dupSize = instruction.IntData;
		var dupSwapSize = instruction.SecondIntData;

		DebugLog.Log($"DoDup dupType:{dupType} dupTypeSize:{dupTypeSize} STACK : ");
		foreach (var item in Ctx.Stack)
		{
			Console.WriteLine($" - {item} (VMType.{GetTypeOfObject(item)}, {VMTypeToSize(GetTypeOfObject(item))} bytes)");
		}

		if (dupSwapSize != 0)
		{
			throw new NotImplementedException();
		}
		else
		{
			// Normal duplication mode
			var size = (dupSize + 1) * dupTypeSize;
			List<object> toDuplicate = new();

			while (size > 0)
			{
				var curr = Ctx.Stack.Pop();
				toDuplicate.Add(curr);
				size -= VMTypeToSize(GetTypeOfObject(curr));
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
					Ctx.Stack.Push(toDuplicate[j]);
				}
			}
		}

		return (ExecutionResult.Success, null);
	}

	public static int VMTypeToSize(VMType type) => type switch
	{
		VMType.v => 16,
		VMType.d => 8,
		VMType.l => 8,
		VMType.i => 4,
		VMType.b => 4,
		VMType.s => 4,
		VMType.e => 4
	};

	public static VMType GetTypeOfObject(object obj) => obj switch
	{
		int or uint => VMType.i,
		string => VMType.s,
		bool => VMType.b,
		float or double => VMType.d,
		RValue => VMType.v,
		_ => throw new NotImplementedException($"Can't get type of {obj}")
	};
}
