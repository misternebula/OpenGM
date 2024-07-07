﻿using System;
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

	public static void PopToGlobalArray(string varName, int index, object obj)
	{
		RValue valueToSet;
		if (obj is RValue r)
		{
			valueToSet = r;
		}
		else
		{
			valueToSet = new RValue(obj);
		}

		VariableResolver.ArraySet(
			index, 
			valueToSet, 
			() => VariableResolver.GlobalVariables.TryGetValue(varName, out var val) ? val as List<RValue> : null,
			list => VariableResolver.GlobalVariables[varName] = list);
	}

	public static void PopToSelfArray(GamemakerObject self, string varName, int index, object obj)
	{
		RValue valueToSet;
		if (obj is RValue r)
		{
			valueToSet = r;
		}
		else
		{
			valueToSet = new RValue(obj);
		}

		VariableResolver.ArraySet(
			index,
			valueToSet,
			() => self.SelfVariables.TryGetValue(varName, out var val) ? val as List<RValue> : null,
			list => self.SelfVariables[varName] = list);
	}

	public static (ExecutionResult, object) DoPop(VMScriptInstruction instruction)
	{
		if (instruction.TypeOne == VMType.e)
		{
			// weird swap thingy
			throw new NotImplementedException();
		}

		GetVariableInfo(instruction.StringData, out string variableName, out VariableType variableType, out VariablePrefix variablePrefix, out int assetId);

		if (variablePrefix == VariablePrefix.None)
		{
			// we're just popping to a normal variable. thank god.
			var dataPopped = PopType(instruction.TypeTwo);

			if (variableType == VariableType.Global)
			{
				PopToGlobal(variableName, dataPopped);
				return (ExecutionResult.Success, null);
			}
		}
		else if (variablePrefix == VariablePrefix.Array || variablePrefix == VariablePrefix.ArrayPopAF || variablePrefix == VariablePrefix.ArrayPushAF)
		{
			int index;
			int instanceId;
			object value;
			if (instruction.TypeOne == VMType.v)
			{
				index = (int)PopType(VMType.i);
				instanceId = (int)PopType(VMType.i);
				value = PopType(instruction.TypeTwo);
			}
			else
			{
				value = PopType(instruction.TypeTwo);
				index = (int)PopType(VMType.i);
				instanceId = (int)PopType(VMType.i);
			}

			if (instanceId == GMConstants.global)
			{
				if (variablePrefix == VariablePrefix.Array)
				{
					PopToGlobalArray(variableName, index, value);
					return (ExecutionResult.Success, null);
				}
			}
			else if (instanceId == GMConstants.self)
			{
				if (variablePrefix == VariablePrefix.Array)
				{
					PopToSelfArray(Ctx.Self, variableName, index, value);
					return (ExecutionResult.Success, null);
				}
			}
		}

		return (ExecutionResult.Failed, $"Don't know how to execute {instruction.Raw}");
	}
}
