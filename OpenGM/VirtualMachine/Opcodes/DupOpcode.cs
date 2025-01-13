using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenGM.IO;

namespace OpenGM.VirtualMachine;

public static partial class VMExecutor
{
	// see https://github.com/UnderminersTeam/Underanalyzer/blob/main/Underanalyzer/Decompiler/AST/BlockSimulator.cs#L104
	public static (ExecutionResult, object?) DoDup(VMCodeInstruction instruction)
	{
		var dupType = instruction.TypeOne;
		var dupTypeSize = VMTypeToSize(dupType);
		var dupSize = instruction.IntData;
		var dupSwapSize = instruction.SecondIntData >> 3;

		if (VerboseStackLogs)
		{
			DebugLog.Log($"DoDup dupType:{dupType} dupTypeSize:{dupTypeSize} STACK : ");
			foreach (var item in Self.Stack)
			{
				Console.WriteLine($" - {item.value} (VMType.{item.type}, {VMTypeToSize(item.type)} bytes)");
			}
		}

		if (dupSwapSize != 0)
		{
			if (dupType == VMType.v && dupSize == 0)
			{
				// Exit early; basically a no-op instruction
				return (ExecutionResult.Success, null);
			}

			// Load top data from stack
			var topSize = dupSize * dupTypeSize;
			var topStack = new Stack<(object? value, VMType type)>();
			while (topSize > 0)
			{
#pragma warning disable CS0618
				var curr = Self.Stack.Pop(); // i think this is the only time its okay to use untyped pop
#pragma warning restore CS0618
				topStack.Push(curr);
				topSize -= VMTypeToSize(curr.type);
			}

			// Load bottom data from stack
			var bottomSize = dupSwapSize * dupTypeSize;
			var bottomStack = new Stack<(object? value, VMType type)>();
			while (bottomSize > 0)
			{
#pragma warning disable CS0618
				var curr = Self.Stack.Pop(); // i think this is the only time its okay to use untyped pop
#pragma warning restore CS0618
				bottomStack.Push(curr);
				bottomSize -= VMTypeToSize(curr.type);
			}

			// Ensure we didn't read too much data accidentally
			if (topSize < 0 || bottomSize < 0)
			{
				throw new NotImplementedException();
			}

			// Push top data back first (so that it ends up at the bottom)
			while (topStack.Count > 0)
			{
#pragma warning disable CS0618
				Self.Stack.Push(topStack.Pop());
#pragma warning restore CS0618
			}

			// Push bottom data back second (so that it ends up at the top)
			while (bottomStack.Count > 0)
			{
#pragma warning disable CS0618
				Self.Stack.Push(bottomStack.Pop());
#pragma warning restore CS0618
			}
		}
		else
		{
			// Normal duplication mode
			var size = (dupSize + 1) * dupTypeSize;
			List<(object? value, VMType type)> toDuplicate = new();

			while (size > 0)
			{
#pragma warning disable CS0618
				var curr = Self.Stack.Pop(); // i think this is the only time its okay to use untyped pop
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
					Self.Stack.Push(toDuplicate[j]);
#pragma warning restore CS0618
				}
			}
		}

		return (ExecutionResult.Success, null);
	}
}
