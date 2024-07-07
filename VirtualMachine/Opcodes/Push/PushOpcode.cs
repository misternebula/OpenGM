using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DELTARUNITYStandalone.VirtualMachine;

public static partial class VMExecutor
{
	public static void GetVariableInfo(string instructionStringData, out string variableName, out VariableType variableType, out VariablePrefix prefix, out int assetIndex)
	{
		variableName = instructionStringData;
		prefix = VariablePrefix.None;

		var indexingArray = variableName.StartsWith("[array]");
		if (indexingArray)
		{
			prefix = VariablePrefix.Array;
			variableName = variableName[7..]; // skip [array]
		}

		var stackTop = variableName.StartsWith("[stacktop]");
		if (stackTop)
		{
			prefix = VariablePrefix.Stacktop;
			variableName = variableName[10..]; // skip [stacktop]
		}

		var arraypopaf = variableName.StartsWith("[arraypopaf]");
		if (arraypopaf)
		{
			prefix = VariablePrefix.ArrayPopAF;
			variableName = variableName[12..]; // skip [arraypopaf]
		}

		var arraypushaf = variableName.StartsWith("[arraypushaf]");
		if (arraypushaf)
		{
			prefix = VariablePrefix.ArrayPushAF;
			variableName = variableName[13..]; // skip [arraypushaf]
		}

		variableType = VariableType.None;

		assetIndex = -1;
		var split = variableName.Split('.');
		var context = split[0];
		variableName = split[1];

		if (context == "global")
		{
			variableType = VariableType.Global;
		}
		else if (context == "local")
		{
			variableType = VariableType.Local;
		}
		else if (context == "self")
		{
			variableType = VariableType.Self;
		}
		else if (context == "other")
		{
			variableType = VariableType.Other;
		}
		else if (context == "builtin")
		{
			variableType = VariableType.BuiltIn;
		}
		else if (context == "arg")
		{
			variableType = VariableType.Argument;
		}
		else if (int.TryParse(context, out var index))
		{
			variableType = VariableType.Index;
			assetIndex = index;
		}
		else
		{
			throw new NotImplementedException($"Unknown variable type : {context}");
		}
	}

	public static void PushGlobal(string varName)
	{
		Ctx.Stack.Push(VariableResolver.GlobalVariables[varName]);
	}

	public static (ExecutionResult, object) DoPush(VMScriptInstruction instruction)
	{
		switch (instruction.TypeOne)
		{
			case VMType.i:
			case VMType.e:
				Ctx.Stack.Push(instruction.IntData);
				return (ExecutionResult.Success, null);
			case VMType.l:
				Ctx.Stack.Push(instruction.LongData);
				return (ExecutionResult.Success, null);
			case VMType.b:
				Ctx.Stack.Push(instruction.BoolData);
				return (ExecutionResult.Success, null);
			case VMType.d:
				Ctx.Stack.Push(instruction.DoubleData);
				return (ExecutionResult.Success, null);
			case VMType.s:
				Ctx.Stack.Push(instruction.StringData);
				return (ExecutionResult.Success, null);
			case VMType.v:
				return DoPushV(instruction);
		}

		return (ExecutionResult.Failed, $"Don't know how to push {instruction.Raw}");
	}

	public static (ExecutionResult, object) DoPushV(VMScriptInstruction instruction)
	{
		GetVariableInfo(instruction.StringData, out string variableName, out VariableType variableType, out VariablePrefix variablePrefix, out int assetId);

		if (variablePrefix == VariablePrefix.None)
		{
			if (variableType == VariableType.Global)
			{
				PushGlobal(variableName);
				return (ExecutionResult.Success, null);
			}
		}

		return (ExecutionResult.Failed, $"Don't know how to push {instruction.Raw}");
	}
}
