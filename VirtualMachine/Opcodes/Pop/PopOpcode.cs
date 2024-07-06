using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DELTARUNITYStandalone.VirtualMachine;

public static partial class VMExecutor
{
	public static void PopToGlobal(string varName, object obj)
	{
		if (obj is RValue)
		{
			VariableResolver.GlobalVariables[varName] = obj;
		}
		else
		{
			VariableResolver.GlobalVariables[varName] = new RValue(obj);
		}
	}

	public static (ExecutionResult, object) DoPop(VMScriptInstruction instruction)
	{
		var dataPopped = PopType(instruction.TypeTwo);

		if (instruction.TypeOne == VMType.e)
		{
			// weird swap thingy
			throw new NotImplementedException();
		}

		GetVariableInfo(instruction.StringData, out string variableName, out VariableType variableType, out VariablePrefix variablePrefix, out int assetId);

		if (variablePrefix == VariablePrefix.None)
		{
			// we're just popping to a normal variable. thank god.

			if (variableType == VariableType.Global)
			{
				PopToGlobal(variableName, dataPopped);
				return (ExecutionResult.Success, null);
			}
		}

		return (ExecutionResult.Failed, $"Don't know how to execute {instruction.Raw}");
	}
}
